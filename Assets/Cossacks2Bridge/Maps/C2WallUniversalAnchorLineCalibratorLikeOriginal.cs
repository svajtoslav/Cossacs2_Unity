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
        private const bool C2WallUniversalAnchorLineCalibratorV2EnabledLikeOriginal = false; // V3: disabled after calibration TXT is saved; runtime DAMBA chain now consumes the TXT
        private const int C2WallUniversalAnchorLineCalibratorV2SpriteIndexLikeOriginal = 60;
        private const float C2WallUniversalAnchorLineCalibratorV2HeightBodiesLikeOriginal = 10.0f;
        private const float C2WallUniversalAnchorLineCalibratorV2PointSizeLikeOriginal = 0.075f;
        private const string C2WallUniversalAnchorLineCalibratorV2ContractLikeOriginal =
            "THREE_OBJECT_LINE_CALIBRATION_CENTER_MAIN_SAVE_ANCHOR_TO_ANCHOR_RELATIONS";

        private GameObject _c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal;
        private readonly Transform[] _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal = new Transform[3];
        private readonly Transform[,] _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal = new Transform[3, 4];
        private readonly Vector3[] _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal = new Vector3[4];
        private readonly bool[] _c2WallUniversalAnchorLineCalibratorHasLocalAnchorV2LikeOriginal = new bool[4];
        private WallSpriteDescV1LikeOriginal _c2WallUniversalAnchorLineCalibratorDescV2LikeOriginal;
        private Bounds _c2WallUniversalAnchorLineCalibratorModelLocalBoundsV2LikeOriginal;
        private string _c2WallUniversalAnchorLineCalibratorLoadedAnchorPathV2LikeOriginal = string.Empty;

        private static readonly string[] C2WallUniversalAnchorLineObjectRolesV2LikeOriginal =
        {
            "LEFT",
            "CENTER_MAIN",
            "RIGHT"
        };

        private static readonly Color[] C2WallUniversalAnchorLineObjectColorsV2LikeOriginal =
        {
            new Color(0.15f, 0.85f, 1.0f, 1.0f),
            Color.white,
            new Color(1.0f, 0.35f, 1.0f, 1.0f)
        };

        private void BuildWallUniversalAnchorLineCalibratorV2LikeOriginal()
        {
            if (!C2WallUniversalAnchorLineCalibratorV2EnabledLikeOriginal || _map == null || _terrainRoot == null)
                return;

            if (_c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal != null)
                SafeDestroy(_c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal);

            WallSpriteCatalogV1LikeOriginal catalog = LoadWallSpriteCatalogV1LikeOriginal();
            if (catalog == null ||
                !catalog.ByIndex.TryGetValue(C2WallUniversalAnchorLineCalibratorV2SpriteIndexLikeOriginal, out _c2WallUniversalAnchorLineCalibratorDescV2LikeOriginal) ||
                _c2WallUniversalAnchorLineCalibratorDescV2LikeOriginal == null)
            {
                Debug.LogWarning("[C2:WALL ANCHOR LINE CAL V2] sprite W" +
                                 C2WallUniversalAnchorLineCalibratorV2SpriteIndexLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                                 " missing; line calibrator not spawned.");
                return;
            }

            WallC2MParsedMeshV23LikeOriginal c2m =
                TryLoadWallC2MVisualMeshV23LikeOriginal(_c2WallUniversalAnchorLineCalibratorDescV2LikeOriginal.ModelPath, out string audit);
            if (c2m == null)
            {
                Debug.LogWarning("[C2:WALL ANCHOR LINE CAL V2] C2M load failed '" +
                                 (_c2WallUniversalAnchorLineCalibratorDescV2LikeOriginal.ModelPath ?? string.Empty) +
                                 "' audit='" + audit + "'");
                return;
            }

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            Bounds sourceBounds = BuildWallDambaCalibratorLocalBoundsV1LikeOriginal(c2m, kernel.HeightScale);
            _c2WallUniversalAnchorLineCalibratorModelLocalBoundsV2LikeOriginal = sourceBounds;
            float bodyHeight = Mathf.Max(8.0f, sourceBounds.size.y);
            Vector3 center = _terrainBounds.center;
            center.y = _terrainBounds.max.y + bodyHeight * C2WallUniversalAnchorLineCalibratorV2HeightBodiesLikeOriginal;

            _c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal = new GameObject("C2_WALL_UNIVERSAL_ANCHOR_LINE_CALIBRATOR_V2");
            _c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal.transform.SetParent(transform, false);
            _c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal.transform.position = center;

            bool loadedAnchors = LoadWallUniversalAnchorLocalPointsForLineV2LikeOriginal();

            for (int i = 0; i < 3; i++)
            {
                Transform model = new GameObject(C2WallUniversalAnchorLineObjectRolesV2LikeOriginal[i] + "_W" +
                                                 _c2WallUniversalAnchorLineCalibratorDescV2LikeOriginal.SpriteIndex.ToString(CultureInfo.InvariantCulture)).transform;
                model.SetParent(_c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal.transform, false);
                model.localRotation = Quaternion.identity;
                model.localScale = Vector3.one;
                _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[i] = model;

                AttachWallDambaCalibratorMeshV1LikeOriginal(
                    model.gameObject,
                    _c2WallUniversalAnchorLineCalibratorDescV2LikeOriginal,
                    c2m,
                    "ANCHOR_LINE_V2_" + C2WallUniversalAnchorLineObjectRolesV2LikeOriginal[i]);

                CreateWallUniversalAnchorLinePointsForObjectV2LikeOriginal(i);
            }

            MeshFilter mf = _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[1] != null
                ? _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[1].GetComponent<MeshFilter>()
                : null;
            _c2WallUniversalAnchorLineCalibratorModelLocalBoundsV2LikeOriginal =
                mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one * bodyHeight);

            ResetWallUniversalAnchorLineObjectsV2LikeOriginal();
            LoadWallUniversalAnchorLineCalibratorFileIfPresentV2LikeOriginal();

            Debug.Log("[C2:WALL ANCHOR LINE CAL V2] spawned 3 objects above map center. " +
                      "Move LEFT/CENTER/RIGHT in Scene view, align anchors, press WRITE 3 OBJECT LINK TXT. " +
                      "anchorsLoaded=" + loadedAnchors + " source='" + _c2WallUniversalAnchorLineCalibratorLoadedAnchorPathV2LikeOriginal.Replace('\\', '/') + "'");
        }

        private void UpdateWallUniversalAnchorLineCalibratorV2LikeOriginal()
        {
            if (!C2WallUniversalAnchorLineCalibratorV2EnabledLikeOriginal ||
                _c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal == null)
                return;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                WriteWallUniversalAnchorLineCalibratorFileV2LikeOriginal();

            if (Input.GetKeyDown(KeyCode.Backspace))
                ResetWallUniversalAnchorLineObjectsV2LikeOriginal();
#endif
        }

