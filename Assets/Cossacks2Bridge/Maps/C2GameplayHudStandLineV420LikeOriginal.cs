using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2GameplayHudV1
    {
        private sealed class StandGroundLineBindingV420
        {
            internal C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            internal Image First, Second;
            internal int X, Y, Width = -1;
        }
        private readonly List<StandGroundLineBindingV420> _standGroundLineBindingsV420 = new List<StandGroundLineBindingV420>(2);

        internal static int StandGroundLineWidthV420(int delay, int maximum, bool standing)
        {
            // UnitsInterface.cpp substitutes maximum when delay==0 outside StandGround;
            // VUI_Actions.cpp::va_BR_StandGroundLine::SetFrameState draws the current bar.
            if (maximum <= 0) return 0;
            if (!standing && delay == 0) delay = maximum;
            return Mathf.Clamp(41 * (maximum - delay) / maximum, 0, 41);
        }

        private void BindStandGroundLineV420LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int x, int y)
        {
            var color = new Color32(255, 0, 0, 223);
            var binding = new StandGroundLineBindingV420 {
                Unit = unit, X = x, Y = y,
                First = AddSolidSinglePassV140ALikeOriginal("weapon_standground_line_v403", color, x, y, 0, 13, false),
                Second = AddSolidSinglePassV140ALikeOriginal("weapon_standground_line_v403_v140a_doublepass", color, x, y, 0, 13, false)
            };
            _standGroundLineBindingsV420.Add(binding);
            RefreshStandGroundLineV420LikeOriginal(binding);
        }

        private void RefreshStandGroundLineBindingsV420LikeOriginal()
        {
            for (int i = 0; i < _standGroundLineBindingsV420.Count; i++)
                RefreshStandGroundLineV420LikeOriginal(_standGroundLineBindingsV420[i]);
        }

        private void RefreshStandGroundLineV420LikeOriginal(StandGroundLineBindingV420 binding)
        {
            int width = 0;
            if (binding.Unit != null && C2FormationRuntimeV167LikeOriginal.TryGetStandGroundSnapshotV403LikeOriginal(
                binding.Unit, out int delay, out int maximum, out bool standing, out int damage, out int shield))
                width = StandGroundLineWidthV420(delay, maximum, standing);
            if (binding.Width == width) return;
            binding.Width = width;
            if (binding.First != null) { Place(binding.First.rectTransform, binding.X, binding.Y, width, 13); binding.First.enabled = width > 0; }
            if (binding.Second != null) { Place(binding.Second.rectTransform, binding.X, binding.Y, width, 13); binding.Second.enabled = width > 0; }
        }
    }
}
