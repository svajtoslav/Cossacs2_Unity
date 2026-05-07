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
        private const bool C2WallUniversalAnchorCalibratorV1EnabledLikeOriginal = false; // V2: disabled after anchor TXT is created; line calibrator uses saved anchors
        private const int C2WallUniversalAnchorCalibratorV1SpriteIndexLikeOriginal = 60;
        private const float C2WallUniversalAnchorCalibratorV1HeightBodiesLikeOriginal = 10.0f;
        private const float C2WallUniversalAnchorCalibratorV1PointSizeLikeOriginal = 0.095f;
        private const string C2WallUniversalAnchorCalibratorV1ContractLikeOriginal =
            "ONE_MAIN_MODEL_WITH_4_SCENE_ONLY_LOCAL_ANCHORS_EXPORT_RELATIVE_TO_MODEL";

        private GameObject _c2WallUniversalAnchorCalibratorRootV1LikeOriginal;
        private Transform _c2WallUniversalAnchorCalibratorModelV1LikeOriginal;
        private readonly Transform[] _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal = new Transform[4];
        private WallSpriteDescV1LikeOriginal _c2WallUniversalAnchorCalibratorDescV1LikeOriginal;
        private float _c2WallUniversalAnchorCalibratorPixelToWorldXV1LikeOriginal = 0.5f;
        private float _c2WallUniversalAnchorCalibratorPixelToWorldZV1LikeOriginal = -0.5f;
        private float _c2WallUniversalAnchorCalibratorHeightPixelToWorldYV1LikeOriginal = 1.0f;
        private Bounds _c2WallUniversalAnchorCalibratorModelLocalBoundsV1LikeOriginal;
        private float _c2WallUniversalAnchorCalibratorLastWriteTimeV1LikeOriginal = -1000.0f;

        private static readonly string[] C2WallUniversalAnchorPointNamesV1LikeOriginal =
        {
            "P0_LEFT_RED",
            "P1_RIGHT_GREEN",
            "P2_BACK_BLUE",
            "P3_FRONT_YELLOW"
        };

        private static readonly Color[] C2WallUniversalAnchorPointColorsV1LikeOriginal =
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow
        };

        private void BuildWallUniversalAnchorCalibratorV1LikeOriginal()
        {
            if (!C2WallUniversalAnchorCalibratorV1EnabledLikeOriginal || _map == null || _terrainRoot == null)
                return;

            if (_c2WallUniversalAnchorCalibratorRootV1LikeOriginal != null)
                SafeDestroy(_c2WallUniversalAnchorCalibratorRootV1LikeOriginal);

            WallSpriteCatalogV1LikeOriginal catalog = LoadWallSpriteCatalogV1LikeOriginal();
            if (catalog == null ||
                !catalog.ByIndex.TryGetValue(C2WallUniversalAnchorCalibratorV1SpriteIndexLikeOriginal, out _c2WallUniversalAnchorCalibratorDescV1LikeOriginal) ||
                _c2WallUniversalAnchorCalibratorDescV1LikeOriginal == null)
            {
                return;
            }

            WallC2MParsedMeshV23LikeOriginal c2m =
                TryLoadWallC2MVisualMeshV23LikeOriginal(_c2WallUniversalAnchorCalibratorDescV1LikeOriginal.ModelPath, out string audit);
            if (c2m == null)
            {
                return;
            }

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            _c2WallUniversalAnchorCalibratorPixelToWorldXV1LikeOriginal = kernel.BackingStepXWorld / 32.0f;
            _c2WallUniversalAnchorCalibratorPixelToWorldZV1LikeOriginal = kernel.BackingStepZWorld * WorldZSign / 32.0f;
            _c2WallUniversalAnchorCalibratorHeightPixelToWorldYV1LikeOriginal = WallOriginalZUnitToWorldScaleV8LikeOriginal();

            Bounds sourceBounds = BuildWallDambaCalibratorLocalBoundsV1LikeOriginal(c2m, kernel.HeightScale);
            float bodyHeight = Mathf.Max(8.0f, sourceBounds.size.y);
            Vector3 center = _terrainBounds.center;
            center.y = _terrainBounds.max.y + bodyHeight * C2WallUniversalAnchorCalibratorV1HeightBodiesLikeOriginal;

            _c2WallUniversalAnchorCalibratorRootV1LikeOriginal = new GameObject("C2_WALL_UNIVERSAL_ANCHOR_CALIBRATOR_V1");
            _c2WallUniversalAnchorCalibratorRootV1LikeOriginal.transform.SetParent(transform, false);
            _c2WallUniversalAnchorCalibratorRootV1LikeOriginal.transform.position = center;

            _c2WallUniversalAnchorCalibratorModelV1LikeOriginal =
                new GameObject("MAIN_W" +
                               _c2WallUniversalAnchorCalibratorDescV1LikeOriginal.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                               "_" + (_c2WallUniversalAnchorCalibratorDescV1LikeOriginal.Name ?? "wall")).transform;
            _c2WallUniversalAnchorCalibratorModelV1LikeOriginal.SetParent(_c2WallUniversalAnchorCalibratorRootV1LikeOriginal.transform, false);
            _c2WallUniversalAnchorCalibratorModelV1LikeOriginal.localPosition = Vector3.zero;
            _c2WallUniversalAnchorCalibratorModelV1LikeOriginal.localRotation = Quaternion.identity;
            _c2WallUniversalAnchorCalibratorModelV1LikeOriginal.localScale = Vector3.one;

            AttachWallDambaCalibratorMeshV1LikeOriginal(
                _c2WallUniversalAnchorCalibratorModelV1LikeOriginal.gameObject,
                _c2WallUniversalAnchorCalibratorDescV1LikeOriginal,
                c2m,
                "UNIVERSAL_MAIN");

            MeshFilter mf = _c2WallUniversalAnchorCalibratorModelV1LikeOriginal.GetComponent<MeshFilter>();
            _c2WallUniversalAnchorCalibratorModelLocalBoundsV1LikeOriginal =
                mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one * bodyHeight);

            CreateWallUniversalAnchorPointsV1LikeOriginal();
            LoadWallUniversalAnchorCalibratorFileIfPresentV1LikeOriginal();

        }

        private void UpdateWallUniversalAnchorCalibratorV1LikeOriginal()
        {
            if (!C2WallUniversalAnchorCalibratorV1EnabledLikeOriginal ||
                _c2WallUniversalAnchorCalibratorRootV1LikeOriginal == null)
                return;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                WriteWallUniversalAnchorCalibratorFileV1LikeOriginal();

            if (Input.GetKeyDown(KeyCode.Backspace))
                ResetWallUniversalAnchorPointsV1LikeOriginal();
#endif
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            DrawSyntheticDambaSavePoseButtonV93LikeOriginal();

            if (!C2WallUniversalAnchorCalibratorV1EnabledLikeOriginal ||
                _c2WallUniversalAnchorCalibratorRootV1LikeOriginal == null)
                return;

            GUILayout.BeginArea(new Rect(12, 92, 285, 118), "C2 Universal Anchors V1", GUI.skin.window);
            GUILayout.Label("One model + 4 scene-only points");
            if (GUILayout.Button("WRITE 4 ANCHORS TXT", GUILayout.Height(26)))
                WriteWallUniversalAnchorCalibratorFileV1LikeOriginal();
            if (GUILayout.Button("RESET POINTS TO BOUNDS", GUILayout.Height(22)))
                ResetWallUniversalAnchorPointsV1LikeOriginal();
            GUILayout.Label("Scene: drag colored spheres/handles");
            GUILayout.EndArea();
        }

        private void OnWallUniversalAnchorCalibratorSceneGuiV1LikeOriginal(SceneView sceneView)
        {
            if (!C2WallUniversalAnchorCalibratorV1EnabledLikeOriginal ||
                _c2WallUniversalAnchorCalibratorRootV1LikeOriginal == null ||
                _c2WallUniversalAnchorCalibratorModelV1LikeOriginal == null)
                return;

            DrawWallUniversalAnchorCalibratorSceneGuiV1LikeOriginal(sceneView);
            DrawWallUniversalAnchorCalibratorPointsV1LikeOriginal(sceneView);
        }

        private void DrawWallUniversalAnchorCalibratorSceneGuiV1LikeOriginal(SceneView sceneView)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(12, 92, 285, 118), "C2 Universal Anchors V1", GUI.skin.window);
            GUILayout.Label("Move 4 colored anchors on model edges");
            if (GUILayout.Button("WRITE 4 ANCHORS TXT", GUILayout.Height(26)))
                WriteWallUniversalAnchorCalibratorFileV1LikeOriginal();
            if (GUILayout.Button("RESET POINTS TO BOUNDS", GUILayout.Height(22)))
            {
                ResetWallUniversalAnchorPointsV1LikeOriginal();
                sceneView.Repaint();
            }
            GUILayout.Label("TXT: local world + local pixels + height");
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void DrawWallUniversalAnchorCalibratorPointsV1LikeOriginal(SceneView sceneView)
        {
            Transform model = _c2WallUniversalAnchorCalibratorModelV1LikeOriginal;
            Vector3 previous = Vector3.zero;
            bool hasPrevious = false;

            for (int i = 0; i < _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal.Length; i++)
            {
                Transform point = _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal[i];
                if (point == null)
                    continue;

                Color c = C2WallUniversalAnchorPointColorsV1LikeOriginal[Mathf.Clamp(i, 0, C2WallUniversalAnchorPointColorsV1LikeOriginal.Length - 1)];
                Vector3 world = point.position;
                float handleSize = HandleUtility.GetHandleSize(world) * C2WallUniversalAnchorCalibratorV1PointSizeLikeOriginal;

                Handles.color = c;
                Handles.SphereHandleCap(0, world, Quaternion.identity, handleSize, EventType.Repaint);
                Handles.Label(world + Vector3.up * handleSize * 1.65f, point.name);

                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.PositionHandle(world, point.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(point, "Move C2 wall universal anchor");
                    point.position = moved;
                    EditorUtility.SetDirty(point);
                    sceneView.Repaint();
                }

                if (hasPrevious)
                {
                    Handles.color = Color.white;
                    Handles.DrawLine(previous, world);
                }

                previous = world;
                hasPrevious = true;
            }

            Transform p0 = _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal[0];
            Transform p3 = _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal[3];
            if (p0 != null && p3 != null)
            {
                Handles.color = Color.white;
                Handles.DrawLine(p3.position, p0.position);
            }

            if (model != null)
            {
                Handles.color = Color.cyan;
                Handles.Label(model.position + Vector3.up * HandleUtility.GetHandleSize(model.position) * 0.25f,
                    "MAIN W" + (_c2WallUniversalAnchorCalibratorDescV1LikeOriginal != null
                        ? _c2WallUniversalAnchorCalibratorDescV1LikeOriginal.SpriteIndex.ToString(CultureInfo.InvariantCulture)
                        : "?"));
            }
        }
