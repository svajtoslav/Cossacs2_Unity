using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const bool C2WallDambaPairCalibratorV1EnabledLikeOriginal = false; // V2: disabled; replaced by universal 4-anchor calibrator
        private const int C2WallDambaPairCalibratorV1PrimarySprite = 60;
        private const int C2WallDambaPairCalibratorV1SecondarySprite = 60;
        private const float C2WallDambaPairCalibratorV1InitialStepPixels = 84.0f;
        private const float C2WallDambaPairCalibratorV1FineStepPixels = 1.0f;
        private const float C2WallDambaPairCalibratorV1FastStepPixels = 10.0f;
        private const float C2WallDambaPairCalibratorV1HeightBodies = 10.0f;
        private const bool C2WallDambaPairCalibratorV1AutoWriteLikeOriginal = false;

        private GameObject _c2WallDambaPairCalibratorRootV1LikeOriginal;
        private Transform _c2WallDambaPairCalibratorAnchorAV1LikeOriginal;
        private Transform _c2WallDambaPairCalibratorAnchorBV1LikeOriginal;
        private Vector2 _c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal;
        private float _c2WallDambaPairCalibratorDeltaHeightPixelsV1LikeOriginal;
        private int _c2WallDambaPairCalibratorSelectedV1LikeOriginal = 1;
        private WallSpriteDescV1LikeOriginal _c2WallDambaPairCalibratorDescAV1LikeOriginal;
        private WallSpriteDescV1LikeOriginal _c2WallDambaPairCalibratorDescBV1LikeOriginal;
        private float _c2WallDambaPairCalibratorPixelToWorldXV1LikeOriginal = 0.5f;
        private float _c2WallDambaPairCalibratorPixelToWorldZV1LikeOriginal = -0.5f;
        private float _c2WallDambaPairCalibratorLastAutoWriteTimeV1LikeOriginal = -1000.0f;
        private Vector3 _c2WallDambaPairCalibratorLastAutoWriteALocalV1LikeOriginal = new Vector3(float.NaN, float.NaN, float.NaN);
        private Vector3 _c2WallDambaPairCalibratorLastAutoWriteBLocalV1LikeOriginal = new Vector3(float.NaN, float.NaN, float.NaN);

#if UNITY_EDITOR
        private void OnEnable()
        {
            SceneView.duringSceneGui -= OnWallDambaPairCalibratorSceneGuiV1LikeOriginal;
            SceneView.duringSceneGui += OnWallDambaPairCalibratorSceneGuiV1LikeOriginal;

            SceneView.duringSceneGui -= OnWallUniversalAnchorCalibratorSceneGuiV1LikeOriginal;
            SceneView.duringSceneGui += OnWallUniversalAnchorCalibratorSceneGuiV1LikeOriginal;

            SceneView.duringSceneGui -= OnWallUniversalAnchorLineCalibratorSceneGuiV2LikeOriginal;
            SceneView.duringSceneGui += OnWallUniversalAnchorLineCalibratorSceneGuiV2LikeOriginal;

            SceneView.duringSceneGui -= OnSyntheticDambaSceneGuiV93LikeOriginal;
            SceneView.duringSceneGui += OnSyntheticDambaSceneGuiV93LikeOriginal;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnWallDambaPairCalibratorSceneGuiV1LikeOriginal;
            SceneView.duringSceneGui -= OnWallUniversalAnchorCalibratorSceneGuiV1LikeOriginal;
            SceneView.duringSceneGui -= OnWallUniversalAnchorLineCalibratorSceneGuiV2LikeOriginal;
            SceneView.duringSceneGui -= OnSyntheticDambaSceneGuiV93LikeOriginal;
        }
