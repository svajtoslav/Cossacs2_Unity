using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Observes existing work only: no camera, input, simulation or renderer switches.
    internal static class C2FrameCostProbe
    {
        internal enum Phase
        {
            Units, Formation, Orders, Movement, DrawList, Sprites, Batches, Spawn,
            Paths, Hud, HudLate, Combat, CombatGlobal, Morale, Fatigue,
            Smoke, Projectiles, BuildingProjection, Interaction, WorkerTasks, MotionState, MotionSingleStep, MotionTerrain, MotionWorld, MotionContact, KeepPositions, FinishCollision, FinishRetarget, Count
        }

        internal struct Stat
        {
            internal long Ticks, Bytes, Calls, MaxFrameTicks, MaxFrameBytes;
        }

        private static readonly Stat[] Frame = new Stat[(int)Phase.Count];
        private static readonly Stat[] Window = new Stat[(int)Phase.Count];
        private static readonly Stat[] SlowestFrame = new Stat[(int)Phase.Count];
        private static readonly string[] Names = Enum.GetNames(typeof(Phase));
        private static bool _enabled;
        // Coarse system scopes stay enabled. Millions of per-object native
        // allocation-counter reads would themselves distort large-army FPS.
        // Enable detailed scopes explicitly for a bounded diagnostic run.
        internal static bool DetailedPerUnit = Array.IndexOf(Environment.GetCommandLineArgs(), "-c2DetailedUnitCosts") >= 0;
        private static int _frame = -1, _firstFrame, _lastFrame, _focusedFrames;
        private static int _slowFrame;
        private static long _slowTicks;
        private static float _windowStart;
        private static long _windowStartTicks;
        private static Unity.Profiling.ProfilerRecorder _allocated;
        internal static long AllocatedBytes => _allocated.Valid ? _allocated.CurrentValue : 0;

        internal readonly struct Scope : IDisposable
        {
            private readonly int _phase;
            private readonly long _ticks, _bytes;
            private readonly bool _active;
            internal Scope(Phase phase)
            {
                bool fine = phase == Phase.Fatigue || phase == Phase.MotionState ||
                    phase == Phase.MotionSingleStep || phase == Phase.MotionTerrain ||
                    phase == Phase.MotionWorld || phase == Phase.MotionContact ||
                    phase == Phase.FinishCollision || phase == Phase.FinishRetarget;
                _active = _enabled && (DetailedPerUnit || !fine);
                _phase = (int)phase;
                _ticks = _bytes = 0;
                if (!_active) return;
                Advance(Time.frameCount);
                _bytes = AllocatedBytes;
                _ticks = Stopwatch.GetTimestamp();
            }
            public void Dispose()
            {
                if (!_active) return;
                long ticks = Stopwatch.GetTimestamp() - _ticks;
                long bytes = Math.Max(0, AllocatedBytes - _bytes);
                ref Stat stat = ref Frame[_phase];
                stat.Ticks += ticks;
                stat.Bytes += bytes;
                stat.Calls++;
            }
        }

        internal static Scope Measure(Phase phase) => new Scope(phase);

        internal static void Start()
        {
            if (_allocated.Valid) _allocated.Dispose();
            _allocated = Unity.Profiling.ProfilerRecorder.StartNew(
                Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            Array.Clear(Frame, 0, Frame.Length);
            Array.Clear(Window, 0, Window.Length);
            Array.Clear(SlowestFrame, 0, SlowestFrame.Length);
            _frame = -1;
            _firstFrame = _lastFrame = Time.frameCount;
            _focusedFrames = 0;
            _slowTicks = 0;
            _slowFrame = -1;
            _windowStart = Time.realtimeSinceStartup;
            _windowStartTicks = Stopwatch.GetTimestamp();
            _enabled = true;
        }

        private static void Advance(int frame)
        {
            if (frame == _frame) return;
            if (_frame >= 0)
            {
                long rootTicks = 0;
                for (int i = 0; i < Frame.Length; i++)
                {
                    Stat f = Frame[i];
                    ref Stat w = ref Window[i];
                    w.Ticks += f.Ticks;
                    w.Bytes += f.Bytes;
                    w.Calls += f.Calls;
                    w.MaxFrameTicks = Math.Max(w.MaxFrameTicks, f.Ticks);
                    w.MaxFrameBytes = Math.Max(w.MaxFrameBytes, f.Bytes);
                    // These Unity callbacks are disjoint. All child phases below
                    // are inclusive and must not be added to their parent phase.
                    if (i == (int)Phase.Units || i == (int)Phase.Hud ||
                        i == (int)Phase.HudLate || i == (int)Phase.Combat ||
                        i == (int)Phase.Smoke || i == (int)Phase.Projectiles ||
                        i == (int)Phase.BuildingProjection || i == (int)Phase.Interaction ||
                        i == (int)Phase.WorkerTasks)
                        rootTicks += f.Ticks;
                }
                if (rootTicks > _slowTicks)
                {
                    _slowTicks = rootTicks;
                    _slowFrame = _frame;
                    Array.Copy(Frame, SlowestFrame, Frame.Length);
                }
                Array.Clear(Frame, 0, Frame.Length);
                _lastFrame = _frame;
            }
            _frame = frame;
            if (Application.isFocused) _focusedFrames++;
        }

        internal static void Report(float now, bool force = false)
        {
            if (!_enabled || (!force && now - _windowStart < 5f)) return;
            Advance(Time.frameCount);
            int frames = Math.Max(1, _lastFrame - _firstFrame + 1);
            double ms = 1000.0 / Stopwatch.Frequency;
            var b = new StringBuilder(4096);
            b.Append("[C2:PHASE COST] timeSec=").Append(F(now)).Append(" elapsedSec=").Append(F((Stopwatch.GetTimestamp()-_windowStartTicks)/(double)Stopwatch.Frequency));
            b.Append(" frames=").Append(frames).Append(" focusedFrames=").Append(_focusedFrames);
            b.Append(" units=").Append(C2NeutralPeasantUnitInfoV2LikeOriginal.C2ActiveUnitCountForDiagnostics);
            b.Append(" detailedPerUnit=").Append(DetailedPerUnit);
            b.Append(" values=avgMs/peakFrameMs/avgBytes/peakFrameBytes/calls inclusive=true alloc=UnityFrameCounter allocAvailable=").Append(_allocated.Valid);
            for (int i = 0; i < Window.Length; i++)
            {
                Stat s = Window[i];
                b.Append(" | ").Append(Names[i]).Append('=');
                b.Append(F(s.Ticks * ms / frames)).Append('/').Append(F(s.MaxFrameTicks * ms));
                b.Append('/').Append(s.Bytes / frames).Append('/').Append(s.MaxFrameBytes).Append('/').Append(s.Calls);
            }
            b.Append(" | slowFrame=").Append(_slowFrame).Append(" measuredCallbacksMs=").Append(F(_slowTicks * ms));
            for (int i = 0; i < SlowestFrame.Length; i++)
            {
                Stat s = SlowestFrame[i];
                if (s.Calls == 0) continue;
                b.Append(" ").Append(Names[i]).Append(':').Append(F(s.Ticks * ms)).Append("ms,").Append(s.Bytes).Append('B');
            }
            // Formatting/logging happens outside measured scopes, once per window.
            Debug.Log(b.ToString());
            Array.Clear(Window, 0, Window.Length);
            Array.Clear(SlowestFrame, 0, SlowestFrame.Length);
            _slowTicks = 0;
            _slowFrame = -1;
            _firstFrame = _frame;
            _focusedFrames = Application.isFocused ? 1 : 0;
            _windowStart = now;
            _windowStartTicks = Stopwatch.GetTimestamp();
        }

        internal static void Stop()
        {
            Report(Time.realtimeSinceStartup, true);
            _enabled = false;
            if (_allocated.Valid) _allocated.Dispose();
        }
        private static string F(double n) => n.ToString("0.000", CultureInfo.InvariantCulture);
    }
}
