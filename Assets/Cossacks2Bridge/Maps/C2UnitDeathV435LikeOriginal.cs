using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2UnitOriginalRuntime
    {
        internal int OriginalDeathCounterV435LikeOriginal;
        internal bool OriginalDeathExpiredV435LikeOriginal;
    }

    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        private bool _hasExpiredDeathsV435LikeOriginal;
        private int _nextRuntimeUnitOrderV435LikeOriginal;

        // NewMon.cpp::LongProcesses, non-building Sdoxlo branch. FrmDec is 2
        // after Ddex1.cpp game initialization. This counter runs on simulation
        // quanta, including offscreen units, never on Unity rendering frames.
        private void TickOriginalDeathV435LikeOriginal(C2UnitOriginalRuntime u)
        {
            if (u.OriginalDeathExpiredV435LikeOriginal) return;
            int death = ResolveAnimationIndexLikeOriginal(u.Md, "#DEATH");
            bool expire = death < 0;
            if (!expire)
            {
                if (u.OriginalDeathCounterV435LikeOriginal == 0)
                    u.OriginalDeathCounterV435LikeOriginal = 1;
                if (u.CurrentAnimIndex == death)
                {
                    u.OriginalDeathCounterV435LikeOriginal += 2;
                    var traits = u.Info != null ? C2CombatCoreV408LikeOriginal.GetTraitsV408LikeOriginal(u.Info) : null;
                    bool water = traits != null && traits.LockType == 1;
                    if (water)
                        expire = u.OriginalDeathCounterV435LikeOriginal > 100 && u.FrameFinishedLikeOriginal;
                }
                // Native code also increments Sdoxlo once after the DEATH block.
                u.OriginalDeathCounterV435LikeOriginal++;
                expire |= u.OriginalDeathCounterV435LikeOriginal > 3200;
            }
            if (!expire) return;
            ExpireOriginalDeathV435LikeOriginal(u);
        }

        private void ExpireOriginalDeathV435LikeOriginal(C2UnitOriginalRuntime u)
        {
            u.OriginalDeathExpiredV435LikeOriginal = true;
            u.ComplexVisualV437?.Dispose();
            u.ComplexVisualV437 = null;
            if (u.OriginalComplexObjectV430LikeOriginal != null)
                C2OriginalMovementSystemV425LikeOriginal.UnlockComplexObjectV430LikeOriginal(u);
            _hasExpiredDeathsV435LikeOriginal = true;
            u.HasMoveTargetLikeOriginal = false;
            SetRuntimeActiveLikeOriginal(u, false);
        }

        private void RemoveExpiredDeathsV435LikeOriginal()
        {
            if (!_hasExpiredDeathsV435LikeOriginal) return;
            _hasExpiredDeathsV435LikeOriginal = false;
            int removed = 0;
            for (int i = _units.Count - 1; i >= 0; i--)
            {
                var u = _units[i];
                if (u == null || !u.OriginalDeathExpiredV435LikeOriginal) continue;
                if (_selected == u) _selected = null;
                if (u.Info != null)
                {
                    C2FormationRuntimeV167LikeOriginal.ReleaseDeadMemberV435LikeOriginal(u.Info);
                    u.Info.C2ReleaseV365LikeOriginal();
                }
                if (u.Mesh != null) Destroy(u.Mesh);
                if (u.Root != null) Destroy(u.Root);
                _units.RemoveAt(i);
                removed++;
            }
            // Cell lists otherwise retain released objects until their next refresh.
            // Shared sprite textures/materials belong to the renderer cache, not a corpse.
            _unitCollisionBucketsValidLikeOriginal = false;
            _originalSetInCellTime256LikeOriginal = 0;
            _unitDrawCellsLikeOriginal.Clear();
            _drawUnitsCurrentLikeOriginal.RemoveAll(u => u == null || u.OriginalDeathExpiredV435LikeOriginal);
            _drawUnitsPreviousLikeOriginal.RemoveAll(u => u == null || u.OriginalDeathExpiredV435LikeOriginal);
            if (removed > 0) Debug.Log("[C2 DEATH V435] removed=" + removed + " remaining=" + _units.Count);
        }
    }

    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        internal static void ReleaseDeadMemberV435LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            RuntimeFormationV172LikeOriginal group;
            if (TryGetRuntimeGroupByUnitV172LikeOriginal(unit, out group) && group != null)
                for (int i = 0; i < group.Units.Count; i++)
                    if (ReferenceEquals(group.Units[i], unit)) group.Units[i] = null;
            // Keep vacant formation slots: compacting them changes loss percentages,
            // soldier ordinals and command positions before a native reformation.
            _groupIdByUnitInstanceV172LikeOriginal.Remove(unit.GetInstanceID());
            _unitKillsV402LikeOriginal.Remove(unit);
        }
    }
}
