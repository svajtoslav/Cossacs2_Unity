using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal struct C2MinimapUnitSampleLikeOriginal
    {
        public float RealX;
        public float RealY;
        public int Nation;
        public bool Selected;
    }

    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        internal void C2CopyMinimapUnitSamplesLikeOriginal(List<C2MinimapUnitSampleLikeOriginal> destination)
        {
            if (destination == null) return;
            destination.Clear();

            // mapa.cpp::GMiniShow16 deliberately subsamples a very large object table.
            // Keep the same roughly-3000 marker ceiling instead of scanning/drawing every
            // unit into a 128x128 texture every frame.
            int stride = (_units.Count / 3000) + 1;
            for (int i = 0; i < _units.Count; i += stride)
            {
                C2UnitOriginalRuntime unit = _units[i];
                if (unit == null || unit.Root == null || unit.State == C2UnitOriginalState.Death ||
                    unit.HiddenInsideBuildingLikeOriginal || unit.Probe == null)
                    continue;

                int nation = unit.Probe.Nation;
                if (!AreNationsAlliedForFogLikeOriginal(nation) &&
                    GetOriginalObjectVisibilityValueInFogLikeOriginal(unit) == 0)
                    continue;

                destination.Add(new C2MinimapUnitSampleLikeOriginal
                {
                    RealX = unit.RuntimeRealXLikeOriginal,
                    RealY = unit.RuntimeRealYLikeOriginal,
                    Nation = nation,
                    Selected = unit.Selected
                });
            }
        }

        internal bool C2MinimapIsObjectVisibleLikeOriginal(int nation, int originalPixelX, int originalPixelY)
        {
            if (nation == 7 || AreNationsAlliedForFogLikeOriginal(nation))
                return true;
            return IsOriginalMapPointVisibleInFogLikeOriginal(originalPixelX, originalPixelY);
        }
    }

    public sealed partial class C2BattleTerrainMode
    {
        private C2MinimapRuntimeLikeOriginal _c2MinimapRuntimeLikeOriginal;

        public Rect C2MinimapScreenRectV377LikeOriginal()
        {
            return _c2MinimapRuntimeLikeOriginal != null
                ? _c2MinimapRuntimeLikeOriginal.OccupiedScreenRectV377LikeOriginal() : new Rect();
        }

        // mapa.cpp::HandleMouse + Mini2World: the minimap is also a map-coordinate
        // input surface. LMB scrolls the camera, while RMB keeps the mapped world
        // coordinate and feeds it into the ordinary unit/brigade command chain.
        internal bool C2MinimapTryScreenToOriginalPixelV381LikeOriginal(
            Vector2 screenPoint, out float originalPixelX, out float originalPixelY, out Vector3 world)
        {
            originalPixelX = 0.0f;
            originalPixelY = 0.0f;
            world = Vector3.zero;
            if (_c2MinimapRuntimeLikeOriginal == null ||
                !_c2MinimapRuntimeLikeOriginal.TryScreenToOriginalPixelV381LikeOriginal(
                    screenPoint, out originalPixelX, out originalPixelY))
                return false;

            world = C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(
                originalPixelX, originalPixelY);
            return true;
        }

        private void InstallC2MinimapRuntimeLikeOriginal()
        {
            if (_map == null) return;
            if (_c2MinimapRuntimeLikeOriginal != null)
                SafeDestroy(_c2MinimapRuntimeLikeOriginal.gameObject);

            GameObject root = new GameObject("GameplayHud_MinimapRuntime_LikeOriginal");
            root.transform.SetParent(transform, false);
            _c2MinimapRuntimeLikeOriginal = root.AddComponent<C2MinimapRuntimeLikeOriginal>();
            _c2MinimapRuntimeLikeOriginal.InitializeLikeOriginal(this);
        }

        private void C2MinimapCenterStrictCameraLikeOriginal(float normalizedX, float normalizedYFromTop)
        {
            // The free camera is a separate test tool. Minimap input is intentionally
            // connected only to the normal Cossacks II gameplay camera.
            if (_freeCameraMode || _map == null || !_strictCameraStateInitialized)
                return;

            float scale = GetStrictScaleLikeOriginal();
            float viewVol = GetStrictViewVolLikeOriginal(scale);
            float realLy = GetStrictRealLyLikeOriginal(scale);
            float smaplx = viewVol / 32.0f;
            float smaply = realLy / 32.0f;

            float mapX = Mathf.Lerp(_map.MinMapX, _map.MaxMapX, Mathf.Clamp01(normalizedX));
            float mapY = Mathf.Lerp(_map.MinMapY, _map.MaxMapY, Mathf.Clamp01(normalizedYFromTop));
            _strictMapX = mapX - smaplx * 0.5f;
            _strictMapY = mapY - smaply;
            _strictStepX = 0.0f;
            _strictStepY = 0.0f;
            ClampStrictIsoMapStateLikeOriginal(viewVol, realLy, scale);
            UpdateCameraTransform();
        }

        [DefaultExecutionOrder(32740)]
        private sealed class C2MinimapRuntimeLikeOriginal : MonoBehaviour, IPointerDownHandler, IDragHandler
        {
            private const int TextureSizeLikeOriginal = 128;
            private const float DisplaySizeLikeOriginal = 120.0f;
            private const float ScreenMarginLikeOriginal = 20.0f;
            private const float DynamicUpdateSecondsLikeOriginal = 0.10f;
            private const float BuildingRefreshSecondsLikeOriginal = 0.75f;

            private readonly List<C2MinimapUnitSampleLikeOriginal> _unitSamples =
                new List<C2MinimapUnitSampleLikeOriginal>(3000);

            private C2BattleTerrainMode _owner;
            private Canvas _canvas;
            private RectTransform _frameRoot;
            private Image _nationFrameImage;
            private GameObject _reopenRoot;
            private Image _modeButtonImage;
            private RectTransform _mapRect;
            private RawImage _mapImage;
            private Texture2D _texture;
            private Color32[] _basePixels;
            private Color32[] _workPixels;
            private Color32[] _groundTileColors;
            private C2UnitOriginalRuntimeAndRendererV1 _unitRuntime;
            private C2BuildingRuntimeInfoV247LikeOriginal[] _buildings = Array.Empty<C2BuildingRuntimeInfoV247LikeOriginal>();
            private float _nextDynamicUpdate;
            private float _nextBuildingRefresh;
            private bool _collapsed;
            private bool _expanded = true;
            private bool _closed;

            internal void InitializeLikeOriginal(C2BattleTerrainMode owner)
            {
                _owner = owner;
                BuildUiLikeOriginal();
                BuildStaticMapLikeOriginal();
                UpdateDynamicMapLikeOriginal();
                Debug.Log("[C2:MINIMAP LIKE ORIGINAL] installed source=COSSACKS2/3DMapEd.cpp::CreateMiniMapPart+mapa.cpp::GMiniShow16" +
                          " texture=128x128 display=120x120 margin=20 unitMarkerCeiling=about3000" +
                          " layers=terrain_texture_light_water_roads_buildings_units_camera_frustum" +
                          " freeTestCamera=untouched");
            }

            private void BuildUiLikeOriginal()
            {
                _canvas = gameObject.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = 32766;
                CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1.0f;
                gameObject.AddComponent<GraphicRaycaster>();

                GameObject borderGo = new GameObject("MinimapNationFrame_LikeOriginal", typeof(RectTransform), typeof(Image));
                borderGo.transform.SetParent(transform, false);
                _frameRoot = (RectTransform)borderGo.transform;
                _frameRoot.anchorMin = new Vector2(1.0f, 0.0f);
                _frameRoot.anchorMax = new Vector2(1.0f, 0.0f);
                _frameRoot.pivot = new Vector2(1.0f, 0.0f);
                _frameRoot.anchoredPosition = new Vector2(-ScreenMarginLikeOriginal, ScreenMarginLikeOriginal);
                _nationFrameImage = borderGo.GetComponent<Image>();
                _nationFrameImage.raycastTarget = false;

                GameObject innerGo = new GameObject("MinimapDarkInnerFrame_LikeOriginal", typeof(RectTransform), typeof(Image));
                innerGo.transform.SetParent(borderGo.transform, false);
                RectTransform inner = (RectTransform)innerGo.transform;
                inner.anchorMin = Vector2.zero;
                inner.anchorMax = Vector2.one;
                inner.offsetMin = new Vector2(3.0f, 3.0f);
                inner.offsetMax = new Vector2(-3.0f, -3.0f);
                Image innerImage = innerGo.GetComponent<Image>();
                innerImage.color = new Color32(25, 21, 13, 248);
                innerImage.raycastTarget = false;

                GameObject mapGo = new GameObject("MinimapSurface_LikeOriginal", typeof(RectTransform), typeof(RawImage));
                mapGo.transform.SetParent(innerGo.transform, false);
                _mapRect = (RectTransform)mapGo.transform;
                _mapRect.anchorMin = new Vector2(0.5f, 0.0f);
                _mapRect.anchorMax = new Vector2(0.5f, 0.0f);
                _mapRect.pivot = new Vector2(0.5f, 0.0f);
                _mapImage = mapGo.GetComponent<RawImage>();
                _mapImage.color = Color.white;
                _mapImage.raycastTarget = true;

                // respanel.DialogsSystem.xml::MiniButton is one tri-state F8
                // control, not three invented window buttons. Its original
                // game_buttons frames are: 0=full, 2=small, 1=hidden.
                _reopenRoot = new GameObject("MiniButton_F8_LikeOriginal", typeof(RectTransform), typeof(Image), typeof(Button));
                _reopenRoot.transform.SetParent(transform, false);
                RectTransform reopenRect = (RectTransform)_reopenRoot.transform;
                reopenRect.anchorMin = new Vector2(1.0f, 0.0f);
                reopenRect.anchorMax = new Vector2(1.0f, 0.0f);
                reopenRect.pivot = new Vector2(1.0f, 0.0f);
                reopenRect.sizeDelta = new Vector2(32.0f, 32.0f);
                _modeButtonImage = _reopenRoot.GetComponent<Image>();
                _modeButtonImage.preserveAspect = true;
                Button modeButton = _reopenRoot.GetComponent<Button>();
                modeButton.targetGraphic = _modeButtonImage;
                modeButton.onClick.AddListener(CycleMinimapModeLikeOriginal);

                _texture = new Texture2D(TextureSizeLikeOriginal, TextureSizeLikeOriginal, TextureFormat.RGBA32, false, false);
                _texture.name = "C2_Minimap_128_LikeOriginal";
                _texture.filterMode = FilterMode.Point;
                _texture.wrapMode = TextureWrapMode.Clamp;
                _mapImage.texture = _texture;
                _basePixels = new Color32[TextureSizeLikeOriginal * TextureSizeLikeOriginal];
                _workPixels = new Color32[_basePixels.Length];
                ApplyMinimapLayoutLikeOriginal();
                RefreshNationFrameLikeOriginal();
            }

            private void CycleMinimapModeLikeOriginal()
            {
                // vui_GHK_MiniMapMode::Action: full -> small -> hidden -> full.
                if (_closed)
                {
                    _closed = false;
                    _expanded = true;
                }
                else if (_expanded)
                    _expanded = false;
                else
                    _closed = true;
                _collapsed = false;
                ApplyMinimapLayoutLikeOriginal();
            }

            public Rect OccupiedScreenRectV377LikeOriginal()
            {
                bool showMap = !_collapsed && !_closed;
                float display = _expanded ? DisplaySizeLikeOriginal * 4.0f : DisplaySizeLikeOriginal;
                float width = showMap ? display + 8.0f : 32.0f;
                float height = showMap ? display + 44.0f : 32.0f;
                return new Rect(Screen.width - ScreenMarginLikeOriginal - width, ScreenMarginLikeOriginal, width, height);
            }

            private void ApplyMinimapLayoutLikeOriginal()
            {
                if (_frameRoot == null || _mapRect == null) return;
                float display = _expanded ? DisplaySizeLikeOriginal * 4.0f : DisplaySizeLikeOriginal;
                bool showMap = !_collapsed && !_closed;
                _frameRoot.gameObject.SetActive(showMap);
                _mapRect.gameObject.SetActive(showMap);
                _mapRect.sizeDelta = new Vector2(display, display);
                _mapRect.anchoredPosition = new Vector2(0.0f, 0.0f);
                _frameRoot.sizeDelta = new Vector2(display + 8.0f, display + 8.0f);
                if (_reopenRoot != null)
                {
                    RectTransform buttonRect = (RectTransform)_reopenRoot.transform;
                    buttonRect.anchoredPosition = showMap
                        ? new Vector2(-ScreenMarginLikeOriginal, ScreenMarginLikeOriginal + display + 12.0f)
                        : new Vector2(-ScreenMarginLikeOriginal, ScreenMarginLikeOriginal);
                }
                if (_modeButtonImage != null)
                {
                    int spriteId = _closed ? 1 : (_expanded ? 0 : 2);
                    _modeButtonImage.sprite = C2GameplayOriginalSpriteCacheV1.LoadSprite(
                        "Interf3\\game_buttons", spriteId, "minimap_mode_F8_original");
                    _modeButtonImage.color = Color.white;
                }
            }

            private void RefreshNationFrameLikeOriginal()
            {
                if (_nationFrameImage == null) return;
                int player = Mathf.Clamp(C2EditorRuntimeStateV333LikeOriginal.ControlledNation, 0,
                    C2PlayerColorsLikeOriginal.MaxPlayers - 1);
                Color32 color = C2PlayerColorsLikeOriginal.GetNatColorByPlayer(player);
                color.a = 255;
                _nationFrameImage.color = color;
            }

            private void Update()
            {
                if (_owner == null || _canvas == null) return;
                if (MinimapModeHotkeyPressedLikeOriginal())
                    CycleMinimapModeLikeOriginal();
                bool visible = !_owner._freeCameraMode;
                if (_canvas.enabled != visible) _canvas.enabled = visible;
                if (!visible || Time.unscaledTime < _nextDynamicUpdate) return;
                RefreshNationFrameLikeOriginal();
                _nextDynamicUpdate = Time.unscaledTime + DynamicUpdateSecondsLikeOriginal;
                if (!_closed && !_collapsed)
                    UpdateDynamicMapLikeOriginal();
            }

            private static bool MinimapModeHotkeyPressedLikeOriginal()
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null &&
                    (UnityEngine.InputSystem.Keyboard.current.f8Key.wasPressedThisFrame ||
                     UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame))
                    return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (UnityEngine.Input.GetKeyDown(KeyCode.F8) || UnityEngine.Input.GetKeyDown(KeyCode.M))
                    return true;
#endif
                return false;
            }

            private void BuildStaticMapLikeOriginal()
            {
                ParsedMap map = _owner != null ? _owner._map : null;
                if (map == null || _basePixels == null) return;

                BuildGroundTileAverageColorsLikeOriginal();
                float minX = map.MinMapX;
                float minY = map.MinMapY;
                float maxX = Mathf.Max(minX + 1.0f, map.MaxMapX);
                float maxY = Mathf.Max(minY + 1.0f, map.MaxMapY);

                for (int yTop = 0; yTop < TextureSizeLikeOriginal; yTop++)
                {
                    float v = (yTop + 0.5f) / TextureSizeLikeOriginal;
                    int vertexY = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(minY, maxY - 1.0f, v)), 0, Mathf.Max(0, map.MaxTH - 1));
                    for (int x = 0; x < TextureSizeLikeOriginal; x++)
                    {
                        float u = (x + 0.5f) / TextureSizeLikeOriginal;
                        int vertexX = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(minX, maxX - 1.0f, u)), 0, Mathf.Max(0, map.VertInLine - 1));
                        int vertex = vertexX + vertexY * map.VertInLine;

                        int baseTile = map.TexMap != null && vertex < map.TexMap.Length ? map.TexMap[vertex] & 63 : 0;
                        Color32 color = _groundTileColors != null && baseTile < _groundTileColors.Length
                            ? _groundTileColors[baseTile]
                            : new Color32(92, 108, 63, 255);

                        if (map.HasTilesExChunk && map.TexMapEx != null && map.WTexMapEx != null &&
                            vertex < map.TexMapEx.Length && vertex < map.WTexMapEx.Length)
                        {
                            int exTile = map.TexMapEx[vertex] & 63;
                            int weight = map.WTexMapEx[vertex];
                            Color32 ex = _groundTileColors != null && exTile < _groundTileColors.Length
                                ? _groundTileColors[exTile]
                                : color;
                            color = MixColorLikeOriginal(color, ex, weight);
                        }

                        int lighting = map.Heights != null && vertex >= 0 && vertex < map.Heights.Length
                            ? _owner.GetLighting3DLikeOriginal(vertex)
                            : 128;
                        color = ApplyLightingLikeOriginal(color, lighting);

                        if (map.Heights != null && vertex >= 0 && vertex < map.Heights.Length && map.Heights[vertex] < 0)
                            color = MixColorLikeOriginal(color, new Color32(54, 87, 103, 255), 168);

                        _basePixels[PixelIndexLikeOriginal(x, yTop)] = color;
                    }
                }

                RasterizeRoadNetLikeOriginal();
                Array.Copy(_basePixels, _workPixels, _basePixels.Length);
                _texture.SetPixels32(_basePixels);
                _texture.Apply(false, false);
            }

            private void BuildGroundTileAverageColorsLikeOriginal()
            {
                _groundTileColors = new Color32[64];
                for (int i = 0; i < _groundTileColors.Length; i++)
                    _groundTileColors[i] = new Color32(92, 108, 63, 255);

                TerrainTextureResourcesLikeOriginal resources = _owner.TryLoadTerrainSurfaceResourcesLikeOriginal();
                Texture2D atlas = resources != null ? resources.GroundAtlas : null;
                if (atlas == null || atlas.width < 8 || atlas.height < 8) return;

                try
                {
                    Color32[] pixels = atlas.GetPixels32();
                    int tileWidth = Mathf.Max(1, atlas.width / 8);
                    int tileHeight = Mathf.Max(1, atlas.height / 8);
                    for (int tile = 0; tile < 64; tile++)
                    {
                        int tileX = tile & 7;
                        int tileY = tile >> 3;
                        long r = 0, g = 0, b = 0, count = 0;
                        int x0 = tileX * tileWidth;
                        int y0 = tileY * tileHeight;
                        for (int y = y0; y < Mathf.Min(atlas.height, y0 + tileHeight); y++)
                        {
                            int row = y * atlas.width;
                            for (int x = x0; x < Mathf.Min(atlas.width, x0 + tileWidth); x += 2)
                            {
                                Color32 c = pixels[row + x];
                                r += c.r; g += c.g; b += c.b; count++;
                            }
                        }
                        if (count > 0)
                            _groundTileColors[tile] = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 255);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[C2:MINIMAP LIKE ORIGINAL] GroundTex average unavailable: " + ex.GetType().Name + ": " + ex.Message);
                }
            }

            private void RasterizeRoadNetLikeOriginal()
            {
                ParsedMap map = _owner != null ? _owner._map : null;
                if (map == null || map.RoadKnots == null || map.RoadKnots.Length == 0) return;
                List<RoadDescLikeOriginal> descs = _owner.LoadRoadDescsLikeOriginal();

                for (int i = 0; i < map.RoadKnots.Length; i++)
                {
                    ParsedRoadNetKnotLikeOriginal knot = map.RoadKnots[i];
                    if (knot.Hidden != 0 || knot.Links == null) continue;
                    for (int linkIndex = 0; linkIndex < knot.NLinks && linkIndex < knot.Links.Length; linkIndex++)
                    {
                        int otherIndex = knot.Links[linkIndex];
                        if (otherIndex <= i || otherIndex < 0 || otherIndex >= map.RoadKnots.Length) continue;
                        ParsedRoadNetKnotLikeOriginal other = map.RoadKnots[otherIndex];
                        if (other.Hidden != 0) continue;

                        int x0, y0, x1, y1;
                        if (!TryMapCellToTextureLikeOriginal(knot.X / 32.0f, knot.Y / 32.0f, out x0, out y0) ||
                            !TryMapCellToTextureLikeOriginal(other.X / 32.0f, other.Y / 32.0f, out x1, out y1))
                            continue;

                        int type = knot.LinkType != null && linkIndex < knot.LinkType.Length ? knot.LinkType[linkIndex] : 0;
                        RoadDescLikeOriginal desc = GetRoadDescLikeOriginal(descs, type);
                        Color32 roadColor = desc != null ? desc.FallbackColor : new Color32(145, 106, 67, 255);
                        roadColor.a = 255;
                        DrawLineLikeOriginal(_basePixels, x0, y0, x1, y1, roadColor);
                    }
                }
            }

            private void UpdateDynamicMapLikeOriginal()
            {
                if (_owner == null || _owner._map == null || _texture == null || _basePixels == null) return;
                Array.Copy(_basePixels, _workPixels, _basePixels.Length);

                if (_unitRuntime == null)
                    _unitRuntime = UnityEngine.Object.FindObjectOfType<C2UnitOriginalRuntimeAndRendererV1>();

                if (Time.unscaledTime >= _nextBuildingRefresh)
                {
                    _nextBuildingRefresh = Time.unscaledTime + BuildingRefreshSecondsLikeOriginal;
                    _buildings = UnityEngine.Object.FindObjectsOfType<C2BuildingRuntimeInfoV247LikeOriginal>();
                }

                DrawBuildingsLikeOriginal();
                DrawUnitsLikeOriginal();
                DrawCameraFrustumLikeOriginal();

                _texture.SetPixels32(_workPixels);
                _texture.Apply(false, false);
            }

            private void DrawBuildingsLikeOriginal()
            {
                if (_buildings == null) return;
                for (int i = 0; i < _buildings.Length; i++)
                {
                    C2BuildingRuntimeInfoV247LikeOriginal building = _buildings[i];
                    if (building == null || !building.isActiveAndEnabled || building.RuntimeConstructionVisualChildV303)
                        continue;
                    int originalPixelX = Mathf.RoundToInt(building.RealX / 16.0f);
                    int originalPixelY = Mathf.RoundToInt(building.RealY / 16.0f);
                    if (_unitRuntime != null &&
                        !_unitRuntime.C2MinimapIsObjectVisibleLikeOriginal(building.Nation, originalPixelX, originalPixelY))
                        continue;

                    int x, y;
                    if (!TryMapCellToTextureLikeOriginal(originalPixelX / 32.0f, originalPixelY / 32.0f, out x, out y))
                        continue;
                    Color32 c = C2PlayerColorsLikeOriginal.GetNatColorByPlayer(building.Nation);
                    DrawMarkerLikeOriginal(_workPixels, x, y, 1, c);
                    SetPixelLikeOriginal(_workPixels, x, y, new Color32(255, 255, 255, 255));
                }
            }

            private void DrawUnitsLikeOriginal()
            {
                if (_unitRuntime == null) return;
                _unitRuntime.C2CopyMinimapUnitSamplesLikeOriginal(_unitSamples);
                for (int i = 0; i < _unitSamples.Count; i++)
                {
                    C2MinimapUnitSampleLikeOriginal unit = _unitSamples[i];
                    int x, y;
                    if (!TryMapCellToTextureLikeOriginal(unit.RealX / 512.0f, unit.RealY / 512.0f, out x, out y))
                        continue;
                    Color32 c = unit.Selected
                        ? new Color32(255, 255, 255, 255)
                        : C2PlayerColorsLikeOriginal.GetNatColorByPlayer(unit.Nation);
                    DrawMarkerLikeOriginal(_workPixels, x, y, unit.Selected ? 1 : 0, c);
                }
            }

            private void DrawCameraFrustumLikeOriginal()
            {
                Camera cam = _owner != null ? _owner._strictIsoCamera : null;
                if (cam == null || !cam.enabled) return;
                Rect r = cam.pixelRect;
                Vector2[] screen =
                {
                    new Vector2(r.xMin + 0.5f, r.yMin + 0.5f),
                    new Vector2(r.xMax - 0.5f, r.yMin + 0.5f),
                    new Vector2(r.xMax - 0.5f, r.yMax - 0.5f),
                    new Vector2(r.xMin + 0.5f, r.yMax - 0.5f)
                };
                int[] xs = new int[4];
                int[] ys = new int[4];
                for (int i = 0; i < 4; i++)
                {
                    float originalX, originalY;
                    if (!_owner.C2OriginalCameraPlaneScreenToPixelV1LikeOriginal(cam, screen[i], out originalX, out originalY) ||
                        !TryMapCellToTextureLikeOriginal(originalX / 32.0f, originalY / 32.0f, out xs[i], out ys[i]))
                        return;
                }
                Color32 white = new Color32(255, 255, 255, 255);
                for (int i = 0; i < 4; i++)
                    DrawLineLikeOriginal(_workPixels, xs[i], ys[i], xs[(i + 1) & 3], ys[(i + 1) & 3], white);
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                // mapa.cpp moves the camera from the minimap only while Lpressed.
                // RMB must stay available for CmdSendToXY / brigade movement.
                if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                    return;
                CenterCameraFromPointerLikeOriginal(eventData);
            }

            public void OnDrag(PointerEventData eventData)
            {
                // Preserve the original button split while dragging too: LMB scrolls,
                // RMB never recenters the camera.
                if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                    return;
                CenterCameraFromPointerLikeOriginal(eventData);
            }

            private void CenterCameraFromPointerLikeOriginal(PointerEventData eventData)
            {
                if (_owner == null || _mapRect == null || eventData == null) return;
                Vector2 local;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _mapRect, eventData.position, eventData.pressEventCamera, out local))
                    return;
                Rect rect = _mapRect.rect;
                float u = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
                float vFromBottom = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
                _owner.C2MinimapCenterStrictCameraLikeOriginal(u, 1.0f - vFromBottom);
            }

            internal bool TryScreenToOriginalPixelV381LikeOriginal(
                Vector2 screenPoint, out float originalPixelX, out float originalPixelY)
            {
                originalPixelX = 0.0f;
                originalPixelY = 0.0f;
                if (_owner == null || _owner._map == null || _mapRect == null ||
                    !_mapRect.gameObject.activeInHierarchy)
                    return false;

                // ScreenSpaceOverlay minimap: null event camera is the exact UI conversion.
                if (!RectTransformUtility.RectangleContainsScreenPoint(_mapRect, screenPoint, null))
                    return false;

                Vector2 local;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _mapRect, screenPoint, null, out local))
                    return false;

                Rect rect = _mapRect.rect;
                float u = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
                float vFromBottom = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
                float normalizedYFromTop = 1.0f - vFromBottom;

                // mapa.cpp::Mini2World returns map cells; HandleMouse then converts both
                // axes to ordinary map pixels (x<<5, (y<<4)<<1).
                float mapCellX = Mathf.Lerp(_owner._map.MinMapX, _owner._map.MaxMapX, Mathf.Clamp01(u));
                float mapCellY = Mathf.Lerp(_owner._map.MinMapY, _owner._map.MaxMapY,
                    Mathf.Clamp01(normalizedYFromTop));
                originalPixelX = mapCellX * 32.0f;
                originalPixelY = mapCellY * 32.0f;
                return true;
            }

            private bool TryMapCellToTextureLikeOriginal(float mapCellX, float mapCellY, out int x, out int yTop)
            {
                x = 0;
                yTop = 0;
                ParsedMap map = _owner != null ? _owner._map : null;
                if (map == null) return false;
                float width = Mathf.Max(1.0f, map.MaxMapX - map.MinMapX);
                float height = Mathf.Max(1.0f, map.MaxMapY - map.MinMapY);
                float u = (mapCellX - map.MinMapX) / width;
                float v = (mapCellY - map.MinMapY) / height;
                if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f) return false;
                x = Mathf.Clamp(Mathf.RoundToInt(u * (TextureSizeLikeOriginal - 1)), 0, TextureSizeLikeOriginal - 1);
                yTop = Mathf.Clamp(Mathf.RoundToInt(v * (TextureSizeLikeOriginal - 1)), 0, TextureSizeLikeOriginal - 1);
                return true;
            }

            private static Color32 MixColorLikeOriginal(Color32 a, Color32 b, int bWeight)
            {
                int w = Mathf.Clamp(bWeight, 0, 255);
                int iw = 255 - w;
                return new Color32(
                    (byte)((a.r * iw + b.r * w) / 255),
                    (byte)((a.g * iw + b.g * w) / 255),
                    (byte)((a.b * iw + b.b * w) / 255),
                    255);
            }

            private static Color32 ApplyLightingLikeOriginal(Color32 color, int light)
            {
                float factor = Mathf.Clamp(light / 128.0f, 0.35f, 1.65f);
                return new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * factor), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * factor), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * factor), 0, 255),
                    255);
            }

            private static int PixelIndexLikeOriginal(int x, int yTop)
            {
                return x + (TextureSizeLikeOriginal - 1 - yTop) * TextureSizeLikeOriginal;
            }

            private static void SetPixelLikeOriginal(Color32[] pixels, int x, int yTop, Color32 color)
            {
                if (pixels == null || x < 0 || yTop < 0 || x >= TextureSizeLikeOriginal || yTop >= TextureSizeLikeOriginal)
                    return;
                pixels[PixelIndexLikeOriginal(x, yTop)] = color;
            }

            private static void DrawMarkerLikeOriginal(Color32[] pixels, int cx, int cy, int radius, Color32 color)
            {
                for (int y = cy - radius; y <= cy + radius; y++)
                    for (int x = cx - radius; x <= cx + radius; x++)
                        SetPixelLikeOriginal(pixels, x, y, color);
            }

            private static void DrawLineLikeOriginal(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 color)
            {
                int dx = Mathf.Abs(x1 - x0);
                int sx = x0 < x1 ? 1 : -1;
                int dy = -Mathf.Abs(y1 - y0);
                int sy = y0 < y1 ? 1 : -1;
                int error = dx + dy;
                while (true)
                {
                    SetPixelLikeOriginal(pixels, x0, y0, color);
                    if (x0 == x1 && y0 == y1) break;
                    int twice = error * 2;
                    if (twice >= dy) { error += dy; x0 += sx; }
                    if (twice <= dx) { error += dx; y0 += sy; }
                }
            }

            private void OnDestroy()
            {
                if (_texture != null)
                    UnityEngine.Object.Destroy(_texture);
                _texture = null;
                _mapImage = null;
                _mapRect = null;
                _frameRoot = null;
                _nationFrameImage = null;
                _reopenRoot = null;
                _modeButtonImage = null;
                _canvas = null;
                _owner = null;
            }
        }
    }
}
