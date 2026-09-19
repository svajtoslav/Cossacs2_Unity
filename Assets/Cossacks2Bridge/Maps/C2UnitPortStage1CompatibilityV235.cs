// C2UnitPortStage1CompatibilityV235.cs
// V252: compatibility bridge for building HUD/selection/rally.
// Keeps the current building renderer and LINESORT V251 intact.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2GameplayHudV1 : MonoBehaviour
    {
        public static void ForceRefreshLikeOriginal()
        {
            // V241: no-op compatibility hook.
            // The ported HUD refreshes itself from its Update/Rebuild path.
        }
    }

    public sealed class C2SettlementBuildingSelectableV1LikeOriginal : MonoBehaviour
    {
        public C2BattleTerrainMode OwnerMode;
        public bool IsSelected;
        public bool NotSelectable;
        public string SourceMonsterId = string.Empty;
        public string KindName = string.Empty;
        public int RecordIndex;
        public int RealX;
        public int RealY;
        public int RealDir;
        public int Nation;
        public int SortKey;
        public int LifeLikeOriginal = 1;
        public int LifeMaxLikeOriginal = 1;
        public int StageLikeOriginal = 1;
        public int StageMaxLikeOriginal = 1;
        public bool ReadyLikeOriginal = true;
        public float MapPixelToWorld = 0.1f;
        public float SelectionHalfPixelsX = 48.0f;
        public float SelectionHalfPixelsY = 32.0f;
        public float MarkerYOffset = 0.022f;

        // V252: original-like OB->DstX/DstY rally/exit destination for produced units.
        public bool HasRallyPointV155LikeOriginal;
        public int RallyRealXV155LikeOriginal;
        public int RallyRealYV155LikeOriginal;

        // Original selected building blink uses sin(GetTickCount()/200.0f).
        // Time.realtimeSinceStartup * 5.0 is the same angular speed.
        private const float SelectedPulseMinV252LikeOriginal = 0.80f;
        private const float SelectedPulseMaxV252LikeOriginal = 1.40f;
        private const float SelectedPulseSpeedV252LikeOriginal = 5.0f;

        private Renderer[] _pulseRenderersV252LikeOriginal;
        private MaterialPropertyBlock _pulseBlockV252LikeOriginal;
        private readonly Dictionary<Renderer, Color> _baseRendererColorV252LikeOriginal =
            new Dictionary<Renderer, Color>();
        private bool _wasSelectedVisualV252LikeOriginal;

        public bool TryGetRallyPointRealV155LikeOriginal(out int realX, out int realY)
        {
            realX = RallyRealXV155LikeOriginal;
            realY = RallyRealYV155LikeOriginal;
            return HasRallyPointV155LikeOriginal;
        }

        public void SetSelected(bool selected)
        {
            if (NotSelectable) selected = false;
            IsSelected = selected;

            if (selected && HasRallyPointV155LikeOriginal)
                C2BuildingRallyPointRuntimeV155LikeOriginal.AttachOrUpdateMarker(this, "select_show_existing_rally_v252");

            ApplySelectedPulseV252LikeOriginal(true);
        }

        public void SetRallyPointV155LikeOriginal(int realX, int realY, string source)
        {
            HasRallyPointV155LikeOriginal = true;
            RallyRealXV155LikeOriginal = realX;
            RallyRealYV155LikeOriginal = realY;
            C2BuildingRallyPointRuntimeV155LikeOriginal.RememberRallyPointStateV159LikeOriginal(this, source, IsSelected);
            C2BuildingRallyPointRuntimeV155LikeOriginal.AttachOrUpdateMarker(this, source);
        }

        public void ClearRallyPointV155LikeOriginal()
        {
            HasRallyPointV155LikeOriginal = false;
            RallyRealXV155LikeOriginal = 0;
            RallyRealYV155LikeOriginal = 0;
            C2BuildingRallyPointRuntimeV155LikeOriginal.ForgetRallyPointStateV159LikeOriginal(this);
            C2BuildingRallyPointRuntimeV155LikeOriginal.AttachOrUpdateMarker(this, "clear_v252");
        }

        private void LateUpdate()
        {
            if (IsSelected)
            {
                ApplySelectedPulseV252LikeOriginal(false);
                _wasSelectedVisualV252LikeOriginal = true;
            }
            else if (_wasSelectedVisualV252LikeOriginal)
            {
                _wasSelectedVisualV252LikeOriginal = false;
                ApplySelectedPulseV252LikeOriginal(true);
            }
        }

        private void OnDisable()
        {
            if (HasRallyPointV155LikeOriginal)
                C2BuildingRallyPointRuntimeV155LikeOriginal.RememberRallyPointStateV159LikeOriginal(this, "building_disable_v252", IsSelected);

            IsSelected = false;
            ApplySelectedPulseV252LikeOriginal(true);
        }

        private void OnDestroy()
        {
            if (HasRallyPointV155LikeOriginal)
                C2BuildingRallyPointRuntimeV155LikeOriginal.RememberRallyPointStateV159LikeOriginal(this, "building_destroy_v252", IsSelected);
        }

        private void ApplySelectedPulseV252LikeOriginal(bool force)
        {
            if (_pulseBlockV252LikeOriginal == null)
                _pulseBlockV252LikeOriginal = new MaterialPropertyBlock();

            if (_pulseRenderersV252LikeOriginal == null || force)
                _pulseRenderersV252LikeOriginal = GetComponentsInChildren<Renderer>(true);

            float brightness = 1.0f;
            if (IsSelected)
            {
                float wave = (Mathf.Sin(Time.realtimeSinceStartup * SelectedPulseSpeedV252LikeOriginal) + 1.0f) * 0.5f;
                brightness = Mathf.Lerp(SelectedPulseMinV252LikeOriginal, SelectedPulseMaxV252LikeOriginal, wave);
            }

            for (int i = 0; _pulseRenderersV252LikeOriginal != null && i < _pulseRenderersV252LikeOriginal.Length; i++)
            {
                Renderer r = _pulseRenderersV252LikeOriginal[i];
                if (r == null) continue;

                if (r.transform != null)
                {
                    string tn = r.transform.name ?? string.Empty;
                    if (tn.IndexOf("selection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tn.IndexOf("rally", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        tn.IndexOf("exitpoint", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                }

                Color baseColor = GetBaseRendererColorV252LikeOriginal(r);
                Color finalColor = baseColor;
                if (!IsLikelyShadowRendererV252LikeOriginal(r))
                {
                    finalColor.r = baseColor.r * brightness;
                    finalColor.g = baseColor.g * brightness;
                    finalColor.b = baseColor.b * brightness;
                }

                r.GetPropertyBlock(_pulseBlockV252LikeOriginal);
                _pulseBlockV252LikeOriginal.SetColor("_Color", finalColor);
                r.SetPropertyBlock(_pulseBlockV252LikeOriginal);
            }
        }

        private Color GetBaseRendererColorV252LikeOriginal(Renderer r)
        {
            if (r == null)
                return Color.white;

            Color c;
            if (_baseRendererColorV252LikeOriginal.TryGetValue(r, out c))
                return c;

            c = Color.white;
            Material mat = r.sharedMaterial;
            if (mat != null && mat.HasProperty("_Color"))
                c = mat.GetColor("_Color");

            _baseRendererColorV252LikeOriginal[r] = c;
            return c;
        }

        private static bool IsLikelyShadowRendererV252LikeOriginal(Renderer r)
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
            distPx = Vector2.Distance(p, screenRect.center);
            return screenRect.Contains(p, true);
        }

        public bool TryGetScreenRectLikeOriginal(Camera cam, out Rect screenRect)
        {
            screenRect = default(Rect);
            if (cam == null) return false;

            Bounds bounds;
            if (!TryCollectRendererBoundsLikeOriginal(out bounds))
                return false;

            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
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

        private bool TryCollectRendererBoundsLikeOriginal(out Bounds bounds)
        {
            bounds = default(Bounds);
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            bool has = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled) continue;
                if (r.transform != null && r.transform.name.IndexOf("selection", StringComparison.OrdinalIgnoreCase) >= 0)
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

            if (has) return true;

            // Fallback for a just-created bridge component that has no renderer children yet.
            Vector3 center = transform.position;
            float sx = Mathf.Max(8.0f, SelectionHalfPixelsX) * Mathf.Max(0.0001f, MapPixelToWorld);
            float sz = Mathf.Max(8.0f, SelectionHalfPixelsY) * Mathf.Max(0.0001f, MapPixelToWorld);
            bounds = new Bounds(center, new Vector3(sx * 2.0f, 32.0f * Mathf.Max(0.0001f, MapPixelToWorld), sz * 2.0f));
            return true;
        }
    }
}
