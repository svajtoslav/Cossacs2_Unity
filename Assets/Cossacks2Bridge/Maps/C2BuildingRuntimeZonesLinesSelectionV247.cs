// C2BuildingRuntimeZonesLinesSelectionV247.cs
// V249: 23_05 bridge layer. Keeps building menu/production and fixes LINESORT overlay with engine DrawSpriteBuilding coordinates.
// Keeps C2BuildingObjectsLikeOriginal.cs as the only renderer, but attaches gameplay/debug data to its rendered buildings:
// - selectable building component for HUD/menu
// - production spawn wrapper
// - original MD zones: LOCKPOINTS, BUILDLOCKPOINTS, CHECKPOINTS, BUILDPOINTS, BORNPOINTS/BORNPOINTS2, CONCENTRATOR/CONCENTRATOR2
// - Q debug overlay using the same local sprite-line transform as engine DrawSpriteBuilding
//
// Original references copied into implementation comments:
// NewMon.cpp parses BORNPOINTS/CONCENTRATOR as x*16+8,y*16+8;
// BORNPOINTS2/CONCENTRATOR2 as x,y<<1;
// Build.cpp spawn path uses Real=((corner<<4)+BornPt)<<4.
// V292: visible Q-mode audit overlay: big always-on-top round points and connected paths for V283 vs raw audit.
// V293: LineRenderer was invisible on some pipelines; draw explicit thick cylinder/tube segments between points.
// V294: mode 5 shows only one active service path at a time; Q cycles path variants and production uses the active path.
// V295: route tester is screen-space UI overlay, one selected/nearest building only, no stale pre-Q debug objects.
// V296: route tester returns to world-attached overlay. One active route type is drawn for every building that has it; thin lines are drawn after buildings but before units.
// V297: hard rule: production exit = BORN_V283 (green), building entry/input = CONC_V283 (fuchsia). RAW routes are audit-only and removed from Q cycling.
// V301: Q cycle starts with MD lock/build zones and draws the diagnostic overlay wider, including visible service path nodes.
// V302: keep Q overlay on the visible battle-camera layer; layer 7 is reserved for sprite-depth prepass and is removed from the base camera mask.
// V303: construction visuals are visual-only; the construction site root owns the MD zones/routes and Q overlay audit.
// V304: draw explicit zone borders and project construction-site service markers through the same visual ALIGN matrix.
// V305: production exit uses the projected real path for construction-site 3p-align buildings.
// V309: damp the raw-vs-visual correction in world X/Z, so the tuned offset is stable
// when the same building is placed at a different map cell/hex parity.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public struct C2BuildingRuntimeZoneQuadV247LikeOriginal
    {
        public string Kind;
        public Vector3 A;
        public Vector3 B;
        public Vector3 C;
        public Vector3 D;
        public int CellX;
        public int CellY;

        public C2BuildingRuntimeZoneQuadV247LikeOriginal(string kind, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int cellX, int cellY)
        {
            Kind = kind ?? string.Empty;
            A = a;
            B = b;
            C = c;
            D = d;
            CellX = cellX;
            CellY = cellY;
        }
    }

    public struct C2BuildingRuntimeMarkerV247LikeOriginal
    {
        public string Kind;
        public Vector3 Center;
        public Vector3 A;
        public Vector3 B;
        public Vector3 C;
        public Vector3 D;
        public int LocalX;
        public int LocalY;
        public int RealX;
        public int RealY;

        public C2BuildingRuntimeMarkerV247LikeOriginal(string kind, Vector3 center, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int localX, int localY, int realX, int realY)
        {
            Kind = kind ?? string.Empty;
            Center = center;
            A = a;
            B = b;
            C = c;
            D = d;
            LocalX = localX;
            LocalY = localY;
            RealX = realX;
            RealY = realY;
        }
    }

    public struct C2BuildingRuntimeLineV247LikeOriginal
    {
        public string AnimationName;
        public int FrameIndex;
        public Vector3 A;
        public Vector3 B;
        public int X1;
        public int Y1;
        public int X2;
        public int Y2;
        public bool IsPoint;
        public bool IsGround;
        public bool IsTop;

        public C2BuildingRuntimeLineV247LikeOriginal(string animationName, int frameIndex, Vector3 a, Vector3 b, int x1, int y1, int x2, int y2, bool isPoint, bool isGround, bool isTop)
        {
            AnimationName = animationName ?? string.Empty;
            FrameIndex = frameIndex;
            A = a;
            B = b;
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            IsPoint = isPoint;
            IsGround = isGround;
            IsTop = isTop;
        }
    }

    public struct C2BuildingRuntime3DBarV378LikeOriginal
    {
        public float OriginalX0;
        public float OriginalY0;
        public float OriginalX1;
        public float OriginalY1;
        public int Height;

        public C2BuildingRuntime3DBarV378LikeOriginal(
            float originalX0, float originalY0, float originalX1, float originalY1, int height)
        {
            OriginalX0 = originalX0;
            OriginalY0 = originalY0;
            OriginalX1 = originalX1;
            OriginalY1 = originalY1;
            Height = height;
        }

        public bool ContainsOriginalPointV378LikeOriginal(float originalX, float originalY)
        {
            // COSSACKS2/3DBars.cpp::GetBar3DHeight compares the point in
            // the same (x-y, x+y) basis. Preserve its directed bounds:
            // malformed/reversed source bars remain inactive like the original.
            float xx = originalX - originalY;
            float yy = originalY + originalX;
            return xx >= OriginalX0 - OriginalY0 && xx <= OriginalX1 - OriginalY1 &&
                   yy >= OriginalY0 + OriginalX0 && yy <= OriginalY1 + OriginalX1;
        }
    }

    public sealed class C2BuildingRuntimeInfoV247LikeOriginal : MonoBehaviour
    {
        public C2BattleTerrainMode OwnerMode;
        public string SourceMonsterId = string.Empty;
        public string MdName = string.Empty;
        public int RecordIndex;
        public int RealX;
        public int RealY;
        public int RealDir;
        public int Nation;
        public int CornerCellX;
        public int CornerCellY;
        public bool NotSelectable;
        public int BuildStages;
        public int RecordStage;
        public bool UseBuildLockPoints;
        public string MdPath = string.Empty;
        public float MapPixelScaleV277 = 1.0f;
        public float VisualPixelScaleV277 = 1.0f;
        public float VisualToMapScaleV277 = 1.0f;
        public bool RuntimeConstructionSiteV303;
        public bool RuntimeConstructionVisualChildV303;

        public readonly List<C2BuildingRuntimeZoneQuadV247LikeOriginal> ZoneQuads = new List<C2BuildingRuntimeZoneQuadV247LikeOriginal>(64);
        public readonly List<C2BuildingRuntimeMarkerV247LikeOriginal> ServiceMarkers = new List<C2BuildingRuntimeMarkerV247LikeOriginal>(16);
        public readonly List<C2BuildingRuntimeLineV247LikeOriginal> LineSortLines = new List<C2BuildingRuntimeLineV247LikeOriginal>(32);
        public readonly List<C2BuildingRuntime3DBarV378LikeOriginal> Bars3D = new List<C2BuildingRuntime3DBarV378LikeOriginal>(4);
        public readonly List<Vector2> BornExitPathReal = new List<Vector2>(8);
        public readonly List<Vector2> ConcentratorPathReal = new List<Vector2>(8);
        public bool UsesVisualProjectedServiceOverlayV304;
        public string VisualProjectedServiceOverlayAuditV304 = string.Empty;
        public readonly List<Vector2> BornExitPathVisualRealV305 = new List<Vector2>(8);
        public readonly List<Vector2> ConcentratorPathVisualRealV305 = new List<Vector2>(8);
        // V291 raw-original service paths are kept for audit.
        // V379: gameplay BORN/CONCENTRATOR paths now intentionally match these
        // original Build.cpp/NewMon.cpp coordinates exactly.
        public readonly List<Vector2> BornExitPathRawAuditV291 = new List<Vector2>(8);
        public readonly List<Vector2> ConcentratorPathRawAuditV291 = new List<Vector2>(8);

        private static GameObject s_overlayRootV247;
        private static Material s_lineMaterialV247;
        private static Sprite s_routeCircleSpriteV295;
        private static Texture2D s_routeCircleTextureV295;
        private static Material s_routeLineMaterialV296;
        private static readonly HashSet<Vector2Int> s_blockedCellsCacheV247 = new HashSet<Vector2Int>();
        private static float s_blockedCellsCacheUntilV247 = -1.0f;
        private static readonly Dictionary<long, List<C2BuildingRuntimeInfoV247LikeOriginal>> s_3DBarOwnersByCellV378 =
            new Dictionary<long, List<C2BuildingRuntimeInfoV247LikeOriginal>>(512);
        private readonly List<long> _registered3DBarCellsV378 = new List<long>(16);

        private const int C2DebugOverlayLayerV294 = 0;
        private const int C2BornConcRouteCountV294 = 2;
        private const float C2ZoneOverlayGrowPixelsV301 = 8.0f;
        private const float C2BuildZoneOverlayGrowPixelsV301 = 6.0f;
        private const float C2ServiceRouteLineWidthV301 = 4.25f;
        private const float C2ServiceMarkerScaleV301 = 1.85f;
        private const float C2ZoneBorderWidthWorldV304 = 3.25f;
        private static int s_activeBornConcRouteV294;

        public static int ActiveBornConcRouteV294LikeOriginal
        {
            get { return Mathf.Clamp(s_activeBornConcRouteV294, 0, C2BornConcRouteCountV294 - 1); }
        }

        public static int BornConcRouteCountV294LikeOriginal
        {
            get { return C2BornConcRouteCountV294; }
        }

        public static void SetActiveBornConcRouteV294LikeOriginal(int route)
        {
            s_activeBornConcRouteV294 = Mathf.Clamp(route, 0, C2BornConcRouteCountV294 - 1);
        }

        public static int StepActiveBornConcRouteV294LikeOriginal()
        {
            s_activeBornConcRouteV294 = (s_activeBornConcRouteV294 + 1) % C2BornConcRouteCountV294;
            return s_activeBornConcRouteV294;
        }

        public static string ActiveBornConcRouteLabelV294LikeOriginal
        {
            get { return BornConcRouteLabelV294LikeOriginal(ActiveBornConcRouteV294LikeOriginal); }
        }

        public static string BornConcRouteLabelV294LikeOriginal(int route)
        {
            if (route == 0) return "BORN_V377_ORIGINAL_EXIT_GREEN";
            if (route == 1) return "CONC_V283_ENTRY_FUCHSIA";
            return "BORN_V377_ORIGINAL_EXIT_GREEN";
        }

        private void OnEnable()
        {
            s_blockedCellsCacheUntilV247 = -1.0f;
            Register3DBarCellsV378LikeOriginal();
        }

        private void OnDisable()
        {
            s_blockedCellsCacheUntilV247 = -1.0f;
            Unregister3DBarCellsV378LikeOriginal();
        }

        public void ClearRuntimeDataV247LikeOriginal()
        {
            s_blockedCellsCacheUntilV247 = -1.0f;
            Unregister3DBarCellsV378LikeOriginal();
            ZoneQuads.Clear();
            ServiceMarkers.Clear();
            LineSortLines.Clear();
            Bars3D.Clear();
            BornExitPathReal.Clear();
            ConcentratorPathReal.Clear();
            UsesVisualProjectedServiceOverlayV304 = false;
            VisualProjectedServiceOverlayAuditV304 = string.Empty;
            BornExitPathVisualRealV305.Clear();
            ConcentratorPathVisualRealV305.Clear();
            BornExitPathRawAuditV291.Clear();
            ConcentratorPathRawAuditV291.Clear();
        }

        internal void Register3DBarCellsV378LikeOriginal()
        {
            Unregister3DBarCellsV378LikeOriginal();
            if (Bars3D == null || Bars3D.Count == 0 || RuntimeConstructionVisualChildV303) return;

            var unique = new HashSet<long>();
            for (int i = 0; i < Bars3D.Count; i++)
            {
                C2BuildingRuntime3DBarV378LikeOriginal bar = Bars3D[i];
                if (bar.Height <= 0 ||
                    bar.OriginalX1 - bar.OriginalY1 < bar.OriginalX0 - bar.OriginalY0 ||
                    bar.OriginalY1 + bar.OriginalX1 < bar.OriginalY0 + bar.OriginalX0)
                    continue;

                float l1 = ((bar.OriginalX1 + bar.OriginalY1) -
                            (bar.OriginalX0 + bar.OriginalY0)) * 0.5f;
                float l2 = ((bar.OriginalX1 - bar.OriginalY1) -
                            (bar.OriginalX0 - bar.OriginalY0)) * 0.5f;
                float minX = Mathf.Min(bar.OriginalX0, bar.OriginalX0 + l1,
                    bar.OriginalX0 + l2, bar.OriginalX1);
                float maxX = Mathf.Max(bar.OriginalX0, bar.OriginalX0 + l1,
                    bar.OriginalX0 + l2, bar.OriginalX1);
                float minY = Mathf.Min(bar.OriginalY0, bar.OriginalY0 + l1,
                    bar.OriginalY0 - l2, bar.OriginalY1);
                float maxY = Mathf.Max(bar.OriginalY0, bar.OriginalY0 + l1,
                    bar.OriginalY0 - l2, bar.OriginalY1);
                int minCellX = Mathf.FloorToInt(minX) >> 8;
                int maxCellX = Mathf.FloorToInt(maxX) >> 8;
                int minCellY = Mathf.FloorToInt(minY) >> 8;
                int maxCellY = Mathf.FloorToInt(maxY) >> 8;
                for (int cellY = minCellY; cellY <= maxCellY; cellY++)
                for (int cellX = minCellX; cellX <= maxCellX; cellX++)
                    unique.Add((((long)cellX) << 32) ^ (uint)cellY);
            }

            foreach (long key in unique)
            {
                if (!s_3DBarOwnersByCellV378.TryGetValue(key, out List<C2BuildingRuntimeInfoV247LikeOriginal> owners))
                {
                    owners = new List<C2BuildingRuntimeInfoV247LikeOriginal>(2);
                    s_3DBarOwnersByCellV378.Add(key, owners);
                }
                if (!owners.Contains(this)) owners.Add(this);
                _registered3DBarCellsV378.Add(key);
            }
        }

        private void Unregister3DBarCellsV378LikeOriginal()
        {
            for (int i = 0; i < _registered3DBarCellsV378.Count; i++)
            {
                long key = _registered3DBarCellsV378[i];
                if (!s_3DBarOwnersByCellV378.TryGetValue(key, out List<C2BuildingRuntimeInfoV247LikeOriginal> owners))
                    continue;
                owners.Remove(this);
                if (owners.Count == 0) s_3DBarOwnersByCellV378.Remove(key);
            }
            _registered3DBarCellsV378.Clear();
        }

        internal static bool TryIntersect3DBarSegmentV378LikeOriginal(
            C2BattleTerrainMode mode,
            Vector3 worldFrom,
            Vector3 worldTo,
            out C2SettlementBuildingSelectableV1LikeOriginal owner,
            out float hitRealX,
            out float hitRealY)
        {
            owner = null;
            hitRealX = 0.0f;
            hitRealY = 0.0f;
            if (mode == null) return false;

            if (!mode.C2NoUnitWorldToOriginalPixelLikeOriginal(worldFrom, out float fromX, out float fromY) ||
                !mode.C2NoUnitWorldToOriginalPixelLikeOriginal(worldTo, out float toX, out float toY))
                return false;

            int steps = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Max(Mathf.Abs(toX - fromX), Mathf.Abs(toY - fromY)) / 8.0f),
                1,
                256);
            for (int step = 1; step <= steps; step++)
            {
                float t = step / (float)steps;
                float originalX = Mathf.Lerp(fromX, toX, t);
                float originalY = Mathf.Lerp(fromY, toY, t);
                Vector3 world = Vector3.Lerp(worldFrom, worldTo, t);
                C2SettlementBuildingSelectableV1LikeOriginal bestOwner = null;
                float bestTop = float.NegativeInfinity;

                int cellX = Mathf.FloorToInt(originalX) >> 8;
                int cellY = Mathf.FloorToInt(originalY) >> 8;
                long key = (((long)cellX) << 32) ^ (uint)cellY;
                if (!s_3DBarOwnersByCellV378.TryGetValue(
                        key, out List<C2BuildingRuntimeInfoV247LikeOriginal> owners))
                    continue;

                for (int i = owners.Count - 1; i >= 0; i--)
                {
                    C2BuildingRuntimeInfoV247LikeOriginal info = owners[i];
                    if (info == null)
                    {
                        owners.RemoveAt(i);
                        continue;
                    }
                    if (!info.isActiveAndEnabled || info.OwnerMode != mode || info.RuntimeConstructionVisualChildV303 ||
                        info.Bars3D == null || info.Bars3D.Count == 0)
                        continue;

                    C2SettlementBuildingSelectableV1LikeOriginal selectable =
                        info.GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
                    if (selectable == null || selectable.LifeLikeOriginal <= 0) continue;

                    for (int barIndex = 0; barIndex < info.Bars3D.Count; barIndex++)
                    {
                        C2BuildingRuntime3DBarV378LikeOriginal bar = info.Bars3D[barIndex];
                        if (bar.Height <= 0 || !bar.ContainsOriginalPointV378LikeOriginal(originalX, originalY)) continue;
                        mode.C2Building3DBarWorldHeightV378LikeOriginal(
                            originalX, originalY, bar.Height, out float groundWorldY, out float topWorldY);
                        if (world.y < groundWorldY || world.y > topWorldY || topWorldY <= bestTop) continue;
                        bestTop = topWorldY;
                        bestOwner = selectable;
                    }
                }
                if (owners.Count == 0) s_3DBarOwnersByCellV378.Remove(key);

                if (bestOwner != null)
                {
                    owner = bestOwner;
                    hitRealX = originalX * 16.0f;
                    hitRealY = originalY * 16.0f;
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetProducedUnitExitPathForBuildingRealLikeOriginal(
            int buildingRecordIndex,
            string buildingMd,
            int buildingRealX,
            int buildingRealY,
            out Vector2[] path,
            out string audit)
        {
            path = null;
            audit = "not_found";

            C2BuildingRuntimeInfoV247LikeOriginal[] infos = UnityEngine.Object.FindObjectsOfType<C2BuildingRuntimeInfoV247LikeOriginal>();
            if (infos == null || infos.Length == 0)
            {
                audit = "no_runtime_building_infos_v247";
                return false;
            }

            C2BuildingRuntimeInfoV247LikeOriginal best = null;
            int bestScore = int.MaxValue;
            string md = (buildingMd ?? string.Empty).Trim();

            for (int i = 0; i < infos.Length; i++)
            {
                C2BuildingRuntimeInfoV247LikeOriginal info = infos[i];
                if (info == null || !info.isActiveAndEnabled)
                    continue;

                int score = int.MaxValue;

                if (info.RecordIndex == buildingRecordIndex)
                {
                    score = 0;
                }
                else
                {
                    bool mdMatch =
                        NonEmptyEqualsV259LikeOriginal(info.SourceMonsterId, md) ||
                        NonEmptyEqualsV259LikeOriginal(info.MdName, md);

                    // V259: never use substring or empty-token matches here.
                    // The old code effectively matched every building when MdName/SourceMonsterId was empty,
                    // so production from one barracks could take BORNPOINTS from another nation's barracks.
                    if (mdMatch)
                    {
                        int dx = Mathf.Abs(info.RealX - buildingRealX);
                        int dy = Mathf.Abs(info.RealY - buildingRealY);
                        int dist = dx + dy;
                        if (dist <= 2048)
                            score = dist + 1000;
                    }
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    best = info;
                }
            }

            if (best == null)
            {
                audit = "no_exact_matching_building_info_v259 record=" + buildingRecordIndex.ToString(CultureInfo.InvariantCulture) +
                        " md='" + md + "'" +
                        " real=(" + buildingRealX.ToString(CultureInfo.InvariantCulture) + "," + buildingRealY.ToString(CultureInfo.InvariantCulture) + ")";
                return false;
            }

            // V297 hard rule: produced units leave through the green BORN_V283 path.
            // CONCENTRATOR/CONCENTRATOR2 is kept for entry/input routing and debug overlay,
            // but must not replace the production exit path.
            List<Vector2> selectedExitPathV309 = C2BuildingRuntimeV309GetProductionBornExitPathLikeOriginal(best);
            if (selectedExitPathV309 == null || selectedExitPathV309.Count == 0)
            {
                audit = "building_has_no_BORN_V283_exit_v297" +
                        " record=" + best.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                        " md='" + (best.MdName ?? string.Empty) + "'" +
                        " concV283=" + (best.ConcentratorPathReal != null ? best.ConcentratorPathReal.Count : 0).ToString(CultureInfo.InvariantCulture);
                return false;
            }

            path = selectedExitPathV309.ToArray();
            audit = "v377_original_md_BORN_exit" +
                    " path=" + path.Length.ToString(CultureInfo.InvariantCulture) +
                    " record=" + best.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                    " md='" + (best.MdName ?? string.Empty) + "'" +
                    " corner=(" + best.CornerCellX.ToString(CultureInfo.InvariantCulture) + "," + best.CornerCellY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " bornV283=" + best.BornExitPathReal.Count.ToString(CultureInfo.InvariantCulture) +
                    " bornRawAuditV291=" + (best.BornExitPathRawAuditV291 != null ? best.BornExitPathRawAuditV291.Count : 0).ToString(CultureInfo.InvariantCulture) +
                    " bornVisualV305=" + (best.BornExitPathVisualRealV305 != null ? best.BornExitPathVisualRealV305.Count : 0).ToString(CultureInfo.InvariantCulture) +
                    " concV283_entry_available=" + best.ConcentratorPathReal.Count.ToString(CultureInfo.InvariantCulture) +
                    " route=original_md_cells_v377" +
                    " rule=original_corner_cell_plus_md_BORN_exit";
            return true;
        }

        public static List<Vector2> C2BuildingRuntimeV309GetProductionBornExitPathLikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal info)
        {
            // COSSACKS2 ProduceObjLink: corner-cell origin + MD BORNPOINTS in order.
            // V379: these are gameplay coordinates and are intentionally NOT affected
            // by Unity visual sprite scaling.
            if (info == null) return null;
            return info.BornExitPathReal;
        }



        private static List<Vector2> C2BuildingRuntimeV294GetActiveServicePathLikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal info, out string label)
        {
            int route = ActiveBornConcRouteV294LikeOriginal;
            label = BornConcRouteLabelV294LikeOriginal(route);
            if (info == null) return null;
            if (route == 0) return C2BuildingRuntimeV309GetProductionBornExitPathLikeOriginal(info);
            // V297: the fuchsia CONC_V283 path is the entry/input path and is displayed
            // in its original MD order. Production exit no longer uses this method.
            if (route == 1) return info.ConcentratorPathReal;
            return C2BuildingRuntimeV309GetProductionBornExitPathLikeOriginal(info);
        }

        private static List<Vector2> CopyReversedPathV296LikeOriginal(List<Vector2> src)
        {
            if (src == null) return null;
            var dst = new List<Vector2>(src.Count);
            for (int i = src.Count - 1; i >= 0; i--)
                dst.Add(src[i]);
            return dst;
        }

        private static List<Vector3> C2BuildingRuntimeV294GetActiveServiceWorldPathLikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal info, out Color color, out string label)
        {
            label = ActiveBornConcRouteLabelV294LikeOriginal;
            color = MarkerColorV247LikeOriginal("BORNPOINTS");
            var result = new List<Vector3>(8);
            if (info == null || info.ServiceMarkers == null) return result;

            int route = ActiveBornConcRouteV294LikeOriginal;
            if (route == 0) color = MarkerColorV247LikeOriginal("BORNPOINTS");
            else color = MarkerColorV247LikeOriginal("CONCENTRATOR");

            if (route == 0)
            {
                List<Vector2> productionPath = C2BuildingRuntimeV309GetProductionBornExitPathLikeOriginal(info);
                C2BattleTerrainMode mode = info.OwnerMode != null ? info.OwnerMode : UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
                for (int i = 0; mode != null && productionPath != null && i < productionPath.Count; i++)
                {
                    Vector2 real = productionPath[i];
                    Vector3 world = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(real.x / 16.0f, real.y / 16.0f);
                    world.y += 0.36f;
                    result.Add(world);
                }
                if (result.Count > 0)
                    return result;
            }

            for (int i = 0; i < info.ServiceMarkers.Count; i++)
            {
                C2BuildingRuntimeMarkerV247LikeOriginal m = info.ServiceMarkers[i];
                bool add = false;
                if (route == 0) add = C2BuildingRuntimeV291IsBornKindLikeOriginal(m.Kind);
                else if (route == 1) add = string.Equals(m.Kind, "CONCENTRATOR", StringComparison.OrdinalIgnoreCase) || string.Equals(m.Kind, "CONCENTRATOR2", StringComparison.OrdinalIgnoreCase);
                else if (route == 2) add = C2BuildingRuntimeV291IsRawBornKindLikeOriginal(m.Kind);
                else add = C2BuildingRuntimeV291IsRawConcKindLikeOriginal(m.Kind);
                if (add) result.Add(m.Center);
            }
            return result;
        }

        private static bool NonEmptyEqualsV259LikeOriginal(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return false;
            return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static int RealToCellV247LikeOriginal(float real)
        {
            return Mathf.FloorToInt(real / 256.0f);
        }

        private static float CellCenterToRealV247LikeOriginal(int cell)
        {
            return cell * 256.0f + 128.0f;
        }

        private static bool ZoneKindBlocksNowV247LikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal info, string kind)
        {
            if (info == null || info.RuntimeConstructionVisualChildV303) return false;
            if (string.Equals(kind, "LOCKPOINTS", StringComparison.OrdinalIgnoreCase))
                return !info.UseBuildLockPoints;
            if (string.Equals(kind, "BUILDLOCKPOINTS", StringComparison.OrdinalIgnoreCase))
                return info.UseBuildLockPoints;
            return false;
        }

        private static HashSet<Vector2Int> GetBlockedCellsV247LikeOriginal()
        {
            if (s_blockedCellsCacheUntilV247 >= 0.0f)
                return s_blockedCellsCacheV247;

            // Building obstruction cells change only on construction-state events.
            // OnEnable/OnDisable/ClearRuntimeData invalidate this cache. A periodic
            // scene-wide scan is unnecessary while hundreds of units are moving.
            s_blockedCellsCacheUntilV247 = float.PositiveInfinity;
            s_blockedCellsCacheV247.Clear();

            C2BuildingRuntimeInfoV247LikeOriginal[] infos =
                UnityEngine.Object.FindObjectsByType<C2BuildingRuntimeInfoV247LikeOriginal>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            if (infos == null || infos.Length == 0)
                return s_blockedCellsCacheV247;

            for (int i = 0; i < infos.Length; i++)
            {
                C2BuildingRuntimeInfoV247LikeOriginal info = infos[i];
                if (info == null || !info.isActiveAndEnabled) continue;

                for (int q = 0; q < info.ZoneQuads.Count; q++)
                {
                    C2BuildingRuntimeZoneQuadV247LikeOriginal z = info.ZoneQuads[q];
                    if (!ZoneKindBlocksNowV247LikeOriginal(info, z.Kind)) continue;
                    s_blockedCellsCacheV247.Add(new Vector2Int(z.CellX, z.CellY));
                }
            }

            return s_blockedCellsCacheV247;
        }

        public static bool IsBlockedCellV247LikeOriginal(int cellX, int cellY)
        {
            return GetBlockedCellsV247LikeOriginal().Contains(new Vector2Int(cellX, cellY));
        }

        public static bool IsBlockedRealV247LikeOriginal(float realX, float realY)
        {
            return IsBlockedCellV247LikeOriginal(RealToCellV247LikeOriginal(realX), RealToCellV247LikeOriginal(realY));
        }

        public static bool IsBlockedForUnitRealV247LikeOriginal(float realX, float realY, int radiusCells)
        {
            if (C2BattleTerrainMode.C2IsHardWaterRealLikeOriginal(realX, realY))
                return true;

            int r = Mathf.Clamp(radiusCells, 0, 8);
            int cx = RealToCellV247LikeOriginal(realX);
            int cy = RealToCellV247LikeOriginal(realY);
            HashSet<Vector2Int> blocked = GetBlockedCellsV247LikeOriginal();

            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (blocked.Contains(new Vector2Int(cx + dx, cy + dy)))
                        return true;
                }
            }

            return false;
        }

        public static bool TryFindNearestFreeRealV247LikeOriginal(
            float realX,
            float realY,
            out float freeRealX,
            out float freeRealY,
            int maxRadiusCells,
            int radiusCells = 1)
        {
            freeRealX = realX;
            freeRealY = realY;

            if (!IsBlockedForUnitRealV247LikeOriginal(realX, realY, radiusCells))
                return true;

            int cx = RealToCellV247LikeOriginal(realX);
            int cy = RealToCellV247LikeOriginal(realY);
            int maxR = Mathf.Clamp(maxRadiusCells, 1, 96);

            float bestScore = float.PositiveInfinity;
            int bestX = cx;
            int bestY = cy;
            bool found = false;

            for (int r = 1; r <= maxR; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;

                        int tx = cx + dx;
                        int ty = cy + dy;
                        float rx = CellCenterToRealV247LikeOriginal(tx);
                        float ry = CellCenterToRealV247LikeOriginal(ty);
                        if (IsBlockedForUnitRealV247LikeOriginal(rx, ry, radiusCells)) continue;

                        float sx = rx - realX;
                        float sy = ry - realY;
                        float score = sx * sx + sy * sy;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestX = tx;
                            bestY = ty;
                            found = true;
                        }
                    }
                }

                if (found) break;
            }

            if (!found) return false;

            freeRealX = CellCenterToRealV247LikeOriginal(bestX);
            freeRealY = CellCenterToRealV247LikeOriginal(bestY);
            return true;
        }

        public static bool CanTravelStraightRealV247LikeOriginal(float fromRealX, float fromRealY, float toRealX, float toRealY, int radiusCells = 1)
        {
            if (float.IsNaN(fromRealX) || float.IsInfinity(fromRealX) || float.IsNaN(fromRealY) || float.IsInfinity(fromRealY) ||
                float.IsNaN(toRealX) || float.IsInfinity(toRealX) || float.IsNaN(toRealY) || float.IsInfinity(toRealY)) return false;
            if (IsBlockedForUnitRealV247LikeOriginal(fromRealX, fromRealY, radiusCells) ||
                IsBlockedForUnitRealV247LikeOriginal(toRealX, toRealY, radiusCells)) return false;

            // Check every crossed original 16-pixel motion-field cell. Fixed-distance
            // samples miss arbitrarily short intersections at corners; changing a
            // segment into animation steps then changes which cells get tested.
            // Water uses 32-pixel cells, so this traversal also covers that field.
            int cx = RealToCellV247LikeOriginal(fromRealX), cy = RealToCellV247LikeOriginal(fromRealY);
            int endX = RealToCellV247LikeOriginal(toRealX), endY = RealToCellV247LikeOriginal(toRealY);
            double dx = (double)toRealX - fromRealX, dy = (double)toRealY - fromRealY;
            int stepX = Math.Sign(dx), stepY = Math.Sign(dy);
            double deltaX = stepX == 0 ? double.PositiveInfinity : 256.0 / Math.Abs(dx);
            double deltaY = stepY == 0 ? double.PositiveInfinity : 256.0 / Math.Abs(dy);
            double nextX = stepX == 0 ? double.PositiveInfinity :
                (((double)cx + (stepX > 0 ? 1 : 0)) * 256.0 - fromRealX) / dx;
            double nextY = stepY == 0 ? double.PositiveInfinity :
                (((double)cy + (stepY > 0 ? 1 : 0)) * 256.0 - fromRealY) / dy;
            while (cx != endX || cy != endY)
            {
                double tx = cx == endX ? double.PositiveInfinity : nextX;
                double ty = cy == endY ? double.PositiveInfinity : nextY;
                if (Math.Abs(tx - ty) <= 1e-12)
                {
                    // Match the pathfinder's diagonal rule: both side cells must
                    // be free before crossing a shared grid corner.
                    if (IsBlockedForUnitRealV247LikeOriginal(CellCenterToRealV247LikeOriginal(cx + stepX), CellCenterToRealV247LikeOriginal(cy), radiusCells) ||
                        IsBlockedForUnitRealV247LikeOriginal(CellCenterToRealV247LikeOriginal(cx), CellCenterToRealV247LikeOriginal(cy + stepY), radiusCells)) return false;
                    cx += stepX; cy += stepY; nextX += deltaX; nextY += deltaY;
                }
                else if (tx < ty) { cx += stepX; nextX += deltaX; }
                else { cy += stepY; nextY += deltaY; }
                if (IsBlockedForUnitRealV247LikeOriginal(CellCenterToRealV247LikeOriginal(cx), CellCenterToRealV247LikeOriginal(cy), radiusCells)) return false;
            }
            return true;
        }

        private static float PathHeuristicV247LikeOriginal(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            int mn = Mathf.Min(dx, dy);
            int mx = Mathf.Max(dx, dy);
            return (mx - mn) + mn * 1.41421356f;
        }

        private struct PathOpenNodeV347LikeOriginal
        {
            public Vector2Int Cell;
            public float F;
            public float G;
        }

        private static void PathHeapPushV347LikeOriginal(
            List<PathOpenNodeV347LikeOriginal> heap,
            PathOpenNodeV347LikeOriginal value)
        {
            int at = heap.Count;
            heap.Add(value);
            while (at > 0)
            {
                int parent = (at - 1) >> 1;
                PathOpenNodeV347LikeOriginal pv = heap[parent];
                if (pv.F < value.F || pv.F == value.F && pv.G <= value.G)
                    break;
                heap[at] = pv;
                at = parent;
            }
            heap[at] = value;
        }

        private static PathOpenNodeV347LikeOriginal PathHeapPopV347LikeOriginal(
            List<PathOpenNodeV347LikeOriginal> heap)
        {
            PathOpenNodeV347LikeOriginal result = heap[0];
            int lastIndex = heap.Count - 1;
            PathOpenNodeV347LikeOriginal tail = heap[lastIndex];
            heap.RemoveAt(lastIndex);
            if (lastIndex == 0)
                return result;

            int at = 0;
            int count = heap.Count;
            while (true)
            {
                int left = at * 2 + 1;
                if (left >= count) break;
                int right = left + 1;
                int child = left;
                if (right < count)
                {
                    PathOpenNodeV347LikeOriginal lv = heap[left];
                    PathOpenNodeV347LikeOriginal rv = heap[right];
                    if (rv.F < lv.F || rv.F == lv.F && rv.G < lv.G)
                        child = right;
                }
                PathOpenNodeV347LikeOriginal cv = heap[child];
                if (tail.F < cv.F || tail.F == cv.F && tail.G <= cv.G)
                    break;
                heap[at] = cv;
                at = child;
            }
            heap[at] = tail;
            return result;
        }

        public static bool TryBuildPathRealV247LikeOriginal(
            float startRealX,
            float startRealY,
            float wantedRealX,
            float wantedRealY,
            out Vector2[] waypoints,
            int maxSearchCells)
        {
            bool directTravelClear;
            return TryBuildPathRealV247LikeOriginal(
                startRealX,
                startRealY,
                wantedRealX,
                wantedRealY,
                out waypoints,
                out directTravelClear,
                maxSearchCells);
        }

        public static bool TryBuildPathRealV247LikeOriginal(
            float startRealX,
            float startRealY,
            float wantedRealX,
            float wantedRealY,
            out Vector2[] waypoints,
            out bool directTravelClear,
            int maxSearchCells,
            int radiusCells = 1)
        {
            waypoints = null;
            directTravelClear = false;

            float dstX = wantedRealX;
            float dstY = wantedRealY;
            bool targetAdjusted = false;

            float freeDstX;
            float freeDstY;
            if (TryFindNearestFreeRealV247LikeOriginal(dstX, dstY, out freeDstX, out freeDstY, 96, radiusCells))
            {
                float adx = freeDstX - wantedRealX;
                float ady = freeDstY - wantedRealY;
                targetAdjusted = (adx * adx + ady * ady) > 64.0f;
                dstX = freeDstX;
                dstY = freeDstY;
            }

            if (CanTravelStraightRealV247LikeOriginal(startRealX, startRealY, dstX, dstY, radiusCells))
            {
                if (targetAdjusted)
                {
                    waypoints = new Vector2[] { new Vector2(dstX, dstY) };
                    return true;
                }
                directTravelClear = true;
                return false;
            }

            float freeStartX = startRealX;
            float freeStartY = startRealY;
            bool startAdjusted = false;
            if (IsBlockedForUnitRealV247LikeOriginal(startRealX, startRealY, radiusCells))
            {
                if (!TryFindNearestFreeRealV247LikeOriginal(startRealX, startRealY, out freeStartX, out freeStartY, 96, radiusCells))
                    return false;
                startAdjusted = true;
            }

            Vector2Int start = new Vector2Int(RealToCellV247LikeOriginal(freeStartX), RealToCellV247LikeOriginal(freeStartY));
            Vector2Int target = new Vector2Int(RealToCellV247LikeOriginal(dstX), RealToCellV247LikeOriginal(dstY));

            int directDx = Mathf.Abs(target.x - start.x);
            int directDy = Mathf.Abs(target.y - start.y);
            int pad = Mathf.Clamp(Mathf.Max(24, Mathf.Max(directDx, directDy) / 2 + 16), 24, 128);
            int minX = Mathf.Min(start.x, target.x) - pad;
            int maxX = Mathf.Max(start.x, target.x) + pad;
            int minY = Mathf.Min(start.y, target.y) - pad;
            int maxY = Mathf.Max(start.y, target.y) + pad;

            // The old bridge used a linear scan over the open set.  Allowing
            // 12-24k expansions turned a single unreachable shore order into
            // a multi-second main-thread stall.  Original C2 routes through
            // bounded topology regions and retries later; keep this local
            // LOCKPOINT fallback bounded as well.
            int maxSearch = Mathf.Clamp(maxSearchCells, 256, 4096);

            // The original routes through a topology table. This local fallback
            // still has to search LOCKPOINT cells, but its frontier must not be
            // linearly rescanned. The former List min-search + Contains made a
            // single 4096-cell route take 100+ ms and froze the game when a few
            // groups moved. A binary heap keeps identical A* costs and bounds.
            var open = new List<PathOpenNodeV347LikeOriginal>(256);
            var came = new Dictionary<Vector2Int, Vector2Int>(1024);
            var bestG = new Dictionary<Vector2Int, float>(1024);
            var closed = new HashSet<Vector2Int>();

            bestG[start] = 0.0f;
            PathHeapPushV347LikeOriginal(open, new PathOpenNodeV347LikeOriginal
            {
                Cell = start,
                G = 0.0f,
                F = PathHeuristicV247LikeOriginal(start, target)
            });
            bool found = false;
            int expanded = 0;

            while (open.Count > 0 && expanded < maxSearch)
            {
                PathOpenNodeV347LikeOriginal openNode = PathHeapPopV347LikeOriginal(open);
                Vector2Int cur = openNode.Cell;

                if (closed.Contains(cur)) continue;
                float currentBestG;
                if (!bestG.TryGetValue(cur, out currentBestG) || openNode.G > currentBestG)
                    continue; // stale heap entry after a cheaper route was found
                closed.Add(cur);
                expanded++;

                if (cur == target)
                {
                    found = true;
                    break;
                }

                for (int ny = -1; ny <= 1; ny++)
                {
                    for (int nx = -1; nx <= 1; nx++)
                    {
                        if (nx == 0 && ny == 0) continue;
                        Vector2Int nb = new Vector2Int(cur.x + nx, cur.y + ny);
                        if (nb.x < minX || nb.x > maxX || nb.y < minY || nb.y > maxY) continue;
                        if (closed.Contains(nb)) continue;

                        float nbRealX = CellCenterToRealV247LikeOriginal(nb.x);
                        float nbRealY = CellCenterToRealV247LikeOriginal(nb.y);
                        if (IsBlockedForUnitRealV247LikeOriginal(nbRealX, nbRealY, radiusCells)) continue;

                        if (nx != 0 && ny != 0)
                        {
                            float sideXReal = CellCenterToRealV247LikeOriginal(cur.x + nx);
                            float sideYReal = CellCenterToRealV247LikeOriginal(cur.y);
                            float upXReal = CellCenterToRealV247LikeOriginal(cur.x);
                            float upYReal = CellCenterToRealV247LikeOriginal(cur.y + ny);
                            if (IsBlockedForUnitRealV247LikeOriginal(sideXReal, sideYReal, radiusCells)) continue;
                            if (IsBlockedForUnitRealV247LikeOriginal(upXReal, upYReal, radiusCells)) continue;
                        }

                        float step = (nx != 0 && ny != 0) ? 1.41421356f : 1.0f;
                        float ng = bestG[cur] + step;

                        float oldG;
                        if (bestG.TryGetValue(nb, out oldG) && ng >= oldG)
                            continue;

                        bestG[nb] = ng;
                        came[nb] = cur;
                        PathHeapPushV347LikeOriginal(open, new PathOpenNodeV347LikeOriginal
                        {
                            Cell = nb,
                            G = ng,
                            F = ng + PathHeuristicV247LikeOriginal(nb, target)
                        });
                    }
                }
            }

            if (!found) return false;

            var cells = new List<Vector2Int>(128);
            Vector2Int p = target;
            cells.Add(p);
            while (p != start)
            {
                Vector2Int prev;
                if (!came.TryGetValue(p, out prev)) break;
                p = prev;
                cells.Add(p);
            }
            cells.Reverse();

            var result = new List<Vector2>(cells.Count + 1);
            if (startAdjusted)
                result.Add(new Vector2(freeStartX, freeStartY));

            for (int i = 0; i < cells.Count; i++)
                result.Add(new Vector2(CellCenterToRealV247LikeOriginal(cells[i].x), CellCenterToRealV247LikeOriginal(cells[i].y)));

            result.Add(new Vector2(dstX, dstY));

            // C2 SmartSend checks direct visibility before choosing the next topology
            // point. Keep only visible bends, not a stop/turn at every 16-pixel cell.
            var smooth = new List<Vector2>();
            Vector2 anchor = new Vector2(startRealX, startRealY);
            int cursor = 0;
            if (startAdjusted) { smooth.Add(result[0]); anchor = result[0]; cursor = 1; }
            while (cursor < result.Count)
            {
                int next = cursor;
                for (int k = result.Count - 1; k > cursor; k--)
                    if (CanTravelStraightRealV247LikeOriginal(anchor.x, anchor.y, result[k].x, result[k].y, radiusCells))
                    { next = k; break; }
                smooth.Add(result[next]);
                anchor = result[next];
                cursor = next + 1;
            }
            waypoints = smooth.ToArray();
            return waypoints != null && waypoints.Length > 0;
        }

        public static void HardCleanupOverlayV295LikeOriginal()
        {
            DestroyOverlayV247LikeOriginal();
        }

        public static void RebuildOverlayForCurrentModeLikeOriginal()
        {
            int mode = C2BuildingPassabilityOverlayHotkeyLikeOriginal.CurrentModeLikeOriginal;
            DestroyOverlayV247LikeOriginal();

            if (mode == 0)
                return;

            C2BuildingRuntimeInfoV247LikeOriginal[] infos = UnityEngine.Object.FindObjectsOfType<C2BuildingRuntimeInfoV247LikeOriginal>();
            if (infos == null || infos.Length == 0)
            {
                Debug.Log("[C2:BUILDING Q OVERLAY V301] mode=" + mode.ToString(CultureInfo.InvariantCulture) + " drawn=0 reason=no_building_infos");
                return;
            }

            s_overlayRootV247 = new GameObject("C2_BUILDING_Q_OVERLAY_V294_MODE_" + mode.ToString(CultureInfo.InvariantCulture));
            s_overlayRootV247.hideFlags = HideFlags.DontSave;
            int overlayLayer = ResolveVisibleOverlayLayerV302LikeOriginal();
            SetLayerRecursivelyV294LikeOriginal(s_overlayRootV247, overlayLayer);
            EnsureBattleCamerasSeeLayerV302LikeOriginal(overlayLayer);

            int quads = 0;
            int markers = 0;
            int lines = 0;
            int activeBuildings = 0;
            int withActiveLayer = 0;
            int withoutActiveLayer = 0;
            string missingSamples = string.Empty;

            if (mode >= 1 && mode <= 4)
            {
                string kind = ModeToZoneKindV247LikeOriginal(mode);
                var quadList = new List<C2BuildingRuntimeZoneQuadV247LikeOriginal>(256);
                for (int i = 0; i < infos.Length; i++)
                {
                    C2BuildingRuntimeInfoV247LikeOriginal info = infos[i];
                    if (info == null || !info.isActiveAndEnabled) continue;
                    activeBuildings++;
                    int infoQuads = 0;
                    for (int q = 0; q < info.ZoneQuads.Count; q++)
                    {
                        C2BuildingRuntimeZoneQuadV247LikeOriginal z = info.ZoneQuads[q];
                        if (string.Equals(z.Kind, kind, StringComparison.OrdinalIgnoreCase))
                        {
                            quadList.Add(z);
                            infoQuads++;
                        }
                    }

                    if (infoQuads > 0)
                    {
                        withActiveLayer++;
                    }
                    else
                    {
                        withoutActiveLayer++;
                        if (CountOverlaySamplesV303LikeOriginal(missingSamples) < 8)
                        {
                            if (!string.IsNullOrEmpty(missingSamples)) missingSamples += " | ";
                            missingSamples += C2BuildingRuntimeV303OverlayInfoSummaryLikeOriginal(info);
                        }
                    }
                }

                quads = quadList.Count;
                CreateQuadMeshV247LikeOriginal(s_overlayRootV247.transform, kind, quadList, ModeToColorV247LikeOriginal(mode), ModeToZoneGrowPixelsV301LikeOriginal(mode));
                CreateZoneBoundaryMeshV304LikeOriginal(s_overlayRootV247.transform, kind, quadList, ModeToBorderColorV304LikeOriginal(mode));
            }
            else if (mode == 5)
            {
                // V297: draw the selected hard route type on every building that has it.
                // Route 0 = green BORN exit, route 1 = fuchsia CONC entry/input.
                // No RAW debug routes here; RAW remains audit-only in logs.
                int routeBuildings = 0;
                for (int i = 0; i < infos.Length; i++)
                {
                    C2BuildingRuntimeInfoV247LikeOriginal info = infos[i];
                    if (info == null || !info.isActiveAndEnabled) continue;
                    activeBuildings++;
                    if (!HasActiveRouteV295LikeOriginal(info))
                    {
                        withoutActiveLayer++;
                        if (CountOverlaySamplesV303LikeOriginal(missingSamples) < 8)
                        {
                            if (!string.IsNullOrEmpty(missingSamples)) missingSamples += " | ";
                            missingSamples += C2BuildingRuntimeV303OverlayInfoSummaryLikeOriginal(info);
                        }
                        continue;
                    }
                    withActiveLayer++;
                    int madeLines;
                    int madeMarkers;
                    CreateWorldThinActiveRouteOverlayV297LikeOriginal(s_overlayRootV247.transform, info, out madeLines, out madeMarkers);
                    if (madeLines > 0 || madeMarkers > 0) routeBuildings++;
                    lines += madeLines;
                    markers += madeMarkers;
                }
                Debug.Log("[C2:ACTIVE ROUTE V301] mode=5 route=" + ActiveBornConcRouteV294LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " label='" + ActiveBornConcRouteLabelV294LikeOriginal + "'" +
                          " routeBuildings=" + routeBuildings.ToString(CultureInfo.InvariantCulture) +
                          " markers=" + markers.ToString(CultureInfo.InvariantCulture) +
                          " lines=" + lines.ToString(CultureInfo.InvariantCulture) +
                          " layer=" + overlayLayer.ToString(CultureInfo.InvariantCulture));
        }
            else if (mode == 6)
            {
                for (int i = 0; i < infos.Length; i++)
                {
                    C2BuildingRuntimeInfoV247LikeOriginal info = infos[i];
                    if (info == null || !info.isActiveAndEnabled) continue;
                    activeBuildings++;
                    int beforeLines = lines;
                    for (int l = 0; l < info.LineSortLines.Count; l++)
                    {
                        C2BuildingRuntimeLineV247LikeOriginal line = info.LineSortLines[l];
                        // V248: the MD syntax is usually "GROUND LINE ..."; these are exactly the
                        // visible LINESORT guide lines we need to inspect. V247 skipped them, so mode 6
                        // often drew zero lines.
                        if (CreateLineV247LikeOriginal(s_overlayRootV247.transform, info, line)) lines++;
                    }
                    if (lines > beforeLines)
                    {
                        withActiveLayer++;
                    }
                    else
                    {
                        withoutActiveLayer++;
                        if (CountOverlaySamplesV303LikeOriginal(missingSamples) < 8)
                        {
                            if (!string.IsNullOrEmpty(missingSamples)) missingSamples += " | ";
                            missingSamples += C2BuildingRuntimeV303OverlayInfoSummaryLikeOriginal(info);
                        }
                    }
                }
            }

            Debug.Log("[C2:BUILDING Q OVERLAY V301] mode=" + mode.ToString(CultureInfo.InvariantCulture) +
                      " label='" + C2BuildingPassabilityOverlayHotkeyLikeOriginal.CurrentModeLabelLikeOriginal + "'" +
                      " activeRoute=" + ActiveBornConcRouteV294LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " activeRouteLabel='" + ActiveBornConcRouteLabelV294LikeOriginal + "'" +
                      " buildings=" + infos.Length.ToString(CultureInfo.InvariantCulture) +
                      " activeBuildings=" + activeBuildings.ToString(CultureInfo.InvariantCulture) +
                      " withActiveLayer=" + withActiveLayer.ToString(CultureInfo.InvariantCulture) +
                      " withoutActiveLayer=" + withoutActiveLayer.ToString(CultureInfo.InvariantCulture) +
                      " quads=" + quads.ToString(CultureInfo.InvariantCulture) +
                      " markers=" + markers.ToString(CultureInfo.InvariantCulture) +
                      " lines=" + lines.ToString(CultureInfo.InvariantCulture) +
                      (string.IsNullOrEmpty(missingSamples) ? string.Empty : " missingSamples=[" + missingSamples + "]"));
        }

        private static int CountOverlaySamplesV303LikeOriginal(string samples)
        {
            if (string.IsNullOrEmpty(samples)) return 0;
            int count = 1;
            for (int i = 0; i < samples.Length; i++)
                if (samples[i] == '|') count++;
            return count;
        }

        private static string C2BuildingRuntimeV303OverlayInfoSummaryLikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal info)
        {
            if (info == null) return "<null>";
            return "md='" + (info.MdName ?? string.Empty) + "'" +
                   " src='" + (info.SourceMonsterId ?? string.Empty) + "'" +
                   " rec=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                   " obj='" + (info.gameObject != null ? info.gameObject.name : string.Empty) + "'" +
                   " siteRoot=" + (info.RuntimeConstructionSiteV303 ? "1" : "0") +
                   " visualChild=" + (info.RuntimeConstructionVisualChildV303 ? "1" : "0") +
                   " lock=" + CountZoneKindV303LikeOriginal(info, "LOCKPOINTS").ToString(CultureInfo.InvariantCulture) +
                   " buildLock=" + CountZoneKindV303LikeOriginal(info, "BUILDLOCKPOINTS").ToString(CultureInfo.InvariantCulture) +
                   " check=" + CountZoneKindV303LikeOriginal(info, "CHECKPOINTS").ToString(CultureInfo.InvariantCulture) +
                   " build=" + CountZoneKindV303LikeOriginal(info, "BUILDPOINTS").ToString(CultureInfo.InvariantCulture) +
                   " born=" + CountMarkerKindsV303LikeOriginal(info, true).ToString(CultureInfo.InvariantCulture) +
                   " conc=" + CountMarkerKindsV303LikeOriginal(info, false).ToString(CultureInfo.InvariantCulture) +
                   " lineSort=" + (info.LineSortLines != null ? info.LineSortLines.Count : 0).ToString(CultureInfo.InvariantCulture);
        }

        internal static int CountZoneKindV303LikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal info, string kind)
        {
            if (info == null || info.ZoneQuads == null) return 0;
            int count = 0;
            for (int i = 0; i < info.ZoneQuads.Count; i++)
                if (string.Equals(info.ZoneQuads[i].Kind, kind, StringComparison.OrdinalIgnoreCase))
                    count++;
            return count;
        }

        private static int CountMarkerKindsV303LikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal info, bool born)
        {
            if (info == null || info.ServiceMarkers == null) return 0;
            int count = 0;
            for (int i = 0; i < info.ServiceMarkers.Count; i++)
            {
                string kind = info.ServiceMarkers[i].Kind ?? string.Empty;
                if (born)
                {
                    if (string.Equals(kind, "BORNPOINTS", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(kind, "BORNPOINTS2", StringComparison.OrdinalIgnoreCase))
                        count++;
                }
                else if (string.Equals(kind, "CONCENTRATOR", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(kind, "CONCENTRATOR2", StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
            return count;
        }

        private static string ModeToZoneKindV247LikeOriginal(int mode)
        {
            if (mode == 1) return "LOCKPOINTS";
            if (mode == 2) return "BUILDLOCKPOINTS";
            if (mode == 3) return "CHECKPOINTS";
            if (mode == 4) return "BUILDPOINTS";
            return string.Empty;
        }

        private static Color ModeToColorV247LikeOriginal(int mode)
        {
            if (mode == 1) return new Color(1.0f, 0.02f, 0.02f, 0.86f);
            if (mode == 2) return new Color(1.0f, 0.45f, 0.00f, 0.82f);
            if (mode == 3) return new Color(1.0f, 0.92f, 0.00f, 0.82f);
            if (mode == 4) return new Color(0.00f, 0.90f, 1.0f, 0.86f);
            return new Color(1f, 1f, 1f, 0.5f);
        }

        private static Color ModeToBorderColorV304LikeOriginal(int mode)
        {
            if (mode == 1) return new Color(1.0f, 0.0f, 0.0f, 1.0f);
            if (mode == 2) return new Color(1.0f, 0.55f, 0.0f, 1.0f);
            if (mode == 3) return new Color(1.0f, 1.0f, 0.05f, 1.0f);
            if (mode == 4) return new Color(0.0f, 1.0f, 1.0f, 1.0f);
            return Color.white;
        }

        private static float ModeToZoneGrowPixelsV301LikeOriginal(int mode)
        {
            if (mode == 1 || mode == 2)
                return C2ZoneOverlayGrowPixelsV301;
            if (mode == 3 || mode == 4)
                return C2BuildZoneOverlayGrowPixelsV301;
            return 0.0f;
        }

        private static Color MarkerColorV247LikeOriginal(string kind)
        {
            // V291 overlay colors in mode 5:
            // green  = stable scaled V283 BORN/exit used by gameplay
            // magenta= stable scaled V283 CONCENTRATOR used by gameplay
            // blue   = raw-original V289 BORN audit only
            // orange = raw-original V289 CONCENTRATOR audit only
            if (C2BuildingRuntimeV291IsRawBornKindLikeOriginal(kind))
                return new Color(0.1f, 0.35f, 1.0f, 0.90f);
            if (C2BuildingRuntimeV291IsRawConcKindLikeOriginal(kind))
                return new Color(1.0f, 0.55f, 0.05f, 0.90f);
            if (C2BuildingRuntimeV291IsBornKindLikeOriginal(kind))
                return new Color(0.1f, 1.0f, 0.1f, 0.80f);

            return new Color(1.0f, 0.1f, 1.0f, 0.80f);
        }

        private static bool C2BuildingRuntimeV291IsBornKindLikeOriginal(string kind)
        {
            return string.Equals(kind, "BORNPOINTS", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(kind, "BORNPOINTS2", StringComparison.OrdinalIgnoreCase);
        }

        private static bool C2BuildingRuntimeV291IsRawBornKindLikeOriginal(string kind)
        {
            return string.Equals(kind, "BORNPOINTS_RAW_AUDIT_V291", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(kind, "BORNPOINTS2_RAW_AUDIT_V291", StringComparison.OrdinalIgnoreCase);
        }

        private static bool C2BuildingRuntimeV291IsRawConcKindLikeOriginal(string kind)
        {
            return string.Equals(kind, "CONCENTRATOR_RAW_AUDIT_V291", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(kind, "CONCENTRATOR2_RAW_AUDIT_V291", StringComparison.OrdinalIgnoreCase);
        }

        private static void DestroyOverlayV247LikeOriginal()
        {
            if (s_overlayRootV247 != null)
            {
                GameObject old = s_overlayRootV247;
                s_overlayRootV247 = null;
                if (Application.isPlaying) UnityEngine.Object.Destroy(old);
                else UnityEngine.Object.DestroyImmediate(old);
            }

            // V294 hard cleanup: older V292/V293 debug primitives could remain visible
            // after switching from mode 5 to mode 6 because they were normal scene
            // primitives. Remove all known debug overlay object prefixes.
            GameObject[] all = UnityEngine.Object.FindObjectsOfType<GameObject>();
            if (all == null) return;
            for (int i = 0; i < all.Length; i++)
            {
                GameObject go = all[i];
                if (go == null) continue;
                string n = go.name ?? string.Empty;
                bool kill =
                    n.StartsWith("C2_BUILDING_Q_OVERLAY_", StringComparison.Ordinal) ||
                    n.StartsWith("C2_Q_BIG_", StringComparison.Ordinal) ||
                    n.StartsWith("C2_Q_ACTIVE_ROUTE_", StringComparison.Ordinal) ||
                    n.StartsWith("C2_Q_ACTIVE_ROUTE_UI_", StringComparison.Ordinal) ||
                    n.StartsWith("C2_ROUTE_SCREEN_CANVAS_V295", StringComparison.Ordinal) ||
                    n.StartsWith("C2_Q_ACTIVE_ROUTE_WORLD_V296", StringComparison.Ordinal) ||
                    n.StartsWith("C2_Q_ACTIVE_ROUTE_WORLD_V297", StringComparison.Ordinal);
                if (!kill) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                else UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static Material EnsureMaterialV247LikeOriginal(Color color)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");

            Material mat = new Material(shader);
            mat.hideFlags = HideFlags.DontSave;
            mat.SetOverrideTag("Queue", "Overlay");
            mat.SetOverrideTag("RenderType", "Transparent");
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", Texture2D.whiteTexture);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", color);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 2.5f);
            mat.EnableKeyword("_EMISSION");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_Cull", (int)CullMode.Off);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_ZTest", (int)CompareFunction.Always);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 4999;
            return mat;
        }

        private static Material EnsureLineMaterialV247LikeOriginal()
        {
            if (s_lineMaterialV247 != null)
                return s_lineMaterialV247;

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");

            s_lineMaterialV247 = new Material(shader);
            s_lineMaterialV247.hideFlags = HideFlags.DontSave;
            s_lineMaterialV247.SetOverrideTag("Queue", "Overlay");
            s_lineMaterialV247.SetOverrideTag("RenderType", "Transparent");
            if (s_lineMaterialV247.HasProperty("_MainTex")) s_lineMaterialV247.SetTexture("_MainTex", Texture2D.whiteTexture);
            if (s_lineMaterialV247.HasProperty("_Color")) s_lineMaterialV247.SetColor("_Color", Color.white);
            if (s_lineMaterialV247.HasProperty("_TintColor")) s_lineMaterialV247.SetColor("_TintColor", Color.white);
            s_lineMaterialV247.DisableKeyword("_ALPHATEST_ON");
            s_lineMaterialV247.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            s_lineMaterialV247.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            s_lineMaterialV247.SetInt("_Cull", (int)CullMode.Off);
            s_lineMaterialV247.SetInt("_ZWrite", 0);
            s_lineMaterialV247.SetInt("_ZTest", (int)CompareFunction.Always);
            s_lineMaterialV247.EnableKeyword("_ALPHABLEND_ON");
            s_lineMaterialV247.renderQueue = 4999;
            return s_lineMaterialV247;
        }

        private static void CreateQuadMeshV247LikeOriginal(Transform root, string name, List<C2BuildingRuntimeZoneQuadV247LikeOriginal> quads, Color color, float growOriginalPixels = 0.0f)
        {
            if (root == null || quads == null || quads.Count == 0)
                return;

            var go = new GameObject("C2_Q_" + name + "_V301");
            go.transform.SetParent(root, false);
            go.layer = root.gameObject.layer;

            var verts = new List<Vector3>(quads.Count * 4);
            var tris = new List<int>(quads.Count * 6);

            for (int i = 0; i < quads.Count; i++)
            {
                C2BuildingRuntimeZoneQuadV247LikeOriginal q = quads[i];
                Vector3 qa = q.A;
                Vector3 qb = q.B;
                Vector3 qc = q.C;
                Vector3 qd = q.D;
                ExpandCellQuadForOverlayV301LikeOriginal(q, growOriginalPixels, ref qa, ref qb, ref qc, ref qd);

                int b = verts.Count;
                verts.Add(qa);
                verts.Add(qb);
                verts.Add(qc);
                verts.Add(qd);
                tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 3);
            }

            Mesh mesh = new Mesh();
            mesh.name = go.name + "_Mesh";
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = EnsureMaterialV247LikeOriginal(color);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 32760;
        }

        private static void CreateZoneBoundaryMeshV304LikeOriginal(Transform root, string name, List<C2BuildingRuntimeZoneQuadV247LikeOriginal> quads, Color color)
        {
            if (root == null || quads == null || quads.Count == 0)
                return;

            var cells = new HashSet<long>();
            for (int i = 0; i < quads.Count; i++)
                cells.Add(CellKeyV304LikeOriginal(quads[i].CellX, quads[i].CellY));

            var verts = new List<Vector3>(quads.Count * 8);
            var tris = new List<int>(quads.Count * 12);
            for (int i = 0; i < quads.Count; i++)
            {
                C2BuildingRuntimeZoneQuadV247LikeOriginal q = quads[i];
                int x = q.CellX;
                int y = q.CellY;
                if (!cells.Contains(CellKeyV304LikeOriginal(x - 1, y)))
                    AppendZoneBoundarySegmentV304LikeOriginal(verts, tris, q.A, q.D);
                if (!cells.Contains(CellKeyV304LikeOriginal(x + 1, y)))
                    AppendZoneBoundarySegmentV304LikeOriginal(verts, tris, q.B, q.C);
                if (!cells.Contains(CellKeyV304LikeOriginal(x, y - 1)))
                    AppendZoneBoundarySegmentV304LikeOriginal(verts, tris, q.A, q.B);
                if (!cells.Contains(CellKeyV304LikeOriginal(x, y + 1)))
                    AppendZoneBoundarySegmentV304LikeOriginal(verts, tris, q.D, q.C);
            }

            if (verts.Count == 0)
                return;

            var go = new GameObject("C2_Q_" + name + "_BORDER_V304");
            go.transform.SetParent(root, false);
            go.layer = root.gameObject.layer;

            Mesh mesh = new Mesh();
            mesh.name = go.name + "_Mesh";
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            mr.sharedMaterial = EnsureMaterialV247LikeOriginal(color);
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 32761;
        }

        private static long CellKeyV304LikeOriginal(int x, int y)
        {
            return (((long)x) << 32) ^ (uint)y;
        }

        private static void AppendZoneBoundarySegmentV304LikeOriginal(List<Vector3> verts, List<int> tris, Vector3 p0, Vector3 p1)
        {
            Vector3 dir = p1 - p0;
            dir.y = 0.0f;
            if (dir.sqrMagnitude < 0.0001f)
                return;
            dir.Normalize();
            Vector3 side = new Vector3(-dir.z, 0.0f, dir.x) * (C2ZoneBorderWidthWorldV304 * 0.5f);
            p0.y += 0.18f;
            p1.y += 0.18f;

            int b = verts.Count;
            verts.Add(p0 - side);
            verts.Add(p0 + side);
            verts.Add(p1 + side);
            verts.Add(p1 - side);
            tris.Add(b + 0); tris.Add(b + 1); tris.Add(b + 2);
            tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 3);
        }

        private static void ExpandCellQuadForOverlayV301LikeOriginal(
            C2BuildingRuntimeZoneQuadV247LikeOriginal q,
            float growOriginalPixels,
            ref Vector3 a,
            ref Vector3 b,
            ref Vector3 c,
            ref Vector3 d)
        {
            if (growOriginalPixels <= 0.001f)
                return;

            float factor = 1.0f + (growOriginalPixels * 2.0f / 16.0f);
            Vector3 center = (a + b + c + d) * 0.25f;
            a = ScaleQuadCornerXZV301LikeOriginal(center, a, factor);
            b = ScaleQuadCornerXZV301LikeOriginal(center, b, factor);
            c = ScaleQuadCornerXZV301LikeOriginal(center, c, factor);
            d = ScaleQuadCornerXZV301LikeOriginal(center, d, factor);
        }

        private static Vector3 ScaleQuadCornerXZV301LikeOriginal(Vector3 center, Vector3 corner, float factor)
        {
            return new Vector3(
                center.x + (corner.x - center.x) * factor,
                corner.y,
                center.z + (corner.z - center.z) * factor);
        }

        private static void CreateMarkerMeshV247LikeOriginal(Transform root, string name, List<C2BuildingRuntimeMarkerV247LikeOriginal> markers)
        {
            if (root == null || markers == null || markers.Count == 0)
                return;

            var born = new List<C2BuildingRuntimeZoneQuadV247LikeOriginal>();
            var conc = new List<C2BuildingRuntimeZoneQuadV247LikeOriginal>();
            var bornRawAudit = new List<C2BuildingRuntimeZoneQuadV247LikeOriginal>();
            var concRawAudit = new List<C2BuildingRuntimeZoneQuadV247LikeOriginal>();

            for (int i = 0; i < markers.Count; i++)
            {
                C2BuildingRuntimeMarkerV247LikeOriginal m = markers[i];
                var q = new C2BuildingRuntimeZoneQuadV247LikeOriginal(m.Kind, m.A, m.B, m.C, m.D, 0, 0);
                if (C2BuildingRuntimeV291IsRawBornKindLikeOriginal(m.Kind))
                    bornRawAudit.Add(q);
                else if (C2BuildingRuntimeV291IsRawConcKindLikeOriginal(m.Kind))
                    concRawAudit.Add(q);
                else if (C2BuildingRuntimeV291IsBornKindLikeOriginal(m.Kind))
                    born.Add(q);
                else
                    conc.Add(q);
            }

            CreateQuadMeshV247LikeOriginal(root, name + "_BORN_V283", born, MarkerColorV247LikeOriginal("BORNPOINTS"));
            CreateQuadMeshV247LikeOriginal(root, name + "_CONCENTRATOR_V283", conc, MarkerColorV247LikeOriginal("CONCENTRATOR"));
            CreateQuadMeshV247LikeOriginal(root, name + "_BORN_RAW_AUDIT_V291", bornRawAudit, MarkerColorV247LikeOriginal("BORNPOINTS_RAW_AUDIT_V291"));
            CreateQuadMeshV247LikeOriginal(root, name + "_CONC_RAW_AUDIT_V291", concRawAudit, MarkerColorV247LikeOriginal("CONCENTRATOR_RAW_AUDIT_V291"));
        }

        private static C2BuildingRuntimeInfoV247LikeOriginal ChooseActiveRouteInfoV295LikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal[] infos, out string reason)
        {
            reason = "none";
            if (infos == null || infos.Length == 0) return null;

            // Prefer selected HUD building. That is the one the user is producing from.
            C2SettlementBuildingSelectableV1LikeOriginal selected = null;
            try { selected = C2GameplayHudV1.C2GameplayHudV133SelectedBuildingLikeOriginal; }
            catch { selected = null; }

            if (selected != null)
            {
                C2BuildingRuntimeInfoV247LikeOriginal bestSelected = null;
                int bestSelectedScore = int.MaxValue;
                for (int i = 0; i < infos.Length; i++)
                {
                    C2BuildingRuntimeInfoV247LikeOriginal info = infos[i];
                    if (info == null || !info.isActiveAndEnabled) continue;
                    if (!HasActiveRouteV295LikeOriginal(info)) continue;

                    int dist = Mathf.Abs(info.RealX - selected.RealX) + Mathf.Abs(info.RealY - selected.RealY);
                    int score = dist;
                    if (info.RecordIndex == selected.RecordIndex) score = -1000000 + dist;
                    if (score < bestSelectedScore)
                    {
                        bestSelectedScore = score;
                        bestSelected = info;
                    }
                }
                if (bestSelected != null)
                {
                    reason = "selected_building record=" + selected.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                             " infoRecord=" + bestSelected.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                             " score=" + bestSelectedScore.ToString(CultureInfo.InvariantCulture);
                    return bestSelected;
                }
            }

            // Fallback: building whose active path is nearest to screen center.
            Camera cam = FindRouteOverlayCameraV295LikeOriginal();
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            C2BuildingRuntimeInfoV247LikeOriginal best = null;
            float bestScoreF = float.MaxValue;
            for (int i = 0; i < infos.Length; i++)
            {
                C2BuildingRuntimeInfoV247LikeOriginal info = infos[i];
                if (info == null || !info.isActiveAndEnabled) continue;
                if (!HasActiveRouteV295LikeOriginal(info)) continue;
                Color c; string label;
                List<Vector3> path = C2BuildingRuntimeV294GetActiveServiceWorldPathLikeOriginal(info, out c, out label);
                if (path == null || path.Count == 0) continue;
                Vector3 w = path[0];
                Vector3 sp = cam != null ? cam.WorldToScreenPoint(w) : new Vector3(w.x, w.z, 1.0f);
                if (sp.z < -0.01f) continue;
                float d = (new Vector2(sp.x, sp.y) - center).sqrMagnitude;
                if (d < bestScoreF)
                {
                    bestScoreF = d;
                    best = info;
                }
            }
            if (best != null)
            {
                reason = "nearest_to_screen_center infoRecord=" + best.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                         " score=" + bestScoreF.ToString("F1", CultureInfo.InvariantCulture);
            }
            return best;
        }

        private static bool HasActiveRouteV295LikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal info)
        {
            if (info == null) return false;
            string label;
            List<Vector2> route = C2BuildingRuntimeV294GetActiveServicePathLikeOriginal(info, out label);
            return route != null && route.Count > 0;
        }

        private static Camera FindRouteOverlayCameraV295LikeOriginal()
        {
            Camera cam = null;
            GameObject go = GameObject.Find("C2_BattleTerrainCamera_Iso");
            if (go != null) cam = go.GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindObjectOfType<Camera>();
            return cam;
        }

        private static void CreateWorldThinActiveRouteOverlayV297LikeOriginal(Transform root, C2BuildingRuntimeInfoV247LikeOriginal info, out int lineObjects, out int markerObjects)
        {
            lineObjects = 0;
            markerObjects = 0;
            if (root == null || info == null) return;

            Color color;
            string routeLabel;
            List<Vector3> worldPath = C2BuildingRuntimeV294GetActiveServiceWorldPathLikeOriginal(info, out color, out routeLabel);
            if (worldPath == null || worldPath.Count == 0) return;

            // V297: no screen-space projection. Keep the debug path attached to the map.
            // Very small lift avoids terrain z-fighting without making the line float.
            var pts = new Vector3[worldPath.Count];
            for (int i = 0; i < worldPath.Count; i++)
                pts[i] = worldPath[i] + Vector3.up * 2.0f;

            if (pts.Length >= 2)
            {
                GameObject go = new GameObject("C2_Q_ACTIVE_ROUTE_WORLD_V297_LINE_" +
                                               (info.MdName ?? "Bld") + "_" +
                                               info.RecordIndex.ToString(CultureInfo.InvariantCulture) + "_" +
                                               (routeLabel ?? string.Empty));
                go.transform.SetParent(root, false);
                SetLayerRecursivelyV294LikeOriginal(go, C2DebugOverlayLayerV294);

                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = EnsureRouteLineMaterialV297LikeOriginal(color);
                lr.positionCount = pts.Length;
                lr.useWorldSpace = true;
                lr.SetPositions(pts);
                lr.widthMultiplier = C2ServiceRouteLineWidthV301;
                lr.startWidth = C2ServiceRouteLineWidthV301;
                lr.endWidth = C2ServiceRouteLineWidthV301;
                lr.alignment = LineAlignment.View;
                lr.textureMode = LineTextureMode.Stretch;
                lr.numCornerVertices = 1;
                lr.numCapVertices = 2;
                lr.sortingOrder = 32760;
                lr.startColor = new Color(color.r, color.g, color.b, 0.95f);
                lr.endColor = new Color(color.r, color.g, color.b, 0.95f);
                lineObjects++;
            }

            markerObjects += CreateActiveServiceMarkerMeshV301LikeOriginal(root, info, color, routeLabel);
        }

        private static int CreateActiveServiceMarkerMeshV301LikeOriginal(Transform root, C2BuildingRuntimeInfoV247LikeOriginal info, Color color, string routeLabel)
        {
            if (root == null || info == null || info.ServiceMarkers == null || info.ServiceMarkers.Count == 0)
                return 0;

            var markers = new List<C2BuildingRuntimeZoneQuadV247LikeOriginal>(info.ServiceMarkers.Count);
            for (int i = 0; i < info.ServiceMarkers.Count; i++)
            {
                C2BuildingRuntimeMarkerV247LikeOriginal m = info.ServiceMarkers[i];
                if (!ActiveRouteUsesMarkerV301LikeOriginal(m.Kind))
                    continue;

                Vector3 a = ScaleMarkerCornerV301LikeOriginal(m.Center, m.A);
                Vector3 b = ScaleMarkerCornerV301LikeOriginal(m.Center, m.B);
                Vector3 c = ScaleMarkerCornerV301LikeOriginal(m.Center, m.C);
                Vector3 d = ScaleMarkerCornerV301LikeOriginal(m.Center, m.D);
                markers.Add(new C2BuildingRuntimeZoneQuadV247LikeOriginal(m.Kind, a, b, c, d, 0, 0));
            }

            if (markers.Count == 0)
                return 0;

            string safeLabel = string.IsNullOrEmpty(routeLabel) ? "SERVICE" : routeLabel;
            CreateQuadMeshV247LikeOriginal(
                root,
                "ACTIVE_ROUTE_MARKERS_V301_" + safeLabel + "_" + info.RecordIndex.ToString(CultureInfo.InvariantCulture),
                markers,
                new Color(color.r, color.g, color.b, 0.78f));
            return markers.Count;
        }

        private static bool ActiveRouteUsesMarkerV301LikeOriginal(string kind)
        {
            int route = ActiveBornConcRouteV294LikeOriginal;
            if (route == 0)
                return C2BuildingRuntimeV291IsBornKindLikeOriginal(kind);

            return string.Equals(kind, "CONCENTRATOR", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(kind, "CONCENTRATOR2", StringComparison.OrdinalIgnoreCase);
        }

        private static Vector3 ScaleMarkerCornerV301LikeOriginal(Vector3 center, Vector3 corner)
        {
            return ScaleQuadCornerXZV301LikeOriginal(center, corner, C2ServiceMarkerScaleV301);
        }

        private static Material EnsureRouteLineMaterialV297LikeOriginal(Color color)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");

            // One material per current active route color is not worth caching separately;
            // recreate is acceptable because overlay rebuild happens only on Q-mode changes.
            Material mat = new Material(shader);
            mat.hideFlags = HideFlags.DontSave;
            mat.SetOverrideTag("Queue", "Overlay");
            mat.SetOverrideTag("RenderType", "Transparent");
            Color c = new Color(color.r, color.g, color.b, 0.95f);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", Texture2D.whiteTexture);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", c);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c * 1.8f);
            mat.EnableKeyword("_EMISSION");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_Cull", (int)CullMode.Off);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_ZTest", (int)CompareFunction.Always);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 4999;
            return mat;
        }

        private static int CreateScreenSpaceActiveRouteOverlayV295LikeOriginal(Transform root, C2BuildingRuntimeInfoV247LikeOriginal info)
        {
            if (root == null || info == null) return 0;

            Color color;
            string routeLabel;
            List<Vector3> worldPath = C2BuildingRuntimeV294GetActiveServiceWorldPathLikeOriginal(info, out color, out routeLabel);
            if (worldPath == null || worldPath.Count == 0) return 0;

            Camera cam = FindRouteOverlayCameraV295LikeOriginal();
            if (cam == null) return 0;

            GameObject canvasGo = new GameObject("C2_ROUTE_SCREEN_CANVAS_V295_" + (info.MdName ?? "Bld") + "_" + info.RecordIndex.ToString(CultureInfo.InvariantCulture), typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(root, false);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            canvas.overrideSorting = true;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1.0f;

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            var screenPts = new List<Vector2>(worldPath.Count);
            for (int i = 0; i < worldPath.Count; i++)
            {
                // Lift only for projection source. Screen-space overlay itself is depth-free.
                Vector3 sp = cam.WorldToScreenPoint(worldPath[i] + Vector3.up * 56.0f);
                if (sp.z < -0.01f) continue;
                screenPts.Add(new Vector2(sp.x, sp.y));
            }
            if (screenPts.Count == 0) return 0;

            Color core = new Color(color.r, color.g, color.b, 1.0f);
            Color halo = new Color(color.r, color.g, color.b, 0.35f);
            int made = 0;
            for (int i = 0; i < screenPts.Count - 1; i++)
            {
                made += CreateUiLineV295LikeOriginal(canvasRect, screenPts[i], screenPts[i + 1], halo, 28.0f, "HALO", i);
                made += CreateUiLineV295LikeOriginal(canvasRect, screenPts[i], screenPts[i + 1], core, 10.0f, "CORE", i);
            }
            for (int i = 0; i < screenPts.Count; i++)
            {
                made += CreateUiCircleV295LikeOriginal(canvasRect, screenPts[i], halo, 58.0f, "HALO", i);
                made += CreateUiCircleV295LikeOriginal(canvasRect, screenPts[i], core, 34.0f, "CORE", i);
            }

            Debug.Log("[C2:ACTIVE ROUTE SCREEN V295] infoRecord=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                      " md='" + (info.MdName ?? string.Empty) + "'" +
                      " route=" + ActiveBornConcRouteV294LikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " label='" + (routeLabel ?? string.Empty) + "'" +
                      " worldPoints=" + worldPath.Count.ToString(CultureInfo.InvariantCulture) +
                      " screenPoints=" + screenPts.Count.ToString(CultureInfo.InvariantCulture) +
                      " uiObjects=" + made.ToString(CultureInfo.InvariantCulture));
            return made;
        }

        private static int CreateUiLineV295LikeOriginal(RectTransform parent, Vector2 a, Vector2 b, Color color, float width, string suffix, int index)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return 0;
            GameObject go = new GameObject("C2_Q_ACTIVE_ROUTE_UI_LINE_V295_" + suffix + "_" + index.ToString(CultureInfo.InvariantCulture), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.sizeDelta = new Vector2(len, width);
            rt.localRotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            Image img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return 1;
        }

        private static int CreateUiCircleV295LikeOriginal(RectTransform parent, Vector2 pos, Color color, float diameter, string suffix, int index)
        {
            GameObject go = new GameObject("C2_Q_ACTIVE_ROUTE_UI_POINT_V295_" + suffix + "_" + index.ToString(CultureInfo.InvariantCulture), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(diameter, diameter);
            Image img = go.GetComponent<Image>();
            img.sprite = EnsureRouteCircleSpriteV295LikeOriginal();
            img.color = color;
            img.raycastTarget = false;
            return 1;
        }

        private static Sprite EnsureRouteCircleSpriteV295LikeOriginal()
        {
            if (s_routeCircleSpriteV295 != null) return s_routeCircleSpriteV295;
            const int side = 64;
            s_routeCircleTextureV295 = new Texture2D(side, side, TextureFormat.RGBA32, false);
            s_routeCircleTextureV295.hideFlags = HideFlags.DontSave;
            Color32 clear = new Color32(255, 255, 255, 0);
            Color32 white = new Color32(255, 255, 255, 255);
            float r = (side - 2) * 0.5f;
            Vector2 c = new Vector2((side - 1) * 0.5f, (side - 1) * 0.5f);
            for (int y = 0; y < side; y++)
            {
                for (int x = 0; x < side; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    s_routeCircleTextureV295.SetPixel(x, y, d <= r ? white : clear);
                }
            }
            s_routeCircleTextureV295.Apply(false, true);
            s_routeCircleSpriteV295 = Sprite.Create(s_routeCircleTextureV295, new Rect(0, 0, side, side), new Vector2(0.5f, 0.5f), side);
            s_routeCircleSpriteV295.hideFlags = HideFlags.DontSave;
            return s_routeCircleSpriteV295;
        }

        private static int CreateBigServicePathOverlayV292LikeOriginal(Transform root, C2BuildingRuntimeInfoV247LikeOriginal info)
        {
            if (root == null || info == null || info.ServiceMarkers == null || info.ServiceMarkers.Count == 0)
                return 0;

            Color color;
            string routeLabel;
            List<Vector3> active = C2BuildingRuntimeV294GetActiveServiceWorldPathLikeOriginal(info, out color, out routeLabel);
            if (active == null || active.Count == 0)
                return 0;

            string prefix = (info.MdName ?? "Bld") + "_" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) + "_" + routeLabel;
            return CreateBigPathOverlayV292LikeOriginal(root, prefix, active, color, 28.0f, 16.0f);
        }

        private static int CreateBigPathOverlayV292LikeOriginal(Transform root, string name, List<Vector3> points, Color color, float pointDiameter, float lineWidth)
        {
            if (root == null || points == null || points.Count == 0)
                return 0;

            Color coreColor = new Color(color.r, color.g, color.b, 1.0f);
            Color haloColor = new Color(color.r, color.g, color.b, 0.35f);
            Material coreMat = EnsureMaterialV247LikeOriginal(coreColor);
            Material haloMat = EnsureMaterialV247LikeOriginal(haloColor);

            int made = 0;
            float lift = 42.0f;

            // V294: create explicit glowing tubes. They are on the same overlay layer as
            // building sprites and use ZTest Always + very high renderQueue, so they are
            // visible through the pseudo-3D building. Draw only the active route.
            if (points.Count >= 2)
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Vector3 a = points[i] + Vector3.up * lift;
                    Vector3 b = points[i + 1] + Vector3.up * lift;
                    Vector3 d = b - a;
                    float len = d.magnitude;
                    if (len < 0.001f) continue;

                    made += CreateTubeSegmentV294LikeOriginal(root, name, i, a, b, Mathf.Max(8.0f, lineWidth * 1.45f), haloMat, "HALO");
                    made += CreateTubeSegmentV294LikeOriginal(root, name, i, a + Vector3.up * 0.8f, b + Vector3.up * 0.8f, Mathf.Max(4.0f, lineWidth * 0.62f), coreMat, "CORE");
                }
            }

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 pos = points[i] + Vector3.up * (lift + 2.0f);
                made += CreateSphereMarkerV294LikeOriginal(root, name, i, pos, Mathf.Max(20.0f, pointDiameter * 1.55f), haloMat, "HALO");
                made += CreateSphereMarkerV294LikeOriginal(root, name, i, pos + Vector3.up * 0.8f, Mathf.Max(10.0f, pointDiameter * 0.78f), coreMat, "CORE");
            }

            return made;
        }

        private static int CreateTubeSegmentV294LikeOriginal(Transform root, string name, int index, Vector3 a, Vector3 b, float radius, Material mat, string suffix)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.001f) return 0;

            GameObject tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tube.name = "C2_Q_ACTIVE_ROUTE_TUBE_V294_" + (name ?? "path") + "_" + suffix + "_" + index.ToString(CultureInfo.InvariantCulture);
            tube.transform.SetParent(root, false);
            tube.transform.position = (a + b) * 0.5f;
            tube.transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            tube.transform.localScale = new Vector3(radius, len * 0.5f, radius);
            SetLayerRecursivelyV294LikeOriginal(tube, C2DebugOverlayLayerV294);

            Collider col = tube.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(col);
                else UnityEngine.Object.DestroyImmediate(col);
            }

            Renderer r = tube.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.sortingOrder = 32767;
            }
            return 1;
        }

        private static int CreateSphereMarkerV294LikeOriginal(Transform root, string name, int index, Vector3 pos, float diameter, Material mat, string suffix)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "C2_Q_ACTIVE_ROUTE_POINT_V294_" + (name ?? "path") + "_" + suffix + "_" + index.ToString(CultureInfo.InvariantCulture);
            sphere.transform.SetParent(root, false);
            sphere.transform.position = pos;
            sphere.transform.localScale = Vector3.one * diameter;
            SetLayerRecursivelyV294LikeOriginal(sphere, C2DebugOverlayLayerV294);

            Collider col = sphere.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(col);
                else UnityEngine.Object.DestroyImmediate(col);
            }

            Renderer r = sphere.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.sortingOrder = 32767;
            }
            return 1;
        }

        private static void SetLayerRecursivelyV294LikeOriginal(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            Transform t = go.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                if (child != null) SetLayerRecursivelyV294LikeOriginal(child.gameObject, layer);
            }
        }

        private static int ResolveVisibleOverlayLayerV302LikeOriginal()
        {
            Camera cam = FindRouteOverlayCameraV295LikeOriginal();
            int mask = cam != null ? cam.cullingMask : -1;
            if ((mask & 1) != 0)
                return 0;

            for (int layer = 0; layer < 32; layer++)
            {
                if (layer == 7)
                    continue;
                if ((mask & (1 << layer)) != 0)
                    return layer;
            }

            return 0;
        }

        private static void EnsureBattleCamerasSeeLayerV302LikeOriginal(int layer)
        {
            if (layer < 0 || layer > 31)
                return;

            int bit = 1 << layer;
            try
            {
                Camera[] cams = Camera.allCameras;
                for (int i = 0; i < cams.Length; i++)
                {
                    Camera cam = cams[i];
                    if (cam == null) continue;
                    string name = cam.name ?? string.Empty;
                    if (name.IndexOf("SpriteDepth", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    if (name.IndexOf("BattleTerrainCamera", StringComparison.OrdinalIgnoreCase) < 0 &&
                        name.IndexOf("C2_BattleTerrainCamera", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    cam.cullingMask |= bit;
                }
            }
            catch
            {
            }
        }

        private static bool CreateLineV247LikeOriginal(Transform root, C2BuildingRuntimeInfoV247LikeOriginal info, C2BuildingRuntimeLineV247LikeOriginal line)
        {
            // Show only the current render stage, through its actual pseudo matrix.
            if (root == null || info == null || info.OwnerMode == null ||
                !info.OwnerMode.HasProjectedBuildingLineLikeOriginal(info, line)) return false;

            var go = new GameObject("C2_Q_LINESORT_V249_" +
                                    (info != null ? info.RecordIndex.ToString(CultureInfo.InvariantCulture) : "x") +
                                    "_" + (line.AnimationName ?? string.Empty) +
                                    "_" + line.FrameIndex.ToString(CultureInfo.InvariantCulture));
            go.transform.SetParent(root, false);
            go.layer = root.gameObject.layer;
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = EnsureLineMaterialV247LikeOriginal();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            // V249: the camera scale is about 1.2 world units per screen pixel on Skirmish2;
            // 0.42 world units was sub-pixel and therefore looked invisible. Keep the debug
            // line thick and always-on-top; this does not affect gameplay sorting.
            lr.widthMultiplier = 4.0f;
            lr.startWidth = 4.0f;
            lr.endWidth = 4.0f;
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 2;
            lr.sortingOrder = 32760;
            lr.startColor = new Color(0.0f, 0.95f, 1.0f, 1.0f);
            lr.endColor = new Color(0.0f, 0.95f, 1.0f, 1.0f);
            info.OwnerMode.RegisterProjectedBuildingLineLikeOriginal(info, line, lr);
            return true;
        }
    }

    public sealed partial class C2BattleTerrainMode
    {
        private void C2BuildingRuntimeV247AttachBuildingLikeOriginal(
            GameObject parent,
            C2Building3InuRecordLikeOriginal record,
            C2BuildingMdInfoLikeOriginal md,
            float visualPixelScale,
            float mapPixelScale,
            bool attachSelectableAndDestructionV303LikeOriginal = true)
        {
            if (parent == null || md == null)
                return;

            float scale = Mathf.Max(0.0001f, mapPixelScale > 0.0001f ? mapPixelScale : WallOriginalXYUnitToWorldScaleV8LikeOriginal());
            float safeVisualScale = Mathf.Max(0.0001f, visualPixelScale > 0.0001f ? visualPixelScale : scale);
            float visualToMapScale = Mathf.Clamp(safeVisualScale / scale, 0.25f, 4.0f);

            // V379 FOUNDATION-A:
            // Gameplay geometry from UnitsMD is already expressed in the original
            // Cossacks II map/cell coordinate system. The visual sprite scale is a
            // Unity renderer adaptation only and must NEVER move LOCK/BUILD/CHECK/
            // BUILDPOINTS or BORN/CONCENTRATOR service points.
            //
            // Original references:
            //   OneObject::GetCornerXY + NM->LockX/LockY
            //   OneObject::FindPoint + NM->BuildPtX/BuildPtY
            //   Build.cpp::ProduceObjLink + NM->BornPtX/BornPtY
            //
            // Therefore gameplay scale is exactly 1.0 regardless of renderer scale.
            const float mdGameplayScaleV379 = 1.0f;
            const float bornConcGameplayScaleV379 = 1.0f;

            // V248:
            // Runtime construction sites already have the real selectable component on the site root
            // with SourceMonsterId = UnitId, for example BldKaz(AU). The pseudo-3D visual child is
            // recreated every build phase and uses mdName/visual names, so if it is selectable the HUD
            // resolves the wrong member and the building menu/production list disappears.
            C2RuntimeConstructionSitePseudo3DV245LikeOriginal constructionSite =
                parent.GetComponentInParent<C2RuntimeConstructionSitePseudo3DV245LikeOriginal>();
            bool isRuntimeConstructionVisualChild = constructionSite != null && parent.gameObject != constructionSite.gameObject;

            C2SettlementBuildingSelectableV1LikeOriginal selectable = parent.GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
            if (attachSelectableAndDestructionV303LikeOriginal && !isRuntimeConstructionVisualChild)
            {
                if (selectable == null) selectable = parent.AddComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
                selectable.OwnerMode = this;
                selectable.SourceMonsterId = record.MonsterId ?? string.Empty;
                selectable.KindName = !string.IsNullOrEmpty(md.MdName) ? md.MdName : (record.MonsterId ?? string.Empty);
                selectable.RecordIndex = record.Index;
                selectable.RealX = record.RealX;
                selectable.RealY = record.RealY;
                selectable.RealDir = record.RealDir;
                selectable.Nation = record.Nation;
                int stageMax = Mathf.Max(1, md.BuildStages > 0 ? md.BuildStages : 64);
                int stage = record.Stage > 0x8000 ? Mathf.Clamp(0xFFFF - record.Stage, 0, stageMax) : stageMax;
                int lifeMax = Mathf.Max(1, md.Life > 0 ? md.Life : 1);
                selectable.LifeMaxLikeOriginal = lifeMax;
                selectable.LifeLikeOriginal = record.Stage > 0x8000 ? Mathf.Clamp((stage * lifeMax) / stageMax, 0, lifeMax) : lifeMax;
                selectable.StageMaxLikeOriginal = stageMax;
                selectable.StageLikeOriginal = stage;
                selectable.ReadyLikeOriginal = record.Stage <= 0x8000;
                selectable.NotSelectable = md.NotSelectable;
                selectable.SortKey = 12000 + (record.RealY >> 8) * 32 + record.Index;
                selectable.MapPixelToWorld = Mathf.Max(0.0001f, scale);
                selectable.SelectionHalfPixelsX = Mathf.Max(32.0f, (md.PicLx > 0 ? md.PicLx : 128) * 0.5f);
                selectable.SelectionHalfPixelsY = Mathf.Max(24.0f, (md.PicLy > 0 ? md.PicLy : 96) * 0.5f);
            }
            else if (attachSelectableAndDestructionV303LikeOriginal && selectable != null)
            {
                selectable.NotSelectable = true;
                selectable.IsSelected = false;
            }

            C2BuildingRuntimeInfoV247LikeOriginal info = parent.GetComponent<C2BuildingRuntimeInfoV247LikeOriginal>();
            if (info == null) info = parent.AddComponent<C2BuildingRuntimeInfoV247LikeOriginal>();
            info.ClearRuntimeDataV247LikeOriginal();
            info.OwnerMode = this;
            info.SourceMonsterId = record.MonsterId ?? string.Empty;
            info.MdName = md.MdName ?? string.Empty;
            info.MdPath = md.MdPath ?? string.Empty;
            info.RecordIndex = record.Index;
            info.RealX = record.RealX;
            info.RealY = record.RealY;
            info.RealDir = record.RealDir;
            info.Nation = record.Nation;
            info.NotSelectable = md.NotSelectable;
            info.BuildStages = md.BuildStages;
            info.RecordStage = record.Stage;
            info.UseBuildLockPoints = md.BuildLockPoints.Count > 0 && record.Stage > 0x8000;
            info.MapPixelScaleV277 = scale;
            info.VisualPixelScaleV277 = safeVisualScale;
            info.VisualToMapScaleV277 = visualToMapScale;
            info.RuntimeConstructionSiteV303 = constructionSite != null && parent.gameObject == constructionSite.gameObject;
            info.RuntimeConstructionVisualChildV303 = isRuntimeConstructionVisualChild;

            int cornerX;
            int cornerY;
            C2BuildingRuntimeV247BuildingCornerCellLikeOriginal(record, md, out cornerX, out cornerY);
            info.CornerCellX = cornerX;
            info.CornerCellY = cornerY;

            Debug.Log("[C2:BORN_CONC AUDIT V281 BUILD_ATTACH] md='" + (info.MdName ?? string.Empty) +
                      "' sourceMonster='" + (info.SourceMonsterId ?? string.Empty) +
                      "' record=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                      " nation=" + info.Nation.ToString(CultureInfo.InvariantCulture) +
                      " runtimeVisualChild=" + isRuntimeConstructionVisualChild +
                      " runtimeSiteRoot=" + info.RuntimeConstructionSiteV303 +
                      " selectableDestructionAttach=" + attachSelectableAndDestructionV303LikeOriginal +
                      " real=(" + info.RealX.ToString(CultureInfo.InvariantCulture) + "/" + info.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                      " realPix=(" + (info.RealX / 16.0f).ToString("0.0", CultureInfo.InvariantCulture) + "/" + (info.RealY / 16.0f).ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                      " cornerCell=(" + info.CornerCellX.ToString(CultureInfo.InvariantCulture) + "/" + info.CornerCellY.ToString(CultureInfo.InvariantCulture) + ")" +
                      " cornerPix=(" + (info.CornerCellX << 4).ToString(CultureInfo.InvariantCulture) + "/" + (info.CornerCellY << 4).ToString(CultureInfo.InvariantCulture) + ")" +
                      " picDxDy=(" + md.PicDx.ToString(CultureInfo.InvariantCulture) + "/" + md.PicDy.ToString(CultureInfo.InvariantCulture) + ")" +
                      " picLxLy=(" + md.PicLx.ToString(CultureInfo.InvariantCulture) + "/" + md.PicLy.ToString(CultureInfo.InvariantCulture) + ")" +
                      " mapPixelScale=" + scale.ToString("0.######", CultureInfo.InvariantCulture) +
                      " visualPixelScale=" + safeVisualScale.ToString("0.######", CultureInfo.InvariantCulture) +
                      " visualToMapScale=" + visualToMapScale.ToString("0.######", CultureInfo.InvariantCulture) +
                      " gameplayGeometryScaleV379=" + mdGameplayScaleV379.ToString("0.######", CultureInfo.InvariantCulture) +
                      " parent='" + (parent != null ? parent.name : string.Empty) + "' parentWorld=" + (parent != null ? parent.transform.position.ToString("F3") : "<null>") +
                      " mdPath='" + (md.MdPath ?? string.Empty) + "'");

            C2BuildingRuntimeV247AddCellZoneListLikeOriginal(info, "LOCKPOINTS", md.LockPoints, 0.20f, mdGameplayScaleV379);
            C2BuildingRuntimeV247AddCellZoneListLikeOriginal(info, "BUILDLOCKPOINTS", md.BuildLockPoints, 0.23f, mdGameplayScaleV379);
            C2BuildingRuntimeV247AddCellZoneListLikeOriginal(info, "CHECKPOINTS", md.CheckPoints, 0.26f, mdGameplayScaleV379);
            C2BuildingRuntimeV247AddCellZoneListLikeOriginal(info, "BUILDPOINTS", md.BuildPoints, 0.29f, mdGameplayScaleV379);
            C2BuildingRuntimeV378Add3DBarsLikeOriginal(info, record, md);

            bool useVisualServiceOverlayV304 = false; // V377: display the actual gameplay route.
            string bornVisualAuditV304;
            string born2VisualAuditV304;
            string concVisualAuditV304;
            string conc2VisualAuditV304;
            List<Vector2> bornVisualRealV305;
            List<Vector2> born2VisualRealV305;
            List<Vector2> concVisualRealV305;
            List<Vector2> conc2VisualRealV305;
            List<Vector3> bornVisualCentersV304 = C2BuildingRuntimeV304BuildVisualServiceCentersLikeOriginal(
                record, md, md.BornPoints, safeVisualScale, 0.36f, useVisualServiceOverlayV304, "BORNPOINTS", out bornVisualAuditV304, out bornVisualRealV305);
            List<Vector3> born2VisualCentersV304 = C2BuildingRuntimeV304BuildVisualServiceCentersLikeOriginal(
                record, md, md.BornPoints2, safeVisualScale, 0.39f, useVisualServiceOverlayV304, "BORNPOINTS2", out born2VisualAuditV304, out born2VisualRealV305);
            List<Vector3> concVisualCentersV304 = C2BuildingRuntimeV304BuildVisualServiceCentersLikeOriginal(
                record, md, md.Concentrator, safeVisualScale, 0.42f, useVisualServiceOverlayV304, "CONCENTRATOR", out concVisualAuditV304, out concVisualRealV305);
            List<Vector3> conc2VisualCentersV304 = C2BuildingRuntimeV304BuildVisualServiceCentersLikeOriginal(
                record, md, md.Concentrator2, safeVisualScale, 0.45f, useVisualServiceOverlayV304, "CONCENTRATOR2", out conc2VisualAuditV304, out conc2VisualRealV305);
            info.UsesVisualProjectedServiceOverlayV304 =
                (bornVisualCentersV304 != null && bornVisualCentersV304.Count > 0) ||
                (born2VisualCentersV304 != null && born2VisualCentersV304.Count > 0) ||
                (concVisualCentersV304 != null && concVisualCentersV304.Count > 0) ||
                (conc2VisualCentersV304 != null && conc2VisualCentersV304.Count > 0);
            info.VisualProjectedServiceOverlayAuditV304 =
                "born=" + bornVisualAuditV304 +
                " born2=" + born2VisualAuditV304 +
                " conc=" + concVisualAuditV304 +
                " conc2=" + conc2VisualAuditV304;

            C2BuildingRuntimeV247AddServicePointListLikeOriginal(info, "BORNPOINTS", md.BornPoints, false, 0.36f, bornConcGameplayScaleV379, bornVisualCentersV304);
            C2BuildingRuntimeV247AddServicePointListLikeOriginal(info, "BORNPOINTS2", md.BornPoints2, true, 0.39f, bornConcGameplayScaleV379, born2VisualCentersV304);
            C2BuildingRuntimeV247AddServicePointListLikeOriginal(info, "CONCENTRATOR", md.Concentrator, false, 0.42f, bornConcGameplayScaleV379, concVisualCentersV304);
            C2BuildingRuntimeV247AddServicePointListLikeOriginal(info, "CONCENTRATOR2", md.Concentrator2, true, 0.45f, bornConcGameplayScaleV379, conc2VisualCentersV304);

            // V291: raw-original markers are audit-only. They are drawn in Q mode 5 as
            // blue/orange points, but do not feed production paths or worker destinations.
            C2BuildingRuntimeV291AddRawAuditServicePointListLikeOriginal(info, "BORNPOINTS_RAW_AUDIT_V291", md.BornPoints, 0.54f);
            C2BuildingRuntimeV291AddRawAuditServicePointListLikeOriginal(info, "BORNPOINTS2_RAW_AUDIT_V291", md.BornPoints2, 0.57f);
            C2BuildingRuntimeV291AddRawAuditServicePointListLikeOriginal(info, "CONCENTRATOR_RAW_AUDIT_V291", md.Concentrator, 0.60f);
            C2BuildingRuntimeV291AddRawAuditServicePointListLikeOriginal(info, "CONCENTRATOR2_RAW_AUDIT_V291", md.Concentrator2, 0.63f);

            List<Vector2> bornForProduction = md.BornPoints2.Count > 0 ? md.BornPoints2 : md.BornPoints;
            for (int i = 0; i < bornForProduction.Count; i++)
            {
                int localX = Mathf.RoundToInt(bornForProduction[i].x);
                int localY = Mathf.RoundToInt(bornForProduction[i].y);
                info.BornExitPathReal.Add(C2BuildingRuntimeV379LocalPixelToGameplayRealLikeOriginal(info, localX, localY));
            }

            List<Vector2> bornVisualForProductionV305 = md.BornPoints2.Count > 0 ? born2VisualRealV305 : bornVisualRealV305;
            if (bornVisualForProductionV305 != null && bornVisualForProductionV305.Count == bornForProduction.Count)
                info.BornExitPathVisualRealV305.AddRange(bornVisualForProductionV305);

            List<Vector2> concForRuntime = md.Concentrator2.Count > 0 ? md.Concentrator2 : md.Concentrator;
            for (int i = 0; i < concForRuntime.Count; i++)
            {
                int localX = Mathf.RoundToInt(concForRuntime[i].x);
                int localY = Mathf.RoundToInt(concForRuntime[i].y);
                info.ConcentratorPathReal.Add(C2BuildingRuntimeV379LocalPixelToGameplayRealLikeOriginal(info, localX, localY));
            }

            List<Vector2> concVisualForRuntimeV305 = md.Concentrator2.Count > 0 ? conc2VisualRealV305 : concVisualRealV305;
            if (concVisualForRuntimeV305 != null && concVisualForRuntimeV305.Count == concForRuntime.Count)
                info.ConcentratorPathVisualRealV305.AddRange(concVisualForRuntimeV305);

            for (int i = 0; i < bornForProduction.Count; i++)
            {
                int localX = Mathf.RoundToInt(bornForProduction[i].x);
                int localY = Mathf.RoundToInt(bornForProduction[i].y);
                info.BornExitPathRawAuditV291.Add(C2BuildingRuntimeV291LocalPixelToRawRealAuditLikeOriginal(info, localX, localY));
            }

            for (int i = 0; i < concForRuntime.Count; i++)
            {
                int localX = Mathf.RoundToInt(concForRuntime[i].x);
                int localY = Mathf.RoundToInt(concForRuntime[i].y);
                info.ConcentratorPathRawAuditV291.Add(C2BuildingRuntimeV291LocalPixelToRawRealAuditLikeOriginal(info, localX, localY));
            }

            string bornSourceV281 = md.BornPoints2.Count > 0 ? "BORNPOINTS2" : "BORNPOINTS";
            string concSourceV281 = md.Concentrator2.Count > 0 ? "CONCENTRATOR2" : "CONCENTRATOR";
            Debug.Log("[C2:BORN_CONC AUDIT V281 PATHS] md='" + (info.MdName ?? string.Empty) +
                      "' record=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                      " real=(" + info.RealX.ToString(CultureInfo.InvariantCulture) + "/" + info.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                      " cornerCell=(" + info.CornerCellX.ToString(CultureInfo.InvariantCulture) + "/" + info.CornerCellY.ToString(CultureInfo.InvariantCulture) + ")" +
                      " visualToMapScale=" + visualToMapScale.ToString("0.######", CultureInfo.InvariantCulture) +
                      " gameplayServiceScaleV379=" + bornConcGameplayScaleV379.ToString("0.######", CultureInfo.InvariantCulture) +
                      " bornSource=" + bornSourceV281 +
                      " bornRawLocal=" + C2BuildingRuntimeV281FormatLocalPixelsLikeOriginal(bornForProduction) +
                      " bornFinalReal=" + C2BuildingRuntimeV281FormatRealPathLikeOriginal(info.BornExitPathReal) +
                      " bornVisualRealV305=" + C2BuildingRuntimeV281FormatRealPathLikeOriginal(info.BornExitPathVisualRealV305) +
                      " bornFinalRealRawAuditV291=" + C2BuildingRuntimeV281FormatRealPathLikeOriginal(info.BornExitPathRawAuditV291) +
                      " visualProjectedOverlayV304=" + (info.UsesVisualProjectedServiceOverlayV304 ? "1" : "0") +
                      " visualProjectedAuditV304=[" + (info.VisualProjectedServiceOverlayAuditV304 ?? string.Empty) + "]" +
                      " born0WorldScreen=" + C2BuildingRuntimeV281FormatWorldScreenForRealLikeOriginal(info.BornExitPathReal.Count > 0 ? info.BornExitPathReal[0] : Vector2.zero, info.BornExitPathReal.Count > 0) +
                      " born0RawAuditWorldScreenV291=" + C2BuildingRuntimeV281FormatWorldScreenForRealLikeOriginal(info.BornExitPathRawAuditV291.Count > 0 ? info.BornExitPathRawAuditV291[0] : Vector2.zero, info.BornExitPathRawAuditV291.Count > 0) +
                      " concSource=" + concSourceV281 +
                      " concRawLocal=" + C2BuildingRuntimeV281FormatLocalPixelsLikeOriginal(concForRuntime) +
                      " concFinalReal=" + C2BuildingRuntimeV281FormatRealPathLikeOriginal(info.ConcentratorPathReal) +
                      " concVisualRealV305=" + C2BuildingRuntimeV281FormatRealPathLikeOriginal(info.ConcentratorPathVisualRealV305) +
                      " concFinalRealRawAuditV291=" + C2BuildingRuntimeV281FormatRealPathLikeOriginal(info.ConcentratorPathRawAuditV291) +
                      " conc0WorldScreen=" + C2BuildingRuntimeV281FormatWorldScreenForRealLikeOriginal(info.ConcentratorPathReal.Count > 0 ? info.ConcentratorPathReal[0] : Vector2.zero, info.ConcentratorPathReal.Count > 0) +
                      " conc0RawAuditWorldScreenV291=" + C2BuildingRuntimeV281FormatWorldScreenForRealLikeOriginal(info.ConcentratorPathRawAuditV291.Count > 0 ? info.ConcentratorPathRawAuditV291[0] : Vector2.zero, info.ConcentratorPathRawAuditV291.Count > 0));

            C2BuildingRuntimeV379LogGameplayGeometryContractLikeOriginal(info, md, visualToMapScale);

            C2BuildingRuntimeV291LogDoorBoundsAuditLikeOriginal(parent, info);
            C2BuildingRuntimeV310LogRelativeSpriteAuditLikeOriginal(parent, info);

            C2BuildingRuntimeV247AddLineSortLikeOriginal(info, md, cornerX, cornerY, safeVisualScale);

            // V260: destruction component is per building instance, like original OBJ state.
            if (attachSelectableAndDestructionV303LikeOriginal)
                C2BuildingDestructionV260AttachLikeOriginal(parent, record, md);
        }

        internal bool C2BuildingRuntimeV303AttachConstructionSiteZonesLikeOriginal(
            GameObject site,
            string mdName,
            string unitId,
            int nation,
            int realX,
            int realY,
            int buildStages,
            int stage,
            bool ready,
            out string audit)
        {
            audit = "not_started";
            if (site == null)
            {
                audit = "site=<null>";
                return false;
            }

            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found)
            {
                audit = "md_not_found name='" + (mdName ?? string.Empty) + "'";
                return false;
            }

            int stages = Mathf.Max(1, buildStages > 0 ? buildStages : (md.BuildStages > 0 ? md.BuildStages : 64));
            int builtStage = ready ? -1 : Mathf.Clamp(stage, 0, Mathf.Max(1, stages - 1));
            string monsterId = !string.IsNullOrWhiteSpace(unitId) ? unitId : mdName;
            C2Building3InuRecordLikeOriginal record = C2BuildRuntimeRecordV245LikeOriginal(monsterId, nation, realX, realY, builtStage, stages, md.Life);

            C2SettlementBuildingSelectableV1LikeOriginal selectable = site.GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
            if (selectable != null && selectable.RecordIndex != 0)
                record.Index = selectable.RecordIndex;
            else
                record.Index = 200000 + Mathf.Abs(site.GetEntityId().GetHashCode() % 100000);
            record.MonsterId = monsterId ?? string.Empty;

            float mapPixelScale = C2MapPixelToWorldScaleV277LikeOriginal();
            float spriteScale = BuildingSpritePixelToWorldScaleV276LikeOriginal(mapPixelScale);
            C2BuildingRuntimeV247AttachBuildingLikeOriginal(site, record, md, spriteScale, mapPixelScale, false);

            C2BuildingRuntimeInfoV247LikeOriginal info = site.GetComponent<C2BuildingRuntimeInfoV247LikeOriginal>();
            if (info != null)
            {
                info.RuntimeConstructionSiteV303 = true;
                info.RuntimeConstructionVisualChildV303 = false;
            }

            audit = "contract=V303_CONSTRUCTION_SITE_ROOT_OWNS_ZONES md='" + (md.MdName ?? string.Empty) + "'" +
                    " unit='" + (monsterId ?? string.Empty) + "'" +
                    " record=" + record.Index.ToString(CultureInfo.InvariantCulture) +
                    " real=(" + realX.ToString(CultureInfo.InvariantCulture) + "," + realY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " ready=" + ready.ToString() +
                    " stage=" + stage.ToString(CultureInfo.InvariantCulture) + "/" + stages.ToString(CultureInfo.InvariantCulture) +
                    " lock=" + (info != null ? C2BuildingRuntimeInfoV247LikeOriginal.CountZoneKindV303LikeOriginal(info, "LOCKPOINTS") : 0).ToString(CultureInfo.InvariantCulture) +
                    " buildLock=" + (info != null ? C2BuildingRuntimeInfoV247LikeOriginal.CountZoneKindV303LikeOriginal(info, "BUILDLOCKPOINTS") : 0).ToString(CultureInfo.InvariantCulture) +
                    " check=" + (info != null ? C2BuildingRuntimeInfoV247LikeOriginal.CountZoneKindV303LikeOriginal(info, "CHECKPOINTS") : 0).ToString(CultureInfo.InvariantCulture) +
                    " build=" + (info != null ? C2BuildingRuntimeInfoV247LikeOriginal.CountZoneKindV303LikeOriginal(info, "BUILDPOINTS") : 0).ToString(CultureInfo.InvariantCulture) +
                    " born=" + (info != null && info.BornExitPathReal != null ? info.BornExitPathReal.Count : 0).ToString(CultureInfo.InvariantCulture) +
                    " conc=" + (info != null && info.ConcentratorPathReal != null ? info.ConcentratorPathReal.Count : 0).ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static void C2BuildingRuntimeV247BuildingCornerCellLikeOriginal(
            C2Building3InuRecordLikeOriginal record,
            C2BuildingMdInfoLikeOriginal md,
            out int cornerX,
            out int cornerY)
        {
            int picSX = md != null ? (md.PicDx << 4) : 0;
            int picSY = md != null ? (md.PicDy << 5) : 0;
            cornerX = (record.RealX + picSX) >> 8;
            cornerY = (record.RealY + picSY) >> 8;
        }

        private static readonly HashSet<string> s_gameplayGeometryAuditKeysV379 =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static void C2BuildingRuntimeV379LogGameplayGeometryContractLikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info,
            C2BuildingMdInfoLikeOriginal md,
            float visualToMapScale)
        {
            if (info == null || md == null)
                return;

            string key = (info.MdName ?? string.Empty) + "|" +
                         (info.RuntimeConstructionSiteV303 ? "site" : "placed");
            if (!s_gameplayGeometryAuditKeysV379.Add(key))
                return;

            bool bornExact = PathsEqualV379LikeOriginal(info.BornExitPathReal, info.BornExitPathRawAuditV291);
            bool concExact = PathsEqualV379LikeOriginal(info.ConcentratorPathReal, info.ConcentratorPathRawAuditV291);
            Debug.Log("[C2:GAMEPLAY GEOMETRY V379] md='" + (info.MdName ?? string.Empty) +
                      "' record=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                      " visualToMapScale=" + visualToMapScale.ToString("0.######", CultureInfo.InvariantCulture) +
                      " gameplayScale=1" +
                      " lock=" + (md.LockPoints != null ? md.LockPoints.Count : 0).ToString(CultureInfo.InvariantCulture) +
                      " buildLock=" + (md.BuildLockPoints != null ? md.BuildLockPoints.Count : 0).ToString(CultureInfo.InvariantCulture) +
                      " check=" + (md.CheckPoints != null ? md.CheckPoints.Count : 0).ToString(CultureInfo.InvariantCulture) +
                      " build=" + (md.BuildPoints != null ? md.BuildPoints.Count : 0).ToString(CultureInfo.InvariantCulture) +
                      " bornExact=" + (bornExact ? "1" : "0") +
                      " concExact=" + (concExact ? "1" : "0") +
                      " bars3D=" + (md.Bars3D != null ? md.Bars3D.Count : 0).ToString(CultureInfo.InvariantCulture));
        }

        private static bool PathsEqualV379LikeOriginal(List<Vector2> a, List<Vector2> b)
        {
            if (a == null || b == null || a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (Mathf.Abs(a[i].x - b[i].x) > 0.01f ||
                    Mathf.Abs(a[i].y - b[i].y) > 0.01f)
                    return false;
            }
            return true;
        }

        private static void C2BuildingRuntimeV378Add3DBarsLikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info,
            C2Building3InuRecordLikeOriginal record,
            C2BuildingMdInfoLikeOriginal md)
        {
            if (info == null || md == null || md.Bars3D == null || md.Bars3D.Count == 0) return;

            // NewMon.cpp::CreateNewMonsterAt, Cossacks II branch:
            // bx0=(RealX>>4)+PicDx; by0=(RealY>>4)+(PicDy<<1)
            // Add3DBar(X0,Y0,X0+L1+L2,Y0+L1-L2,H,...)
            int bx0 = (record.RealX >> 4) + md.PicDx;
            int by0 = (record.RealY >> 4) + (md.PicDy << 1);
            for (int i = 0; i < md.Bars3D.Count; i++)
            {
                C2Building3DBarLikeOriginal source = md.Bars3D[i];
                float x0 = bx0 + source.X;
                float y0 = by0 + (source.Y << 1);
                float x1 = x0 + source.L1 + source.L2;
                float y1 = y0 + source.L1 - source.L2;
                // V379: 3DBARS are gameplay collision/height geometry. NewMon.cpp
                // feeds these values directly to Add3DBar; renderer scale is not part
                // of the original calculation.
                info.Bars3D.Add(new C2BuildingRuntime3DBarV378LikeOriginal(
                    x0,
                    y0,
                    x1,
                    y1,
                    source.Height));
            }

            // OnEnable runs before the MD payload is attached. Register only after
            // the complete Cossacks II bar list has been populated.
            info.Register3DBarCellsV378LikeOriginal();
        }

        internal void C2Building3DBarWorldHeightV378LikeOriginal(
            float originalX,
            float originalY,
            int originalHeight,
            out float groundWorldY,
            out float topWorldY)
        {
            groundWorldY = WallOriginalXYToWorldV1LikeOriginal(originalX, originalY, 0.0f).y;
            topWorldY = WallOriginalXYToWorldV1LikeOriginal(originalX, originalY, originalHeight).y;
        }

        private void C2BuildingRuntimeV247AddCellZoneListLikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info,
            string kind,
            List<Vector2> localCells,
            float yOffset,
            float visualToMapScale)
        {
            if (info == null || localCells == null || localCells.Count == 0)
                return;

            HashSet<long> emitted = new HashSet<long>();
            for (int i = 0; i < localCells.Count; i++)
            {
                int sourceCellX = info.CornerCellX + Mathf.RoundToInt(localCells[i].x);
                int sourceCellY = info.CornerCellY + Mathf.RoundToInt(localCells[i].y);

                C2BuildingFootprintCellBoundsLikeOriginal(info.RealX, info.RealY, sourceCellX, sourceCellY,
                    visualToMapScale, out int minCellX, out int minCellY, out int maxCellX, out int maxCellY);

                for (int cellY = minCellY; cellY <= maxCellY; cellY++)
                {
                    for (int cellX = minCellX; cellX <= maxCellX; cellX++)
                    {
                        long key = (((long)cellX) << 32) ^ (uint)cellY;
                        if (!emitted.Add(key))
                            continue;

                        Vector3 a = WallOriginalXYToWorldV1LikeOriginal(cellX * 16.0f, cellY * 16.0f, 0.0f);
                        Vector3 b = WallOriginalXYToWorldV1LikeOriginal((cellX + 1) * 16.0f, cellY * 16.0f, 0.0f);
                        Vector3 c = WallOriginalXYToWorldV1LikeOriginal((cellX + 1) * 16.0f, (cellY + 1) * 16.0f, 0.0f);
                        Vector3 d = WallOriginalXYToWorldV1LikeOriginal(cellX * 16.0f, (cellY + 1) * 16.0f, 0.0f);
                        a.y += yOffset; b.y += yOffset; c.y += yOffset; d.y += yOffset;
                        info.ZoneQuads.Add(new C2BuildingRuntimeZoneQuadV247LikeOriginal(kind, a, b, c, d, cellX, cellY));
                    }
                }
            }
        }

        private List<Vector3> C2BuildingRuntimeV304BuildVisualServiceCentersLikeOriginal(
            C2Building3InuRecordLikeOriginal record,
            C2BuildingMdInfoLikeOriginal md,
            List<Vector2> localPixels,
            float visualPixelScale,
            float yOffset,
            bool enabled,
            string kind,
            out string audit,
            out List<Vector2> visualRealPathV305)
        {
            audit = "disabled";
            visualRealPathV305 = null;
            if (!enabled)
                return null;
            if (md == null || localPixels == null || localPixels.Count == 0)
            {
                audit = (kind ?? string.Empty) + ":empty";
                return null;
            }

            float safeScale = Mathf.Max(0.0001f, visualPixelScale);
            float nominalPerspectiveDistance = BuildingNominalPerspectiveDistanceWorldV292LikeOriginal();
            C2BuildingVisualProjectionContextV292LikeOriginal projection =
                BuildBuildingVisualProjectionContextV292LikeOriginal(md, safeScale, nominalPerspectiveDistance);
            if (!projection.Enabled)
            {
                audit = (kind ?? string.Empty) + ":projection_disabled";
                return null;
            }

            Vector3 buildingWorld = BuildingWorldPosLikeOriginal(record);
            var result = new List<Vector3>(localPixels.Count);
            var realResultV305 = new List<Vector2>(localPixels.Count);
            bool realProjectionOkV305 = true;
            for (int i = 0; i < localPixels.Count; i++)
            {
                int localX = Mathf.RoundToInt(localPixels[i].x);
                int localY = Mathf.RoundToInt(localPixels[i].y);

                // Service points in MD are in the same floor pixel coordinate space as
                // PicDx/PicDy. PicDy contributes twice in original map pixels, while
                // BORNPOINTS2/CONCENTRATOR2 are already parsed with y<<1.
                Vector3 originalDraw = SkewPtLikeOriginal(md.PicDx + localX, md.PicDy * 2.0f + localY, 0.0f);
                Vector3 local = ProjectOriginalDrawSpaceToUnityLocalV292LikeOriginal(originalDraw, safeScale, projection);
                Vector3 world = buildingWorld + local;
                world.y += yOffset + 0.35f;
                result.Add(world);

                float originalX;
                float originalY;
                if (C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(world, out originalX, out originalY))
                {
                    realResultV305.Add(new Vector2(
                        Mathf.RoundToInt(originalX * 16.0f),
                        Mathf.RoundToInt(originalY * 16.0f)));
                }
                else
                {
                    realProjectionOkV305 = false;
                }
            }

            if (realProjectionOkV305 && realResultV305.Count == result.Count)
                visualRealPathV305 = realResultV305;
            audit = (kind ?? string.Empty) + ":projected count=" + result.Count.ToString(CultureInfo.InvariantCulture) +
                    " scale=" + safeScale.ToString("0.######", CultureInfo.InvariantCulture) +
                    " realV305=" + (visualRealPathV305 != null ? visualRealPathV305.Count.ToString(CultureInfo.InvariantCulture) : "failed");
            return result;
        }

        private void C2BuildingRuntimeV247AddServicePointListLikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info,
            string kind,
            List<Vector2> localPixels,
            bool version2,
            float yOffset,
            float visualToMapScale,
            List<Vector3> visualCentersV304 = null)
        {
            if (info == null || localPixels == null || localPixels.Count == 0)
                return;

            for (int i = 0; i < localPixels.Count; i++)
            {
                int localX = Mathf.RoundToInt(localPixels[i].x);
                int localY = Mathf.RoundToInt(localPixels[i].y);

                float originalX = C2BuildingRuntimeV247ScaleOriginalXLikeOriginal(info, (info.CornerCellX << 4) + localX, visualToMapScale);
                float originalY = C2BuildingRuntimeV247ScaleOriginalYLikeOriginal(info, (info.CornerCellY << 4) + localY, visualToMapScale);
                int realX = Mathf.RoundToInt(originalX * 16.0f);
                int realY = Mathf.RoundToInt(originalY * 16.0f);
                float markerRadius = Mathf.Max(4.0f, 4.0f * Mathf.Max(1.0f, visualToMapScale));

                Vector3 center;
                Vector3 a;
                Vector3 b;
                Vector3 c;
                Vector3 d;
                if (visualCentersV304 != null && i < visualCentersV304.Count)
                {
                    center = visualCentersV304[i];
                    a = new Vector3(center.x - markerRadius, center.y, center.z - markerRadius);
                    b = new Vector3(center.x + markerRadius, center.y, center.z - markerRadius);
                    c = new Vector3(center.x + markerRadius, center.y, center.z + markerRadius);
                    d = new Vector3(center.x - markerRadius, center.y, center.z + markerRadius);
                }
                else
                {
                    center = WallOriginalXYToWorldV1LikeOriginal(originalX, originalY, 0.0f);
                    a = WallOriginalXYToWorldV1LikeOriginal(originalX - markerRadius, originalY - markerRadius, 0.0f);
                    b = WallOriginalXYToWorldV1LikeOriginal(originalX + markerRadius, originalY - markerRadius, 0.0f);
                    c = WallOriginalXYToWorldV1LikeOriginal(originalX + markerRadius, originalY + markerRadius, 0.0f);
                    d = WallOriginalXYToWorldV1LikeOriginal(originalX - markerRadius, originalY + markerRadius, 0.0f);
                    center.y += yOffset; a.y += yOffset; b.y += yOffset; c.y += yOffset; d.y += yOffset;
                }

                info.ServiceMarkers.Add(new C2BuildingRuntimeMarkerV247LikeOriginal(kind, center, a, b, c, d, localX, localY, realX, realY));
            }
        }

        private void C2BuildingRuntimeV291AddRawAuditServicePointListLikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info,
            string kind,
            List<Vector2> localPixels,
            float yOffset)
        {
            if (info == null || localPixels == null || localPixels.Count == 0)
                return;

            for (int i = 0; i < localPixels.Count; i++)
            {
                int localX = Mathf.RoundToInt(localPixels[i].x);
                int localY = Mathf.RoundToInt(localPixels[i].y);
                Vector2 real = C2BuildingRuntimeV291LocalPixelToRawRealAuditLikeOriginal(info, localX, localY);
                float originalX = real.x / 16.0f;
                float originalY = real.y / 16.0f;
                float markerRadius = 5.5f;

                Vector3 center = WallOriginalXYToWorldV1LikeOriginal(originalX, originalY, 0.0f);
                Vector3 a = WallOriginalXYToWorldV1LikeOriginal(originalX - markerRadius, originalY - markerRadius, 0.0f);
                Vector3 b = WallOriginalXYToWorldV1LikeOriginal(originalX + markerRadius, originalY - markerRadius, 0.0f);
                Vector3 c = WallOriginalXYToWorldV1LikeOriginal(originalX + markerRadius, originalY + markerRadius, 0.0f);
                Vector3 d = WallOriginalXYToWorldV1LikeOriginal(originalX - markerRadius, originalY + markerRadius, 0.0f);
                center.y += yOffset; a.y += yOffset; b.y += yOffset; c.y += yOffset; d.y += yOffset;

                info.ServiceMarkers.Add(new C2BuildingRuntimeMarkerV247LikeOriginal(kind, center, a, b, c, d, localX, localY, Mathf.RoundToInt(real.x), Mathf.RoundToInt(real.y)));
            }
        }

        private static Vector2 C2BuildingRuntimeV379LocalPixelToGameplayRealLikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info,
            int localX,
            int localY)
        {
            if (info == null)
                return Vector2.zero;

            // Build.cpp / NewMon.cpp use ((corner<<4)+local)<<4 directly.
            return new Vector2(((info.CornerCellX << 4) + localX) * 16.0f,
                               ((info.CornerCellY << 4) + localY) * 16.0f);
        }

        private static Vector2 C2BuildingRuntimeV291LocalPixelToRawRealAuditLikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info,
            int localX,
            int localY)
        {
            return C2BuildingRuntimeV379LocalPixelToGameplayRealLikeOriginal(info, localX, localY);
        }

        private static Vector2 C2BuildingRuntimeV247LocalPixelToScaledRealLikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info,
            int localX,
            int localY,
            float visualToMapScale)
        {
            if (info == null)
                return Vector2.zero;

            float originalX = C2BuildingRuntimeV247ScaleOriginalXLikeOriginal(info, (info.CornerCellX << 4) + localX, visualToMapScale);
            float originalY = C2BuildingRuntimeV247ScaleOriginalYLikeOriginal(info, (info.CornerCellY << 4) + localY, visualToMapScale);
            return new Vector2(Mathf.RoundToInt(originalX * 16.0f), Mathf.RoundToInt(originalY * 16.0f));
        }

        private static string C2BuildingRuntimeV281FormatLocalPixelsLikeOriginal(List<Vector2> points)
        {
            if (points == null) return "<null>";
            if (points.Count == 0) return "<empty>";
            var sb = new System.Text.StringBuilder(points.Count * 24);
            for (int i = 0; i < points.Count; i++)
            {
                if (i != 0) sb.Append(" -> ");
                sb.Append(i.ToString(CultureInfo.InvariantCulture)).Append(":local(")
                  .Append(Mathf.RoundToInt(points[i].x).ToString(CultureInfo.InvariantCulture)).Append(",")
                  .Append(Mathf.RoundToInt(points[i].y).ToString(CultureInfo.InvariantCulture)).Append(")");
            }
            return sb.ToString();
        }

        private static string C2BuildingRuntimeV281FormatRealPathLikeOriginal(List<Vector2> path)
        {
            if (path == null) return "<null>";
            if (path.Count == 0) return "<empty>";
            var sb = new System.Text.StringBuilder(path.Count * 42);
            for (int i = 0; i < path.Count; i++)
            {
                if (i != 0) sb.Append(" -> ");
                sb.Append(i.ToString(CultureInfo.InvariantCulture)).Append(":real(")
                  .Append(path[i].x.ToString("0", CultureInfo.InvariantCulture)).Append(",")
                  .Append(path[i].y.ToString("0", CultureInfo.InvariantCulture)).Append(") pix(")
                  .Append((path[i].x / 16.0f).ToString("0.0", CultureInfo.InvariantCulture)).Append(",")
                  .Append((path[i].y / 16.0f).ToString("0.0", CultureInfo.InvariantCulture)).Append(")");
            }
            return sb.ToString();
        }

        private string C2BuildingRuntimeV281FormatWorldScreenForRealLikeOriginal(Vector2 real, bool valid)
        {
            if (!valid) return "<none>";
            Vector3 world = WallOriginalXYToWorldV1LikeOriginal(real.x / 16.0f, real.y / 16.0f, 0.0f);
            Camera cam = Camera.main;
            if (cam == null)
            {
                Camera[] cams = Camera.allCameras;
                if (cams != null && cams.Length > 0) cam = cams[0];
            }
            string screen = "<no_camera>";
            if (cam != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(world);
                screen = sp.ToString("F3") + " camera='" + cam.name + "'";
            }
            return "real(" + real.x.ToString("0", CultureInfo.InvariantCulture) + "/" + real.y.ToString("0", CultureInfo.InvariantCulture) + ")" +
                   " pix(" + (real.x / 16.0f).ToString("0.0", CultureInfo.InvariantCulture) + "/" + (real.y / 16.0f).ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                   " world=" + world.ToString("F3") + " screen=" + screen;
        }

        internal static void C2BuildingFootprintCellBoundsLikeOriginal(
            int realX, int realY, int cellX, int cellY, float scale,
            out int minX, out int minY, out int maxX, out int maxY)
        {
            float x0 = C2BuildingRuntimeV247ScaleOriginalCoordLikeOriginal(realX / 16.0f, cellX * 16.0f, scale);
            float x1 = C2BuildingRuntimeV247ScaleOriginalCoordLikeOriginal(realX / 16.0f, (cellX + 1) * 16.0f, scale);
            float y0 = C2BuildingRuntimeV247ScaleOriginalCoordLikeOriginal(realY / 16.0f, cellY * 16.0f, scale);
            float y1 = C2BuildingRuntimeV247ScaleOriginalCoordLikeOriginal(realY / 16.0f, (cellY + 1) * 16.0f, scale);
            minX = Mathf.FloorToInt(Mathf.Min(x0, x1) / 16.0f);
            minY = Mathf.FloorToInt(Mathf.Min(y0, y1) / 16.0f);
            maxX = Mathf.Max(minX, Mathf.FloorToInt((Mathf.Max(x0, x1) - 0.001f) / 16.0f));
            maxY = Mathf.Max(minY, Mathf.FloorToInt((Mathf.Max(y0, y1) - 0.001f) / 16.0f));
        }

        internal float C2BuildingFootprintScaleLikeOriginal()
        {
            // V379 FOUNDATION-A:
            // LOCKPOINTS/BUILDLOCKPOINTS/CHECKPOINTS/BUILDPOINTS are authored in
            // original gameplay cells. They are not visual sprite pixels.
            return 1.0f;
        }

        internal Vector2 C2BuildingFootprintPointLikeOriginal(int realX, int realY, float originalX, float originalY)
        {
            // Keep signature for existing callers, but preserve original gameplay
            // coordinates exactly. realX/realY are intentionally unused here.
            return new Vector2(originalX, originalY);
        }

        private static float C2BuildingRuntimeV247ScaleOriginalXLikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info,
            float originalX,
            float visualToMapScale)
        {
            float rootX = info != null ? info.RealX / 16.0f : 0.0f;
            return C2BuildingRuntimeV247ScaleOriginalCoordLikeOriginal(rootX, originalX, visualToMapScale);
        }

        private static float C2BuildingRuntimeV247ScaleOriginalYLikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info,
            float originalY,
            float visualToMapScale)
        {
            float rootY = info != null ? info.RealY / 16.0f : 0.0f;
            return C2BuildingRuntimeV247ScaleOriginalCoordLikeOriginal(rootY, originalY, visualToMapScale);
        }

        private static float C2BuildingRuntimeV247ScaleOriginalCoordLikeOriginal(
            float rootOriginal,
            float original,
            float visualToMapScale)
        {
            return rootOriginal + (original - rootOriginal) * Mathf.Max(0.0001f, visualToMapScale);
        }

        private void C2BuildingRuntimeV291LogDoorBoundsAuditLikeOriginal(GameObject parent, C2BuildingRuntimeInfoV247LikeOriginal info)
        {
            if (parent == null || info == null)
                return;

            if (!C2BuildingRuntimeV291ShouldAuditDoorLikeOriginal(info))
                return;

            Renderer[] renderers = parent.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                Debug.Log("[C2:BORN_CONC DOOR AUDIT V291] md='" + (info.MdName ?? string.Empty) +
                          "' record=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                          " result=no_renderers parent='" + parent.name + "'");
                return;
            }

            bool hasBounds = false;
            Bounds bounds = new Bounds(parent.transform.position, Vector3.zero);
            int usedRenderers = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled) continue;
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
                usedRenderers++;
            }

            if (!hasBounds)
            {
                Debug.Log("[C2:BORN_CONC DOOR AUDIT V291] md='" + (info.MdName ?? string.Empty) +
                          "' record=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                          " result=no_enabled_renderer_bounds parent='" + parent.name + "'");
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                Camera[] cams = Camera.allCameras;
                if (cams != null && cams.Length > 0) cam = cams[0];
            }

            Vector2 bornScaled = info.BornExitPathReal.Count > 0 ? info.BornExitPathReal[0] : Vector2.zero;
            Vector2 bornRaw = info.BornExitPathRawAuditV291.Count > 0 ? info.BornExitPathRawAuditV291[0] : Vector2.zero;
            Vector2 concScaled = info.ConcentratorPathReal.Count > 0 ? info.ConcentratorPathReal[0] : Vector2.zero;
            Vector2 concRaw = info.ConcentratorPathRawAuditV291.Count > 0 ? info.ConcentratorPathRawAuditV291[0] : Vector2.zero;

            Vector3 bornScaledWorld = info.BornExitPathReal.Count > 0 ? WallOriginalXYToWorldV1LikeOriginal(bornScaled.x / 16.0f, bornScaled.y / 16.0f, 0.0f) : Vector3.zero;
            Vector3 bornRawWorld = info.BornExitPathRawAuditV291.Count > 0 ? WallOriginalXYToWorldV1LikeOriginal(bornRaw.x / 16.0f, bornRaw.y / 16.0f, 0.0f) : Vector3.zero;
            Vector3 concScaledWorld = info.ConcentratorPathReal.Count > 0 ? WallOriginalXYToWorldV1LikeOriginal(concScaled.x / 16.0f, concScaled.y / 16.0f, 0.0f) : Vector3.zero;
            Vector3 concRawWorld = info.ConcentratorPathRawAuditV291.Count > 0 ? WallOriginalXYToWorldV1LikeOriginal(concRaw.x / 16.0f, concRaw.y / 16.0f, 0.0f) : Vector3.zero;

            string bornScaledAudit = C2BuildingRuntimeV291NearestBottomAuditLikeOriginal(bounds, bornScaledWorld, cam, info.BornExitPathReal.Count > 0);
            string bornRawAudit = C2BuildingRuntimeV291NearestBottomAuditLikeOriginal(bounds, bornRawWorld, cam, info.BornExitPathRawAuditV291.Count > 0);
            string concScaledAudit = C2BuildingRuntimeV291NearestBottomAuditLikeOriginal(bounds, concScaledWorld, cam, info.ConcentratorPathReal.Count > 0);
            string concRawAudit = C2BuildingRuntimeV291NearestBottomAuditLikeOriginal(bounds, concRawWorld, cam, info.ConcentratorPathRawAuditV291.Count > 0);

            C2RuntimeConstructionSitePseudo3DV245LikeOriginal site = parent.GetComponentInParent<C2RuntimeConstructionSitePseudo3DV245LikeOriginal>();
            bool runtimeVisualChild = site != null && site.gameObject != parent;

            Debug.Log("[C2:BORN_CONC DOOR AUDIT V291] md='" + (info.MdName ?? string.Empty) +
                      "' sourceMonster='" + (info.SourceMonsterId ?? string.Empty) +
                      "' record=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                      " runtimeVisualChild=" + runtimeVisualChild.ToString() +
                      " real=(" + info.RealX.ToString(CultureInfo.InvariantCulture) + "/" + info.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                      " cornerCell=(" + info.CornerCellX.ToString(CultureInfo.InvariantCulture) + "/" + info.CornerCellY.ToString(CultureInfo.InvariantCulture) + ")" +
                      " visualToMapScale=" + info.VisualToMapScaleV277.ToString("0.######", CultureInfo.InvariantCulture) +
                      " renderers=" + usedRenderers.ToString(CultureInfo.InvariantCulture) + "/" + renderers.Length.ToString(CultureInfo.InvariantCulture) +
                      " boundsMin=" + bounds.min.ToString("F3") +
                      " boundsMax=" + bounds.max.ToString("F3") +
                      " boundsCenter=" + bounds.center.ToString("F3") +
                      " bornScaledV283=" + C2BuildingRuntimeV291FormatRealPointLikeOriginal(bornScaled, info.BornExitPathReal.Count > 0) +
                      " bornRawAuditV289=" + C2BuildingRuntimeV291FormatRealPointLikeOriginal(bornRaw, info.BornExitPathRawAuditV291.Count > 0) +
                      " bornScaledVsRawDeltaReal=(" + (bornScaled.x - bornRaw.x).ToString("0", CultureInfo.InvariantCulture) + "/" + (bornScaled.y - bornRaw.y).ToString("0", CultureInfo.InvariantCulture) + ")" +
                      " bornScaledBottomDelta=" + bornScaledAudit +
                      " bornRawBottomDelta=" + bornRawAudit +
                      " concScaledV283=" + C2BuildingRuntimeV291FormatRealPointLikeOriginal(concScaled, info.ConcentratorPathReal.Count > 0) +
                      " concRawAuditV289=" + C2BuildingRuntimeV291FormatRealPointLikeOriginal(concRaw, info.ConcentratorPathRawAuditV291.Count > 0) +
                      " concScaledVsRawDeltaReal=(" + (concScaled.x - concRaw.x).ToString("0", CultureInfo.InvariantCulture) + "/" + (concScaled.y - concRaw.y).ToString("0", CultureInfo.InvariantCulture) + ")" +
                      " concScaledBottomDelta=" + concScaledAudit +
                      " concRawBottomDelta=" + concRawAudit);
        }

        private static bool C2BuildingRuntimeV291ShouldAuditDoorLikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal info)
        {
            if (info == null) return false;
            string md = (info.MdName ?? string.Empty).ToLowerInvariant();
            string src = (info.SourceMonsterId ?? string.Empty).ToLowerInvariant();
            return md.Contains("kaz") || src.Contains("kaz");
        }

        private void C2BuildingRuntimeV310LogRelativeSpriteAuditLikeOriginal(GameObject parent, C2BuildingRuntimeInfoV247LikeOriginal info)
        {
            if (parent == null || info == null)
                return;

            if (!C2BuildingRuntimeV291ShouldAuditDoorLikeOriginal(info))
                return;

            Renderer[] renderers = parent.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return;

            bool hasBounds = false;
            Bounds bounds = new Bounds(parent.transform.position, Vector3.zero);
            int usedRenderers = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled) continue;
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
                usedRenderers++;
            }

            if (!hasBounds)
                return;

            Camera cam = Camera.main;
            if (cam == null)
            {
                Camera[] cams = Camera.allCameras;
                if (cams != null && cams.Length > 0) cam = cams[0];
            }

            C2RuntimeConstructionSitePseudo3DV245LikeOriginal site = parent.GetComponentInParent<C2RuntimeConstructionSitePseudo3DV245LikeOriginal>();
            bool runtimeVisualChild = site != null && site.gameObject != parent;
            int stageMax = Mathf.Max(1, info.BuildStages > 0 ? info.BuildStages : 64);
            int stage = info.RecordStage > 0x8000 ? Mathf.Clamp(0xFFFF - info.RecordStage, 0, stageMax) : stageMax;
            bool ready = info.RecordStage <= 0x8000;
            List<Vector2> bornV309 = C2BuildingRuntimeInfoV247LikeOriginal.C2BuildingRuntimeV309GetProductionBornExitPathLikeOriginal(info);

            Debug.Log("[C2:BUILDING REL AUDIT V310 SUMMARY] md='" + (info.MdName ?? string.Empty) +
                      "' sourceMonster='" + (info.SourceMonsterId ?? string.Empty) +
                      "' record=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                      " nation=" + info.Nation.ToString(CultureInfo.InvariantCulture) +
                      " ready=" + (ready ? "1" : "0") +
                      " stage=" + stage.ToString(CultureInfo.InvariantCulture) + "/" + stageMax.ToString(CultureInfo.InvariantCulture) +
                      " recordStageRaw=" + info.RecordStage.ToString(CultureInfo.InvariantCulture) +
                      " runtimeVisualChild=" + (runtimeVisualChild ? "1" : "0") +
                      " runtimeSiteRoot=" + (info.RuntimeConstructionSiteV303 ? "1" : "0") +
                      " real=(" + info.RealX.ToString(CultureInfo.InvariantCulture) + "/" + info.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                      " cornerCell=(" + info.CornerCellX.ToString(CultureInfo.InvariantCulture) + "/" + info.CornerCellY.ToString(CultureInfo.InvariantCulture) + ")" +
                      " visualToMapScale=" + info.VisualToMapScaleV277.ToString("0.######", CultureInfo.InvariantCulture) +
                      " renderers=" + usedRenderers.ToString(CultureInfo.InvariantCulture) + "/" + renderers.Length.ToString(CultureInfo.InvariantCulture) +
                      " parent='" + parent.name + "'" +
                      " parentWorld=" + parent.transform.position.ToString("F3") +
                      " boundsMin=" + bounds.min.ToString("F3") +
                      " boundsMax=" + bounds.max.ToString("F3") +
                      " boundsCenter=" + bounds.center.ToString("F3"));

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled)
                    continue;

                Transform rt = r.transform;
                Bounds rb = r.bounds;
                Debug.Log("[C2:BUILDING REL AUDIT V310 RENDERER] md='" + (info.MdName ?? string.Empty) +
                          "' record=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                          " idx=" + i.ToString(CultureInfo.InvariantCulture) +
                          " type='" + r.GetType().Name + "'" +
                          " name='" + C2BuildingRuntimeV310TransformPathLikeOriginal(rt, parent.transform) + "'" +
                          " localPos=" + (rt != null ? rt.localPosition.ToString("F3") : "<null>") +
                          " worldPos=" + (rt != null ? rt.position.ToString("F3") : "<null>") +
                          " boundsMin=" + rb.min.ToString("F3") +
                          " boundsMax=" + rb.max.ToString("F3") +
                          " boundsCenter=" + rb.center.ToString("F3") +
                          " centerDeltaFromCombinedXZ=(" + (rb.center.x - bounds.center.x).ToString("0.000", CultureInfo.InvariantCulture) + "/" +
                          (rb.center.z - bounds.center.z).ToString("0.000", CultureInfo.InvariantCulture) + ")");
            }

            Debug.Log("[C2:BUILDING REL AUDIT V310 PATHS] md='" + (info.MdName ?? string.Empty) +
                      "' record=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                      " " + C2BuildingRuntimeV310FormatPathRelativeToBoundsLikeOriginal("bornV309Final", bornV309, bounds, cam) +
                      " " + C2BuildingRuntimeV310FormatPathRelativeToBoundsLikeOriginal("bornVisualV305", info.BornExitPathVisualRealV305, bounds, cam) +
                      " " + C2BuildingRuntimeV310FormatPathRelativeToBoundsLikeOriginal("bornScaledV283", info.BornExitPathReal, bounds, cam) +
                      " " + C2BuildingRuntimeV310FormatPathRelativeToBoundsLikeOriginal("bornRawV291", info.BornExitPathRawAuditV291, bounds, cam) +
                      " " + C2BuildingRuntimeV310FormatPathRelativeToBoundsLikeOriginal("concScaledV283", info.ConcentratorPathReal, bounds, cam) +
                      " " + C2BuildingRuntimeV310FormatPathRelativeToBoundsLikeOriginal("concRawV291", info.ConcentratorPathRawAuditV291, bounds, cam));

            Debug.Log("[C2:BUILDING REL AUDIT V310 ZONES] md='" + (info.MdName ?? string.Empty) +
                      "' record=" + info.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                      " " + C2BuildingRuntimeV310FormatZoneKindRelativeToBoundsLikeOriginal(info, "LOCKPOINTS", bounds) +
                      " " + C2BuildingRuntimeV310FormatZoneKindRelativeToBoundsLikeOriginal(info, "BUILDLOCKPOINTS", bounds) +
                      " " + C2BuildingRuntimeV310FormatZoneKindRelativeToBoundsLikeOriginal(info, "CHECKPOINTS", bounds) +
                      " " + C2BuildingRuntimeV310FormatZoneKindRelativeToBoundsLikeOriginal(info, "BUILDPOINTS", bounds));
        }

        private string C2BuildingRuntimeV310FormatPathRelativeToBoundsLikeOriginal(string label, List<Vector2> path, Bounds bounds, Camera cam)
        {
            if (path == null) return label + "=<null>";
            if (path.Count == 0) return label + "=<empty>";

            var sb = new System.Text.StringBuilder(path.Count * 180);
            sb.Append(label).Append("[count=").Append(path.Count.ToString(CultureInfo.InvariantCulture)).Append("]=");
            for (int i = 0; i < path.Count; i++)
            {
                if (i != 0) sb.Append(" -> ");
                Vector2 real = path[i];
                Vector3 world = WallOriginalXYToWorldV1LikeOriginal(real.x / 16.0f, real.y / 16.0f, 0.0f);
                sb.Append(i.ToString(CultureInfo.InvariantCulture))
                  .Append(":real(").Append(real.x.ToString("0", CultureInfo.InvariantCulture)).Append("/")
                  .Append(real.y.ToString("0", CultureInfo.InvariantCulture)).Append(")")
                  .Append(" world=").Append(world.ToString("F3"))
                  .Append(" dCenterXZ=(").Append((world.x - bounds.center.x).ToString("0.000", CultureInfo.InvariantCulture)).Append("/")
                  .Append((world.z - bounds.center.z).ToString("0.000", CultureInfo.InvariantCulture)).Append(")")
                  .Append(" dMinXZ=(").Append((world.x - bounds.min.x).ToString("0.000", CultureInfo.InvariantCulture)).Append("/")
                  .Append((world.z - bounds.min.z).ToString("0.000", CultureInfo.InvariantCulture)).Append(")")
                  .Append(" dMaxXZ=(").Append((world.x - bounds.max.x).ToString("0.000", CultureInfo.InvariantCulture)).Append("/")
                  .Append((world.z - bounds.max.z).ToString("0.000", CultureInfo.InvariantCulture)).Append(")")
                  .Append(" bottom{").Append(C2BuildingRuntimeV291NearestBottomAuditLikeOriginal(bounds, world, cam, true)).Append("}");
            }
            return sb.ToString();
        }

        private static string C2BuildingRuntimeV310FormatZoneKindRelativeToBoundsLikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal info, string kind, Bounds bounds)
        {
            if (info == null || info.ZoneQuads == null)
                return kind + "=<none>";

            bool has = false;
            int count = 0;
            float minX = 0.0f;
            float minZ = 0.0f;
            float maxX = 0.0f;
            float maxZ = 0.0f;

            for (int i = 0; i < info.ZoneQuads.Count; i++)
            {
                C2BuildingRuntimeZoneQuadV247LikeOriginal q = info.ZoneQuads[i];
                if (!string.Equals(q.Kind, kind, StringComparison.OrdinalIgnoreCase))
                    continue;

                count++;
                C2BuildingRuntimeV310EncapsulateZonePointXZLikeOriginal(q.A, ref has, ref minX, ref minZ, ref maxX, ref maxZ);
                C2BuildingRuntimeV310EncapsulateZonePointXZLikeOriginal(q.B, ref has, ref minX, ref minZ, ref maxX, ref maxZ);
                C2BuildingRuntimeV310EncapsulateZonePointXZLikeOriginal(q.C, ref has, ref minX, ref minZ, ref maxX, ref maxZ);
                C2BuildingRuntimeV310EncapsulateZonePointXZLikeOriginal(q.D, ref has, ref minX, ref minZ, ref maxX, ref maxZ);
            }

            if (!has)
                return kind + "[count=0]=<empty>";

            float centerX = (minX + maxX) * 0.5f;
            float centerZ = (minZ + maxZ) * 0.5f;
            return kind + "[count=" + count.ToString(CultureInfo.InvariantCulture) + "]" +
                   " extentXZ=(" + minX.ToString("0.000", CultureInfo.InvariantCulture) + "/" + minZ.ToString("0.000", CultureInfo.InvariantCulture) +
                   "..." + maxX.ToString("0.000", CultureInfo.InvariantCulture) + "/" + maxZ.ToString("0.000", CultureInfo.InvariantCulture) + ")" +
                   " dCenterXZ=(" + (centerX - bounds.center.x).ToString("0.000", CultureInfo.InvariantCulture) + "/" +
                   (centerZ - bounds.center.z).ToString("0.000", CultureInfo.InvariantCulture) + ")" +
                   " dMinXZ=(" + (minX - bounds.min.x).ToString("0.000", CultureInfo.InvariantCulture) + "/" +
                   (minZ - bounds.min.z).ToString("0.000", CultureInfo.InvariantCulture) + ")" +
                   " dMaxXZ=(" + (maxX - bounds.max.x).ToString("0.000", CultureInfo.InvariantCulture) + "/" +
                   (maxZ - bounds.max.z).ToString("0.000", CultureInfo.InvariantCulture) + ")";
        }

        private static void C2BuildingRuntimeV310EncapsulateZonePointXZLikeOriginal(
            Vector3 p,
            ref bool has,
            ref float minX,
            ref float minZ,
            ref float maxX,
            ref float maxZ)
        {
            if (!has)
            {
                minX = p.x;
                maxX = p.x;
                minZ = p.z;
                maxZ = p.z;
                has = true;
                return;
            }

            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.z < minZ) minZ = p.z;
            if (p.z > maxZ) maxZ = p.z;
        }

        private static string C2BuildingRuntimeV310TransformPathLikeOriginal(Transform t, Transform stopAt)
        {
            if (t == null) return "<null>";
            var names = new List<string>(8);
            Transform cur = t;
            while (cur != null)
            {
                names.Add(cur.name ?? string.Empty);
                if (cur == stopAt) break;
                cur = cur.parent;
            }

            names.Reverse();
            var sb = new System.Text.StringBuilder(64);
            for (int i = 0; i < names.Count; i++)
            {
                if (i != 0) sb.Append("/");
                sb.Append(names[i]);
            }
            return sb.ToString();
        }

        private static string C2BuildingRuntimeV291FormatRealPointLikeOriginal(Vector2 real, bool valid)
        {
            if (!valid) return "<none>";
            return "real(" + real.x.ToString("0", CultureInfo.InvariantCulture) + "/" + real.y.ToString("0", CultureInfo.InvariantCulture) + ")" +
                   " pix(" + (real.x / 16.0f).ToString("0.0", CultureInfo.InvariantCulture) + "/" + (real.y / 16.0f).ToString("0.0", CultureInfo.InvariantCulture) + ")";
        }

        private static string C2BuildingRuntimeV291NearestBottomAuditLikeOriginal(Bounds bounds, Vector3 pointWorld, Camera cam, bool valid)
        {
            if (!valid) return "<none>";

            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 center = bounds.center;
            Vector3[] candidates = new Vector3[9];
            candidates[0] = new Vector3(center.x, min.y, center.z);
            candidates[1] = new Vector3(min.x, min.y, min.z);
            candidates[2] = new Vector3(max.x, min.y, min.z);
            candidates[3] = new Vector3(min.x, min.y, max.z);
            candidates[4] = new Vector3(max.x, min.y, max.z);
            candidates[5] = new Vector3(center.x, min.y, min.z);
            candidates[6] = new Vector3(center.x, min.y, max.z);
            candidates[7] = new Vector3(min.x, min.y, center.z);
            candidates[8] = new Vector3(max.x, min.y, center.z);

            int best = 0;
            float bestDist = float.MaxValue;
            Vector3 pointScreen = Vector3.zero;
            if (cam != null) pointScreen = cam.WorldToScreenPoint(pointWorld);

            for (int i = 0; i < candidates.Length; i++)
            {
                float d;
                if (cam != null)
                {
                    Vector3 cs = cam.WorldToScreenPoint(candidates[i]);
                    float dx = pointScreen.x - cs.x;
                    float dy = pointScreen.y - cs.y;
                    d = dx * dx + dy * dy;
                }
                else
                {
                    float dx = pointWorld.x - candidates[i].x;
                    float dz = pointWorld.z - candidates[i].z;
                    d = dx * dx + dz * dz;
                }

                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }

            Vector3 anchor = candidates[best];
            string screenText = "<no_camera>";
            if (cam != null)
            {
                Vector3 anchorScreen = cam.WorldToScreenPoint(anchor);
                Vector3 pScreen = cam.WorldToScreenPoint(pointWorld);
                screenText = "pointScreen=" + pScreen.ToString("F2") +
                             " anchorScreen=" + anchorScreen.ToString("F2") +
                             " deltaScreen=(" + (pScreen.x - anchorScreen.x).ToString("0.0", CultureInfo.InvariantCulture) + "/" +
                             (pScreen.y - anchorScreen.y).ToString("0.0", CultureInfo.InvariantCulture) + ")";
            }

            return "nearestBottomIdx=" + best.ToString(CultureInfo.InvariantCulture) +
                   " anchorWorld=" + anchor.ToString("F3") +
                   " pointWorld=" + pointWorld.ToString("F3") +
                   " deltaWorldXZ=(" + (pointWorld.x - anchor.x).ToString("0.000", CultureInfo.InvariantCulture) + "/" +
                   (pointWorld.z - anchor.z).ToString("0.000", CultureInfo.InvariantCulture) + ") " + screenText;
        }

        private void C2BuildingRuntimeV247AddLineSortLikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info,
            C2BuildingMdInfoLikeOriginal md,
            int cornerX,
            int cornerY,
            float visualPixelScale)
        {
            if (info == null || md == null || md.Animations == null)
                return;

            float scale = Mathf.Max(0.0001f, visualPixelScale);
            Vector3 buildingWorld = WallOriginalXYToWorldV1LikeOriginal(info.RealX >> 4, info.RealY >> 4, 0.0f);
            Vector3 debugLift = Vector3.up * 0.72f;

            foreach (KeyValuePair<string, C2BuildingAnimationLikeOriginal> kv in md.Animations)
            {
                C2BuildingAnimationLikeOriginal anim = kv.Value;
                if (anim == null || anim.LineSort == null || anim.LineSort.Count == 0)
                    continue;

                int count = anim.LineSort.Count;
                if (anim.Frames != null && anim.Frames.Count > 0)
                    count = Mathf.Min(count, anim.Frames.Count);

                for (int i = 0; i < count; i++)
                {
                    C2BuildingLineSortLikeOriginal ls = anim.LineSort[i];
                    bool isGround = ls.IsGround;
                    bool isTop = ls.IsTop;

                    // Engine reference: NewAnimation::DrawSpriteBuilding uses LineInfo[i*4]
                    // with the current NewFrame pivot, then GetAlignGroundTransform /
                    // GetRolledBillboardTransform / GetAlignLineTransform. The previous V247/V248
                    // overlay used map-cell coordinates, so the counted 429 lines were often drawn
                    // away from the actual sprite or too small to see.
                    if (isGround || isTop)
                        continue;

                    bool isPoint = ls.X1 == ls.X2 && ls.Y1 == ls.Y2;
                    C2BuildingAnimFrameLikeOriginal frame = anim.Frames != null && i < anim.Frames.Count
                        ? anim.Frames[i]
                        : new C2BuildingAnimFrameLikeOriginal(0, 0);

                    FramePivotLikeOriginal(md, frame, out int dx, out int dy);
                    Vector3 pivot = SkewPtLikeOriginal(-dx, -dy, 0.0f);
                    C2BuildingMatrix4LikeOriginal tm = GetAlignLineTransformLikeOriginal(pivot, ls.X1, ls.Y1, ls.X2, ls.Y2);

                    Vector3 p0;
                    Vector3 p1;
                    if (isPoint)
                    {
                        p0 = tm.TransformPoint(new Vector3(ls.X1 - 6, ls.Y1, 0.0f));
                        p1 = tm.TransformPoint(new Vector3(ls.X1 + 6, ls.Y1, 0.0f));
                    }
                    else
                    {
                        p0 = tm.TransformPoint(new Vector3(ls.X1, ls.Y1, 0.0f));
                        p1 = tm.TransformPoint(new Vector3(ls.X2, ls.Y2, 0.0f));
                    }

                    Vector3 a = buildingWorld + OriginalDrawSpaceToUnityLocalLikeOriginal(p0, scale) + debugLift;
                    Vector3 b = buildingWorld + OriginalDrawSpaceToUnityLocalLikeOriginal(p1, scale) + debugLift;

                    info.LineSortLines.Add(new C2BuildingRuntimeLineV247LikeOriginal(
                        anim.Name ?? kv.Key,
                        i,
                        a,
                        b,
                        ls.X1,
                        ls.Y1,
                        ls.X2,
                        ls.Y2,
                        isPoint,
                        isGround,
                        isTop));
                }
            }
        }

        private Vector3 C2BuildingRuntimeV247LocalPixelToWorldLikeOriginal(int cornerX, int cornerY, int localX, int localY, float yOffset)
        {
            Vector3 w = WallOriginalXYToWorldV1LikeOriginal((cornerX << 4) + localX, (cornerY << 4) + localY, 0.0f);
            w.y += yOffset;
            return w;
        }
    }
}