#endif

        private void CreateWallUniversalAnchorPointsV1LikeOriginal()
        {
            if (_c2WallUniversalAnchorCalibratorModelV1LikeOriginal == null)
                return;

            for (int i = 0; i < _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal.Length; i++)
            {
                Transform old = _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal[i];
                if (old != null)
                    SafeDestroy(old.gameObject);

                var go = new GameObject(C2WallUniversalAnchorPointNamesV1LikeOriginal[i]);
                go.transform.SetParent(_c2WallUniversalAnchorCalibratorModelV1LikeOriginal, false);
                _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal[i] = go.transform;
            }

            ResetWallUniversalAnchorPointsV1LikeOriginal();
        }

        private void ResetWallUniversalAnchorPointsV1LikeOriginal()
        {
            if (_c2WallUniversalAnchorCalibratorModelV1LikeOriginal == null)
                return;

            Bounds b = _c2WallUniversalAnchorCalibratorModelLocalBoundsV1LikeOriginal;
            if (b.size.sqrMagnitude < 0.000001f)
                b = new Bounds(Vector3.zero, Vector3.one * 16.0f);

            float y = b.center.y;
            Vector3[] local =
            {
                new Vector3(b.min.x, y, b.center.z),
                new Vector3(b.max.x, y, b.center.z),
                new Vector3(b.center.x, y, b.min.z),
                new Vector3(b.center.x, y, b.max.z)
            };

            for (int i = 0; i < _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal.Length; i++)
            {
                Transform p = _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal[i];
                if (p == null)
                    continue;
                p.localPosition = local[i];
                p.localRotation = Quaternion.identity;
                p.localScale = Vector3.one;
            }
        }

        private void WriteWallUniversalAnchorCalibratorFileV1LikeOriginal()
        {
            try
            {
                if (_c2WallUniversalAnchorCalibratorRootV1LikeOriginal == null ||
                    _c2WallUniversalAnchorCalibratorModelV1LikeOriginal == null)
                    return;

                string path = ResolveWallUniversalAnchorCalibratorOutputPathV1LikeOriginal();
                string projectPath = ResolveWallUniversalAnchorCalibratorProjectOutputPathV1LikeOriginal();

                string text = BuildWallUniversalAnchorCalibratorTextV1LikeOriginal();

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, text, Encoding.UTF8);

                Directory.CreateDirectory(Path.GetDirectoryName(projectPath));
                File.WriteAllText(projectPath, text, Encoding.UTF8);

                _c2WallUniversalAnchorCalibratorLastWriteTimeV1LikeOriginal = Time.realtimeSinceStartup;
            }
            catch (Exception ex)
            {
                Debug.LogError("[C2:WALL ANCHOR CAL V1] write failed:\n" + ex);
            }
        }

        private string BuildWallUniversalAnchorCalibratorTextV1LikeOriginal()
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("# C2 WALL universal anchor calibration V1");
            sb.AppendLine("# This file stores 4 local anchor points relative to ONE main 3D model.");
            sb.AppendLine("# Points are scene-only helpers; they have no renderer and must not appear in Game view.");
            sb.AppendLine("contract=" + C2WallUniversalAnchorCalibratorV1ContractLikeOriginal);
            sb.AppendLine("map=" + (_mapRelativePath ?? string.Empty));

            if (_c2WallUniversalAnchorCalibratorDescV1LikeOriginal != null)
            {
                sb.AppendLine("sprite.index=" + _c2WallUniversalAnchorCalibratorDescV1LikeOriginal.SpriteIndex.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("sprite.name=" + (_c2WallUniversalAnchorCalibratorDescV1LikeOriginal.Name ?? string.Empty));
                sb.AppendLine("model.path=" + (_c2WallUniversalAnchorCalibratorDescV1LikeOriginal.ModelPath ?? string.Empty));
                sb.AppendLine("sprite.width=" + _c2WallUniversalAnchorCalibratorDescV1LikeOriginal.Width.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("sprite.height=" + _c2WallUniversalAnchorCalibratorDescV1LikeOriginal.Height.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine("pixelToWorld.x=" + _c2WallUniversalAnchorCalibratorPixelToWorldXV1LikeOriginal.ToString("0.######", CultureInfo.InvariantCulture));
            sb.AppendLine("pixelToWorld.z=" + _c2WallUniversalAnchorCalibratorPixelToWorldZV1LikeOriginal.ToString("0.######", CultureInfo.InvariantCulture));
            sb.AppendLine("heightPixelToWorld.y=" + _c2WallUniversalAnchorCalibratorHeightPixelToWorldYV1LikeOriginal.ToString("0.######", CultureInfo.InvariantCulture));
            sb.AppendLine("root.world.x=" + _c2WallUniversalAnchorCalibratorRootV1LikeOriginal.transform.position.x.ToString("0.######", CultureInfo.InvariantCulture));
            sb.AppendLine("root.world.y=" + _c2WallUniversalAnchorCalibratorRootV1LikeOriginal.transform.position.y.ToString("0.######", CultureInfo.InvariantCulture));
            sb.AppendLine("root.world.z=" + _c2WallUniversalAnchorCalibratorRootV1LikeOriginal.transform.position.z.ToString("0.######", CultureInfo.InvariantCulture));
            sb.AppendLine("point.count=4");

            for (int i = 0; i < _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal.Length; i++)
            {
                Transform p = _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal[i];
                if (p == null)
                    continue;

                Vector3 local = p.localPosition;
                Vector3 world = p.position;
                Vector2 localPixels = WallUniversalAnchorLocalWorldToPixelV1LikeOriginal(local);
                float heightPixels = Mathf.Abs(_c2WallUniversalAnchorCalibratorHeightPixelToWorldYV1LikeOriginal) > 0.000001f
                    ? local.y / _c2WallUniversalAnchorCalibratorHeightPixelToWorldYV1LikeOriginal
                    : 0.0f;

                string prefix = "point" + i.ToString(CultureInfo.InvariantCulture);
                sb.AppendLine(prefix + ".name=" + p.name);
                sb.AppendLine(prefix + ".localWorld.x=" + local.x.ToString("0.######", CultureInfo.InvariantCulture));
                sb.AppendLine(prefix + ".localWorld.y=" + local.y.ToString("0.######", CultureInfo.InvariantCulture));
                sb.AppendLine(prefix + ".localWorld.z=" + local.z.ToString("0.######", CultureInfo.InvariantCulture));
                sb.AppendLine(prefix + ".localPixels.x=" + localPixels.x.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine(prefix + ".localPixels.y=" + localPixels.y.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine(prefix + ".heightPixels=" + heightPixels.ToString("0.###", CultureInfo.InvariantCulture));
                sb.AppendLine(prefix + ".world.x=" + world.x.ToString("0.######", CultureInfo.InvariantCulture));
                sb.AppendLine(prefix + ".world.y=" + world.y.ToString("0.######", CultureInfo.InvariantCulture));
                sb.AppendLine(prefix + ".world.z=" + world.z.ToString("0.######", CultureInfo.InvariantCulture));
            }

            sb.AppendLine("rule.stage1=manually place four anchors on meaningful model edges");
            sb.AppendLine("rule.stage2=spawn three objects and save transform offsets by matching selected anchor names");
            sb.AppendLine("rule.stage3=map placement ignores sky height; final objects are snapped by anchors, not by raw object pivots");
            return sb.ToString();
        }

        private Vector2 WallUniversalAnchorLocalWorldToPixelV1LikeOriginal(Vector3 local)
        {
            float px = Mathf.Abs(_c2WallUniversalAnchorCalibratorPixelToWorldXV1LikeOriginal) > 0.000001f
                ? local.x / _c2WallUniversalAnchorCalibratorPixelToWorldXV1LikeOriginal
                : 0.0f;

            float py = Mathf.Abs(_c2WallUniversalAnchorCalibratorPixelToWorldZV1LikeOriginal) > 0.000001f
                ? local.z / _c2WallUniversalAnchorCalibratorPixelToWorldZV1LikeOriginal
                : 0.0f;

            return new Vector2(px, py);
        }

        private string ResolveWallUniversalAnchorCalibratorOutputPathV1LikeOriginal()
        {
            string mapPath = _mapRelativePath ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(mapPath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "current_map";

            string dir = Path.Combine(Application.persistentDataPath, "C2WallCalibration");
            return Path.Combine(dir, fileName + "_wall_universal_anchors_v1.txt");
        }

        private string ResolveWallUniversalAnchorCalibratorProjectOutputPathV1LikeOriginal()
        {
            string mapPath = _mapRelativePath ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(mapPath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "current_map";

            string dir = Path.Combine(Application.dataPath, "Cossacks2Bridge", "Maps", "C2WallCalibration");
            return Path.Combine(dir, fileName + "_wall_universal_anchors_v1.txt");
        }

        private void LoadWallUniversalAnchorCalibratorFileIfPresentV1LikeOriginal()
        {
            string projectPath = ResolveWallUniversalAnchorCalibratorProjectOutputPathV1LikeOriginal();
            string persistentPath = ResolveWallUniversalAnchorCalibratorOutputPathV1LikeOriginal();
            string path = File.Exists(projectPath) ? projectPath : (File.Exists(persistentPath) ? persistentPath : null);
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                var parsed = new Vector3[4];
                var has = new bool[4];

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i] ?? string.Empty;
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;

                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();

                    for (int p = 0; p < 4; p++)
                    {
                        string prefix = "point" + p.ToString(CultureInfo.InvariantCulture) + ".localWorld.";
                        if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                            continue;

                        has[p] = true;
                        string axis = key.Substring(prefix.Length);
                        if (string.Equals(axis, "x", StringComparison.OrdinalIgnoreCase))
                            parsed[p].x = f;
                        else if (string.Equals(axis, "y", StringComparison.OrdinalIgnoreCase))
                            parsed[p].y = f;
                        else if (string.Equals(axis, "z", StringComparison.OrdinalIgnoreCase))
                            parsed[p].z = f;
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    Transform t = _c2WallUniversalAnchorCalibratorPointsV1LikeOriginal[i];
                    if (t != null && has[i])
                        t.localPosition = parsed[i];
                }

            }
            catch (Exception ex)
            {
            }
        }
    }
}
