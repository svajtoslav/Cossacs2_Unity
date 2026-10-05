using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2ComplexAnimationV437LikeOriginal
    {
        internal string Id, Package, ModelPath, AnimationPath, Sound;
        internal bool IsModel, Inverse;
        internal float Scale = 1;
        internal int Dx, Dy, Rotations, FrameCount, StartFrame, DistancePerFrame, SortX1, SortX2, AddHeight, AddDirection;

        // ObjectPartAnimation::DrawAt casts time to DWORD before division/modulo.
        internal int FrameAt(int time)
        {
            if (FrameCount <= 0) return 0;
            uint clock = unchecked((uint)time), count = (uint)FrameCount;
            if (DistancePerFrame == 0) return (int)(clock % count);
            if (DistancePerFrame < 0) return FrameCount - 1 - (int)((clock / (uint)(-(long)DistancePerFrame)) % count);
            return (int)((clock / (uint)DistancePerFrame) % count);
        }
    }

    internal struct C2ComplexPartPoseV437LikeOriginal
    {
        internal C2ComplexAnimationV437LikeOriginal Animation;
        internal int Quant, Part, OriginalX, OriginalY, Frame;
        internal byte Direction;
    }

    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        private static int ComplexIntV437(string text)
        {
            return int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, C2ComplexAnimationV437LikeOriginal> ParseComplexAnimationsV437LikeOriginal(string[] lines)
        {
            var animations = new Dictionary<string, C2ComplexAnimationV437LikeOriginal>(StringComparer.Ordinal);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] header = SplitTokens(CleanComplexDataLineV430LikeOriginal(lines[i]));
                if (header.Length < 2 || (header[0] != "#ANIM" && header[0] != "#OBJECT" && header[0] != "#3DANIM")) continue;
                int next = i + 1;
                string data = NextComplexDataLineV430LikeOriginal(lines, ref next);
                var a = new C2ComplexAnimationV437LikeOriginal { Id = header[1], IsModel = header[0] == "#3DANIM" };
                // sscanf("%f%s...") accepts zero whitespace between the scale
                // and path; retail GPUSPUSHG3/4 use "0.018Models\\...".
                if (a.IsModel)
                {
                    var scale = Regex.Match(data, @"^[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?");
                    if (scale.Success) data = scale.Value + " " + data.Substring(scale.Length);
                }
                string[] t = SplitTokens(data);
                if (t.Length < (a.IsModel ? 7 : 9)) throw new FormatException("Incomplete complex animation " + a.Id);
                if (a.IsModel)
                {
                    a.Scale = float.Parse(t[0], CultureInfo.InvariantCulture);
                    a.ModelPath = t[1]; a.AnimationPath = t[2];
                    a.FrameCount = ComplexIntV437(t[3]); a.DistancePerFrame = ComplexIntV437(t[4]);
                    a.AddHeight = ComplexIntV437(t[5]); a.AddDirection = ComplexIntV437(t[6]);
                    a.Sound = t.Length > 7 ? t[7] : string.Empty;
                }
                else
                {
                    a.Package = t[0]; a.Dx = -ComplexIntV437(t[1]); a.Dy = -ComplexIntV437(t[2]);
                    int rotations = ComplexIntV437(t[3]); a.Rotations = Math.Abs(rotations); a.Inverse = rotations < 0;
                    a.FrameCount = ComplexIntV437(t[4]); a.StartFrame = ComplexIntV437(t[5]);
                    a.DistancePerFrame = ComplexIntV437(t[6]); a.SortX1 = ComplexIntV437(t[7]); a.SortX2 = ComplexIntV437(t[8]);
                    a.AddHeight = t.Length > 9 ? ComplexIntV437(t[9]) : 0;
                }
                // Zero frames is a valid disabled effect (retail PUSFOGlH).
                if (a.FrameCount < 0) throw new FormatException("Invalid complex frame count: " + a.Id);
                // GetAnmIndex scans OPANM from the beginning; duplicate names do not replace it.
                if (!animations.ContainsKey(a.Id)) animations.Add(a.Id, a);
                i = next - 1;
            }
            return animations;
        }

        // Mechanics.cpp::DrawComplexObject. Produces one pose list shared by the
        // 3D and GP passes; no duplication of the gameplay/helper OneObjects.
        internal static void EvaluateComplexPartsV437LikeOriginal(C2ComplexObjectRuntimeV430LikeOriginal cob,
            List<C2ComplexPartPoseV437LikeOriginal> output)
        {
            output.Clear();
            if (cob?.Desc?.AnimationsV437 == null) return;
            for (int qi = 0; qi < cob.Quants.Length; qi++)
            {
                var q = cob.Quants[qi]; var desc = cob.Desc.Chain[qi];
                var state = desc.StatesV432LikeOriginal[cob.FinalState];
                if (state == null) continue;
                var transition = desc.Transitions[cob.StartState + cob.FinalState * 24];
                bool transforming = cob.StartState != cob.FinalState && transition != null && !transition.Direct;
                int count = transforming ? transition.PartsV432LikeOriginal.Length : state.Length;
                int cos = C2OriginalMovementMathV352.TCos[unchecked((byte)q.Fi)];
                int sin = C2OriginalMovementMathV352.TSin[unchecked((byte)q.Fi)];
                int forward = 0;
                for (int pi = 0; pi < count; pi++)
                {
                    bool hidden = false;
                    foreach (var helper in cob.HelpersV432LikeOriginal)
                        if (helper.QuantIndex == qi && helper.QuantPos == pi && helper.MissingV437) { hidden = true; break; }
                    if (hidden) continue;
                    string animation; int x, y, time, direction;
                    var events = transforming ? transition.PartsV432LikeOriginal[pi] : null;
                    int tick = cob.TransTime >> 8;
                    if (transforming && events != null && events.Length != 0 && tick < transition.MaxTransfTime)
                    {
                        C2ComplexTransformElementV432LikeOriginal e = null;
                        foreach (var candidate in events)
                            if (tick >= candidate.StartTime && tick < candidate.StartTime + candidate.TimeAmount) { e = candidate; break; }
                        if (e == null) continue; // Native does not substitute the final pose before an event starts.
                        int dt = tick - e.StartTime, duration = e.TimeAmount;
                        x = e.X0 + (e.X1 - e.X0) * dt / duration;
                        y = e.Y0 + (e.Y1 - e.Y0) * dt / duration;
                        direction = e.Fi0V437 + (e.Fi1V437 - e.Fi0V437) * dt / duration + unchecked((byte)q.Fi);
                        time = e.StartFrameV437 + (e.EndFrameV437 + 1 - e.StartFrameV437) * dt / duration;
                        animation = e.AnimationIdV437;
                    }
                    else
                    {
                        if (pi >= state.Length || state[pi] == null) continue;
                        var e = state[pi]; animation = e.AnimationId; x = e.Dx; y = e.Dy;
                        direction = (int)(q.Fi + e.Dfi);
                        if (!transforming && e.AnmDirV437 == 3)
                        {
                            if (forward >= 4) continue;
                            float a = q.Fi * Mathf.PI / 128, a0 = q.Fi0 * Mathf.PI / 128;
                            float dx = (x * Mathf.Cos(a) - y * Mathf.Sin(a) - x * Mathf.Cos(a0) + y * Mathf.Sin(a0)) * 16 + q.Xc - q.Xc0;
                            float dy = (y * Mathf.Cos(a) + x * Mathf.Sin(a) - y * Mathf.Cos(a0) - x * Mathf.Sin(a0)) * 16 + q.Yc - q.Yc0;
                            cob.ForwardDistanceV437[forward] += Mathf.Sqrt(dx * dx + dy * dy);
                            cob.ForwardDxV437[forward] = (cob.ForwardDxV437[forward] * 47 + dx) / 48;
                            cob.ForwardDyV437[forward] = (cob.ForwardDyV437[forward] * 47 + dy) / 48;
                            direction = (int)(Mathf.Atan2(cob.ForwardDyV437[forward], cob.ForwardDxV437[forward]) * 128 / Mathf.PI + e.Dfi);
                            time = (int)cob.ForwardDistanceV437[forward++];
                            if (e.ReverseClockV437) time = -time;
                        }
                        else
                        {
                            int average = (q.LeftAngle + q.RightAngle) / 2;
                            // The transition fallback uses signed Dy and no reverse flag in the source.
                            int delta = (q.RightAngle - q.LeftAngle) * (transforming ? y : Math.Abs(y)) / 128;
                            time = e.AnmDirV437 == 0 ? average - delta : e.AnmDirV437 == 2 ? average + delta : e.AnmDirV437 == 1 ? average : 0;
                            if (!transforming && e.ReverseClockV437) time = -time;
                        }
                    }
                    C2ComplexAnimationV437LikeOriginal anim;
                    if (!cob.Desc.AnimationsV437.TryGetValue(animation, out anim))
                        throw new InvalidOperationException("Missing complex animation: " + animation);
                    if (anim.FrameCount == 0) continue;
                    int drawX = (int)q.Xc + ((x * cos - y * sin) >> 4);
                    int drawY = (int)(q.Yc / 2) + ((y * cos + x * sin) >> 5);
                    output.Add(new C2ComplexPartPoseV437LikeOriginal {
                        Quant = qi, Part = pi, Animation = anim, Frame = anim.FrameAt(time),
                        OriginalX = drawX / 16, OriginalY = drawY / 8 + cob.Rz * 2,
                        Direction = unchecked((byte)direction)
                    });
                }
            }
        }
    }
}
