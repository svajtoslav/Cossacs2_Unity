// C2MapLoadProfilerV1.cs
// Diagnostic map-load profiler. Safe: logging only, no gameplay/render logic changes.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static class C2MapLoadProfilerV1
    {
        public const bool Enabled = true;
        private const int SlowStageWarningMs = 1000;
        private static readonly object s_Lock = new object();
        private static readonly Stopwatch s_Total = new Stopwatch();
        private static readonly List<Entry> s_Entries = new List<Entry>(256);
        private static int s_Session;
        private static string s_MapPath = string.Empty;
        private static string s_SelectedId = string.Empty;

        private struct Entry
        {
            public string Name;
            public long Ms;
            public string Details;
            public long SinceStartMs;
        }

        public static long TotalElapsedMs
        {
            get { return s_Total.IsRunning ? s_Total.ElapsedMilliseconds : 0L; }
        }

        public static long NowMs()
        {
            return s_Total.IsRunning ? s_Total.ElapsedMilliseconds : 0L;
        }

        public static void BeginMapLoad(string mapPath, string selectedId, string source)
        {
            if (!Enabled) return;
            lock (s_Lock)
            {
                s_Session++;
                s_MapPath = mapPath ?? string.Empty;
                s_SelectedId = selectedId ?? string.Empty;
                s_Entries.Clear();
                s_Total.Reset();
                s_Total.Start();
                Debug.Log("[C2:LOAD TIMER V1 BEGIN] session=" + s_Session.ToString(CultureInfo.InvariantCulture) +
                          " source=" + (source ?? string.Empty) +
                          " map='" + s_MapPath + "'" +
                          " selected='" + s_SelectedId + "'");
            }
        }

        public static void SetMap(string mapPath, string selectedId)
        {
            if (!Enabled) return;
            lock (s_Lock)
            {
                s_MapPath = mapPath ?? string.Empty;
                s_SelectedId = selectedId ?? string.Empty;
                Debug.Log("[C2:LOAD TIMER V1 MAP] session=" + s_Session.ToString(CultureInfo.InvariantCulture) +
                          " +" + NowMs().ToString(CultureInfo.InvariantCulture) + "ms" +
                          " map='" + s_MapPath + "'" +
                          " selected='" + s_SelectedId + "'");
            }
        }

        public static ScopeMarker Scope(string name, string details = "")
        {
            return new ScopeMarker(name, details);
        }

        public static void Stage(string name, long ms, string details = "")
        {
            if (!Enabled) return;
            if (string.IsNullOrEmpty(name)) name = "<unnamed>";
            lock (s_Lock)
            {
                Entry e = new Entry
                {
                    Name = name,
                    Ms = ms,
                    Details = details ?? string.Empty,
                    SinceStartMs = NowMs()
                };
                s_Entries.Add(e);

                string line = "[C2:LOAD TIMER V1 STAGE] session=" + s_Session.ToString(CultureInfo.InvariantCulture) +
                              " +" + e.SinceStartMs.ToString(CultureInfo.InvariantCulture) + "ms" +
                              " stage='" + e.Name + "'" +
                              " ms=" + e.Ms.ToString(CultureInfo.InvariantCulture) +
                              (string.IsNullOrEmpty(e.Details) ? string.Empty : " " + e.Details);
                if (e.Ms >= SlowStageWarningMs)
                    Debug.LogWarning(line + " SLOW>=1000ms");
                else
                    Debug.Log(line);
            }
        }

        public static void Instant(string name, string details = "")
        {
            Stage(name, 0L, details);
        }

        public static void DumpTop(string label, int maxCount = 20)
        {
            if (!Enabled) return;
            lock (s_Lock)
            {
                List<Entry> copy = new List<Entry>(s_Entries);
                copy.Sort((a, b) => b.Ms.CompareTo(a.Ms));

                int n = Mathf.Clamp(maxCount, 1, 64);
                StringBuilder sb = new StringBuilder(2048);
                sb.Append("[C2:LOAD TIMER V1 SUMMARY] session=").Append(s_Session.ToString(CultureInfo.InvariantCulture));
                sb.Append(" label='").Append(label ?? string.Empty).Append("'");
                sb.Append(" totalMs=").Append(TotalElapsedMs.ToString(CultureInfo.InvariantCulture));
                sb.Append(" map='").Append(s_MapPath).Append("'");
                sb.Append(" selected='").Append(s_SelectedId).Append("'");
                sb.Append(" top=");

                int written = 0;
                for (int i = 0; i < copy.Count && written < n; i++)
                {
                    if (copy[i].Ms <= 0) continue;
                    if (written > 0) sb.Append(" | ");
                    sb.Append(copy[i].Name).Append('=').Append(copy[i].Ms.ToString(CultureInfo.InvariantCulture)).Append("ms");
                    if (!string.IsNullOrEmpty(copy[i].Details)) sb.Append('(').Append(Trim(copy[i].Details, 120)).Append(')');
                    written++;
                }
                if (written == 0) sb.Append("<none>");
                Debug.LogWarning(sb.ToString());
            }
        }

        public static void Suspect(string name, string details = "")
        {
            if (!Enabled) return;
            Debug.LogWarning("[C2:LOAD TIMER V1 SUSPECT] +" + NowMs().ToString(CultureInfo.InvariantCulture) + "ms " +
                             "stage='" + (name ?? string.Empty) + "' " + (details ?? string.Empty));
        }

        private static string Trim(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? string.Empty;
            return s.Substring(0, Math.Max(0, max)) + "...";
        }

        internal sealed class ScopeMarker : IDisposable
        {
            private readonly string _name;
            private readonly string _details;
            private readonly Stopwatch _sw;
            private bool _disposed;

            public ScopeMarker(string name, string details)
            {
                _name = name ?? "<unnamed>";
                _details = details ?? string.Empty;
                _sw = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _sw.Stop();
                C2MapLoadProfilerV1.Stage(_name, _sw.ElapsedMilliseconds, _details);
            }
        }
    }
}
