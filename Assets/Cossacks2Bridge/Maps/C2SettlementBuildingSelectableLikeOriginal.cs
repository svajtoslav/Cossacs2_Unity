using System;
using System.Collections.Generic;
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

        // V155: original OB->DstX/DstY rally/exit destination for produced units.
        public bool HasRallyPointV155LikeOriginal;
        public int RallyRealXV155LikeOriginal;
        public int RallyRealYV155LikeOriginal;

        private const float HoverBrightnessV2LikeOriginal = 1.40f;   // user-scale 100 -> 140
        private const float SelectedPulseMinV2LikeOriginal = 0.80f; // user-scale 100 -> 80
        private const float SelectedPulseMaxV2LikeOriginal = 1.40f; // user-scale 100 -> 140
        // Original selected building blink uses sin(GetTickCount()/200.0f).
        // In shader time this is Time.y * 5.0.
        private const float SelectedPulseSpeedV2LikeOriginal = 5.0f;

        private bool _selected;
        private bool _hovered;
        private GameObject _selectionMarker;
        private Renderer[] _pulseRenderers;
        private MaterialPropertyBlock _pulseBlock;
        private readonly Dictionary<Renderer, Color> _baseRendererColorV110LikeOriginal =
            new Dictionary<Renderer, Color>();
        private bool _suppressVisualResetOnDisableV111LikeOriginal;

        public bool IsSelected { get { return _selected; } }
        public bool IsHovered { get { return _hovered; } }

        public void SetRallyPointV155LikeOriginal(int realX, int realY, string source)
        {
            HasRallyPointV155LikeOriginal = true;
            RallyRealXV155LikeOriginal = realX;
            RallyRealYV155LikeOriginal = realY;
            C2BuildingRallyPointRuntimeV155LikeOriginal.AttachOrUpdateMarker(this, source);
        }

        public bool TryGetRallyPointRealV155LikeOriginal(out int realX, out int realY)
        {
            realX = RallyRealXV155LikeOriginal;
            realY = RallyRealYV155LikeOriginal;
            return HasRallyPointV155LikeOriginal;
        }

        public void ClearRallyPointV155LikeOriginal()
        {
            HasRallyPointV155LikeOriginal = false;
            RallyRealXV155LikeOriginal = 0;
            RallyRealYV155LikeOriginal = 0;
            C2BuildingRallyPointRuntimeV155LikeOriginal.AttachOrUpdateMarker(this, "clear");
        }

        public void SetSuppressVisualResetOnDisableV111LikeOriginal(bool suppress)
        {
            _suppressVisualResetOnDisableV111LikeOriginal = suppress;
        }

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
            if (NotSelectable)
                selected = false;

            _selected = selected;

            if (_selectionMarker == null)
                _selectionMarker = CreateSelectionMarkerLikeOriginal();

            if (_selectionMarker != null)
                _selectionMarker.SetActive(selected);

            ApplySelectedPulseLikeOriginal(true);
        }

        public void SetHovered(bool hovered)
        {
            if (NotSelectable)
                hovered = false;

            if (_hovered == hovered)
                return;

            _hovered = hovered;
            ApplySelectedPulseLikeOriginal(true);
        }

        private void LateUpdate()
        {
            if (_selected || _hovered)
                ApplySelectedPulseLikeOriginal(false);
        }

        private void OnDisable()
        {
            _hovered = false;
            _selected = false;
            if (_selectionMarker != null) _selectionMarker.SetActive(false);

            // V111: build-placement ghost uses this same composite renderer path.
            // The preview code tints it red/white through MaterialPropertyBlock, then disables
            // this selectable component. V110's OnDisable immediately wrote the base _Color back
            // over that tint, so the cursor showed a normal opaque building until the next refresh.
            // For preview ghosts, never reset renderer color from OnDisable.
            if (_suppressVisualResetOnDisableV111LikeOriginal || IsInsideBuildPreviewGhostV111LikeOriginal())
                return;

            ApplySelectedPulseLikeOriginal(true);
        }

        private void OnDestroy()
        {
            _hovered = false;
            _selected = false;
        }

        private bool IsInsideBuildPreviewGhostV111LikeOriginal()
        {
            Transform t = transform;
            while (t != null)
            {
                string n = t.name ?? string.Empty;
                if (n.IndexOf("C2_BuildingPlacementPreview", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("BuildPreview", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Ghost", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                t = t.parent;
            }
            return false;
        }

        private void ApplySelectedPulseLikeOriginal(bool force)
        {
            if (_pulseBlock == null) _pulseBlock = new MaterialPropertyBlock();
            if (_pulseRenderers == null || force)
                _pulseRenderers = GetComponentsInChildren<Renderer>(true);

            float selectedPulse = _selected ? 1.0f : 0.0f;
            float hoverHighlight = (!_selected && _hovered) ? 1.0f : 0.0f;

            for (int i = 0; _pulseRenderers != null && i < _pulseRenderers.Length; i++)
            {
                Renderer r = _pulseRenderers[i];
                if (r == null) continue;
                if (r.transform != null && r.transform.name.IndexOf("selection_marker", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                float brightness = 1.0f;
                if (_selected)
                {
                    float wave = (Mathf.Sin(Time.realtimeSinceStartup * SelectedPulseSpeedV2LikeOriginal) + 1.0f) * 0.5f;
                    brightness = Mathf.Lerp(SelectedPulseMinV2LikeOriginal, SelectedPulseMaxV2LikeOriginal, wave);
                }
                else if (_hovered)
                {
                    brightness = HoverBrightnessV2LikeOriginal;
                }

                Color baseColor = GetBaseRendererColorV110LikeOriginal(r);
                Color finalColor = baseColor;
                if (!IsLikelyShadowRendererV110LikeOriginal(r))
                {
                    finalColor.r = baseColor.r * brightness;
                    finalColor.g = baseColor.g * brightness;
                    finalColor.b = baseColor.b * brightness;
                }

                r.GetPropertyBlock(_pulseBlock);
                // Do not write Color.white here: building shadow materials often store transparency in _Color.a.
                // V109 overwrote that alpha and made shadows opaque.
                _pulseBlock.SetColor("_Color", finalColor);
                r.SetPropertyBlock(_pulseBlock);
            }
        }

        private Color GetBaseRendererColorV110LikeOriginal(Renderer r)
        {
            if (r == null)
                return Color.white;

            Color c;
            if (_baseRendererColorV110LikeOriginal.TryGetValue(r, out c))
                return c;

            c = Color.white;
            Material mat = r.sharedMaterial;
            if (mat != null && mat.HasProperty("_Color"))
                c = mat.GetColor("_Color");

            _baseRendererColorV110LikeOriginal[r] = c;
            return c;
        }

        private static bool IsLikelyShadowRendererV110LikeOriginal(Renderer r)
        {
            if (r == null)
                return false;

            string rn = r.name ?? string.Empty;
            if (rn.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rn.IndexOf("тень", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rn.IndexOf("ten", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            Material mat = r.sharedMaterial;
            if (mat != null)
            {
                string mn = mat.name ?? string.Empty;
                if (mn.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    mn.IndexOf("тень", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    mn.IndexOf("ten", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.GetColor("_Color");
                    if (c.a > 0.001f && c.a < 0.98f)
                        return true;
                }

                Texture tex = mat.mainTexture;
                if (tex != null)
                {
                    string tn = tex.name ?? string.Empty;
                    if (tn.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tn.IndexOf("тень", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tn.IndexOf("ten", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
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