#endif

        private void BuildWallDambaPairCalibratorV1LikeOriginal()
        {
            if (!C2WallDambaPairCalibratorV1EnabledLikeOriginal || _map == null || _terrainRoot == null)
                return;

            if (_c2WallDambaPairCalibratorRootV1LikeOriginal != null)
                SafeDestroy(_c2WallDambaPairCalibratorRootV1LikeOriginal);

            WallSpriteCatalogV1LikeOriginal catalog = LoadWallSpriteCatalogV1LikeOriginal();
            if (catalog == null ||
                !catalog.ByIndex.TryGetValue(C2WallDambaPairCalibratorV1PrimarySprite, out _c2WallDambaPairCalibratorDescAV1LikeOriginal) ||
                !catalog.ByIndex.TryGetValue(C2WallDambaPairCalibratorV1SecondarySprite, out _c2WallDambaPairCalibratorDescBV1LikeOriginal))
            {
                return;
            }

            WallC2MParsedMeshV23LikeOriginal c2mA = TryLoadWallC2MVisualMeshV23LikeOriginal(_c2WallDambaPairCalibratorDescAV1LikeOriginal.ModelPath, out string auditA);
            WallC2MParsedMeshV23LikeOriginal c2mB = TryLoadWallC2MVisualMeshV23LikeOriginal(_c2WallDambaPairCalibratorDescBV1LikeOriginal.ModelPath, out string auditB);
            if (c2mA == null || c2mB == null)
            {
                return;
            }

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            _c2WallDambaPairCalibratorPixelToWorldXV1LikeOriginal = kernel.BackingStepXWorld / 32.0f;
            _c2WallDambaPairCalibratorPixelToWorldZV1LikeOriginal = kernel.BackingStepZWorld * WorldZSign / 32.0f;
            float zScale = kernel.HeightScale;

            Bounds boundsA = BuildWallDambaCalibratorLocalBoundsV1LikeOriginal(c2mA, zScale);
            Bounds boundsB = BuildWallDambaCalibratorLocalBoundsV1LikeOriginal(c2mB, zScale);
            float bodyHeight = Mathf.Max(8.0f, Mathf.Max(boundsA.size.y, boundsB.size.y));
            Vector3 center = _terrainBounds.center;
            center.y = _terrainBounds.max.y + bodyHeight * C2WallDambaPairCalibratorV1HeightBodies;

            _c2WallDambaPairCalibratorRootV1LikeOriginal = new GameObject("C2_DAMBA_PAIR_CALIBRATOR_V1");
            _c2WallDambaPairCalibratorRootV1LikeOriginal.transform.SetParent(transform, false);
            _c2WallDambaPairCalibratorRootV1LikeOriginal.transform.position = center;

            Vector2 step = GetWallConnectorStepOriginalXYV14LikeOriginal(_c2WallDambaPairCalibratorDescAV1LikeOriginal);
            if (step.sqrMagnitude <= 0.0001f)
                step = new Vector2(C2WallDambaPairCalibratorV1InitialStepPixels, 0.0f);
            _c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal = step.normalized * C2WallDambaPairCalibratorV1InitialStepPixels;
            _c2WallDambaPairCalibratorDeltaHeightPixelsV1LikeOriginal = 0.0f;
            LoadWallDambaPairCalibratorFileIfPresentV1LikeOriginal();

            _c2WallDambaPairCalibratorAnchorAV1LikeOriginal = new GameObject("A_W" + _c2WallDambaPairCalibratorDescAV1LikeOriginal.SpriteIndex).transform;
            _c2WallDambaPairCalibratorAnchorBV1LikeOriginal = new GameObject("B_W" + _c2WallDambaPairCalibratorDescBV1LikeOriginal.SpriteIndex).transform;
            _c2WallDambaPairCalibratorAnchorAV1LikeOriginal.SetParent(_c2WallDambaPairCalibratorRootV1LikeOriginal.transform, false);
            _c2WallDambaPairCalibratorAnchorBV1LikeOriginal.SetParent(_c2WallDambaPairCalibratorRootV1LikeOriginal.transform, false);

            AttachWallDambaCalibratorMeshV1LikeOriginal(_c2WallDambaPairCalibratorAnchorAV1LikeOriginal.gameObject, _c2WallDambaPairCalibratorDescAV1LikeOriginal, c2mA, "A");
            AttachWallDambaCalibratorMeshV1LikeOriginal(_c2WallDambaPairCalibratorAnchorBV1LikeOriginal.gameObject, _c2WallDambaPairCalibratorDescBV1LikeOriginal, c2mB, "B");
            UpdateWallDambaPairCalibratorTransformsV1LikeOriginal();
        }

        private void UpdateWallDambaPairCalibratorV1LikeOriginal()
        {
            if (!C2WallDambaPairCalibratorV1EnabledLikeOriginal ||
                _c2WallDambaPairCalibratorRootV1LikeOriginal == null ||
                _c2WallDambaPairCalibratorAnchorBV1LikeOriginal == null)
                return;

            float step = IsWallDambaCalibratorFastV1LikeOriginal()
                ? C2WallDambaPairCalibratorV1FastStepPixels
                : C2WallDambaPairCalibratorV1FineStepPixels;

            bool changed = false;
            if (WasWallCalKeyDownV1LikeOriginal(KeyCode.Tab))
            {
                _c2WallDambaPairCalibratorSelectedV1LikeOriginal = _c2WallDambaPairCalibratorSelectedV1LikeOriginal == 0 ? 1 : 0;
                changed = true;
            }

            Vector2 delta = Vector2.zero;
            if (WasWallCalKeyDownV1LikeOriginal(KeyCode.LeftArrow)) delta.x -= step;
            if (WasWallCalKeyDownV1LikeOriginal(KeyCode.RightArrow)) delta.x += step;
            if (WasWallCalKeyDownV1LikeOriginal(KeyCode.DownArrow)) delta.y -= step;
            if (WasWallCalKeyDownV1LikeOriginal(KeyCode.UpArrow)) delta.y += step;
            if (delta.sqrMagnitude > 0.0f)
            {
                if (_c2WallDambaPairCalibratorSelectedV1LikeOriginal == 0)
                    _c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal -= delta;
                else
                    _c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal += delta;
                changed = true;
            }

            float heightDelta = 0.0f;
            if (WasWallCalKeyDownV1LikeOriginal(KeyCode.Equals) || WasWallCalKeyDownV1LikeOriginal(KeyCode.Plus) || WasWallCalKeyDownV1LikeOriginal(KeyCode.KeypadPlus))
                heightDelta += step;
            if (WasWallCalKeyDownV1LikeOriginal(KeyCode.Minus) || WasWallCalKeyDownV1LikeOriginal(KeyCode.KeypadMinus))
                heightDelta -= step;
            if (Mathf.Abs(heightDelta) > 0.0001f)
            {
                if (_c2WallDambaPairCalibratorSelectedV1LikeOriginal == 0)
                    _c2WallDambaPairCalibratorDeltaHeightPixelsV1LikeOriginal -= heightDelta;
                else
                    _c2WallDambaPairCalibratorDeltaHeightPixelsV1LikeOriginal += heightDelta;
                changed = true;
            }

            if (WasWallCalKeyDownV1LikeOriginal(KeyCode.Return) || WasWallCalKeyDownV1LikeOriginal(KeyCode.KeypadEnter))
                WriteWallDambaPairCalibratorFileV1LikeOriginal();

            if (C2WallDambaPairCalibratorV1AutoWriteLikeOriginal)
                AutoWriteWallDambaPairCalibratorSnapshotV1LikeOriginal();

            if (changed)
            {
                UpdateWallDambaPairCalibratorTransformsV1LikeOriginal();
            }
        }