#if UNITY_EDITOR
        private void OnWallUniversalAnchorLineCalibratorSceneGuiV2LikeOriginal(SceneView sceneView)
        {
            if (!C2WallUniversalAnchorLineCalibratorV2EnabledLikeOriginal ||
                _c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal == null)
                return;

            DrawWallUniversalAnchorLineCalibratorSceneGuiV2LikeOriginal(sceneView);
            DrawWallUniversalAnchorLineCalibratorObjectsV2LikeOriginal(sceneView);
        }

        private void DrawWallUniversalAnchorLineCalibratorSceneGuiV2LikeOriginal(SceneView sceneView)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(12, 218, 330, 152), "C2 Anchor Line Cal V2", GUI.skin.window);
            GUILayout.Label("3 objects: LEFT / CENTER_MAIN / RIGHT");
            if (GUILayout.Button("WRITE 3 OBJECT LINK TXT", GUILayout.Height(26)))
                WriteWallUniversalAnchorLineCalibratorFileV2LikeOriginal();
            if (GUILayout.Button("SNAP L/R TO CENTER ANCHORS", GUILayout.Height(22)))
            {
                SnapWallUniversalAnchorLineSideObjectsV2LikeOriginal();
                sceneView.Repaint();
            }
            if (GUILayout.Button("RESET 3 OBJECTS FROM ANCHORS", GUILayout.Height(22)))
            {
                ResetWallUniversalAnchorLineObjectsV2LikeOriginal();
                sceneView.Repaint();
            }
            GUILayout.Label("Left: LEFT.P1 -> CENTER.P0");
            GUILayout.Label("Right: RIGHT.P0 -> CENTER.P1");
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void DrawWallUniversalAnchorLineCalibratorObjectsV2LikeOriginal(SceneView sceneView)
        {
            for (int i = 0; i < 3; i++)
            {
                Transform model = _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[i];
                if (model == null)
                    continue;

                Color objectColor = C2WallUniversalAnchorLineObjectColorsV2LikeOriginal[Mathf.Clamp(i, 0, C2WallUniversalAnchorLineObjectColorsV2LikeOriginal.Length - 1)];
                Handles.color = objectColor;
                Handles.Label(model.position + Vector3.up * HandleUtility.GetHandleSize(model.position) * 0.25f,
                    C2WallUniversalAnchorLineObjectRolesV2LikeOriginal[i]);

                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.PositionHandle(model.position, model.rotation);
                Quaternion rotated = Handles.RotationHandle(model.rotation, model.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(model, "Move C2 wall anchor line object");
                    model.position = moved;
                    model.rotation = rotated;
                    EditorUtility.SetDirty(model);
                    sceneView.Repaint();
                }

                DrawWallUniversalAnchorLineObjectAnchorsV2LikeOriginal(i);
            }

            DrawWallUniversalAnchorLineLinksV2LikeOriginal();
        }

        private void DrawWallUniversalAnchorLineObjectAnchorsV2LikeOriginal(int objectIndex)
        {
            Vector3 previous = Vector3.zero;
            bool hasPrevious = false;
            for (int p = 0; p < 4; p++)
            {
                Transform point = _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[objectIndex, p];
                if (point == null)
                    continue;

                Color c = C2WallUniversalAnchorPointColorsV1LikeOriginal[Mathf.Clamp(p, 0, C2WallUniversalAnchorPointColorsV1LikeOriginal.Length - 1)];
                Vector3 world = point.position;
                float handleSize = HandleUtility.GetHandleSize(world) * C2WallUniversalAnchorLineCalibratorV2PointSizeLikeOriginal;
                Handles.color = c;
                Handles.SphereHandleCap(0, world, Quaternion.identity, handleSize, EventType.Repaint);
                Handles.Label(world + Vector3.up * handleSize * 1.4f,
                    C2WallUniversalAnchorLineObjectRolesV2LikeOriginal[objectIndex] + ".P" + p.ToString(CultureInfo.InvariantCulture));

                if (hasPrevious)
                {
                    Handles.color = Color.gray;
                    Handles.DrawLine(previous, world);
                }

                previous = world;
                hasPrevious = true;
            }

            Transform p0 = _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[objectIndex, 0];
            Transform p3 = _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[objectIndex, 3];
            if (p0 != null && p3 != null)
            {
                Handles.color = Color.gray;
                Handles.DrawLine(p3.position, p0.position);
            }
        }

        private void DrawWallUniversalAnchorLineLinksV2LikeOriginal()
        {
            Transform leftRight = _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[0, 1];
            Transform centerLeft = _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[1, 0];
            Transform centerRight = _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[1, 1];
            Transform rightLeft = _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[2, 0];

            if (leftRight != null && centerLeft != null)
            {
                Handles.color = Color.cyan;
                Handles.DrawLine(leftRight.position, centerLeft.position);
                Handles.Label((leftRight.position + centerLeft.position) * 0.5f, "LEFT.P1 -> CENTER.P0 err=" +
                    Vector3.Distance(leftRight.position, centerLeft.position).ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (rightLeft != null && centerRight != null)
            {
                Handles.color = Color.magenta;
                Handles.DrawLine(rightLeft.position, centerRight.position);
                Handles.Label((rightLeft.position + centerRight.position) * 0.5f, "RIGHT.P0 -> CENTER.P1 err=" +
                    Vector3.Distance(rightLeft.position, centerRight.position).ToString("0.###", CultureInfo.InvariantCulture));
            }
        }
#endif

        private void CreateWallUniversalAnchorLinePointsForObjectV2LikeOriginal(int objectIndex)
        {
            Transform model = _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[objectIndex];
            if (model == null)
                return;

            for (int p = 0; p < 4; p++)
            {
                var go = new GameObject(C2WallUniversalAnchorLineObjectRolesV2LikeOriginal[objectIndex] + "__" +
                                        C2WallUniversalAnchorPointNamesV1LikeOriginal[p]);
                go.transform.SetParent(model, false);
                go.transform.localPosition = _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[p];
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[objectIndex, p] = go.transform;
            }
        }

        private void ResetWallUniversalAnchorLineObjectsV2LikeOriginal()
        {
            Transform left = _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[0];
            Transform center = _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[1];
            Transform right = _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[2];
            if (left == null || center == null || right == null)
                return;

            center.localPosition = Vector3.zero;
            center.localRotation = Quaternion.identity;
            center.localScale = Vector3.one;

            left.localRotation = Quaternion.identity;
            left.localScale = Vector3.one;
            left.localPosition = _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[0] -
                                 _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[1];

            right.localRotation = Quaternion.identity;
            right.localScale = Vector3.one;
            right.localPosition = _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[1] -
                                  _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[0];
        }

        private void SnapWallUniversalAnchorLineSideObjectsV2LikeOriginal()
        {
            SnapWallUniversalAnchorLineObjectAnchorToTargetV2LikeOriginal(0, 1, 1, 0);
            SnapWallUniversalAnchorLineObjectAnchorToTargetV2LikeOriginal(2, 0, 1, 1);
        }

        private void SnapWallUniversalAnchorLineObjectAnchorToTargetV2LikeOriginal(int movingObject, int movingAnchor, int targetObject, int targetAnchor)
        {
            Transform model = _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[movingObject];
            Transform a = _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[movingObject, movingAnchor];
            Transform b = _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[targetObject, targetAnchor];
            if (model == null || a == null || b == null)
                return;

            model.position += b.position - a.position;
        }

        private bool LoadWallUniversalAnchorLocalPointsForLineV2LikeOriginal()
        {
            for (int i = 0; i < 4; i++)
            {
                _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[i] = Vector3.zero;
                _c2WallUniversalAnchorLineCalibratorHasLocalAnchorV2LikeOriginal[i] = false;
            }

            string projectPath = ResolveWallUniversalAnchorCalibratorProjectOutputPathV1LikeOriginal();
            string persistentPath = ResolveWallUniversalAnchorCalibratorOutputPathV1LikeOriginal();
            string path = File.Exists(projectPath) ? projectPath : (File.Exists(persistentPath) ? persistentPath : null);
            _c2WallUniversalAnchorLineCalibratorLoadedAnchorPathV2LikeOriginal = path ?? string.Empty;

            bool loaded = false;
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    string[] lines = File.ReadAllLines(path);
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

                            _c2WallUniversalAnchorLineCalibratorHasLocalAnchorV2LikeOriginal[p] = true;
                            string axis = key.Substring(prefix.Length);
                            if (string.Equals(axis, "x", StringComparison.OrdinalIgnoreCase))
                                _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[p].x = f;
                            else if (string.Equals(axis, "y", StringComparison.OrdinalIgnoreCase))
                                _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[p].y = f;
                            else if (string.Equals(axis, "z", StringComparison.OrdinalIgnoreCase))
                                _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[p].z = f;
                        }
                    }

                    loaded = true;
                    for (int p = 0; p < 4; p++)
                        loaded &= _c2WallUniversalAnchorLineCalibratorHasLocalAnchorV2LikeOriginal[p];
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[C2:WALL ANCHOR LINE CAL V2] anchor load failed '" + path.Replace('\\', '/') + "': " + ex.Message);
                    loaded = false;
                }
            }

            if (!loaded)
                FillWallUniversalAnchorLineFallbackPointsV2LikeOriginal();

            return loaded;
        }

        private void FillWallUniversalAnchorLineFallbackPointsV2LikeOriginal()
        {
            Bounds b = _c2WallUniversalAnchorLineCalibratorModelLocalBoundsV2LikeOriginal;
            if (b.size.sqrMagnitude < 0.000001f)
                b = new Bounds(Vector3.zero, Vector3.one * 16.0f);

            float y = b.center.y;
            _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[0] = new Vector3(b.min.x, y, b.center.z);
            _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[1] = new Vector3(b.max.x, y, b.center.z);
            _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[2] = new Vector3(b.center.x, y, b.min.z);
            _c2WallUniversalAnchorLineCalibratorLocalAnchorsV2LikeOriginal[3] = new Vector3(b.center.x, y, b.max.z);
            for (int i = 0; i < 4; i++)
                _c2WallUniversalAnchorLineCalibratorHasLocalAnchorV2LikeOriginal[i] = true;
        }

        private void WriteWallUniversalAnchorLineCalibratorFileV2LikeOriginal()
        {
            try
            {
                if (_c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal == null)
                    return;

                string text = BuildWallUniversalAnchorLineCalibratorTextV2LikeOriginal();
                string path = ResolveWallUniversalAnchorLineCalibratorOutputPathV2LikeOriginal();
                string projectPath = ResolveWallUniversalAnchorLineCalibratorProjectOutputPathV2LikeOriginal();

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, text, Encoding.UTF8);
                Directory.CreateDirectory(Path.GetDirectoryName(projectPath));
                File.WriteAllText(projectPath, text, Encoding.UTF8);

                Debug.Log("[C2:WALL ANCHOR LINE CAL V2] wrote " + path.Replace('\\', '/') + " and " + projectPath.Replace('\\', '/'));
            }
            catch (Exception ex)
            {
                Debug.LogError("[C2:WALL ANCHOR LINE CAL V2] write failed:\n" + ex);
            }
        }

        private string BuildWallUniversalAnchorLineCalibratorTextV2LikeOriginal()
        {
            var sb = new StringBuilder(8192);
            sb.AppendLine("# C2 WALL universal 3-object anchor link calibration V2");
            sb.AppendLine("# Stage1 file defines anchor points inside one model.");
            sb.AppendLine("# Stage2 file defines how same anchored models stand relative to each other.");
            sb.AppendLine("contract=" + C2WallUniversalAnchorLineCalibratorV2ContractLikeOriginal);
            sb.AppendLine("map=" + (_mapRelativePath ?? string.Empty));
            sb.AppendLine("source.anchor.file=" + (_c2WallUniversalAnchorLineCalibratorLoadedAnchorPathV2LikeOriginal ?? string.Empty).Replace('\\', '/'));
            if (_c2WallUniversalAnchorLineCalibratorDescV2LikeOriginal != null)
            {
                sb.AppendLine("sprite.index=" + _c2WallUniversalAnchorLineCalibratorDescV2LikeOriginal.SpriteIndex.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("sprite.name=" + (_c2WallUniversalAnchorLineCalibratorDescV2LikeOriginal.Name ?? string.Empty));
                sb.AppendLine("model.path=" + (_c2WallUniversalAnchorLineCalibratorDescV2LikeOriginal.ModelPath ?? string.Empty));
            }

            sb.AppendLine("object.count=3");
            for (int i = 0; i < 3; i++)
                AppendWallUniversalAnchorLineObjectTextV2LikeOriginal(sb, i);

            AppendWallUniversalAnchorLineLinkTextV2LikeOriginal(sb, "left", 0, 1, 1, 0);
            AppendWallUniversalAnchorLineLinkTextV2LikeOriginal(sb, "right", 2, 0, 1, 1);

            sb.AppendLine("rule.left=place LEFT so LEFT.P1_RIGHT_GREEN coincides with CENTER_MAIN.P0_LEFT_RED");
            sb.AppendLine("rule.right=place RIGHT so RIGHT.P0_LEFT_RED coincides with CENTER_MAIN.P1_RIGHT_GREEN");
            sb.AppendLine("rule.final=map placement must snap by anchor world points; object pivot is only carrier, not truth");
            return sb.ToString();
        }

        private void AppendWallUniversalAnchorLineObjectTextV2LikeOriginal(StringBuilder sb, int objectIndex)
        {
            Transform model = _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[objectIndex];
            if (model == null)
                return;

            string prefix = "object" + objectIndex.ToString(CultureInfo.InvariantCulture);
            sb.AppendLine(prefix + ".role=" + C2WallUniversalAnchorLineObjectRolesV2LikeOriginal[objectIndex]);
            AppendVector3LineV2LikeOriginal(sb, prefix + ".localWorld", model.localPosition);
            AppendVector3LineV2LikeOriginal(sb, prefix + ".world", model.position);
            AppendVector3LineV2LikeOriginal(sb, prefix + ".localEuler", model.localEulerAngles);
            AppendQuaternionLineV2LikeOriginal(sb, prefix + ".localRotation", model.localRotation);

            Transform center = _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[1];
            if (center != null)
            {
                AppendVector3LineV2LikeOriginal(sb, prefix + ".deltaWorldFromCenter", model.position - center.position);
                AppendVector3LineV2LikeOriginal(sb, prefix + ".positionInCenterLocal", center.InverseTransformPoint(model.position));
            }

            sb.AppendLine(prefix + ".point.count=4");
            for (int p = 0; p < 4; p++)
            {
                Transform point = _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[objectIndex, p];
                if (point == null)
                    continue;

                string pp = prefix + ".point" + p.ToString(CultureInfo.InvariantCulture);
                sb.AppendLine(pp + ".name=" + C2WallUniversalAnchorPointNamesV1LikeOriginal[p]);
                AppendVector3LineV2LikeOriginal(sb, pp + ".localWorld", point.localPosition);
                AppendVector3LineV2LikeOriginal(sb, pp + ".world", point.position);
                if (_c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal != null)
                    AppendVector3LineV2LikeOriginal(sb, pp + ".rootLocal", _c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal.transform.InverseTransformPoint(point.position));
                if (center != null)
                    AppendVector3LineV2LikeOriginal(sb, pp + ".centerLocal", center.InverseTransformPoint(point.position));
            }
        }

        private void AppendWallUniversalAnchorLineLinkTextV2LikeOriginal(StringBuilder sb, string name, int movingObject, int movingAnchor, int targetObject, int targetAnchor)
        {
            Transform movingModel = _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[movingObject];
            Transform targetModel = _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[targetObject];
            Transform a = _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[movingObject, movingAnchor];
            Transform b = _c2WallUniversalAnchorLineCalibratorPointsV2LikeOriginal[targetObject, targetAnchor];
            if (movingModel == null || targetModel == null || a == null || b == null)
                return;

            string prefix = "link." + name;
            sb.AppendLine(prefix + ".movingObject=" + C2WallUniversalAnchorLineObjectRolesV2LikeOriginal[movingObject]);
            sb.AppendLine(prefix + ".movingAnchor=" + C2WallUniversalAnchorPointNamesV1LikeOriginal[movingAnchor]);
            sb.AppendLine(prefix + ".targetObject=" + C2WallUniversalAnchorLineObjectRolesV2LikeOriginal[targetObject]);
            sb.AppendLine(prefix + ".targetAnchor=" + C2WallUniversalAnchorPointNamesV1LikeOriginal[targetAnchor]);
            AppendVector3LineV2LikeOriginal(sb, prefix + ".movingRootWorld", movingModel.position);
            AppendVector3LineV2LikeOriginal(sb, prefix + ".targetRootWorld", targetModel.position);
            AppendVector3LineV2LikeOriginal(sb, prefix + ".movingAnchorWorld", a.position);
            AppendVector3LineV2LikeOriginal(sb, prefix + ".targetAnchorWorld", b.position);
            AppendVector3LineV2LikeOriginal(sb, prefix + ".snapDeltaWorld", b.position - a.position);
            AppendVector3LineV2LikeOriginal(sb, prefix + ".rootDeltaWorld", movingModel.position - targetModel.position);
            if (_c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal != null)
                AppendVector3LineV2LikeOriginal(sb, prefix + ".rootDeltaRootLocal", _c2WallUniversalAnchorLineCalibratorRootV2LikeOriginal.transform.InverseTransformVector(movingModel.position - targetModel.position));
            sb.AppendLine(prefix + ".errorWorld=" + Vector3.Distance(a.position, b.position).ToString("0.######", CultureInfo.InvariantCulture));
        }

        private void AppendVector3LineV2LikeOriginal(StringBuilder sb, string prefix, Vector3 v)
        {
            sb.AppendLine(prefix + ".x=" + v.x.ToString("0.######", CultureInfo.InvariantCulture));
            sb.AppendLine(prefix + ".y=" + v.y.ToString("0.######", CultureInfo.InvariantCulture));
            sb.AppendLine(prefix + ".z=" + v.z.ToString("0.######", CultureInfo.InvariantCulture));
        }

        private void AppendQuaternionLineV2LikeOriginal(StringBuilder sb, string prefix, Quaternion q)
        {
            sb.AppendLine(prefix + ".x=" + q.x.ToString("0.######", CultureInfo.InvariantCulture));
            sb.AppendLine(prefix + ".y=" + q.y.ToString("0.######", CultureInfo.InvariantCulture));
            sb.AppendLine(prefix + ".z=" + q.z.ToString("0.######", CultureInfo.InvariantCulture));
            sb.AppendLine(prefix + ".w=" + q.w.ToString("0.######", CultureInfo.InvariantCulture));
        }

        private void LoadWallUniversalAnchorLineCalibratorFileIfPresentV2LikeOriginal()
        {
            string projectPath = ResolveWallUniversalAnchorLineCalibratorProjectOutputPathV2LikeOriginal();
            string persistentPath = ResolveWallUniversalAnchorLineCalibratorOutputPathV2LikeOriginal();
            string path = File.Exists(projectPath) ? projectPath : (File.Exists(persistentPath) ? persistentPath : null);
            if (string.IsNullOrWhiteSpace(path))
                return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                var pos = new Vector3[3];
                var rot = new Quaternion[3];
                var hasPos = new bool[3];
                var hasRot = new bool[3];
                for (int i = 0; i < 3; i++)
                    rot[i] = Quaternion.identity;

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

                    for (int o = 0; o < 3; o++)
                    {
                        string lp = "object" + o.ToString(CultureInfo.InvariantCulture) + ".localWorld.";
                        if (key.StartsWith(lp, StringComparison.OrdinalIgnoreCase))
                        {
                            hasPos[o] = true;
                            string axis = key.Substring(lp.Length);
                            if (string.Equals(axis, "x", StringComparison.OrdinalIgnoreCase)) pos[o].x = f;
                            else if (string.Equals(axis, "y", StringComparison.OrdinalIgnoreCase)) pos[o].y = f;
                            else if (string.Equals(axis, "z", StringComparison.OrdinalIgnoreCase)) pos[o].z = f;
                        }

                        string rp = "object" + o.ToString(CultureInfo.InvariantCulture) + ".localRotation.";
                        if (key.StartsWith(rp, StringComparison.OrdinalIgnoreCase))
                        {
                            hasRot[o] = true;
                            string axis = key.Substring(rp.Length);
                            if (string.Equals(axis, "x", StringComparison.OrdinalIgnoreCase)) rot[o].x = f;
                            else if (string.Equals(axis, "y", StringComparison.OrdinalIgnoreCase)) rot[o].y = f;
                            else if (string.Equals(axis, "z", StringComparison.OrdinalIgnoreCase)) rot[o].z = f;
                            else if (string.Equals(axis, "w", StringComparison.OrdinalIgnoreCase)) rot[o].w = f;
                        }
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    Transform model = _c2WallUniversalAnchorLineCalibratorModelsV2LikeOriginal[i];
                    if (model == null)
                        continue;
                    if (hasPos[i])
                        model.localPosition = pos[i];
                    if (hasRot[i])
                        model.localRotation = rot[i];
                }

                Debug.Log("[C2:WALL ANCHOR LINE CAL V2] loaded " + path.Replace('\\', '/'));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:WALL ANCHOR LINE CAL V2] load failed '" + path.Replace('\\', '/') + "': " + ex.Message);
            }
        }

        private string ResolveWallUniversalAnchorLineCalibratorOutputPathV2LikeOriginal()
        {
            string mapPath = _mapRelativePath ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(mapPath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "current_map";

            string dir = Path.Combine(Application.persistentDataPath, "C2WallCalibration");
            return Path.Combine(dir, fileName + "_wall_universal_line_v2.txt");
        }

        private string ResolveWallUniversalAnchorLineCalibratorProjectOutputPathV2LikeOriginal()
        {
            string mapPath = _mapRelativePath ?? string.Empty;
            string fileName = Path.GetFileNameWithoutExtension(mapPath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "current_map";

            string dir = Path.Combine(Application.dataPath, "Cossacks2Bridge", "Maps", "C2WallCalibration");
            return Path.Combine(dir, fileName + "_wall_universal_line_v2.txt");
        }
    }
}
