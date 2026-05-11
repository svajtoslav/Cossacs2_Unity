using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // V155: original-like building rally point / exit destination.
    // Original fields: OneObject::DstX/DstY.  Visual GP: Interf3\exitpoint, 18-frame loop.
    internal static class C2BuildingRallyPointRuntimeV155LikeOriginal
    {
        private const int ExitPointFramesLikeOriginal = 18;
        private const float ExitPointFrameMsLikeOriginal = 40.0f;
        private const float RallyMarkerYOffsetWorld = 0.18f;
        private const float OriginalPixelToWorldScaleForSpriteRenderer = 10.0f; // cache sprites use PPU=100, map uses ~0.1 world/original px
        private const float OccupiedRadiusReal = 288.0f;
        private static readonly Dictionary<string, int> _nextSlotByRallyKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public static void AttachOrUpdateMarker(C2SettlementBuildingSelectableV1LikeOriginal building, string source)
        {
            if (building == null) return;
            C2BuildingRallyPointMarkerV155LikeOriginal marker = building.GetComponent<C2BuildingRallyPointMarkerV155LikeOriginal>();
            if (marker == null)
                marker = building.gameObject.AddComponent<C2BuildingRallyPointMarkerV155LikeOriginal>();
            marker.Configure(building, source);
        }

        public static bool TryAppendRallyDestinationV155LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            Vector2[] exitPath,
            out Vector2[] resultPath,
            out string audit)
        {
            resultPath = exitPath;
            audit = "rally=none";
            if (building == null || exitPath == null || exitPath.Length == 0)
                return false;

            int rallyX;
            int rallyY;
            if (!building.TryGetRallyPointRealV155LikeOriginal(out rallyX, out rallyY))
                return false;

            Vector2 finalReal = AllocateFinalRallySlotV155LikeOriginal(building, rallyX, rallyY, out audit);

            if (exitPath.Length > 0)
            {
                Vector2 last = exitPath[exitPath.Length - 1];
                if ((last - finalReal).sqrMagnitude < 64.0f)
                {
                    resultPath = exitPath;
                    audit += " append=skip_already_last";
                    return true;
                }
            }

            resultPath = new Vector2[exitPath.Length + 1];
            for (int i = 0; i < exitPath.Length; i++)
                resultPath[i] = exitPath[i];
            resultPath[resultPath.Length - 1] = finalReal;
            audit += " append=1 pathBefore=" + exitPath.Length.ToString(CultureInfo.InvariantCulture) +
                     " pathAfter=" + resultPath.Length.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static Vector2 AllocateFinalRallySlotV155LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            int rallyX,
            int rallyY,
            out string audit)
        {
            string key = "b=" + (building != null ? building.RecordIndex.ToString(CultureInfo.InvariantCulture) : "0") +
                         "|dst=" + rallyX.ToString(CultureInfo.InvariantCulture) + "," + rallyY.ToString(CultureInfo.InvariantCulture);

            int next;
            if (!_nextSlotByRallyKey.TryGetValue(key, out next)) next = 0;

            int chosenSlot = 0;
            Vector2 chosen = new Vector2(rallyX, rallyY);
            bool free = false;
            int slotCount = RallySlotCountV155LikeOriginal();

            for (int attempt = 0; attempt < slotCount; attempt++)
            {
                int slot = (next + attempt) % slotCount;
                Vector2 off = RallySlotOffsetRealV155LikeOriginal(slot);
                Vector2 candidate = new Vector2(rallyX + off.x, rallyY + off.y);
                if (!IsRallyCandidateOccupiedV155LikeOriginal(candidate, OccupiedRadiusReal))
                {
                    chosen = candidate;
                    chosenSlot = slot;
                    free = true;
                    break;
                }
            }

            if (!free)
            {
                chosenSlot = next % slotCount;
                Vector2 off = RallySlotOffsetRealV155LikeOriginal(chosenSlot);
                chosen = new Vector2(rallyX + off.x, rallyY + off.y);
            }

            _nextSlotByRallyKey[key] = (chosenSlot + 1) % slotCount;
            audit = "rally=dstXDstY key='" + key + "' slot=" + chosenSlot.ToString(CultureInfo.InvariantCulture) +
                    " free=" + free +
                    " finalReal=(" + chosen.x.ToString("0", CultureInfo.InvariantCulture) + "," +
                    chosen.y.ToString("0", CultureInfo.InvariantCulture) + ")" +
                    " baseReal=(" + rallyX.ToString(CultureInfo.InvariantCulture) + "," + rallyY.ToString(CultureInfo.InvariantCulture) + ")";
            return chosen;
        }

        private static bool IsRallyCandidateOccupiedV155LikeOriginal(Vector2 candidateReal, float radiusReal)
        {
            float rr = radiusReal * radiusReal;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] units = UnityEngine.Object.FindObjectsOfType<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            for (int i = 0; units != null && i < units.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = units[i];
                if (u == null || !u.isActiveAndEnabled) continue;
                float dx = u.RealXFloat - candidateReal.x;
                float dy = u.RealYFloat - candidateReal.y;
                if (dx * dx + dy * dy <= rr)
                    return true;
            }
            return false;
        }

        private static int RallySlotCountV155LikeOriginal()
        {
            return 25;
        }

        private static Vector2 RallySlotOffsetRealV155LikeOriginal(int slot)
        {
            // Original Build.cpp adds rando()%2048 - 1024 around OB->DstX/DstY.
            // Use a deterministic compact ring so several produced units do not stack pixel-to-pixel.
            const float s = 384.0f;
            switch (slot)
            {
                case 0: return Vector2.zero;
                case 1: return new Vector2(s, 0);
                case 2: return new Vector2(-s, 0);
                case 3: return new Vector2(0, s);
                case 4: return new Vector2(0, -s);
                case 5: return new Vector2(s, s);
                case 6: return new Vector2(-s, s);
                case 7: return new Vector2(s, -s);
                case 8: return new Vector2(-s, -s);
                case 9: return new Vector2(2 * s, 0);
                case 10: return new Vector2(-2 * s, 0);
                case 11: return new Vector2(0, 2 * s);
                case 12: return new Vector2(0, -2 * s);
                case 13: return new Vector2(2 * s, s);
                case 14: return new Vector2(-2 * s, s);
                case 15: return new Vector2(2 * s, -s);
                case 16: return new Vector2(-2 * s, -s);
                case 17: return new Vector2(s, 2 * s);
                case 18: return new Vector2(-s, 2 * s);
                case 19: return new Vector2(s, -2 * s);
                case 20: return new Vector2(-s, -2 * s);
                case 21: return new Vector2(2 * s, 2 * s);
                case 22: return new Vector2(-2 * s, 2 * s);
                case 23: return new Vector2(2 * s, -2 * s);
                default: return new Vector2(-2 * s, -2 * s);
            }
        }

        internal static Sprite LoadExitPointSpriteV155LikeOriginal(int frame)
        {
            frame = ((frame % ExitPointFramesLikeOriginal) + ExitPointFramesLikeOriginal) % ExitPointFramesLikeOriginal;
            return C2GameplayOriginalSpriteCacheV1.LoadSprite("Interf3\\exitpoint", frame, "rally_exitpoint_v155");
        }

        internal static int CurrentExitPointFrameV155LikeOriginal()
        {
            return Mathf.FloorToInt((Time.realtimeSinceStartup * 1000.0f) / ExitPointFrameMsLikeOriginal) % ExitPointFramesLikeOriginal;
        }

        internal static float MarkerScaleV155LikeOriginal()
        {
            return OriginalPixelToWorldScaleForSpriteRenderer;
        }

        internal static float MarkerYOffsetV155LikeOriginal()
        {
            return RallyMarkerYOffsetWorld;
        }
    }

    internal sealed class C2BuildingRallyPointMarkerV155LikeOriginal : MonoBehaviour
    {
        private C2SettlementBuildingSelectableV1LikeOriginal _building;
        private GameObject _markerGo;
        private SpriteRenderer _renderer;
        private int _lastFrame = -1;
        private string _source = string.Empty;
        private bool _logged;

        public void Configure(C2SettlementBuildingSelectableV1LikeOriginal building, string source)
        {
            _building = building != null ? building : GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
            _source = source ?? string.Empty;
            EnsureMarker();
            UpdateMarker(true);
        }

        private void LateUpdate()
        {
            UpdateMarker(false);
        }

        private void EnsureMarker()
        {
            if (_markerGo != null && _renderer != null) return;

            _markerGo = new GameObject("C2_RallyExitPoint_Interf3_exitpoint_V155");
            _markerGo.transform.SetParent(transform, true);
            _renderer = _markerGo.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = 32000;
            _renderer.enabled = false;
            _markerGo.transform.localScale = Vector3.one * C2BuildingRallyPointRuntimeV155LikeOriginal.MarkerScaleV155LikeOriginal();
        }

        private void UpdateMarker(bool force)
        {
            if (_building == null) _building = GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
            EnsureMarker();
            if (_building == null || _markerGo == null || _renderer == null)
                return;

            int realX = 0;
            int realY = 0;
            bool visible = _building.IsSelected && _building.TryGetRallyPointRealV155LikeOriginal(out realX, out realY);
            _renderer.enabled = visible;
            if (!visible)
                return;

            C2BattleTerrainMode mode = _building.OwnerMode != null ? _building.OwnerMode : UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            if (mode != null)
            {
                Vector3 pos = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(realX / 16.0f, realY / 16.0f);
                pos.y += C2BuildingRallyPointRuntimeV155LikeOriginal.MarkerYOffsetV155LikeOriginal();
                _markerGo.transform.position = pos;
            }

            Camera cam = Camera.main;
            Camera[] cams = Camera.allCameras;
            for (int i = 0; cams != null && i < cams.Length; i++)
            {
                Camera c = cams[i];
                if (c == null || !c.isActiveAndEnabled) continue;
                string n = c.name ?? string.Empty;
                if (n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    cam = c;
                    break;
                }
            }
            if (cam != null)
                _markerGo.transform.rotation = cam.transform.rotation;

            int frame = C2BuildingRallyPointRuntimeV155LikeOriginal.CurrentExitPointFrameV155LikeOriginal();
            if (force || frame != _lastFrame || _renderer.sprite == null)
            {
                _lastFrame = frame;
                _renderer.sprite = C2BuildingRallyPointRuntimeV155LikeOriginal.LoadExitPointSpriteV155LikeOriginal(frame);
            }

            if (!_logged)
            {
                _logged = true;
                Debug.Log("[C2:BUILD RALLY V155 MARKER] building=" + _building.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                          " name='" + (_building.SourceMonsterId ?? string.Empty) + "' real=(" +
                          realX.ToString(CultureInfo.InvariantCulture) + "," + realY.ToString(CultureInfo.InvariantCulture) + ")" +
                          " gp='Interf3\\exitpoint' frames=18 source='" + _source + "'");
            }
        }
    }
}