#if UNITY_EDITOR
        private void OnWallDambaPairCalibratorSceneGuiV1LikeOriginal(SceneView sceneView)
        {
            if (!C2WallDambaPairCalibratorV1EnabledLikeOriginal ||
                _c2WallDambaPairCalibratorRootV1LikeOriginal == null ||
                _c2WallDambaPairCalibratorAnchorAV1LikeOriginal == null ||
                _c2WallDambaPairCalibratorAnchorBV1LikeOriginal == null)
                return;

            DrawWallDambaPairCalibratorSceneConnectorsV1LikeOriginal();
            DrawWallDambaPairCalibratorSceneGuiButtonV1LikeOriginal();
            if (C2WallDambaPairCalibratorV1AutoWriteLikeOriginal)
                AutoWriteWallDambaPairCalibratorSnapshotV1LikeOriginal();

            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown)
                return;

            float step = e.shift ? C2WallDambaPairCalibratorV1FastStepPixels : C2WallDambaPairCalibratorV1FineStepPixels;
            bool changed = false;

            if (e.keyCode == KeyCode.Tab)
            {
                _c2WallDambaPairCalibratorSelectedV1LikeOriginal = _c2WallDambaPairCalibratorSelectedV1LikeOriginal == 0 ? 1 : 0;
                changed = true;
            }

            Vector2 delta = Vector2.zero;
            if (e.keyCode == KeyCode.LeftArrow) delta.x -= step;
            if (e.keyCode == KeyCode.RightArrow) delta.x += step;
            if (e.keyCode == KeyCode.DownArrow) delta.y -= step;
            if (e.keyCode == KeyCode.UpArrow) delta.y += step;
            if (delta.sqrMagnitude > 0.0f)
            {
                SyncWallDambaPairCalibratorDeltaFromTransformsV1LikeOriginal();
                if (_c2WallDambaPairCalibratorSelectedV1LikeOriginal == 0)
                    _c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal -= delta;
                else
                    _c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal += delta;
                changed = true;
            }

            float heightDelta = 0.0f;
            if (e.keyCode == KeyCode.Equals || e.keyCode == KeyCode.Plus || e.keyCode == KeyCode.KeypadPlus)
                heightDelta += step;
            if (e.keyCode == KeyCode.Minus || e.keyCode == KeyCode.KeypadMinus)
                heightDelta -= step;
            if (Mathf.Abs(heightDelta) > 0.0001f)
            {
                SyncWallDambaPairCalibratorDeltaFromTransformsV1LikeOriginal();
                if (_c2WallDambaPairCalibratorSelectedV1LikeOriginal == 0)
                    _c2WallDambaPairCalibratorDeltaHeightPixelsV1LikeOriginal -= heightDelta;
                else
                    _c2WallDambaPairCalibratorDeltaHeightPixelsV1LikeOriginal += heightDelta;
                changed = true;
            }

            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                WriteWallDambaPairCalibratorFileV1LikeOriginal();
                e.Use();
                return;
            }

            if (changed)
            {
                UpdateWallDambaPairCalibratorTransformsV1LikeOriginal();
                sceneView.Repaint();
                e.Use();
            }
        }

        private void DrawWallDambaPairCalibratorSceneConnectorsV1LikeOriginal()
        {
            Vector2 connectorA = GetWallConnectorPointForCalibratorV1LikeOriginal(_c2WallDambaPairCalibratorDescAV1LikeOriginal, true);
            Vector2 connectorB = GetWallConnectorPointForCalibratorV1LikeOriginal(_c2WallDambaPairCalibratorDescBV1LikeOriginal, false);
            Vector3 a = _c2WallDambaPairCalibratorAnchorAV1LikeOriginal.TransformPoint(WallDambaCalibratorPixelDeltaToWorldV1LikeOriginal(connectorA));
            Vector3 b = _c2WallDambaPairCalibratorAnchorBV1LikeOriginal.TransformPoint(WallDambaCalibratorPixelDeltaToWorldV1LikeOriginal(connectorB));

            Handles.color = Color.green;
            Handles.SphereHandleCap(0, a, Quaternion.identity, HandleUtility.GetHandleSize(a) * 0.06f, EventType.Repaint);
            Handles.Label(a, "A connector");
            Handles.color = Color.magenta;
            Handles.SphereHandleCap(0, b, Quaternion.identity, HandleUtility.GetHandleSize(b) * 0.06f, EventType.Repaint);
            Handles.Label(b, "B connector");
            Handles.color = Color.yellow;
            Handles.DrawLine(a, b);
        }

        private void DrawWallDambaPairCalibratorSceneGuiButtonV1LikeOriginal()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(12, 12, 220, 70), "DAMBA Pair Cal", GUI.skin.window);
            if (GUILayout.Button("WRITE DAMBA TXT", GUILayout.Height(28)))
                WriteWallDambaPairCalibratorFileV1LikeOriginal();
            GUILayout.Label("Tab A/B, arrows, +/-");
            GUILayout.EndArea();
            Handles.EndGUI();
        }
