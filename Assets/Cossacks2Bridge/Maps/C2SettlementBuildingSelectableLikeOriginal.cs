using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed class C2SettlementBuildingSelectableV1LikeOriginal : MonoBehaviour
    {
        public C2BattleTerrainMode OwnerMode;
        public string SourceMonsterId;
        public string KindName;
        public int RecordIndex;
        public int RealX;
        public int RealY;
        public byte RealDir;
        public bool NotSelectable;
        public int SortKey;
        public float MapPixelToWorld = 0.1f;
        public float SelectionHalfPixelsX = 48.0f;
        public float SelectionHalfPixelsY = 32.0f;
        public float MarkerYOffset = 0.022f;

        private bool _selected;
        private GameObject _selectionMarker;

        public bool IsSelected { get { return _selected; } }

        public void Configure(
            C2BattleTerrainMode ownerMode,
            int recordIndex,
            string monsterId,
            string kindName,
            int realX,
            int realY,
            byte realDir,
            bool notSelectable,
            float mapPixelToWorld,
            float halfPixelsX,
            float halfPixelsY)
        {
            OwnerMode = ownerMode;
            RecordIndex = recordIndex;
            SourceMonsterId = monsterId ?? string.Empty;
            KindName = kindName ?? string.Empty;
            RealX = realX;
            RealY = realY;
            RealDir = realDir;
            NotSelectable = notSelectable;
            MapPixelToWorld = Mathf.Max(0.0001f, mapPixelToWorld);
            SelectionHalfPixelsX = Mathf.Max(8.0f, halfPixelsX);
            SelectionHalfPixelsY = Mathf.Max(8.0f, halfPixelsY);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;

            if (_selectionMarker == null)
                _selectionMarker = CreateSelectionMarkerLikeOriginal();

            if (_selectionMarker != null)
                _selectionMarker.SetActive(selected);
        }

        public bool TryPickScreenPointLikeOriginal(Camera cam, Vector3 screenPosition, out Rect screenRect, out float distPx)
        {
            screenRect = default(Rect);
            distPx = float.PositiveInfinity;

            if (!TryGetScreenRectLikeOriginal(cam, out screenRect))
                return false;

            Vector2 p = new Vector2(screenPosition.x, screenPosition.y);
            Vector2 c = screenRect.center;
            distPx = Vector2.Distance(p, c);
            return screenRect.Contains(p, true);
        }

        public bool TryGetScreenRectLikeOriginal(Camera cam, out Rect screenRect)
        {
            screenRect = default(Rect);
            if (cam == null) return false;

            Bounds b;
            if (!TryCollectRendererBoundsLikeOriginal(out b))
                return false;

            Vector3 min = b.min;
            Vector3 max = b.max;
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, max.y, max.z)
            };

            float x0 = float.PositiveInfinity;
            float y0 = float.PositiveInfinity;
            float x1 = float.NegativeInfinity;
            float y1 = float.NegativeInfinity;
            bool any = false;

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 sp = cam.WorldToScreenPoint(corners[i]);
                if (sp.z < -0.001f) continue;
                any = true;
                if (sp.x < x0) x0 = sp.x;
                if (sp.y < y0) y0 = sp.y;
                if (sp.x > x1) x1 = sp.x;
                if (sp.y > y1) y1 = sp.y;
            }

            if (!any || !float.IsFinite(x0) || !float.IsFinite(y0) || !float.IsFinite(x1) || !float.IsFinite(y1))
                return false;

            if ((x1 - x0) < 2.0f || (y1 - y0) < 2.0f)
                return false;

            screenRect = Rect.MinMaxRect(x0, y0, x1, y1);
            return true;
        }

        public string DebugPickLineLikeOriginal(float distPx)
        {
            return "[C2:SETTLEMENT BUILDING PICK V1] idx=" +
                   RecordIndex.ToString(CultureInfo.InvariantCulture) +
                   " name='" + SourceMonsterId + "'" +
                   " kind='" + KindName + "'" +
                   " real=(" + RealX.ToString(CultureInfo.InvariantCulture) + "," +
                   RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                   " dir=" + RealDir.ToString(CultureInfo.InvariantCulture) +
                   " distPx=" + distPx.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private bool TryCollectRendererBoundsLikeOriginal(out Bounds bounds)
        {
            bounds = default(Bounds);
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            bool has = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled) continue;
                if (r.transform != null && r.transform.name.IndexOf("selection_marker", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                if (!has)
                {
                    bounds = r.bounds;
                    has = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            return has;
        }

        private GameObject CreateSelectionMarkerLikeOriginal()
        {
            var go = new GameObject("selection_marker_building");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * MarkerYOffset;
            go.transform.localRotation = Quaternion.identity;

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = BuildSelectionRingMeshLikeOriginal();

            Shader sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Standard");

            var mat = new Material(sh);
            mat.name = "C2_SettlementBuilding_Selection_V1";
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(1.0f, 0.92f, 0.12f, 1.0f));
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.Always);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            mat.renderQueue = 5000;

            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

            go.SetActive(false);
            return go;
        }

        private Mesh BuildSelectionRingMeshLikeOriginal()
        {
            float halfX = Mathf.Max(0.12f, SelectionHalfPixelsX * MapPixelToWorld);
            float halfZ = Mathf.Max(0.12f, SelectionHalfPixelsY * MapPixelToWorld);
            float t = Mathf.Clamp(MapPixelToWorld * 1.35f, 0.025f, Mathf.Min(halfX, halfZ) * 0.35f);

            Vector3[] outer =
            {
                new Vector3(-halfX, 0.0f, -halfZ),
                new Vector3( halfX, 0.0f, -halfZ),
                new Vector3( halfX, 0.0f,  halfZ),
                new Vector3(-halfX, 0.0f,  halfZ)
            };

            Vector3[] inner =
            {
                new Vector3(-halfX + t, 0.0f, -halfZ + t),
                new Vector3( halfX - t, 0.0f, -halfZ + t),
                new Vector3( halfX - t, 0.0f,  halfZ - t),
                new Vector3(-halfX + t, 0.0f,  halfZ - t)
            };

            float a = ((RealDir & 255) / 256.0f) * Mathf.PI * 2.0f;
            float ca = Mathf.Cos(a);
            float sa = Mathf.Sin(a);

            for (int i = 0; i < 4; i++)
            {
                outer[i] = RotateSelectionPointLikeOriginal(outer[i], ca, sa);
                inner[i] = RotateSelectionPointLikeOriginal(inner[i], ca, sa);
            }

            var verts = new Vector3[8];
            for (int i = 0; i < 4; i++)
            {
                verts[i] = outer[i];
                verts[i + 4] = inner[i];
            }

            var tris = new[]
            {
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7
            };

            var mesh = new Mesh();
            mesh.name = "C2_SettlementBuilding_SelectionPatchFrame_V1";
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 RotateSelectionPointLikeOriginal(Vector3 p, float ca, float sa)
        {
            return new Vector3(p.x * ca - p.z * sa, 0.0f, p.x * sa + p.z * ca);
        }
    }
}
