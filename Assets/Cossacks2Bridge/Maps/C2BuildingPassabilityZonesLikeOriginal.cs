// C2BuildingPassabilityZonesLikeOriginal.cs
// V3: parses original MD building zones and builds a data-only MotionField plus Q red overlay.
// V2 fixed hotkey for Unity New Input System.
// V7 rollback: use the last visibly working Sprites/Default overlay material.
// V8 adds public MotionField queries used by peasants: target redirect + step blocking.
// V9 adds lightweight A* over original LOCKPOINT motion cells so units can route around buildings.
// V10 adds original BUILDLOCKPOINTS stage switching and BUILDPOINT query data. Ready map buildings keep LOCKPOINTS.
// V11 adds original FindPoint-style BUILDPOINT selection: corner+BuildPt, Norma distance, CheckBar 3x3, and path-to-buildpoint API.
// V12 adds original BORNPOINTS and CONCENTRATOR data/API layer for future production spawn and resource delivery.
// V48 refreshes Q overlay after runtime construction foundations/ready LOCKPOINTS are registered.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed class C2BuildingPassabilityOverlayHotkeyLikeOriginal : MonoBehaviour
    {
        // Q cycles original debug overlays:
        // 0 = off, 1 = LOCKPOINTS/BUILDLOCKPOINTS passability, 2 = CHECKPOINTS height-check area.
        public GameObject OverlayRoot;
        public GameObject CheckOverlayRoot;
        public KeyCode ToggleKey = KeyCode.Q;

        private static int s_ModeLikeOriginal;

        public static int CurrentModeLikeOriginal
        {
            get { return s_ModeLikeOriginal; }
        }

        private void OnEnable()
        {
            ApplyModeLikeOriginal();
        }

        private void Update()
        {
            if (C2WasTogglePressedLikeOriginal())
            {
                s_ModeLikeOriginal = (s_ModeLikeOriginal + 1) % 3;
                ApplyModeLikeOriginal();
                C2BuildingPlacementPreviewV27.C2BuildPlacementRefreshDebugOverlayLikeOriginal();
            }
        }

        public void ApplyModeLikeOriginal()
        {
            bool lockActive = s_ModeLikeOriginal == 1;
            bool checkActive = s_ModeLikeOriginal == 2;

            if (OverlayRoot != null && OverlayRoot.activeSelf != lockActive)
                OverlayRoot.SetActive(lockActive);
            if (CheckOverlayRoot != null && CheckOverlayRoot.activeSelf != checkActive)
                CheckOverlayRoot.SetActive(checkActive);
        }

        private bool C2WasTogglePressedLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // Project uses the new Input System package, so UnityEngine.Input.GetKeyDown()
                // throws InvalidOperationException. Q is the original debug key for lock/passability overlay.
                if (ToggleKey == KeyCode.Q)
                    return keyboard.qKey.wasPressedThisFrame;

                // The overlay currently only needs Q; keep the legacy KeyCode field for inspector/backward compatibility.
                return keyboard.qKey.wasPressedThisFrame;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(ToggleKey);
#else
            return false;
#endif
        }
    }

    public sealed partial class C2BattleTerrainMode
    {
        private const bool C2BuildingPassabilityZonesV1Enabled = true;
        private const bool C2BuildingPassabilityZonesV1CreateOverlay = true;
        private const float C2BuildingPassabilityZonesV1YOffset = 0.18f;
        private const int C2BuildingPassabilityZonesV1RenderQueue = 5000;

        private enum C2Settlement3InuMdV2ZoneKindLikeOriginal
        {
            Lock,
            BuildLock,
            Check,
            Build,
            Born,
            Concentrator
        }

        private struct C2Settlement3InuMdV2ZonePointLikeOriginal
        {
            public int X;
            public int Y;

            public C2Settlement3InuMdV2ZonePointLikeOriginal(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private sealed class C2Settlement3InuMdV2ZoneSetLikeOriginal
        {
            public readonly List<C2Settlement3InuMdV2ZonePointLikeOriginal> LockPoints = new List<C2Settlement3InuMdV2ZonePointLikeOriginal>();
            public readonly List<C2Settlement3InuMdV2ZonePointLikeOriginal> BuildLockPoints = new List<C2Settlement3InuMdV2ZonePointLikeOriginal>();
            public readonly List<C2Settlement3InuMdV2ZonePointLikeOriginal> CheckPoints = new List<C2Settlement3InuMdV2ZonePointLikeOriginal>();
            public readonly List<C2Settlement3InuMdV2ZonePointLikeOriginal> BuildPoints = new List<C2Settlement3InuMdV2ZonePointLikeOriginal>();
            public readonly List<C2Settlement3InuMdV2ZonePointLikeOriginal> BornPoints = new List<C2Settlement3InuMdV2ZonePointLikeOriginal>();
            public readonly List<C2Settlement3InuMdV2ZonePointLikeOriginal> ConcentratorPoints = new List<C2Settlement3InuMdV2ZonePointLikeOriginal>();

            public int CheckCenterX;
            public int CheckCenterY;
            public int CheckRadius;
            public string Audit = "";

            public bool HasAny
            {
                get
                {
                    return LockPoints.Count != 0 ||
                           BuildLockPoints.Count != 0 ||
                           CheckPoints.Count != 0 ||
                           BuildPoints.Count != 0 ||
                           BornPoints.Count != 0 ||
                           ConcentratorPoints.Count != 0;
                }
            }
        }

        private sealed class C2BuildingMotionCellLikeOriginal
        {
            public int X;
            public int Y;
            public int RecordIndex;
            public string MonsterId;
            public string MdName;
            public bool IsBuildLock;
            public string Layer;
        }

        private sealed class C2BuildingBuildPointLikeOriginal
        {
            public int X;
            public int Y;
            public int RecordIndex;
            public string MonsterId;
            public string MdName;
            public int CornerX;
            public int CornerY;
        }

        private sealed class C2BuildingCheckPointLikeOriginal
        {
            public int X;
            public int Y;
            public int RecordIndex;
            public string MonsterId;
            public string MdName;
            public int CornerX;
            public int CornerY;
        }

        private enum C2BuildingServicePointRoleLikeOriginal
        {
            Born,
            Concentrator
        }

        private sealed class C2BuildingServicePointLikeOriginal
        {
            // X/Y are motion cells used by FindPoint-like searches.
            public int X;
            public int Y;

            // RealX/RealY are exact original Real coordinates:
            // ((cornerCell << 4) + localPixelOffset) << 4.
            public float RealX;
            public float RealY;

            public int LocalX;
            public int LocalY;
            public int OriginalIndex;
            public int RecordIndex;
            public string MonsterId;
            public string MdName;
            public int CornerX;
            public int CornerY;
            public C2BuildingServicePointRoleLikeOriginal Role;
        }

        private sealed class C2BuildingMotionFieldLikeOriginal
        {
            // Active movement field. Ready buildings add LOCKPOINTS here; under-construction buildings add BUILDLOCKPOINTS here.
            public readonly Dictionary<Vector2Int, C2BuildingMotionCellLikeOriginal> Blocked = new Dictionary<Vector2Int, C2BuildingMotionCellLikeOriginal>();

            // Data-only preview of all parsed BUILDLOCKPOINTS. It is not used for path blocking until a building is under construction.
            public readonly Dictionary<Vector2Int, C2BuildingMotionCellLikeOriginal> BuildBlockedPreview = new Dictionary<Vector2Int, C2BuildingMotionCellLikeOriginal>();

            // BUILDPOINTS are stored as original corner-relative cell points, converted to map cells.
            public readonly List<C2BuildingBuildPointLikeOriginal> BuildPointCells = new List<C2BuildingBuildPointLikeOriginal>();

            // CHECKPOINTS are the original height/fit test cells used by CheckVLine/CheckHLine/maxZ-minZ.
            // They are debug-only here: yellow Q overlay mode, not movement blocking.
            public readonly List<C2BuildingCheckPointLikeOriginal> CheckPointCells = new List<C2BuildingCheckPointLikeOriginal>();

            // BORNPOINTS/CONCENTRATOR are stored both as exact Real points and FindPoint-style cells.
            public readonly List<C2BuildingServicePointLikeOriginal> BornPointCells = new List<C2BuildingServicePointLikeOriginal>();
            public readonly List<C2BuildingServicePointLikeOriginal> ConcentratorPointCells = new List<C2BuildingServicePointLikeOriginal>();

            public int BuildingRecords;
            public int UnderConstructionRecords;
            public int MdWithZones;
            public int LockCellsAdded;
            public int BuildLockCellsActive;
            public int BuildLockCellsPreview;
            public int BuildPointCellsAdded;
            public int BornPointCellsAdded;
            public int ConcentratorPointCellsAdded;
            public int DuplicateCells;
            public int DuplicateBuildLockCells;
            public int CheckPoints;
            public int BuildLockPoints;
            public int BuildPoints;
            public int BornPoints;
            public int ConcentratorPoints;

            public void Clear()
            {
                Blocked.Clear();
                BuildBlockedPreview.Clear();
                BuildPointCells.Clear();
                CheckPointCells.Clear();
                BornPointCells.Clear();
                ConcentratorPointCells.Clear();
                BuildingRecords = 0;
                UnderConstructionRecords = 0;
                MdWithZones = 0;
                LockCellsAdded = 0;
                BuildLockCellsActive = 0;
                BuildLockCellsPreview = 0;
                BuildPointCellsAdded = 0;
                BornPointCellsAdded = 0;
                ConcentratorPointCellsAdded = 0;
                DuplicateCells = 0;
                DuplicateBuildLockCells = 0;
                CheckPoints = 0;
                BuildLockPoints = 0;
                BuildPoints = 0;
                BornPoints = 0;
                ConcentratorPoints = 0;
            }
        }

        private static readonly C2BuildingMotionFieldLikeOriginal s_C2BuildingMotionFieldV1 = new C2BuildingMotionFieldLikeOriginal();
        private static string s_C2BuildingMotionFieldMapV1 = "";
        private static string s_C2BuildingMotionFieldAuditV1 = "";
        private static GameObject s_C2BuildingPassabilityOverlayV1;
        private static GameObject s_C2BuildingCheckpointsOverlayV58;
        private static Transform s_C2BuildingPassabilityOverlayParentV1;

        private static bool C2Settlement3InuMdV2TryParseBuildingZoneCommandLikeOriginal(
            string cmd,
            string[] tokens,
            string[] lines,
            ref int lineIndex,
            C2Settlement3InuMdV2Info info)
        {
            if (info == null || info.Zones == null) return false;
            if (string.IsNullOrEmpty(cmd)) return false;

            if (cmd == "LOCKPOINTS")
                return C2Settlement3InuMdV2ReadZoneBlockLikeOriginal(tokens, lines, ref lineIndex, info.Zones.LockPoints, C2Settlement3InuMdV2ZoneKindLikeOriginal.Lock, false, false, false);

            if (cmd == "BUILDLOCKPOINTS")
                return C2Settlement3InuMdV2ReadZoneBlockLikeOriginal(tokens, lines, ref lineIndex, info.Zones.BuildLockPoints, C2Settlement3InuMdV2ZoneKindLikeOriginal.BuildLock, false, false, false);

            if (cmd == "CHECKPOINTS")
            {
                bool ok = C2Settlement3InuMdV2ReadZoneBlockLikeOriginal(tokens, lines, ref lineIndex, info.Zones.CheckPoints, C2Settlement3InuMdV2ZoneKindLikeOriginal.Check, false, false, false);
                C2Settlement3InuMdV2RecomputeCheckRoundLikeOriginal(info.Zones);
                return ok;
            }

            if (cmd == "BUILDPOINTS")
                return C2Settlement3InuMdV2ReadZoneBlockLikeOriginal(tokens, lines, ref lineIndex, info.Zones.BuildPoints, C2Settlement3InuMdV2ZoneKindLikeOriginal.Build, false, false, false);

            if (cmd == "BORNPOINTS")
                return C2Settlement3InuMdV2ReadZoneBlockLikeOriginal(tokens, lines, ref lineIndex, info.Zones.BornPoints, C2Settlement3InuMdV2ZoneKindLikeOriginal.Born, true, false, false);

            if (cmd == "BORNPOINTS2")
                return C2Settlement3InuMdV2ReadZoneBlockLikeOriginal(tokens, lines, ref lineIndex, info.Zones.BornPoints, C2Settlement3InuMdV2ZoneKindLikeOriginal.Born, false, true, true);

            if (cmd == "CONCENTRATOR")
                return C2Settlement3InuMdV2ReadZoneBlockLikeOriginal(tokens, lines, ref lineIndex, info.Zones.ConcentratorPoints, C2Settlement3InuMdV2ZoneKindLikeOriginal.Concentrator, true, false, false);

            if (cmd == "CONCENTRATOR2")
            {
                bool ok = C2Settlement3InuMdV2ReadZoneBlockLikeOriginal(tokens, lines, ref lineIndex, info.Zones.ConcentratorPoints, C2Settlement3InuMdV2ZoneKindLikeOriginal.Concentrator, false, true, true);

                // Original NewMon.cpp fallback: CONCENTRATOR2 also becomes reversed BORNPOINTS
                // when explicit BORNPOINTS/BORNPOINTS2 were not declared before it.
                if (info.Zones.BornPoints.Count == 0 && info.Zones.ConcentratorPoints.Count != 0)
                {
                    for (int i = info.Zones.ConcentratorPoints.Count - 1; i >= 0; i--)
                    {
                        C2Settlement3InuMdV2ZonePointLikeOriginal cp = info.Zones.ConcentratorPoints[i];
                        info.Zones.BornPoints.Add(new C2Settlement3InuMdV2ZonePointLikeOriginal(cp.X, cp.Y));
                    }
                }

                return ok;
            }

            return false;
        }

        private static bool C2Settlement3InuMdV2ReadZoneBlockLikeOriginal(
            string[] firstTokens,
            string[] lines,
            ref int lineIndex,
            List<C2Settlement3InuMdV2ZonePointLikeOriginal> dst,
            C2Settlement3InuMdV2ZoneKindLikeOriginal kind,
            bool cellToPixelCenter,
            bool directX,
            bool directYShift)
        {
            if (dst == null) return true;
            dst.Clear();

            int expected = 0;
            if (firstTokens != null && firstTokens.Length >= 2 && C2Settlement3InuMdV2LooksLikeIntLikeOriginal(firstTokens[1]))
            {
                expected = Mathf.Max(0, C2Settlement3InuMdV2ToInt(firstTokens[1]));
            }

            if (expected <= 0)
                return true;

            int added = 0;
            C2Settlement3InuMdV2ReadZonePairsFromTokensLikeOriginal(firstTokens, 2, expected, dst, ref added, cellToPixelCenter, directX, directYShift);

            int j = lineIndex + 1;
            for (; j < lines.Length && added < expected; j++)
            {
                string raw = C2Settlement3InuMdV2StripCommentLikeOriginal(lines[j]).Trim();
                if (raw.Length == 0) continue;
                if (raw[0] == '/') continue;

                string[] t = C2Settlement3InuMdV2SplitTokensLikeOriginal(raw);
                if (t.Length == 0) continue;
                if (!C2Settlement3InuMdV2LooksLikeIntLikeOriginal(t[0]))
                    break;

                int before = added;
                C2Settlement3InuMdV2ReadZonePairsFromTokensLikeOriginal(t, 0, expected, dst, ref added, cellToPixelCenter, directX, directYShift);
                if (added == before && before > 0)
                    break;
            }

            if (j > lineIndex + 1)
                lineIndex = j - 1;

            return true;
        }

        private static void C2Settlement3InuMdV2ReadZonePairsFromTokensLikeOriginal(
            string[] tokens,
            int start,
            int expected,
            List<C2Settlement3InuMdV2ZonePointLikeOriginal> dst,
            ref int added,
            bool cellToPixelCenter,
            bool directX,
            bool directYShift)
        {
            if (tokens == null || dst == null) return;
            for (int p = start; p + 1 < tokens.Length && added < expected; p += 2)
            {
                if (!C2Settlement3InuMdV2LooksLikeIntLikeOriginal(tokens[p]) ||
                    !C2Settlement3InuMdV2LooksLikeIntLikeOriginal(tokens[p + 1]))
                    break;

                int x = C2Settlement3InuMdV2ToInt(tokens[p]);
                int y = C2Settlement3InuMdV2ToInt(tokens[p + 1]);

                if (cellToPixelCenter)
                {
                    x = x * 16 + 8;
                    y = y * 16 + 8;
                }
                else
                {
                    if (!directX)
                    {
                        // LOCK/CHECK/BUILD/BUILDLOCK keep raw MD cell offsets.
                    }

                    if (directYShift)
                        y = y << 1;
                }

                dst.Add(new C2Settlement3InuMdV2ZonePointLikeOriginal(x, y));
                added++;
            }
        }

        private static void C2Settlement3InuMdV2RecomputeCheckRoundLikeOriginal(C2Settlement3InuMdV2ZoneSetLikeOriginal zones)
        {
            if (zones == null || zones.CheckPoints.Count == 0)
            {
                if (zones != null)
                {
                    zones.CheckCenterX = 0;
                    zones.CheckCenterY = 0;
                    zones.CheckRadius = 0;
                }
                return;
            }

            int sx = 0;
            int sy = 0;
            for (int i = 0; i < zones.CheckPoints.Count; i++)
            {
                sx += zones.CheckPoints[i].X;
                sy += zones.CheckPoints[i].Y;
            }

            int cx = sx / zones.CheckPoints.Count;
            int cy = sy / zones.CheckPoints.Count;
            int maxd = 0;
            for (int i = 0; i < zones.CheckPoints.Count; i++)
            {
                int dx = zones.CheckPoints[i].X - cx;
                int dy = zones.CheckPoints[i].Y - cy;
                int d = Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy));
                if (d > maxd) maxd = d;
            }

            zones.CheckCenterX = cx;
            zones.CheckCenterY = cy;
            zones.CheckRadius = maxd;
        }

        private void C2Settlement3InuMdV2BeginBuildingZonesLikeOriginal(string mapPath, Transform root)
        {
            if (!C2BuildingPassabilityZonesV1Enabled) return;
            s_C2BuildingMotionFieldV1.Clear();
            s_C2BuildingMotionFieldMapV1 = mapPath ?? "";
            s_C2BuildingMotionFieldAuditV1 = "building_zones_begin map='" + s_C2BuildingMotionFieldMapV1 + "'";
            s_C2BuildingPassabilityOverlayV1 = null;
            s_C2BuildingCheckpointsOverlayV58 = null;
            s_C2BuildingPassabilityOverlayParentV1 = null;
        }

        private void C2Settlement3InuMdV2RegisterBuildingZonesLikeOriginal(
            C2Settlement3InuMdV2Record r,
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Kind kind)
        {
            bool useBuildLockPoints = C2Settlement3InuMdV2RecordUsesBuildLockPointsLikeOriginal(r, md);
            C2Settlement3InuMdV2RegisterBuildingZonesLikeOriginal(r, md, kind, useBuildLockPoints);
        }

        private void C2Settlement3InuMdV2RegisterBuildingZonesLikeOriginal(
            C2Settlement3InuMdV2Record r,
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Kind kind,
            bool useBuildLockPoints)
        {
            if (!C2BuildingPassabilityZonesV1Enabled) return;
            if (md == null || md.Zones == null) return;
            if (!C2Settlement3InuMdV2KindHasBuildingZonesLikeOriginal(kind)) return;

            s_C2BuildingMotionFieldV1.BuildingRecords++;

            if (!md.Zones.HasAny)
                return;

            s_C2BuildingMotionFieldV1.MdWithZones++;
            s_C2BuildingMotionFieldV1.CheckPoints += md.Zones.CheckPoints.Count;
            s_C2BuildingMotionFieldV1.BuildLockPoints += md.Zones.BuildLockPoints.Count;
            s_C2BuildingMotionFieldV1.BuildPoints += md.Zones.BuildPoints.Count;
            s_C2BuildingMotionFieldV1.BornPoints += md.Zones.BornPoints.Count;
            s_C2BuildingMotionFieldV1.ConcentratorPoints += md.Zones.ConcentratorPoints.Count;

            int cornerX;
            int cornerY;
            C2Settlement3InuMdV2BuildingCornerCellLikeOriginal(r, md, out cornerX, out cornerY);

            // CHECKPOINTS debug overlay: same corner+local cell transform used by placement/slope validator.
            for (int i = 0; i < md.Zones.CheckPoints.Count; i++)
            {
                var point = new C2BuildingCheckPointLikeOriginal();
                point.X = cornerX + md.Zones.CheckPoints[i].X;
                point.Y = cornerY + md.Zones.CheckPoints[i].Y;
                point.RecordIndex = r.Index;
                point.MonsterId = r.MonsterId ?? "";
                point.MdName = md.MdName ?? "";
                point.CornerX = cornerX;
                point.CornerY = cornerY;
                s_C2BuildingMotionFieldV1.CheckPointCells.Add(point);
            }

            // BUILDPOINTS are not p*16+8. Original keeps them as raw offsets and later does:
            // x2 = cornerX + BuildPtX[i]; y2 = cornerY + BuildPtY[i];
            for (int i = 0; i < md.Zones.BuildPoints.Count; i++)
            {
                var point = new C2BuildingBuildPointLikeOriginal();
                point.X = cornerX + md.Zones.BuildPoints[i].X;
                point.Y = cornerY + md.Zones.BuildPoints[i].Y;
                point.RecordIndex = r.Index;
                point.MonsterId = r.MonsterId ?? "";
                point.MdName = md.MdName ?? "";
                point.CornerX = cornerX;
                point.CornerY = cornerY;
                s_C2BuildingMotionFieldV1.BuildPointCells.Add(point);
                s_C2BuildingMotionFieldV1.BuildPointCellsAdded++;
            }

            // BORNPOINTS are original pixel offsets from building corner.
            // Spawn formula in Build.cpp is: Real = ((corner << 4) + BornPt) << 4.
            for (int i = 0; i < md.Zones.BornPoints.Count; i++)
            {
                C2Settlement3InuMdV2RegisterServicePointLikeOriginal(
                    s_C2BuildingMotionFieldV1.BornPointCells,
                    r,
                    md,
                    cornerX,
                    cornerY,
                    md.Zones.BornPoints[i],
                    i,
                    C2BuildingServicePointRoleLikeOriginal.Born);
                s_C2BuildingMotionFieldV1.BornPointCellsAdded++;
            }

            // CONCENTRATOR points use the same exact Real formula, and FindPoint uses (ConcPt >> 4) cells.
            for (int i = 0; i < md.Zones.ConcentratorPoints.Count; i++)
            {
                C2Settlement3InuMdV2RegisterServicePointLikeOriginal(
                    s_C2BuildingMotionFieldV1.ConcentratorPointCells,
                    r,
                    md,
                    cornerX,
                    cornerY,
                    md.Zones.ConcentratorPoints[i],
                    i,
                    C2BuildingServicePointRoleLikeOriginal.Concentrator);
                s_C2BuildingMotionFieldV1.ConcentratorPointCellsAdded++;
            }

            // Keep a data-only preview of BUILDLOCKPOINTS for the future construction layer.
            for (int i = 0; i < md.Zones.BuildLockPoints.Count; i++)
            {
                int gx = cornerX + md.Zones.BuildLockPoints[i].X;
                int gy = cornerY + md.Zones.BuildLockPoints[i].Y;
                bool duplicate;
                if (C2Settlement3InuMdV2AddMotionBlockCellLikeOriginal(
                    s_C2BuildingMotionFieldV1.BuildBlockedPreview,
                    gx,
                    gy,
                    r,
                    md,
                    true,
                    "BUILDLOCKPOINTS_PREVIEW",
                    out duplicate))
                    s_C2BuildingMotionFieldV1.BuildLockCellsPreview++;
                else if (duplicate)
                    s_C2BuildingMotionFieldV1.DuplicateBuildLockCells++;
            }

            if (useBuildLockPoints && md.Zones.BuildLockPoints.Count != 0)
            {
                // Original runtime condition: while Stage < ProduceStages and NBLockPt exists, use BLockX/BLockY.
                // For saved-map build-stage records this parser marks construction as Stage > 0x8000.
                s_C2BuildingMotionFieldV1.UnderConstructionRecords++;
                for (int i = 0; i < md.Zones.BuildLockPoints.Count; i++)
                {
                    int gx = cornerX + md.Zones.BuildLockPoints[i].X;
                    int gy = cornerY + md.Zones.BuildLockPoints[i].Y;
                    bool duplicate;
                    if (C2Settlement3InuMdV2AddMotionBlockCellLikeOriginal(
                        s_C2BuildingMotionFieldV1.Blocked,
                        gx,
                        gy,
                        r,
                        md,
                        true,
                        "BUILDLOCKPOINTS_ACTIVE",
                        out duplicate))
                        s_C2BuildingMotionFieldV1.BuildLockCellsActive++;
                    else if (duplicate)
                        s_C2BuildingMotionFieldV1.DuplicateCells++;
                }
                return;
            }

            // Ready/completed buildings keep the already verified LOCKPOINTS path.
            for (int i = 0; i < md.Zones.LockPoints.Count; i++)
            {
                int gx = cornerX + md.Zones.LockPoints[i].X;
                int gy = cornerY + md.Zones.LockPoints[i].Y;
                bool duplicate;
                if (C2Settlement3InuMdV2AddMotionBlockCellLikeOriginal(
                    s_C2BuildingMotionFieldV1.Blocked,
                    gx,
                    gy,
                    r,
                    md,
                    false,
                    "LOCKPOINTS_READY",
                    out duplicate))
                    s_C2BuildingMotionFieldV1.LockCellsAdded++;
                else if (duplicate)
                    s_C2BuildingMotionFieldV1.DuplicateCells++;
            }
        }

        private static bool C2Settlement3InuMdV2RecordUsesBuildLockPointsLikeOriginal(
            C2Settlement3InuMdV2Record r,
            C2Settlement3InuMdV2Info md)
        {
            if (md == null || md.Zones == null || md.Zones.BuildLockPoints.Count == 0) return false;

            // The original runtime test is Stage < ProduceStages.
            // In this Unity 3INU reader, existing map buildings with Stage == 0 are treated as ready/static
            // by the visual path. Build animation records are encoded as Stage > 0x8000 and use 0xFFFF - Stage.
            // Therefore we only auto-switch saved records with that explicit construction marker.
            return r.Stage > 0x8000;
        }

        private static void C2Settlement3InuMdV2RegisterServicePointLikeOriginal(
            List<C2BuildingServicePointLikeOriginal> dst,
            C2Settlement3InuMdV2Record r,
            C2Settlement3InuMdV2Info md,
            int cornerX,
            int cornerY,
            C2Settlement3InuMdV2ZonePointLikeOriginal localPoint,
            int originalIndex,
            C2BuildingServicePointRoleLikeOriginal role)
        {
            if (dst == null) return;

            var point = new C2BuildingServicePointLikeOriginal();
            point.X = cornerX + (localPoint.X >> 4);
            point.Y = cornerY + (localPoint.Y >> 4);
            point.RealX = ((cornerX << 4) + localPoint.X) << 4;
            point.RealY = ((cornerY << 4) + localPoint.Y) << 4;
            point.LocalX = localPoint.X;
            point.LocalY = localPoint.Y;
            point.OriginalIndex = originalIndex;
            point.RecordIndex = r.Index;
            point.MonsterId = r.MonsterId ?? "";
            point.MdName = md != null ? (md.MdName ?? "") : "";
            point.CornerX = cornerX;
            point.CornerY = cornerY;
            point.Role = role;
            dst.Add(point);
        }

        private static bool C2Settlement3InuMdV2AddMotionBlockCellLikeOriginal(
            Dictionary<Vector2Int, C2BuildingMotionCellLikeOriginal> dst,
            int gx,
            int gy,
            C2Settlement3InuMdV2Record r,
            C2Settlement3InuMdV2Info md,
            bool isBuildLock,
            string layer,
            out bool duplicate)
        {
            duplicate = false;
            if (dst == null) return false;

            var key = new Vector2Int(gx, gy);
            if (dst.ContainsKey(key))
            {
                duplicate = true;
                return false;
            }

            var cell = new C2BuildingMotionCellLikeOriginal();
            cell.X = gx;
            cell.Y = gy;
            cell.RecordIndex = r.Index;
            cell.MonsterId = r.MonsterId ?? "";
            cell.MdName = md != null ? (md.MdName ?? "") : "";
            cell.IsBuildLock = isBuildLock;
            cell.Layer = layer ?? "";
            dst.Add(key, cell);
            return true;
        }

        private static bool C2Settlement3InuMdV2KindHasBuildingZonesLikeOriginal(C2Settlement3InuMdV2Kind kind)
        {
            return kind == C2Settlement3InuMdV2Kind.SettlementBuilding ||
                   kind == C2Settlement3InuMdV2Kind.Building ||
                   kind == C2Settlement3InuMdV2Kind.ResourceBuilding ||
                   kind == C2Settlement3InuMdV2Kind.SpriteObject;
        }

        private static void C2Settlement3InuMdV2BuildingCornerCellLikeOriginal(
            C2Settlement3InuMdV2Record r,
            C2Settlement3InuMdV2Info md,
            out int cornerX,
            out int cornerY)
        {
            int picSX = md != null ? (md.PicDx << 4) : 0;
            int picSY = md != null ? (md.PicDy << 5) : 0;
            cornerX = (r.RealX + picSX) >> 8;
            cornerY = (r.RealY + picSY) >> 8;
        }

        private void C2Settlement3InuMdV2FinalizeBuildingZonesLikeOriginal(Transform root)
        {
            if (!C2BuildingPassabilityZonesV1Enabled) return;

            s_C2BuildingMotionFieldAuditV1 =
                "map='" + s_C2BuildingMotionFieldMapV1 + "'" +
                " buildingRecords=" + s_C2BuildingMotionFieldV1.BuildingRecords.ToString(CultureInfo.InvariantCulture) +
                " underConstructionRecords=" + s_C2BuildingMotionFieldV1.UnderConstructionRecords.ToString(CultureInfo.InvariantCulture) +
                " mdWithZones=" + s_C2BuildingMotionFieldV1.MdWithZones.ToString(CultureInfo.InvariantCulture) +
                " lockCells=" + s_C2BuildingMotionFieldV1.Blocked.Count.ToString(CultureInfo.InvariantCulture) +
                " added=" + s_C2BuildingMotionFieldV1.LockCellsAdded.ToString(CultureInfo.InvariantCulture) +
                " activeBlockCells=" + s_C2BuildingMotionFieldV1.Blocked.Count.ToString(CultureInfo.InvariantCulture) +
                " readyLockCells=" + s_C2BuildingMotionFieldV1.LockCellsAdded.ToString(CultureInfo.InvariantCulture) +
                " activeBuildLockCells=" + s_C2BuildingMotionFieldV1.BuildLockCellsActive.ToString(CultureInfo.InvariantCulture) +
                " buildLockPreviewCells=" + s_C2BuildingMotionFieldV1.BuildLockCellsPreview.ToString(CultureInfo.InvariantCulture) +
                " buildPointCells=" + s_C2BuildingMotionFieldV1.BuildPointCellsAdded.ToString(CultureInfo.InvariantCulture) +
                " bornPointCells=" + s_C2BuildingMotionFieldV1.BornPointCellsAdded.ToString(CultureInfo.InvariantCulture) +
                " concentratorPointCells=" + s_C2BuildingMotionFieldV1.ConcentratorPointCellsAdded.ToString(CultureInfo.InvariantCulture) +
                " duplicates=" + s_C2BuildingMotionFieldV1.DuplicateCells.ToString(CultureInfo.InvariantCulture) +
                " buildLockPreviewDuplicates=" + s_C2BuildingMotionFieldV1.DuplicateBuildLockCells.ToString(CultureInfo.InvariantCulture) +
                " checkPts=" + s_C2BuildingMotionFieldV1.CheckPoints.ToString(CultureInfo.InvariantCulture) +
                " buildLockPts=" + s_C2BuildingMotionFieldV1.BuildLockPoints.ToString(CultureInfo.InvariantCulture) +
                " buildPts=" + s_C2BuildingMotionFieldV1.BuildPoints.ToString(CultureInfo.InvariantCulture) +
                " bornPts=" + s_C2BuildingMotionFieldV1.BornPoints.ToString(CultureInfo.InvariantCulture) +
                " concentratorPts=" + s_C2BuildingMotionFieldV1.ConcentratorPoints.ToString(CultureInfo.InvariantCulture);

            if (root != null && C2BuildingPassabilityZonesV1CreateOverlay)
                C2Settlement3InuMdV2CreatePassabilityOverlayLikeOriginal(root);
        }


        private void C2Settlement3InuMdV2RefreshPassabilityOverlayLikeOriginal(string reason)
        {
            if (!C2BuildingPassabilityZonesV1Enabled || !C2BuildingPassabilityZonesV1CreateOverlay)
                return;

            Transform root = s_C2BuildingPassabilityOverlayParentV1 != null
                ? s_C2BuildingPassabilityOverlayParentV1
                : transform;
            if (root == null) return;

            int oldMode = C2BuildingPassabilityOverlayHotkeyLikeOriginal.CurrentModeLikeOriginal;
            if (s_C2BuildingPassabilityOverlayV1 != null)
            {
                GameObject old = s_C2BuildingPassabilityOverlayV1;
                s_C2BuildingPassabilityOverlayV1 = null;
                if (Application.isPlaying) UnityEngine.Object.Destroy(old);
                else UnityEngine.Object.DestroyImmediate(old);
            }
            if (s_C2BuildingCheckpointsOverlayV58 != null)
            {
                GameObject old = s_C2BuildingCheckpointsOverlayV58;
                s_C2BuildingCheckpointsOverlayV58 = null;
                if (Application.isPlaying) UnityEngine.Object.Destroy(old);
                else UnityEngine.Object.DestroyImmediate(old);
            }

            C2Settlement3InuMdV2CreatePassabilityOverlayLikeOriginal(root);
            C2BuildingPassabilityOverlayHotkeyLikeOriginal hotkey = root.gameObject.GetComponent<C2BuildingPassabilityOverlayHotkeyLikeOriginal>();
            if (hotkey != null) hotkey.ApplyModeLikeOriginal();

            Debug.Log("[C2:BUILD PASSABILITY V58 OVERLAY REFRESH] reason='" + (reason ?? string.Empty) +
                      "' mode=" + oldMode.ToString(CultureInfo.InvariantCulture) +
                      " lockCells=" + s_C2BuildingMotionFieldV1.Blocked.Count.ToString(CultureInfo.InvariantCulture) +
                      " buildLockPreview=" + s_C2BuildingMotionFieldV1.BuildBlockedPreview.Count.ToString(CultureInfo.InvariantCulture) +
                      " buildPoints=" + s_C2BuildingMotionFieldV1.BuildPointCells.Count.ToString(CultureInfo.InvariantCulture));
        }

        private static int C2Settlement3InuMdV2RemoveMotionBlocksByRecordIndexLikeOriginal(int recordIndex)
        {
            if (recordIndex < 0) return 0;
            if (s_C2BuildingMotionFieldV1 == null || s_C2BuildingMotionFieldV1.Blocked == null) return 0;

            int removed = 0;
            var toRemove = new List<Vector2Int>();
            foreach (var kv in s_C2BuildingMotionFieldV1.Blocked)
            {
                if (kv.Value != null && kv.Value.RecordIndex == recordIndex)
                    toRemove.Add(kv.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                if (s_C2BuildingMotionFieldV1.Blocked.Remove(toRemove[i]))
                    removed++;
            }

            for (int i = s_C2BuildingMotionFieldV1.CheckPointCells.Count - 1; i >= 0; i--)
            {
                if (s_C2BuildingMotionFieldV1.CheckPointCells[i] != null &&
                    s_C2BuildingMotionFieldV1.CheckPointCells[i].RecordIndex == recordIndex)
                    s_C2BuildingMotionFieldV1.CheckPointCells.RemoveAt(i);
            }
            return removed;
        }

        private void C2Settlement3InuMdV2CreatePassabilityOverlayLikeOriginal(Transform root)
        {
            if (root == null) return;

            var overlay = new GameObject("C2_BuildingPassability_LOCKPOINTS_Q_Overlay_V1");
            overlay.transform.SetParent(root, true);
            overlay.SetActive(false);
            s_C2BuildingPassabilityOverlayV1 = overlay;
            s_C2BuildingPassabilityOverlayParentV1 = root;

            var hotkey = root.gameObject.GetComponent<C2BuildingPassabilityOverlayHotkeyLikeOriginal>();
            if (hotkey == null) hotkey = root.gameObject.AddComponent<C2BuildingPassabilityOverlayHotkeyLikeOriginal>();
            hotkey.OverlayRoot = overlay;
            hotkey.ToggleKey = KeyCode.Q;

            if (s_C2BuildingMotionFieldV1.Blocked.Count == 0 &&
                s_C2BuildingMotionFieldV1.CheckPointCells.Count == 0 &&
                s_C2BuildingMotionFieldV1.BuildPointCells.Count == 0 &&
                s_C2BuildingMotionFieldV1.BornPointCells.Count == 0 &&
                s_C2BuildingMotionFieldV1.ConcentratorPointCells.Count == 0)
                return;

            int overlayQuadCount = s_C2BuildingMotionFieldV1.Blocked.Count +
                                   s_C2BuildingMotionFieldV1.BuildPointCells.Count +
                                   s_C2BuildingMotionFieldV1.BornPointCells.Count +
                                   s_C2BuildingMotionFieldV1.ConcentratorPointCells.Count;
            var verts = new List<Vector3>(overlayQuadCount * 4);
            var colors = new List<Color32>(overlayQuadCount * 4);
            var tris = new List<int>(overlayQuadCount * 6);
            Color32 readyLockOverlayColor = new Color32(255, 0, 0, 148);
            Color32 buildLockOverlayColor = new Color32(255, 150, 0, 148);
            Color32 buildPointOverlayColor = new Color32(0, 220, 255, 210);
            Color32 bornPointOverlayColor = new Color32(80, 255, 80, 220);
            Color32 concentratorPointOverlayColor = new Color32(255, 80, 255, 220);

            foreach (var kv in s_C2BuildingMotionFieldV1.Blocked)
            {
                int gx = kv.Key.x;
                int gy = kv.Key.y;
                Color32 overlayColor = (kv.Value != null && kv.Value.IsBuildLock) ? buildLockOverlayColor : readyLockOverlayColor;

                Vector3 a = WallOriginalXYToWorldV1LikeOriginal(gx * 16.0f, gy * 16.0f, 0.0f);
                Vector3 b = WallOriginalXYToWorldV1LikeOriginal((gx + 1) * 16.0f, gy * 16.0f, 0.0f);
                Vector3 c = WallOriginalXYToWorldV1LikeOriginal((gx + 1) * 16.0f, (gy + 1) * 16.0f, 0.0f);
                Vector3 d = WallOriginalXYToWorldV1LikeOriginal(gx * 16.0f, (gy + 1) * 16.0f, 0.0f);

                a.y += C2BuildingPassabilityZonesV1YOffset;
                b.y += C2BuildingPassabilityZonesV1YOffset;
                c.y += C2BuildingPassabilityZonesV1YOffset;
                d.y += C2BuildingPassabilityZonesV1YOffset;

                int baseIndex = verts.Count;
                verts.Add(a);
                verts.Add(b);
                verts.Add(c);
                verts.Add(d);
                colors.Add(overlayColor);
                colors.Add(overlayColor);
                colors.Add(overlayColor);
                colors.Add(overlayColor);

                tris.Add(baseIndex + 0);
                tris.Add(baseIndex + 1);
                tris.Add(baseIndex + 2);
                tris.Add(baseIndex + 0);
                tris.Add(baseIndex + 2);
                tris.Add(baseIndex + 3);
            }

            // BUILDPOINTS debug: cyan half-cell markers. These are approach/work cells, not blocking cells.
            for (int i = 0; i < s_C2BuildingMotionFieldV1.BuildPointCells.Count; i++)
            {
                C2BuildingBuildPointLikeOriginal bp = s_C2BuildingMotionFieldV1.BuildPointCells[i];
                float minX = bp.X * 16.0f + 4.0f;
                float minY = bp.Y * 16.0f + 4.0f;
                float maxX = bp.X * 16.0f + 12.0f;
                float maxY = bp.Y * 16.0f + 12.0f;

                Vector3 a = WallOriginalXYToWorldV1LikeOriginal(minX, minY, 0.0f);
                Vector3 b = WallOriginalXYToWorldV1LikeOriginal(maxX, minY, 0.0f);
                Vector3 c = WallOriginalXYToWorldV1LikeOriginal(maxX, maxY, 0.0f);
                Vector3 d = WallOriginalXYToWorldV1LikeOriginal(minX, maxY, 0.0f);

                a.y += C2BuildingPassabilityZonesV1YOffset + 0.03f;
                b.y += C2BuildingPassabilityZonesV1YOffset + 0.03f;
                c.y += C2BuildingPassabilityZonesV1YOffset + 0.03f;
                d.y += C2BuildingPassabilityZonesV1YOffset + 0.03f;

                int baseIndex = verts.Count;
                verts.Add(a);
                verts.Add(b);
                verts.Add(c);
                verts.Add(d);

                colors.Add(buildPointOverlayColor);
                colors.Add(buildPointOverlayColor);
                colors.Add(buildPointOverlayColor);
                colors.Add(buildPointOverlayColor);

                tris.Add(baseIndex + 0);
                tris.Add(baseIndex + 1);
                tris.Add(baseIndex + 2);
                tris.Add(baseIndex + 0);
                tris.Add(baseIndex + 2);
                tris.Add(baseIndex + 3);
            }

            // BORNPOINTS debug: green exact exit/spawn points.
            for (int i = 0; i < s_C2BuildingMotionFieldV1.BornPointCells.Count; i++)
            {
                C2BuildingServicePointLikeOriginal bp = s_C2BuildingMotionFieldV1.BornPointCells[i];
                C2Settlement3InuMdV2AddServicePointOverlayQuadLikeOriginal(verts, colors, tris, bp.RealX, bp.RealY, 5.0f, bornPointOverlayColor, C2BuildingPassabilityZonesV1YOffset + 0.06f);
            }

            // CONCENTRATOR debug: magenta exact entry/resource-delivery points.
            for (int i = 0; i < s_C2BuildingMotionFieldV1.ConcentratorPointCells.Count; i++)
            {
                C2BuildingServicePointLikeOriginal cp = s_C2BuildingMotionFieldV1.ConcentratorPointCells[i];
                C2Settlement3InuMdV2AddServicePointOverlayQuadLikeOriginal(verts, colors, tris, cp.RealX, cp.RealY, 6.0f, concentratorPointOverlayColor, C2BuildingPassabilityZonesV1YOffset + 0.09f);
            }

            var mf = overlay.AddComponent<MeshFilter>();
            var mr = overlay.AddComponent<MeshRenderer>();
            var mesh = new Mesh();
            mesh.name = "C2_BuildingPassability_LOCKPOINTS_V1_Mesh";
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            Material mat = C2Settlement3InuMdV2CreatePassabilityOverlayMaterialLikeOriginal();
            mr.sharedMaterial = mat;
            mr.enabled = true;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 32000;

            C2Settlement3InuMdV2CreateCheckpointsOverlayV58LikeOriginal(root, hotkey);
            if (hotkey != null)
            {
                hotkey.OverlayRoot = overlay;
                hotkey.CheckOverlayRoot = s_C2BuildingCheckpointsOverlayV58;
                hotkey.ApplyModeLikeOriginal();
            }
        }

        private void C2Settlement3InuMdV2CreateCheckpointsOverlayV58LikeOriginal(Transform root, C2BuildingPassabilityOverlayHotkeyLikeOriginal hotkey)
        {
            if (root == null) return;
            if (s_C2BuildingMotionFieldV1 == null || s_C2BuildingMotionFieldV1.CheckPointCells.Count == 0) return;

            var overlay = new GameObject("C2_BuildingPassability_CHECKPOINTS_Q_Overlay_V58");
            overlay.transform.SetParent(root, true);
            overlay.SetActive(false);
            s_C2BuildingCheckpointsOverlayV58 = overlay;
            if (hotkey != null) hotkey.CheckOverlayRoot = overlay;

            int overlayQuadCount = s_C2BuildingMotionFieldV1.CheckPointCells.Count;
            var verts = new List<Vector3>((overlayQuadCount + 2048) * 4);
            var colors = new List<Color32>((overlayQuadCount + 2048) * 4);
            var tris = new List<int>((overlayQuadCount + 2048) * 6);
            Color32 checkOverlayColor = new Color32(255, 220, 0, 92);
            Color32 checkBorderColor = new Color32(255, 246, 0, 235);

            Dictionary<int, RectInt> recordBounds = new Dictionary<int, RectInt>();

            for (int i = 0; i < s_C2BuildingMotionFieldV1.CheckPointCells.Count; i++)
            {
                C2BuildingCheckPointLikeOriginal cp = s_C2BuildingMotionFieldV1.CheckPointCells[i];
                if (cp == null) continue;

                C2Settlement3InuMdV2AddOverlayCellQuadV58LikeOriginal(verts, colors, tris, cp.X, cp.Y, checkOverlayColor, C2BuildingPassabilityZonesV1YOffset + 0.11f);

                RectInt b;
                if (!recordBounds.TryGetValue(cp.RecordIndex, out b))
                {
                    b = new RectInt(cp.X, cp.Y, 1, 1);
                }
                else
                {
                    int minX = Mathf.Min(b.xMin, cp.X);
                    int minY = Mathf.Min(b.yMin, cp.Y);
                    int maxX = Mathf.Max(b.xMax - 1, cp.X);
                    int maxY = Mathf.Max(b.yMax - 1, cp.Y);
                    b = new RectInt(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
                }
                recordBounds[cp.RecordIndex] = b;
            }

            foreach (KeyValuePair<int, RectInt> kv in recordBounds)
            {
                RectInt b = kv.Value;
                C2Settlement3InuMdV2AddCheckRectBorderV60LikeOriginal(verts, colors, tris, b.xMin, b.yMin, b.xMax - 1, b.yMax - 1, checkBorderColor);
            }

            var mf = overlay.AddComponent<MeshFilter>();
            var mr = overlay.AddComponent<MeshRenderer>();
            var mesh = new Mesh();
            mesh.name = "C2_BuildingPassability_CHECKPOINTS_V58_Mesh";
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            Material mat = C2Settlement3InuMdV2CreatePassabilityOverlayMaterialLikeOriginal();
            mat.name = "C2_BuildingPassability_CHECKPOINTS_Yellow_Overlay_V58";
            mr.sharedMaterial = mat;
            mr.enabled = true;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = 32001;
        }

        private void C2Settlement3InuMdV2AddCheckRectBorderV60LikeOriginal(
            List<Vector3> verts,
            List<Color32> colors,
            List<int> tris,
            int minX,
            int minY,
            int maxX,
            int maxY,
            Color32 color)
        {
            int bx0 = minX - 1;
            int by0 = minY - 1;
            int bx1 = maxX + 1;
            int by1 = maxY + 1;

            for (int x = bx0; x <= bx1; x++)
            {
                C2Settlement3InuMdV2AddOverlayCellQuadV58LikeOriginal(verts, colors, tris, x, by0, color, C2BuildingPassabilityZonesV1YOffset + 0.18f);
                C2Settlement3InuMdV2AddOverlayCellQuadV58LikeOriginal(verts, colors, tris, x, by1, color, C2BuildingPassabilityZonesV1YOffset + 0.18f);
            }

            for (int y = by0 + 1; y <= by1 - 1; y++)
            {
                C2Settlement3InuMdV2AddOverlayCellQuadV58LikeOriginal(verts, colors, tris, bx0, y, color, C2BuildingPassabilityZonesV1YOffset + 0.18f);
                C2Settlement3InuMdV2AddOverlayCellQuadV58LikeOriginal(verts, colors, tris, bx1, y, color, C2BuildingPassabilityZonesV1YOffset + 0.18f);
            }
        }

        private void C2Settlement3InuMdV2AddOverlayCellQuadV58LikeOriginal(
            List<Vector3> verts,
            List<Color32> colors,
            List<int> tris,
            int gx,
            int gy,
            Color32 overlayColor,
            float yOffset)
        {
            if (verts == null || colors == null || tris == null) return;

            Vector3 a = WallOriginalXYToWorldV1LikeOriginal(gx * 16.0f, gy * 16.0f, 0.0f);
            Vector3 b = WallOriginalXYToWorldV1LikeOriginal((gx + 1) * 16.0f, gy * 16.0f, 0.0f);
            Vector3 c = WallOriginalXYToWorldV1LikeOriginal((gx + 1) * 16.0f, (gy + 1) * 16.0f, 0.0f);
            Vector3 d = WallOriginalXYToWorldV1LikeOriginal(gx * 16.0f, (gy + 1) * 16.0f, 0.0f);

            a.y += yOffset;
            b.y += yOffset;
            c.y += yOffset;
            d.y += yOffset;

            int baseIndex = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            verts.Add(d);
            colors.Add(overlayColor);
            colors.Add(overlayColor);
            colors.Add(overlayColor);
            colors.Add(overlayColor);
            tris.Add(baseIndex + 0);
            tris.Add(baseIndex + 1);
            tris.Add(baseIndex + 2);
            tris.Add(baseIndex + 0);
            tris.Add(baseIndex + 2);
            tris.Add(baseIndex + 3);
        }

        private void C2Settlement3InuMdV2AddServicePointOverlayQuadLikeOriginal(
            List<Vector3> verts,
            List<Color32> colors,
            List<int> tris,
            float realX,
            float realY,
            float halfSizeOriginalPixels,
            Color32 color,
            float yOffset)
        {
            if (verts == null || colors == null || tris == null) return;

            float ox = realX / 16.0f;
            float oy = realY / 16.0f;

            Vector3 a = WallOriginalXYToWorldV1LikeOriginal(ox - halfSizeOriginalPixels, oy - halfSizeOriginalPixels, 0.0f);
            Vector3 b = WallOriginalXYToWorldV1LikeOriginal(ox + halfSizeOriginalPixels, oy - halfSizeOriginalPixels, 0.0f);
            Vector3 c = WallOriginalXYToWorldV1LikeOriginal(ox + halfSizeOriginalPixels, oy + halfSizeOriginalPixels, 0.0f);
            Vector3 d = WallOriginalXYToWorldV1LikeOriginal(ox - halfSizeOriginalPixels, oy + halfSizeOriginalPixels, 0.0f);

            a.y += yOffset;
            b.y += yOffset;
            c.y += yOffset;
            d.y += yOffset;

            int baseIndex = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            verts.Add(d);

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            tris.Add(baseIndex + 0);
            tris.Add(baseIndex + 1);
            tris.Add(baseIndex + 2);
            tris.Add(baseIndex + 0);
            tris.Add(baseIndex + 2);
            tris.Add(baseIndex + 3);
        }

        private static Material C2Settlement3InuMdV2CreatePassabilityOverlayMaterialLikeOriginal()
        {
            // Last known visible overlay path.
            // Sprites/Default was visible in V3; Hidden/Internal-Colored became invisible in the current project.
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Transparent");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Standard");

            var mat = new Material(sh);
            mat.name = "C2_BuildingPassability_LOCKPOINTS_Red_Overlay_V7_VISIBLE_SpritesDefault";
            Color c = new Color(1.0f, 0.0f, 0.0f, 0.58f);

            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", c);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.Always);

            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = C2BuildingPassabilityZonesV1RenderQueue;
            return mat;
        }

        public static bool C2BuildingMotionFieldV1IsBlockedLikeOriginal(int originalMotionCellX, int originalMotionCellY)
        {
            if (!C2BuildingPassabilityZonesV1Enabled) return false;
            return s_C2BuildingMotionFieldV1.Blocked.ContainsKey(new Vector2Int(originalMotionCellX, originalMotionCellY));
        }

        public static int C2BuildingMotionFieldV1RealToCellLikeOriginal(float real)
        {
            // Original building corner/LOCKPOINT grid is in motion cells:
            // cell = Real / 256, because Real = originalPixel * 16 and one motion cell = 16 original pixels.
            return Mathf.FloorToInt(real / 256.0f);
        }

        public static bool C2BuildingMotionFieldV1IsBlockedRealLikeOriginal(float realX, float realY)
        {
            int cx = C2BuildingMotionFieldV1RealToCellLikeOriginal(realX);
            int cy = C2BuildingMotionFieldV1RealToCellLikeOriginal(realY);
            return C2BuildingMotionFieldV1IsBlockedLikeOriginal(cx, cy);
        }

        public static bool C2BuildingMotionFieldV1TryGetBornPointCountLikeOriginal(
            int recordIndex,
            out int bornPointCount)
        {
            bornPointCount = 0;
            if (!C2BuildingPassabilityZonesV1Enabled) return false;
            if (s_C2BuildingMotionFieldV1.BornPointCells.Count == 0) return false;

            for (int i = 0; i < s_C2BuildingMotionFieldV1.BornPointCells.Count; i++)
            {
                C2BuildingServicePointLikeOriginal p = s_C2BuildingMotionFieldV1.BornPointCells[i];
                if (recordIndex >= 0 && p.RecordIndex != recordIndex) continue;
                bornPointCount++;
            }

            return bornPointCount > 0;
        }

        public static bool C2BuildingMotionFieldV1TryGetBornPointRealLikeOriginal(
            int recordIndex,
            int bornPointIndex,
            out float bornPointRealX,
            out float bornPointRealY)
        {
            bornPointRealX = 0.0f;
            bornPointRealY = 0.0f;
            if (!C2BuildingPassabilityZonesV1Enabled) return false;
            if (bornPointIndex < 0) return false;

            int localIndex = 0;
            for (int i = 0; i < s_C2BuildingMotionFieldV1.BornPointCells.Count; i++)
            {
                C2BuildingServicePointLikeOriginal p = s_C2BuildingMotionFieldV1.BornPointCells[i];
                if (recordIndex >= 0 && p.RecordIndex != recordIndex) continue;

                if (localIndex == bornPointIndex)
                {
                    bornPointRealX = p.RealX;
                    bornPointRealY = p.RealY;
                    return true;
                }

                localIndex++;
            }

            return false;
        }

        public static bool C2BuildingMotionFieldV1TryGetFirstBornPointRealLikeOriginal(
            int recordIndex,
            out float bornPointRealX,
            out float bornPointRealY)
        {
            return C2BuildingMotionFieldV1TryGetBornPointRealLikeOriginal(recordIndex, 0, out bornPointRealX, out bornPointRealY);
        }

        public static bool C2BuildingMotionFieldV1TryGetBornExitPathRealLikeOriginal(
            int recordIndex,
            out Vector2[] bornPath)
        {
            bornPath = null;
            if (!C2BuildingPassabilityZonesV1Enabled) return false;
            if (s_C2BuildingMotionFieldV1.BornPointCells.Count == 0) return false;

            var points = new List<C2BuildingServicePointLikeOriginal>();
            for (int i = 0; i < s_C2BuildingMotionFieldV1.BornPointCells.Count; i++)
            {
                C2BuildingServicePointLikeOriginal p = s_C2BuildingMotionFieldV1.BornPointCells[i];
                if (recordIndex >= 0 && p.RecordIndex != recordIndex) continue;
                points.Add(p);
            }

            if (points.Count == 0) return false;
            points.Sort((a, b) => a.OriginalIndex.CompareTo(b.OriginalIndex));

            var path = new Vector2[points.Count];
            for (int i = 0; i < points.Count; i++)
                path[i] = new Vector2(points[i].RealX, points[i].RealY);

            bornPath = path;
            return true;
        }

        public static bool C2BuildingMotionFieldV1TryFindNearestConcentratorCellLikeOriginal(
            int recordIndex,
            int fromCellX,
            int fromCellY,
            out int concentratorCellX,
            out int concentratorCellY,
            bool requireUnlockedPoint)
        {
            concentratorCellX = fromCellX;
            concentratorCellY = fromCellY;
            if (!C2BuildingPassabilityZonesV1Enabled) return false;
            if (s_C2BuildingMotionFieldV1.ConcentratorPointCells.Count == 0) return false;

            int bestIndex = -1;
            int bestScore = 1000000;

            for (int i = 0; i < s_C2BuildingMotionFieldV1.ConcentratorPointCells.Count; i++)
            {
                C2BuildingServicePointLikeOriginal p = s_C2BuildingMotionFieldV1.ConcentratorPointCells[i];
                if (recordIndex >= 0 && p.RecordIndex != recordIndex) continue;
                if (requireUnlockedPoint && !C2BuildingMotionFieldV1IsServicePointUnlockedLikeOriginal(p.X, p.Y)) continue;

                // Original FindPoint with FP_CONCENTRATION uses Shift=4, so ConcPt becomes corner + (ConcPt >> 4).
                int score = C2BuildingMotionFieldV1NormaLikeOriginal(p.X - fromCellX, p.Y - fromCellY);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0) return false;

            C2BuildingServicePointLikeOriginal best = s_C2BuildingMotionFieldV1.ConcentratorPointCells[bestIndex];
            concentratorCellX = best.X;
            concentratorCellY = best.Y;
            return true;
        }

        public static bool C2BuildingMotionFieldV1TryFindNearestConcentratorCellLikeOriginal(
            int fromCellX,
            int fromCellY,
            out int concentratorCellX,
            out int concentratorCellY)
        {
            return C2BuildingMotionFieldV1TryFindNearestConcentratorCellLikeOriginal(
                -1,
                fromCellX,
                fromCellY,
                out concentratorCellX,
                out concentratorCellY,
                true);
        }

        public static bool C2BuildingMotionFieldV1TryFindNearestConcentratorRealLikeOriginal(
            int recordIndex,
            float fromRealX,
            float fromRealY,
            out float concentratorRealX,
            out float concentratorRealY,
            bool requireUnlockedPoint)
        {
            concentratorRealX = fromRealX;
            concentratorRealY = fromRealY;
            if (!C2BuildingPassabilityZonesV1Enabled) return false;
            if (s_C2BuildingMotionFieldV1.ConcentratorPointCells.Count == 0) return false;

            int fromCellX = C2BuildingMotionFieldV1RealToCellLikeOriginal(fromRealX);
            int fromCellY = C2BuildingMotionFieldV1RealToCellLikeOriginal(fromRealY);

            int bestIndex = -1;
            int bestScore = 1000000;

            for (int i = 0; i < s_C2BuildingMotionFieldV1.ConcentratorPointCells.Count; i++)
            {
                C2BuildingServicePointLikeOriginal p = s_C2BuildingMotionFieldV1.ConcentratorPointCells[i];
                if (recordIndex >= 0 && p.RecordIndex != recordIndex) continue;
                if (requireUnlockedPoint && !C2BuildingMotionFieldV1IsServicePointUnlockedLikeOriginal(p.X, p.Y)) continue;

                int score = C2BuildingMotionFieldV1NormaLikeOriginal(p.X - fromCellX, p.Y - fromCellY);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0) return false;

            C2BuildingServicePointLikeOriginal best = s_C2BuildingMotionFieldV1.ConcentratorPointCells[bestIndex];
            concentratorRealX = best.RealX;
            concentratorRealY = best.RealY;
            return true;
        }

        public static bool C2BuildingMotionFieldV1TryFindNearestConcentratorRealLikeOriginal(
            float fromRealX,
            float fromRealY,
            out float concentratorRealX,
            out float concentratorRealY)
        {
            return C2BuildingMotionFieldV1TryFindNearestConcentratorRealLikeOriginal(
                -1,
                fromRealX,
                fromRealY,
                out concentratorRealX,
                out concentratorRealY,
                true);
        }

        public static bool C2BuildingMotionFieldV1TryBuildPathToNearestConcentratorRealLikeOriginal(
            int recordIndex,
            float startRealX,
            float startRealY,
            out float concentratorRealX,
            out float concentratorRealY,
            out Vector2[] waypoints,
            int maxSearchCells)
        {
            waypoints = null;
            concentratorRealX = startRealX;
            concentratorRealY = startRealY;

            if (!C2BuildingMotionFieldV1TryFindNearestConcentratorRealLikeOriginal(
                recordIndex,
                startRealX,
                startRealY,
                out concentratorRealX,
                out concentratorRealY,
                true))
                return false;

            Vector2[] path;
            if (C2BuildingMotionFieldV1TryBuildPathRealLikeOriginal(
                startRealX,
                startRealY,
                concentratorRealX,
                concentratorRealY,
                out path,
                maxSearchCells))
            {
                waypoints = path;
                return true;
            }

            waypoints = new Vector2[] { new Vector2(concentratorRealX, concentratorRealY) };
            return true;
        }

        public static bool C2BuildingMotionFieldV1TryBuildPathToNearestConcentratorRealLikeOriginal(
            float startRealX,
            float startRealY,
            out float concentratorRealX,
            out float concentratorRealY,
            out Vector2[] waypoints)
        {
            return C2BuildingMotionFieldV1TryBuildPathToNearestConcentratorRealLikeOriginal(
                -1,
                startRealX,
                startRealY,
                out concentratorRealX,
                out concentratorRealY,
                out waypoints,
                12000);
        }

        public static bool C2BuildingMotionFieldV1TryFindNearestBuildPointCellLikeOriginal(
            int recordIndex,
            int fromCellX,
            int fromCellY,
            out int buildPointCellX,
            out int buildPointCellY,
            bool requireUnlockedPoint)
        {
            buildPointCellX = fromCellX;
            buildPointCellY = fromCellY;
            if (!C2BuildingPassabilityZonesV1Enabled) return false;
            if (s_C2BuildingMotionFieldV1.BuildPointCells.Count == 0) return false;

            int bestIndex = -1;
            int bestScore = 10000;

            for (int i = 0; i < s_C2BuildingMotionFieldV1.BuildPointCells.Count; i++)
            {
                C2BuildingBuildPointLikeOriginal p = s_C2BuildingMotionFieldV1.BuildPointCells[i];
                if (recordIndex >= 0 && p.RecordIndex != recordIndex) continue;
                if (requireUnlockedPoint && !C2BuildingMotionFieldV1IsBuildPointUnlockedLikeOriginal(p.X, p.Y)) continue;

                // Original OneObject::FindPoint uses Norma(x2-xx, y2-yy), not squared distance.
                int score = C2BuildingMotionFieldV1NormaLikeOriginal(p.X - fromCellX, p.Y - fromCellY);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0) return false;

            C2BuildingBuildPointLikeOriginal best = s_C2BuildingMotionFieldV1.BuildPointCells[bestIndex];
            buildPointCellX = best.X;
            buildPointCellY = best.Y;
            return true;
        }

        public static bool C2BuildingMotionFieldV1TryFindNearestBuildPointCellLikeOriginal(
            int fromCellX,
            int fromCellY,
            out int buildPointCellX,
            out int buildPointCellY)
        {
            return C2BuildingMotionFieldV1TryFindNearestBuildPointCellLikeOriginal(
                -1,
                fromCellX,
                fromCellY,
                out buildPointCellX,
                out buildPointCellY,
                true);
        }

        public static bool C2BuildingMotionFieldV1TryFindNearestBuildPointRealLikeOriginal(
            int recordIndex,
            float fromRealX,
            float fromRealY,
            out float buildPointRealX,
            out float buildPointRealY,
            bool requireUnlockedPoint)
        {
            buildPointRealX = fromRealX;
            buildPointRealY = fromRealY;
            if (!C2BuildingPassabilityZonesV1Enabled) return false;

            int fromCellX = C2BuildingMotionFieldV1RealToCellLikeOriginal(fromRealX);
            int fromCellY = C2BuildingMotionFieldV1RealToCellLikeOriginal(fromRealY);
            int pointCellX;
            int pointCellY;
            if (!C2BuildingMotionFieldV1TryFindNearestBuildPointCellLikeOriginal(
                recordIndex,
                fromCellX,
                fromCellY,
                out pointCellX,
                out pointCellY,
                requireUnlockedPoint))
                return false;

            // Convert original motion cell back to Real center. One cell = 16 original pixels = 256 Real units.
            buildPointRealX = pointCellX * 256.0f + 128.0f;
            buildPointRealY = pointCellY * 256.0f + 128.0f;
            return true;
        }

        public static bool C2BuildingMotionFieldV1TryFindNearestBuildPointRealLikeOriginal(
            float fromRealX,
            float fromRealY,
            out float buildPointRealX,
            out float buildPointRealY)
        {
            return C2BuildingMotionFieldV1TryFindNearestBuildPointRealLikeOriginal(
                -1,
                fromRealX,
                fromRealY,
                out buildPointRealX,
                out buildPointRealY,
                true);
        }

        public static bool C2BuildingMotionFieldV1TryBuildPathToNearestBuildPointRealLikeOriginal(
            int recordIndex,
            float startRealX,
            float startRealY,
            out float buildPointRealX,
            out float buildPointRealY,
            out Vector2[] waypoints,
            int maxSearchCells)
        {
            waypoints = null;
            buildPointRealX = startRealX;
            buildPointRealY = startRealY;

            if (!C2BuildingMotionFieldV1TryFindNearestBuildPointRealLikeOriginal(
                recordIndex,
                startRealX,
                startRealY,
                out buildPointRealX,
                out buildPointRealY,
                true))
                return false;

            Vector2[] path;
            if (C2BuildingMotionFieldV1TryBuildPathRealLikeOriginal(
                startRealX,
                startRealY,
                buildPointRealX,
                buildPointRealY,
                out path,
                maxSearchCells))
            {
                waypoints = path;
                return true;
            }

            // Existing path builder returns false when direct travel is already possible.
            // Keep this API useful for the future builder order by returning the target as a single waypoint.
            waypoints = new Vector2[] { new Vector2(buildPointRealX, buildPointRealY) };
            return true;
        }

        public static bool C2BuildingMotionFieldV1TryBuildPathToNearestBuildPointRealLikeOriginal(
            float startRealX,
            float startRealY,
            out float buildPointRealX,
            out float buildPointRealY,
            out Vector2[] waypoints)
        {
            return C2BuildingMotionFieldV1TryBuildPathToNearestBuildPointRealLikeOriginal(
                -1,
                startRealX,
                startRealY,
                out buildPointRealX,
                out buildPointRealY,
                out waypoints,
                12000);
        }

        private static int C2BuildingMotionFieldV1NormaLikeOriginal(int dx, int dy)
        {
            int ax = Mathf.Abs(dx);
            int ay = Mathf.Abs(dy);
            int mx = Mathf.Max(ax, ay);
            return (mx + ax + ay) >> 1;
        }

        private static bool C2BuildingMotionFieldV1IsBuildPointUnlockedLikeOriginal(int cellX, int cellY)
        {
            return C2BuildingMotionFieldV1IsServicePointUnlockedLikeOriginal(cellX, cellY);
        }

        private static bool C2BuildingMotionFieldV1IsServicePointUnlockedLikeOriginal(int cellX, int cellY)
        {
            // Original FindPoint(... FP_UNLOCKED_POINT) uses CheckBar(x2-1, y2-1, 3, 3).
            // This is the data-only equivalent against the current active building MotionField.
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (C2BuildingMotionFieldV1IsBlockedLikeOriginal(cellX + dx, cellY + dy))
                        return false;
                }
            }

            return true;
        }

        public static bool C2BuildingMotionFieldV1TryFindNearestFreeRealLikeOriginal(
            float wantedRealX,
            float wantedRealY,
            out float freeRealX,
            out float freeRealY,
            int maxRadiusCells)
        {
            freeRealX = wantedRealX;
            freeRealY = wantedRealY;
            if (!C2BuildingPassabilityZonesV1Enabled) return false;

            int cx = C2BuildingMotionFieldV1RealToCellLikeOriginal(wantedRealX);
            int cy = C2BuildingMotionFieldV1RealToCellLikeOriginal(wantedRealY);
            if (!C2BuildingMotionFieldV1IsBlockedLikeOriginal(cx, cy))
                return true;

            int maxR = Mathf.Clamp(maxRadiusCells, 1, 64);
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
                        if (C2BuildingMotionFieldV1IsBlockedLikeOriginal(tx, ty)) continue;

                        // Prefer the closest free cell center in the same original coordinate space.
                        float rx = tx * 256.0f + 128.0f;
                        float ry = ty * 256.0f + 128.0f;
                        float sx = rx - wantedRealX;
                        float sy = ry - wantedRealY;
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
            freeRealX = bestX * 256.0f + 128.0f;
            freeRealY = bestY * 256.0f + 128.0f;
            return true;
        }

        public static bool C2BuildingMotionFieldV1CanTravelStraightRealLikeOriginal(
            float fromRealX,
            float fromRealY,
            float toRealX,
            float toRealY)
        {
            if (!C2BuildingPassabilityZonesV1Enabled) return true;
            if (s_C2BuildingMotionFieldV1.Blocked.Count == 0) return true;

            float dx = toRealX - fromRealX;
            float dy = toRealY - fromRealY;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len <= 0.001f)
                return !C2BuildingMotionFieldV1IsBlockedRealLikeOriginal(toRealX, toRealY);

            // One motion cell is 256 Real units. Sample at half-cell step to avoid tunneling.
            int steps = Mathf.Clamp(Mathf.CeilToInt(len / 128.0f), 1, 4096);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float rx = fromRealX + dx * t;
                float ry = fromRealY + dy * t;
                if (C2BuildingMotionFieldV1IsBlockedRealLikeOriginal(rx, ry))
                    return false;
            }

            return true;
        }

        private struct C2BuildingMotionPathNodeV1LikeOriginal
        {
            public Vector2Int Cell;
            public float G;
            public float F;

            public C2BuildingMotionPathNodeV1LikeOriginal(Vector2Int cell, float g, float f)
            {
                Cell = cell;
                G = g;
                F = f;
            }
        }

        public static bool C2BuildingMotionFieldV1TryBuildPathRealLikeOriginal(
            float startRealX,
            float startRealY,
            float wantedRealX,
            float wantedRealY,
            out Vector2[] waypoints,
            int maxSearchCells)
        {
            waypoints = null;
            if (!C2BuildingPassabilityZonesV1Enabled) return false;
            if (s_C2BuildingMotionFieldV1.Blocked.Count == 0) return false;

            float dstX = wantedRealX;
            float dstY = wantedRealY;
            C2BuildingMotionFieldV1TryFindNearestFreeRealLikeOriginal(dstX, dstY, out dstX, out dstY, 12);

            if (C2BuildingMotionFieldV1CanTravelStraightRealLikeOriginal(startRealX, startRealY, dstX, dstY))
                return false;

            int sx = C2BuildingMotionFieldV1RealToCellLikeOriginal(startRealX);
            int sy = C2BuildingMotionFieldV1RealToCellLikeOriginal(startRealY);
            int tx = C2BuildingMotionFieldV1RealToCellLikeOriginal(dstX);
            int ty = C2BuildingMotionFieldV1RealToCellLikeOriginal(dstY);

            Vector2Int start = new Vector2Int(sx, sy);
            Vector2Int target = new Vector2Int(tx, ty);

            if (C2BuildingMotionFieldV1IsBlockedLikeOriginal(start.x, start.y))
            {
                float freeStartX;
                float freeStartY;
                if (C2BuildingMotionFieldV1TryFindNearestFreeRealLikeOriginal(startRealX, startRealY, out freeStartX, out freeStartY, 8))
                    start = new Vector2Int(C2BuildingMotionFieldV1RealToCellLikeOriginal(freeStartX), C2BuildingMotionFieldV1RealToCellLikeOriginal(freeStartY));
            }

            if (C2BuildingMotionFieldV1IsBlockedLikeOriginal(target.x, target.y))
            {
                float freeTargetX;
                float freeTargetY;
                if (!C2BuildingMotionFieldV1TryFindNearestFreeRealLikeOriginal(dstX, dstY, out freeTargetX, out freeTargetY, 16))
                    return false;
                dstX = freeTargetX;
                dstY = freeTargetY;
                target = new Vector2Int(C2BuildingMotionFieldV1RealToCellLikeOriginal(dstX), C2BuildingMotionFieldV1RealToCellLikeOriginal(dstY));
            }

            int directDx = Mathf.Abs(target.x - start.x);
            int directDy = Mathf.Abs(target.y - start.y);
            int pad = Mathf.Clamp(Mathf.Max(24, Mathf.Max(directDx, directDy) / 2 + 16), 24, 96);
            int minX = Mathf.Min(start.x, target.x) - pad;
            int maxX = Mathf.Max(start.x, target.x) + pad;
            int minY = Mathf.Min(start.y, target.y) - pad;
            int maxY = Mathf.Max(start.y, target.y) + pad;

            int maxNodes = Mathf.Clamp(maxSearchCells, 512, 50000);
            var open = new List<C2BuildingMotionPathNodeV1LikeOriginal>(256);
            var closed = new HashSet<Vector2Int>();
            var came = new Dictionary<Vector2Int, Vector2Int>();
            var bestG = new Dictionary<Vector2Int, float>();

            float h0 = C2BuildingMotionFieldV1PathHeuristicLikeOriginal(start, target);
            open.Add(new C2BuildingMotionPathNodeV1LikeOriginal(start, 0.0f, h0));
            bestG[start] = 0.0f;

            bool found = false;
            int expanded = 0;

            while (open.Count > 0 && expanded < maxNodes)
            {
                int bestIndex = 0;
                float bestF = open[0].F;
                for (int i = 1; i < open.Count; i++)
                {
                    if (open[i].F < bestF)
                    {
                        bestF = open[i].F;
                        bestIndex = i;
                    }
                }

                C2BuildingMotionPathNodeV1LikeOriginal curNode = open[bestIndex];
                open.RemoveAt(bestIndex);

                Vector2Int cur = curNode.Cell;
                if (closed.Contains(cur)) continue;
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
                        if (C2BuildingMotionFieldV1IsBlockedLikeOriginal(nb.x, nb.y)) continue;

                        // Do not cut diagonally through the corner of a blocked building cell.
                        if (nx != 0 && ny != 0)
                        {
                            if (C2BuildingMotionFieldV1IsBlockedLikeOriginal(cur.x + nx, cur.y)) continue;
                            if (C2BuildingMotionFieldV1IsBlockedLikeOriginal(cur.x, cur.y + ny)) continue;
                        }

                        float step = (nx != 0 && ny != 0) ? 1.41421356f : 1.0f;
                        float ng = curNode.G + step;

                        float oldG;
                        if (bestG.TryGetValue(nb, out oldG) && ng >= oldG)
                            continue;

                        bestG[nb] = ng;
                        came[nb] = cur;
                        float nf = ng + C2BuildingMotionFieldV1PathHeuristicLikeOriginal(nb, target);
                        open.Add(new C2BuildingMotionPathNodeV1LikeOriginal(nb, ng, nf));
                    }
                }
            }

            if (!found) return false;

            var cells = new List<Vector2Int>();
            Vector2Int p = target;
            cells.Add(p);
            int guard = 0;
            while (p != start && guard++ < maxNodes)
            {
                Vector2Int prev;
                if (!came.TryGetValue(p, out prev))
                    break;
                p = prev;
                cells.Add(p);
            }
            cells.Reverse();

            if (cells.Count <= 1) return false;

            var simplified = C2BuildingMotionFieldV1SimplifyPathCellsLikeOriginal(cells);
            var result = new List<Vector2>(simplified.Count + 1);

            // Skip the first cell because the unit is already there.
            for (int i = 1; i < simplified.Count; i++)
            {
                Vector2Int c = simplified[i];
                float rx = c.x * 256.0f + 128.0f;
                float ry = c.y * 256.0f + 128.0f;
                result.Add(new Vector2(rx, ry));
            }

            // Preserve the exact free destination as final point when possible.
            Vector2Int lastCell = simplified[simplified.Count - 1];
            if (lastCell.x == target.x && lastCell.y == target.y)
            {
                if (result.Count == 0 || Vector2.Distance(result[result.Count - 1], new Vector2(dstX, dstY)) > 8.0f)
                    result.Add(new Vector2(dstX, dstY));
            }

            if (result.Count == 0) return false;
            waypoints = result.ToArray();
            return true;
        }

        private static float C2BuildingMotionFieldV1PathHeuristicLikeOriginal(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            int mn = Mathf.Min(dx, dy);
            int mx = Mathf.Max(dx, dy);
            return (mx - mn) + 1.41421356f * mn;
        }

        private static List<Vector2Int> C2BuildingMotionFieldV1SimplifyPathCellsLikeOriginal(List<Vector2Int> cells)
        {
            if (cells == null || cells.Count <= 2) return cells ?? new List<Vector2Int>();

            var result = new List<Vector2Int>();
            result.Add(cells[0]);

            int lastDx = 0;
            int lastDy = 0;
            for (int i = 1; i < cells.Count; i++)
            {
                int dx = Math.Sign(cells[i].x - cells[i - 1].x);
                int dy = Math.Sign(cells[i].y - cells[i - 1].y);

                if (i == 1)
                {
                    lastDx = dx;
                    lastDy = dy;
                    continue;
                }

                if (dx != lastDx || dy != lastDy)
                {
                    result.Add(cells[i - 1]);
                    lastDx = dx;
                    lastDy = dy;
                }
            }

            result.Add(cells[cells.Count - 1]);
            return result;
        }

        public static string C2BuildingMotionFieldV1AuditLikeOriginal()
        {
            return s_C2BuildingMotionFieldAuditV1 ?? "";
        }
    }
}
