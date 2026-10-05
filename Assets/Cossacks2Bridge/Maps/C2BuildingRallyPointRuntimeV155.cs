using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // V156: original-like building rally point / exit destination.
    // Original fields: OneObject::DstX/DstY.  Visual GP: Interf3\exitpoint, 18-frame loop.
    internal static class C2BuildingRallyPointRuntimeV155LikeOriginal
    {
        private const int ExitPointFramesLikeOriginal = 18;
        private const float ExitPointFrameMsLikeOriginal = 40.0f;
        private const float RallyMarkerYOffsetWorld = 0.18f;
        private const float OriginalPixelToWorldScaleForSpriteRenderer = 10.0f; // cache sprites use PPU=100, map uses ~0.1 world/original px
        // A 1024-real-unit half-range is only 64 original pixels, so a long production queue
        // still converged into one dense knot. Keep deterministic per-unit destinations but spread
        // the rally area enough for normal collision separation to work.
        private const float OriginalDstScatterHalfRangeRealLikeOriginal = 4096.0f;
        private const int OriginalDstScatterAttemptsLikeOriginal = 16;
        private static readonly Dictionary<string, int> _nextSlotByRallyKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private struct RallyPointStateV159LikeOriginal
        {
            public int RealX;
            public int RealY;
            public bool WasSelected;
            public float LastTouchedTime;
            public string Source;
        }

        private static readonly Dictionary<string, RallyPointStateV159LikeOriginal> _rallyStateByStableBuildingKeyV159 =
            new Dictionary<string, RallyPointStateV159LikeOriginal>(StringComparer.OrdinalIgnoreCase);

        public static void RememberRallyPointStateV159LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, string source, bool wasSelected)
        {
            if (building == null || !building.HasRallyPointV155LikeOriginal)
                return;

            string key = StableBuildingKeyV159LikeOriginal(building);
            if (string.IsNullOrEmpty(key))
                return;

            RallyPointStateV159LikeOriginal state = new RallyPointStateV159LikeOriginal();
            state.RealX = building.RallyRealXV155LikeOriginal;
            state.RealY = building.RallyRealYV155LikeOriginal;
            state.WasSelected = wasSelected;
            state.LastTouchedTime = Time.realtimeSinceStartup;
            state.Source = source ?? string.Empty;
            _rallyStateByStableBuildingKeyV159[key] = state;
        }

        public static void ForgetRallyPointStateV159LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            if (building == null)
                return;

            string key = StableBuildingKeyV159LikeOriginal(building);
            if (!string.IsNullOrEmpty(key))
                _rallyStateByStableBuildingKeyV159.Remove(key);
        }

        public static bool TryRestoreRallyPointStateV159LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            out int realX,
            out int realY,
            out bool wasSelected)
        {
            realX = 0;
            realY = 0;
            wasSelected = false;

            if (building == null)
                return false;

            string key = StableBuildingKeyV159LikeOriginal(building);
            if (string.IsNullOrEmpty(key))
                return false;

            RallyPointStateV159LikeOriginal state;
            if (!_rallyStateByStableBuildingKeyV159.TryGetValue(key, out state))
                return false;

            realX = state.RealX;
            realY = state.RealY;
            wasSelected = state.WasSelected;
            return true;
        }

        private static string StableBuildingKeyV159LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            if (building == null)
                return string.Empty;

            // RecordIndex changes when the build-stage visual is rebuilt.
            // RealX/RealY + md/monster identity stays stable for the same logical building.
            string monster = building.SourceMonsterId ?? string.Empty;
            string kind = building.KindName ?? string.Empty;
            if (monster.Length == 0) monster = building.gameObject != null ? building.gameObject.name ?? string.Empty : string.Empty;

            int cellX = Mathf.RoundToInt(building.RealX / 16.0f);
            int cellY = Mathf.RoundToInt(building.RealY / 16.0f);

            return kind + "|" + monster + "|cell=" +
                   cellX.ToString(CultureInfo.InvariantCulture) + "," +
                   cellY.ToString(CultureInfo.InvariantCulture);
        }

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

            int chosenSeq = next;
            Vector2 firstCandidate = Vector2.zero;
            Vector2 chosen = new Vector2(rallyX, rallyY);
            bool free = false;

            for (int attempt = 0; attempt < OriginalDstScatterAttemptsLikeOriginal; attempt++)
            {
                int seq = next + attempt;
                Vector2 off = OriginalDstScatterOffsetRealV260LikeOriginal(key, seq);
                Vector2 candidate = new Vector2(rallyX + off.x, rallyY + off.y);
                if (attempt == 0)
                    firstCandidate = candidate;

                if (!IsRallyCandidateBlockedV260LikeOriginal(candidate))
                {
                    chosen = candidate;
                    chosenSeq = seq;
                    free = true;
                    break;
                }
            }

            if (!free)
            {
                chosenSeq = next;
                chosen = firstCandidate.sqrMagnitude > 0.0f ? firstCandidate : new Vector2(rallyX, rallyY);
            }

            _nextSlotByRallyKey[key] = chosenSeq + 1;
            audit = "rally=dstXDstY_original_Build_cpp_651_rando key='" + key + "' seq=" + chosenSeq.ToString(CultureInfo.InvariantCulture) +
                    " scatterReal=+/-" + OriginalDstScatterHalfRangeRealLikeOriginal.ToString("0", CultureInfo.InvariantCulture) +
                    " free=" + free +
                    " finalReal=(" + chosen.x.ToString("0", CultureInfo.InvariantCulture) + "," +
                    chosen.y.ToString("0", CultureInfo.InvariantCulture) + ")" +
                    " baseReal=(" + rallyX.ToString(CultureInfo.InvariantCulture) + "," + rallyY.ToString(CultureInfo.InvariantCulture) + ")";
            return chosen;
        }

        private static Vector2 OriginalDstScatterOffsetRealV260LikeOriginal(string key, int seq)
        {
            // Cossacks II Build.cpp, produced unit with OBJ->DstX:
            // dx=OBJ->DstX+(rando()%2048)-1024; dy=OBJ->DstY+(rando()%2048)-1024.
            // Use a stable per-building sequence so repeated produced units do not reserve a visible grid.
            uint baseHash = StableHashV260LikeOriginal(key);
            uint hx = MixHashV260LikeOriginal(baseHash ^ unchecked((uint)seq * 747796405u));
            uint hy = MixHashV260LikeOriginal(baseHash ^ unchecked((uint)seq * 2891336453u) ^ 0x9E3779B9u);
            float ox = SignedScatterRealV260LikeOriginal(hx);
            float oy = SignedScatterRealV260LikeOriginal(hy);
            return new Vector2(ox, oy);
        }

        private static float SignedScatterRealV260LikeOriginal(uint h)
        {
            int v = (int)(h & 8191u);
            return Mathf.Clamp(v - 4096, -OriginalDstScatterHalfRangeRealLikeOriginal, OriginalDstScatterHalfRangeRealLikeOriginal);
        }

        private static uint StableHashV260LikeOriginal(string text)
        {
            unchecked
            {
                uint h = 2166136261u;
                if (!string.IsNullOrEmpty(text))
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        h ^= text[i];
                        h *= 16777619u;
                    }
                }
                return h;
            }
        }

        private static uint MixHashV260LikeOriginal(uint x)
        {
            unchecked
            {
                x ^= x >> 16;
                x *= 0x7feb352du;
                x ^= x >> 15;
                x *= 0x846ca68bu;
                x ^= x >> 16;
                return x;
            }
        }

        private static bool IsRallyCandidateBlockedV260LikeOriginal(Vector2 candidateReal)
        {
            return C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(candidateReal.x, candidateReal.y, 1);
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
        private const float V157MarkerPixels = 64.0f;

        private C2SettlementBuildingSelectableV1LikeOriginal _building;
        private GameObject _markerGo;
        private Canvas _canvas;
        private Image _image;
        private Image _imageLayer2;
        private Image _imageLayer3;
        private RectTransform _rt;
        private int _lastFrame = -1;
        private string _source = string.Empty;
        private bool _logged;
        private static Sprite _fallbackSpriteV157;

        private void OnDestroy()
        {
            if (_markerGo != null)
            {
                try { UnityEngine.Object.Destroy(_markerGo); } catch { }
                _markerGo = null;
                _image = null;
                _imageLayer2 = null;
                _imageLayer3 = null;
                _rt = null;
            }
        }

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
            if (_markerGo != null && _image != null && _imageLayer2 != null && _imageLayer3 != null && _rt != null) return;

            // V158: draw rally point as a HUD-owned ScreenSpaceOverlay UI, not as a world SpriteRenderer.
            // Name starts with GameplayHud_ so C2GameplayHudV1.KillForeignBattleUiRoots does not destroy it.
            // This ignores terrain/building/ground-pipeline depth, so hills and terrain chunks cannot hide it.
            GameObject canvasGo = GameObject.Find("GameplayHud_RallyExitPoint_OverlayCanvas_V158");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("GameplayHud_RallyExitPoint_OverlayCanvas_V158");
                canvasGo.hideFlags = HideFlags.DontSave;
                _canvas = canvasGo.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 32766; // below HUD canvas 32767, above terrain/world.
                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                GraphicRaycaster ray = canvasGo.AddComponent<GraphicRaycaster>();
                ray.enabled = false;
            }
            else
            {
                _canvas = canvasGo.GetComponent<Canvas>();
                if (_canvas == null)
                {
                    _canvas = canvasGo.AddComponent<Canvas>();
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    _canvas.sortingOrder = 32766;
                }
            }

            _markerGo = new GameObject("GameplayHud_RallyExitPoint_Interf3_exitpoint_V158_Overlay");
            _markerGo.hideFlags = HideFlags.DontSave;
            _markerGo.transform.SetParent(canvasGo.transform, false);

            _rt = _markerGo.AddComponent<RectTransform>();
            _rt.anchorMin = new Vector2(0.0f, 0.0f);
            _rt.anchorMax = new Vector2(0.0f, 0.0f);
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _rt.sizeDelta = new Vector2(V157MarkerPixels, V157MarkerPixels);
            // Shared UI cache already converts GP rows to Unity orientation.
            _rt.localScale = Vector3.one;

            _image = _markerGo.AddComponent<Image>();
            _image.raycastTarget = false;
            _image.preserveAspect = true;
            _image.color = Color.white;
            _image.enabled = false;

            // G16 exitpoint frames are intentionally semi-transparent.
            // Original GP drawing makes them visually stronger; in Unity UI we stack the same sprite 3 times.
            _imageLayer2 = CreateStackedImageLayerV158("GameplayHud_RallyExitPoint_StackLayer2_V158");
            _imageLayer3 = CreateStackedImageLayerV158("GameplayHud_RallyExitPoint_StackLayer3_V158");
        }

        private Image CreateStackedImageLayerV158(string name)
        {
            GameObject go = new GameObject(name);
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(_markerGo.transform, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.color = Color.white;
            img.enabled = false;
            return img;
        }

        private void SetStackedImagesVisibleV158(bool visible)
        {
            if (_image != null) _image.enabled = visible;
            if (_imageLayer2 != null) _imageLayer2.enabled = visible;
            if (_imageLayer3 != null) _imageLayer3.enabled = visible;
        }

        private void SetStackedImagesSpriteV158(Sprite sp)
        {
            if (_image != null) _image.sprite = sp;
            if (_imageLayer2 != null) _imageLayer2.sprite = sp;
            if (_imageLayer3 != null) _imageLayer3.sprite = sp;
        }

        private void UpdateMarker(bool force)
        {
            if (_building == null) _building = GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
            EnsureMarker();
            if (_building == null || _markerGo == null || _image == null || _imageLayer2 == null || _imageLayer3 == null || _rt == null)
                return;

            int realX = 0;
            int realY = 0;
            bool visible = _building.IsSelected && _building.TryGetRallyPointRealV155LikeOriginal(out realX, out realY);
            SetStackedImagesVisibleV158(visible);
            if (!visible)
                return;

            Camera cam = Camera.main;
            Camera[] cams = Camera.allCameras;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; cams != null && i < cams.Length; i++)
                {
                    Camera c = cams[i];
                    if (c == null || !c.isActiveAndEnabled) continue;
                    string n = c.name ?? string.Empty;
                    bool isFree = n.IndexOf("Free", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (pass == 0)
                    {
                        if (n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0 && !isFree)
                        {
                            cam = c;
                            i = cams.Length;
                            pass = 2;
                        }
                    }
                    else if (!isFree && n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        cam = c;
                        break;
                    }
                }
            }

            C2BattleTerrainMode mode = _building.OwnerMode != null ? _building.OwnerMode : UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            Vector3 screen = Vector3.zero;
            bool screenOk = false;
            if (mode != null && cam != null)
            {
                Vector3 pos = mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(realX / 16.0f, realY / 16.0f);
                pos.y += C2BuildingRallyPointRuntimeV155LikeOriginal.MarkerYOffsetV155LikeOriginal();
                screen = cam.WorldToScreenPoint(pos);
                screenOk = screen.z > 0.0f && screen.x >= -128.0f && screen.y >= -128.0f &&
                           screen.x <= Screen.width + 128.0f && screen.y <= Screen.height + 128.0f;
                _rt.position = new Vector3(screen.x, screen.y, 0.0f);
            }

            if (!screenOk)
            {
                SetStackedImagesVisibleV158(false);
                return;
            }

            int frame = C2BuildingRallyPointRuntimeV155LikeOriginal.CurrentExitPointFrameV155LikeOriginal();
            if (force || frame != _lastFrame || _image.sprite == null)
            {
                _lastFrame = frame;
                Sprite sp = C2BuildingRallyPointRuntimeV155LikeOriginal.LoadExitPointSpriteV155LikeOriginal(frame);
                bool cacheOk = sp != null;
                if (sp == null) sp = FallbackExitPointSpriteV157();
                SetStackedImagesSpriteV158(sp);
                _rt.sizeDelta = new Vector2(V157MarkerPixels, V157MarkerPixels);
            }
        }

        private static Sprite FallbackExitPointSpriteV157()
        {
            if (_fallbackSpriteV157 != null) return _fallbackSpriteV157;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "C2_RallyExitPoint_Fallback_RedRing_V157";
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 red = new Color32(255, 0, 0, 255);
            Color32 orange = new Color32(255, 196, 0, 255);

            Color32[] px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = clear;

            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    bool ring = d >= 18.0f && d <= 23.0f;
                    bool cross = (Mathf.Abs(dx) <= 2.0f && d <= 16.0f) || (Mathf.Abs(dy) <= 2.0f && d <= 16.0f);
                    if (ring) px[y * size + x] = red;
                    else if (cross) px[y * size + x] = orange;
                }
            }

            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);

            _fallbackSpriteV157 = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1.0f);
            _fallbackSpriteV157.name = "C2_RallyExitPoint_Fallback_RedRing_V157";
            return _fallbackSpriteV157;
        }
    }

}
