using System;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // One authoritative high-level order per unit.  The exact MD animation remains
    // data-driven; these families describe why the runtime selected that animation.
    public enum C2UnitOrderKindV325LikeOriginal
    {
        None = 0,
        Stand,
        Rest,
        Move,
        MoveBack,
        MoveLeft,
        MoveRight,
        RotateLeft,
        RotateRight,
        RotateAtPlace,
        FormationMove,
        BuildApproach,
        BuildWork,
        ResourceApproach,
        ResourceWork,
        ResourceDeposit,
        ResourceReturn,
        Attack,
        PreciseAttack,
        UnitAttack,
        RangedAttack,
        MeleeAttack,
        GrenadeAttack,
        CombatPosture,
        Transition,
        Spawn,
        Death,
        DeathLie,
        Special
    }

    public enum C2OriginalMdAnimationFamilyV325LikeOriginal
    {
        Unknown = 0,
        Stand,
        Rest,
        Motion,
        MotionBack,
        MotionLeft,
        MotionRight,
        Rotate,
        Build,
        Work,
        Attack,
        PreciseAttack,
        UnitAttack,
        Transition,
        Death,
        DeathLie,
        Fist,
        Special
    }

    // OneObject stores order state inline.  This must stay a compact managed
    // state block and must not become one Unity Component per unit.
    public sealed class C2UnitOrderRuntimeV325LikeOriginal
    {
        private C2UnitOrderKindV325LikeOriginal _current = C2UnitOrderKindV325LikeOriginal.Stand;
        private C2UnitOrderKindV325LikeOriginal _previous = C2UnitOrderKindV325LikeOriginal.None;
        private string _source = "spawn";
        private string _detail = string.Empty;
        private uint _sequence;
        private float _startedAt;
        private byte _originalOrdTypeV350;

        public C2UnitOrderKindV325LikeOriginal CurrentLikeOriginal { get { return _current; } }
        public C2UnitOrderKindV325LikeOriginal PreviousLikeOriginal { get { return _previous; } }
        public string SourceLikeOriginal { get { return _source ?? string.Empty; } }
        public string DetailLikeOriginal { get { return _detail ?? string.Empty; } }
        public uint SequenceLikeOriginal { get { return _sequence; } }
        public float StartedAtLikeOriginal { get { return _startedAt; } }
        public byte OriginalOrdTypeV350LikeOriginal { get { return _originalOrdTypeV350; } }
        public bool IsTerminalLikeOriginal
        {
            get { return _current == C2UnitOrderKindV325LikeOriginal.Death || _current == C2UnitOrderKindV325LikeOriginal.DeathLie; }
        }

        public bool IssueLikeOriginal(C2UnitOrderKindV325LikeOriginal next, string source, string detail)
        {
            if (IsTerminalLikeOriginal &&
                next != C2UnitOrderKindV325LikeOriginal.Death &&
                next != C2UnitOrderKindV325LikeOriginal.DeathLie)
                return false;

            if (_current != next)
            {
                _previous = _current;
                _current = next;
                unchecked { _sequence++; }
                _startedAt = Time.realtimeSinceStartup;
            }

            _source = source ?? string.Empty;
            _detail = detail ?? string.Empty;
            return true;
        }

        public static C2UnitOrderRuntimeV325LikeOriginal GetOrCreateLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null) return null;
            C2UnitOrderRuntimeV325LikeOriginal runtime = unit.C2OrderRuntimeStateV362LikeOriginal;
            if (runtime == null)
            {
                runtime = new C2UnitOrderRuntimeV325LikeOriginal();
                unit.C2OrderRuntimeStateV362LikeOriginal = runtime;
            }
            return runtime;
        }

        public static C2UnitOrderRuntimeV325LikeOriginal TryGetLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            return unit != null ? unit.C2OrderRuntimeStateV362LikeOriginal : null;
        }

        public static bool IssueLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2UnitOrderKindV325LikeOriginal next,
            string source,
            string detail)
        {
            C2UnitOrderRuntimeV325LikeOriginal runtime = GetOrCreateLikeOriginal(unit);
            return runtime != null && runtime.IssueLikeOriginal(next, source, detail);
        }

        public static void SetOriginalOrdTypeV350LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit, byte ordType)
        {
            C2UnitOrderRuntimeV325LikeOriginal runtime = GetOrCreateLikeOriginal(unit);
            if (runtime != null) runtime._originalOrdTypeV350 = ordType;
        }

        // Covers the real command vocabulary found in all game MD files rather than
        // assuming that every unit only has six hard-coded animation states.
        public static C2OriginalMdAnimationFamilyV325LikeOriginal ClassifyMdCommandLikeOriginal(string command)
        {
            string value = (command ?? string.Empty).Trim().TrimStart('@', '#').ToUpperInvariant();
            if (value.Length == 0) return C2OriginalMdAnimationFamilyV325LikeOriginal.Unknown;
            if (value.StartsWith("DEATHLIE", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.DeathLie;
            if (value.StartsWith("DEATH", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.Death;
            if (value.StartsWith("BUILD", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.Build;
            if (value.StartsWith("WORK", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.Work;
            if (value.StartsWith("PATTACK", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.PreciseAttack;
            if (value.StartsWith("UATTACK", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.UnitAttack;
            if (value.IndexOf("ATTACK", StringComparison.Ordinal) >= 0) return C2OriginalMdAnimationFamilyV325LikeOriginal.Attack;
            if (value.StartsWith("ROTATE", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.Rotate;
            if (value.StartsWith("TRANS", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.Transition;
            if (value.StartsWith("MOTION_LB", StringComparison.Ordinal) || value.StartsWith("MOTION_RB", StringComparison.Ordinal))
                return C2OriginalMdAnimationFamilyV325LikeOriginal.MotionBack;
            if (value.StartsWith("MOTION_L", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.MotionLeft;
            if (value.StartsWith("MOTION_R", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.MotionRight;
            if (value.StartsWith("MOTION", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.Motion;
            if (value.IndexOf("STAND", StringComparison.Ordinal) >= 0) return C2OriginalMdAnimationFamilyV325LikeOriginal.Stand;
            if (value.StartsWith("REST", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.Rest;
            if (value.StartsWith("FIST", StringComparison.Ordinal)) return C2OriginalMdAnimationFamilyV325LikeOriginal.Fist;
            return C2OriginalMdAnimationFamilyV325LikeOriginal.Special;
        }
    }
}
