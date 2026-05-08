// C2BuildingPassabilityZonesLikeOriginal.cs
// V3: parses original MD building zones and builds a data-only MotionField plus Q red overlay.
// V2 fixed hotkey for Unity New Input System.
// V7 rollback: use the last visibly working Sprites/Default overlay material.
// V8 adds public MotionField queries used by peasants: target redirect + step blocking.
// V9 adds lightweight A* over original LOCKPOINT motion cells so units can route around buildings.

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
        public GameObject OverlayRoot;
        public KeyCode ToggleKey = KeyCode.Q;

        private void Update()
        {
            if (OverlayRoot == null) return;
            if (C2WasTogglePressedLikeOriginal())
            {
                OverlayRoot.SetActive(!OverlayRoot.activeSelf);
            }
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
        }

        private sealed class C2BuildingMotionFieldLikeOriginal
        {
            public readonly Dictionary<Vector2Int, C2BuildingMotionCellLikeOriginal> Blocked = new Dictionary<Vector2Int, C2BuildingMotionCellLikeOriginal>();
            public int BuildingRecords;
            public int MdWithZones;
            public int LockCellsAdded;
            public int DuplicateCells;
            public int CheckPoints;
            public int BuildLockPoints;
            public int BuildPoints;
            public int BornPoints;
            public int ConcentratorPoints;

            public void Clear()
            {
                Blocked.Clear();
                BuildingRecords = 0;
                MdWithZones = 0;
                LockCellsAdded = 0;
                DuplicateCells = 0;
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
                return C2Settlement3InuMdV2ReadZoneBlockLikeOriginal(tokens, lines, ref lineIndex, info.Zones.ConcentratorPoints, C2Settlement3InuMdV2ZoneKindLikeOriginal.Concentrator, false, true, true);

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
        }

        private void C2Settlement3InuMdV2RegisterBuildingZonesLikeOriginal(
            C2Settlement3InuMdV2Record r,
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Kind kind)
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

            for (int i = 0; i < md.Zones.LockPoints.Count; i++)
            {
                int gx = cornerX + md.Zones.LockPoints[i].X;
                int gy = cornerY + md.Zones.LockPoints[i].Y;
                var key = new Vector2Int(gx, gy);
                if (s_C2BuildingMotionFieldV1.Blocked.ContainsKey(key))
                {
                    s_C2BuildingMotionFieldV1.DuplicateCells++;
                    continue;
                }

                var cell = new C2BuildingMotionCellLikeOriginal();
                cell.X = gx;
                cell.Y = gy;
                cell.RecordIndex = r.Index;
                cell.MonsterId = r.MonsterId ?? "";
                cell.MdName = md.MdName ?? "";
                s_C2BuildingMotionFieldV1.Blocked.Add(key, cell);
                s_C2BuildingMotionFieldV1.LockCellsAdded++;
            }
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
                " mdWithZones=" + s_C2BuildingMotionFieldV1.MdWithZones.ToString(CultureInfo.InvariantCulture) +
                " lockCells=" + s_C2BuildingMotionFieldV1.Blocked.Count.ToString(CultureInfo.InvariantCulture) +
                " added=" + s_C2BuildingMotionFieldV1.LockCellsAdded.ToString(CultureInfo.InvariantCulture) +
                " duplicates=" + s_C2BuildingMotionFieldV1.DuplicateCells.ToString(CultureInfo.InvariantCulture) +
                " checkPts=" + s_C2BuildingMotionFieldV1.CheckPoints.ToString(CultureInfo.InvariantCulture) +
                " buildLockPts=" + s_C2BuildingMotionFieldV1.BuildLockPoints.ToString(CultureInfo.InvariantCulture) +
                " buildPts=" + s_C2BuildingMotionFieldV1.BuildPoints.ToString(CultureInfo.InvariantCulture) +
                " bornPts=" + s_C2BuildingMotionFieldV1.BornPoints.ToString(CultureInfo.InvariantCulture) +
                " concentratorPts=" + s_C2BuildingMotionFieldV1.ConcentratorPoints.ToString(CultureInfo.InvariantCulture);

            if (root != null && C2BuildingPassabilityZonesV1CreateOverlay)
                C2Settlement3InuMdV2CreatePassabilityOverlayLikeOriginal(root);
        }

        private void C2Settlement3InuMdV2CreatePassabilityOverlayLikeOriginal(Transform root)
        {
            if (root == null) return;

            var overlay = new GameObject("C2_BuildingPassability_LOCKPOINTS_Q_Overlay_V1");
            overlay.transform.SetParent(root, true);
            overlay.SetActive(false);
            s_C2BuildingPassabilityOverlayV1 = overlay;

            var hotkey = root.gameObject.GetComponent<C2BuildingPassabilityOverlayHotkeyLikeOriginal>();
            if (hotkey == null) hotkey = root.gameObject.AddComponent<C2BuildingPassabilityOverlayHotkeyLikeOriginal>();
            hotkey.OverlayRoot = overlay;
            hotkey.ToggleKey = KeyCode.Q;

            if (s_C2BuildingMotionFieldV1.Blocked.Count == 0)
                return;

            var verts = new List<Vector3>(s_C2BuildingMotionFieldV1.Blocked.Count * 4);
            var colors = new List<Color32>(s_C2BuildingMotionFieldV1.Blocked.Count * 4);
            var tris = new List<int>(s_C2BuildingMotionFieldV1.Blocked.Count * 6);
            Color32 overlayColor = new Color32(255, 0, 0, 148);

            foreach (var kv in s_C2BuildingMotionFieldV1.Blocked)
            {
                int gx = kv.Key.x;
                int gy = kv.Key.y;

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
