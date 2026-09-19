using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private sealed class ProjectedBuildingLineOverlay
        {
            internal C2BuildingPseudoProjectionEntryLikeOriginal Part;
            internal LineRenderer Renderer;
            internal Vector3[] Input, Output = new Vector3[2];
        }
        private readonly List<ProjectedBuildingLineOverlay> _projectedBuildingLineOverlays = new List<ProjectedBuildingLineOverlay>();

        private C2BuildingPseudoProjectionEntryLikeOriginal FindProjectedBuildingLinePartLikeOriginal(
            C2BuildingRuntimeInfoV247LikeOriginal info, C2BuildingRuntimeLineV247LikeOriginal line)
        {
            if (info == null) return null;
            foreach (var entry in _c2BuildingPseudoProjectionEntriesLikeOriginal)
            {
                if (entry.Root == null || !entry.Root.gameObject.activeInHierarchy || entry.SourcePart == null) continue;
                if (entry.Root != info.transform && !entry.Root.IsChildOf(info.transform)) continue;
                if (!string.Equals(entry.SourcePart.AnimationName, line.AnimationName, StringComparison.OrdinalIgnoreCase)) continue;
                if (!entry.Md.Animations.TryGetValue(line.AnimationName, out var anim) || line.FrameIndex < 0 || line.FrameIndex >= anim.Frames.Count) continue;
                var frame = anim.Frames[line.FrameIndex];
                if (frame.FileRef == entry.SourcePart.Frame.FileRef && frame.SpriteId == entry.SourcePart.Frame.SpriteId) return entry;
            }
            return null;
        }

        internal bool HasProjectedBuildingLineLikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal info,
            C2BuildingRuntimeLineV247LikeOriginal line) => FindProjectedBuildingLinePartLikeOriginal(info, line) != null;

        internal void RegisterProjectedBuildingLineLikeOriginal(C2BuildingRuntimeInfoV247LikeOriginal info,
            C2BuildingRuntimeLineV247LikeOriginal line, LineRenderer renderer)
        {
            var entry = FindProjectedBuildingLinePartLikeOriginal(info, line);
            if (entry == null) { renderer.enabled = false; return; }
            FramePivotLikeOriginal(entry.Md, entry.SourcePart.Frame, out int dx, out int dy);
            var tm = GetAlignLineTransformLikeOriginal(SkewPtLikeOriginal(-dx, -dy, 0), line.X1, line.Y1, line.X2, line.Y2);
            var binding = new ProjectedBuildingLineOverlay
            {
                Part = entry, Renderer = renderer,
                Input = new[] { tm.TransformPoint(new Vector3(line.X1 - (line.IsPoint ? 6 : 0), line.Y1, 0)),
                    tm.TransformPoint(new Vector3(line.X2 + (line.IsPoint ? 6 : 0), line.Y2, 0)) }
            };
            _projectedBuildingLineOverlays.Add(binding);
            UpdateProjectedBuildingLineOverlayLikeOriginal(binding, _strictIsoCamera);
        }

        private void UpdateProjectedBuildingLineOverlaysLikeOriginal(Camera cam)
        {
            for (int i = _projectedBuildingLineOverlays.Count - 1; i >= 0; i--)
            {
                var binding = _projectedBuildingLineOverlays[i];
                if (binding.Renderer == null || binding.Part.Root == null)
                {
                    if (binding.Renderer != null) binding.Renderer.enabled = false;
                    _projectedBuildingLineOverlays.RemoveAt(i);
                    continue;
                }
                UpdateProjectedBuildingLineOverlayLikeOriginal(binding, cam);
            }
        }

        private static void UpdateProjectedBuildingLineOverlayLikeOriginal(ProjectedBuildingLineOverlay binding, Camera cam)
        {
            var entry = binding.Part;
            bool valid = entry.Root.gameObject.activeInHierarchy && !entry.ProjectionCulled &&
                TryProjectDrawSpriteBuildingExactLikeOriginal(cam, entry.Root, entry.Md, entry.Scale, binding.Input, binding.Output, entry.LastFrame);
            binding.Renderer.enabled = valid;
            if (!valid) return;
            // The same transform as the displayed sprite, including 3-point fit.
            // BORN/CONCENTRATOR overlays deliberately retain the real gameplay path.
            binding.Renderer.SetPosition(0, entry.Root.TransformPoint(binding.Output[0]));
            binding.Renderer.SetPosition(1, entry.Root.TransformPoint(binding.Output[1]));
        }
    }
}