#endif

        private void UpdateWallDambaPairCalibratorTransformsV1LikeOriginal()
        {
            if (_c2WallDambaPairCalibratorAnchorAV1LikeOriginal == null || _c2WallDambaPairCalibratorAnchorBV1LikeOriginal == null)
                return;

            _c2WallDambaPairCalibratorAnchorAV1LikeOriginal.localPosition = Vector3.zero;
            Vector3 b = WallDambaCalibratorPixelDeltaToWorldV1LikeOriginal(_c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal);
            b.y += _c2WallDambaPairCalibratorDeltaHeightPixelsV1LikeOriginal * WallOriginalZUnitToWorldScaleV8LikeOriginal();
            _c2WallDambaPairCalibratorAnchorBV1LikeOriginal.localPosition = b;
        }

        private void SyncWallDambaPairCalibratorDeltaFromTransformsV1LikeOriginal()
        {
            if (_c2WallDambaPairCalibratorAnchorAV1LikeOriginal == null || _c2WallDambaPairCalibratorAnchorBV1LikeOriginal == null)
                return;

            Vector3 localDelta = _c2WallDambaPairCalibratorAnchorBV1LikeOriginal.localPosition -
                                 _c2WallDambaPairCalibratorAnchorAV1LikeOriginal.localPosition;
            if (Mathf.Abs(_c2WallDambaPairCalibratorPixelToWorldXV1LikeOriginal) > 0.000001f)
                _c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal.x = localDelta.x / _c2WallDambaPairCalibratorPixelToWorldXV1LikeOriginal;
            if (Mathf.Abs(_c2WallDambaPairCalibratorPixelToWorldZV1LikeOriginal) > 0.000001f)
                _c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal.y = localDelta.z / _c2WallDambaPairCalibratorPixelToWorldZV1LikeOriginal;

            float zScale = WallOriginalZUnitToWorldScaleV8LikeOriginal();
            if (Mathf.Abs(zScale) > 0.000001f)
                _c2WallDambaPairCalibratorDeltaHeightPixelsV1LikeOriginal = localDelta.y / zScale;
        }

        private void AttachWallDambaCalibratorMeshV1LikeOriginal(GameObject go, WallSpriteDescV1LikeOriginal desc, WallC2MParsedMeshV23LikeOriginal c2m, string label)
        {
            if (go == null || desc == null || c2m == null)
                return;

            Mesh mesh = BuildWallDambaCalibratorMeshV1LikeOriginal(desc, c2m, "C2_DAMBA_PAIR_CAL_" + label + "_" + desc.Name);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = mesh;
            ApplyWallRendererShadowContractV44LikeOriginal(mr);

            Texture2D tex = TryLoadWallC2MGPObjFrameTextureV42LikeOriginal(c2m, out _, out _);
            Material mat = CreateWallC2MModelMaterialV26LikeOriginal(tex, desc);
            if (mat != null)
            {
                mat.name = "C2_DAMBA_PAIR_CAL_MAT_" + label + "_" + desc.Name;
                mat.renderQueue = 3990;
                if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.Always);
                if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            }
            mr.sharedMaterial = mat;
        }

        private Mesh BuildWallDambaCalibratorMeshV1LikeOriginal(WallSpriteDescV1LikeOriginal desc, WallC2MParsedMeshV23LikeOriginal c2m, string name)
        {
            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            Vector3[] verts = new Vector3[c2m.Vertices.Length];
            Vector3 center = c2m.HasLocalBounds ? (c2m.LocalBoundsMin + c2m.LocalBoundsMax) * 0.5f : Vector3.zero;
            for (int i = 0; i < c2m.Vertices.Length; i++)
            {
                Vector3 local = c2m.Vertices[i] - center;
                verts[i] = new Vector3(
                    local.x * (kernel.BackingStepXWorld / 32.0f),
                    local.z * kernel.HeightScale,
                    local.y * (kernel.BackingStepZWorld * WorldZSign / 32.0f));
            }

            Mesh mesh = TryBuildWallC2MGPObjDrawWChunkBakedMeshV50LikeOriginal(c2m, verts, desc, out _);
            if (mesh != null)
            {
                mesh.name = name + "_DrawWChunk";
                return mesh;
            }

            mesh = new Mesh { name = name };
            if (verts.Length > 65000)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.triangles = c2m.Triangles;
            if (c2m.UV != null && c2m.UV.Length == verts.Length)
                mesh.uv = c2m.UV;
            if (c2m.Colors != null && c2m.Colors.Length == verts.Length)
                mesh.colors32 = c2m.Colors;
            mesh.RecalculateBounds();
            try { mesh.RecalculateNormals(); } catch { }
            return mesh;
        }

        private Bounds BuildWallDambaCalibratorLocalBoundsV1LikeOriginal(WallC2MParsedMeshV23LikeOriginal c2m, float zScale)
        {
            if (c2m == null || c2m.Vertices == null || c2m.Vertices.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one);
            Vector3 min = c2m.Vertices[0];
            Vector3 max = c2m.Vertices[0];
            for (int i = 1; i < c2m.Vertices.Length; i++)
            {
                min = Vector3.Min(min, c2m.Vertices[i]);
                max = Vector3.Max(max, c2m.Vertices[i]);
            }
            Vector3 size = max - min;
            return new Bounds((min + max) * 0.5f, new Vector3(size.x, size.z * zScale, size.y));
        }

        private Vector3 WallDambaCalibratorPixelDeltaToWorldV1LikeOriginal(Vector2 px)
        {
            return new Vector3(
                px.x * _c2WallDambaPairCalibratorPixelToWorldXV1LikeOriginal,
                0.0f,
                px.y * _c2WallDambaPairCalibratorPixelToWorldZV1LikeOriginal);
        }

        private void WriteWallDambaPairCalibratorFileV1LikeOriginal()
        {
            try
            {
                SyncWallDambaPairCalibratorDeltaFromTransformsV1LikeOriginal();
                Vector2 delta = _c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal;
                Vector2 connectorA = GetWallConnectorPointForCalibratorV1LikeOriginal(_c2WallDambaPairCalibratorDescAV1LikeOriginal, true);
                Vector2 connectorB = GetWallConnectorPointForCalibratorV1LikeOriginal(_c2WallDambaPairCalibratorDescBV1LikeOriginal, false);
                string path = ResolveWallDambaPairCalibratorOutputPathV1LikeOriginal();

                var sb = new StringBuilder(1024);
                sb.AppendLine("# C2 DAMBA pair calibration V1");
                sb.AppendLine("map=" + (_mapRelativePath ?? string.Empty));
                sb.AppendLine("objectA=" + FormatWallDambaCalibratorDescV1LikeOriginal(_c2WallDambaPairCalibratorDescAV1LikeOriginal));
                sb.AppendLine("objectB=" + FormatWallDambaCalibratorDescV1LikeOriginal(_c2WallDambaPairCalibratorDescBV1LikeOriginal));
                sb.AppendLine("deltaPixels.x=" + delta.x.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("deltaPixels.y=" + delta.y.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("deltaHeightPixels=" + _c2WallDambaPairCalibratorDeltaHeightPixelsV1LikeOriginal.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("connectorA.local.x=" + connectorA.x.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("connectorA.local.y=" + connectorA.y.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("connectorB.local.x=" + connectorB.x.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("connectorB.local.y=" + connectorB.y.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine("rule=place B at A + deltaPixels; A.connectorRight should coincide with B.connectorLeft after manual calibration");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                string projectPath = ResolveWallDambaPairCalibratorProjectOutputPathV1LikeOriginal();
                Directory.CreateDirectory(Path.GetDirectoryName(projectPath));
                File.WriteAllText(projectPath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError("[C2:DAMBA PAIR CAL V1] write failed:\n" + ex);
            }
        }

        private void AutoWriteWallDambaPairCalibratorSnapshotV1LikeOriginal()
        {
            if (_c2WallDambaPairCalibratorAnchorAV1LikeOriginal == null ||
                _c2WallDambaPairCalibratorAnchorBV1LikeOriginal == null)
                return;

            float now = Time.realtimeSinceStartup;
            if (now - _c2WallDambaPairCalibratorLastAutoWriteTimeV1LikeOriginal < 2.0f)
                return;

            Vector3 a = _c2WallDambaPairCalibratorAnchorAV1LikeOriginal.localPosition;
            Vector3 b = _c2WallDambaPairCalibratorAnchorBV1LikeOriginal.localPosition;
            bool first = float.IsNaN(_c2WallDambaPairCalibratorLastAutoWriteALocalV1LikeOriginal.x);
            bool moved =
                first ||
                (a - _c2WallDambaPairCalibratorLastAutoWriteALocalV1LikeOriginal).sqrMagnitude > 0.000001f ||
                (b - _c2WallDambaPairCalibratorLastAutoWriteBLocalV1LikeOriginal).sqrMagnitude > 0.000001f;
            if (!moved)
                return;

            _c2WallDambaPairCalibratorLastAutoWriteTimeV1LikeOriginal = now;
            _c2WallDambaPairCalibratorLastAutoWriteALocalV1LikeOriginal = a;
            _c2WallDambaPairCalibratorLastAutoWriteBLocalV1LikeOriginal = b;
            WriteWallDambaPairCalibratorFileV1LikeOriginal();
        }

        private string ResolveWallDambaPairCalibratorOutputPathV1LikeOriginal()
        {
            string mapPath = _mapRelativePath ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(mapPath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "current_map";
            string dir = Path.Combine(Application.persistentDataPath, "C2WallCalibration");
            return Path.Combine(dir, fileName + "_damba_pair_calibration.txt");
        }

        private void LoadWallDambaPairCalibratorFileIfPresentV1LikeOriginal()
        {
            string projectPath = ResolveWallDambaPairCalibratorProjectOutputPathV1LikeOriginal();
            string persistentPath = ResolveWallDambaPairCalibratorOutputPathV1LikeOriginal();
            string path = File.Exists(projectPath) ? projectPath : (File.Exists(persistentPath) ? persistentPath : null);
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                float dx = _c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal.x;
                float dy = _c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal.y;
                float dh = _c2WallDambaPairCalibratorDeltaHeightPixelsV1LikeOriginal;
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i] ?? string.Empty;
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;

                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                        continue;

                    if (string.Equals(key, "deltaPixels.x", StringComparison.OrdinalIgnoreCase))
                        dx = f;
                    else if (string.Equals(key, "deltaPixels.y", StringComparison.OrdinalIgnoreCase))
                        dy = f;
                    else if (string.Equals(key, "deltaHeightPixels", StringComparison.OrdinalIgnoreCase))
                        dh = f;
                }

                _c2WallDambaPairCalibratorDeltaPixelsV1LikeOriginal = new Vector2(dx, dy);
                _c2WallDambaPairCalibratorDeltaHeightPixelsV1LikeOriginal = dh;
            }
            catch (Exception ex)
            {
            }
        }

        private string ResolveWallDambaPairCalibratorProjectOutputPathV1LikeOriginal()
        {
            string mapPath = _mapRelativePath ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(mapPath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "current_map";
            string dir = Path.Combine(Application.dataPath, "Cossacks2Bridge", "Maps", "C2WallCalibration");
            return Path.Combine(dir, fileName + "_damba_pair_calibration.txt");
        }

        private static Vector2 GetWallConnectorPointForCalibratorV1LikeOriginal(WallSpriteDescV1LikeOriginal desc, bool right)
        {
            if (desc == null)
                return Vector2.zero;
            if (right && desc.RightEdges.Count > 0)
                return new Vector2(desc.RightEdges[0].X, 2.0f * desc.RightEdges[0].Y);
            if (!right && desc.LeftEdges.Count > 0)
                return new Vector2(desc.LeftEdges[0].X, 2.0f * desc.LeftEdges[0].Y);
            return new Vector2(desc.Width, 2.0f * desc.Height);
        }

        private static string FormatWallDambaCalibratorDescV1LikeOriginal(WallSpriteDescV1LikeOriginal desc)
        {
            if (desc == null)
                return "null";
            return "W" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) + " " + desc.Name + " model=" + (desc.ModelPath ?? string.Empty);
        }

        private static string FormatVector2V1LikeOriginal(Vector2 v)
        {
            return "(" + v.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + v.y.ToString("0.###", CultureInfo.InvariantCulture) + ")";
        }

        private static bool IsWallDambaCalibratorFastV1LikeOriginal()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
            return false;
#endif
        }

        private static bool WasWallCalKeyDownV1LikeOriginal(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }
    }
}
