// C2RuntimeDiagnosticsV1.cs
// V220: focused terrain-atlas diagnostics + texture memory groups + delayed FPS avg/1% low.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static class C2RuntimeDiagnosticsV1
    {
        private const int TopTextureCount = 20;
        private const int TopDuplicateCount = 20;
        private const int PerfEventCapacity = 32;
        private static readonly PerfEvent[] s_perfEvents = new PerfEvent[PerfEventCapacity];
        private static int s_perfEventCursor;
        // Detailed event strings are a diagnostic mode, not gameplay.  Keeping it
        // off prevents camera movement and texture warm-up from allocating log
        // payloads every rendered frame.  The low-cost FPS summary remains active.
        public static bool DetailedPerfEventsEnabled { get; set; }
        internal static int CameraMovingFrame = -1;

        public static void LogLoadTotalAndMemory(string source, GameObject root)
        {
            try
            {
                long totalMs = C2MapLoadProfilerV1.TotalElapsedMs;
                Debug.LogWarning("[C2:LOAD TOTAL V220] source='" + (source ?? string.Empty) + "'" +
                                 " totalMs=" + totalMs.ToString(CultureInfo.InvariantCulture) +
                                 " totalSec=" + (totalMs / 1000.0).ToString("0.000", CultureInfo.InvariantCulture) +
                                 " root='" + (root != null ? root.name : "<null>") + "'");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:LOAD TOTAL V220 FAIL] " + ex.Message);
            }

            LogMemoryBreakdown(source);
        }

        public static void AttachFpsMonitor(GameObject root, long loadTotalMs, string mapPath, string selectedId)
        {
            if (root == null) return;

            C2RuntimeFpsMonitorV1 existing = root.GetComponent<C2RuntimeFpsMonitorV1>();
            C2RuntimeFpsMonitorV1 monitor = existing != null ? existing : root.AddComponent<C2RuntimeFpsMonitorV1>();
            monitor.Configure(loadTotalMs, mapPath, selectedId);
        }

        public static void MarkPerfEvent(string kind, string detail, float elapsedMs = -1.0f)
        {
            if (!DetailedPerfEventsEnabled) return;
            try
            {
                int index = s_perfEventCursor++ % PerfEventCapacity;
                s_perfEvents[index] = new PerfEvent
                {
                    Frame = Time.frameCount,
                    Realtime = Time.realtimeSinceStartup,
                    Kind = kind ?? string.Empty,
                    Detail = detail ?? string.Empty,
                    ElapsedMs = elapsedMs
                };
            }
            catch { }
        }

        internal static string RecentPerfEvents(float now, float maxAgeSeconds)
        {
            StringBuilder sb = new StringBuilder(512);
            int written = 0;
            int cursor = s_perfEventCursor;
            for (int i = 0; i < PerfEventCapacity && written < 10; i++)
            {
                int index = (cursor - 1 - i) % PerfEventCapacity;
                if (index < 0) index += PerfEventCapacity;
                PerfEvent e = s_perfEvents[index];
                if (e.Frame <= 0 || now - e.Realtime > maxAgeSeconds)
                    continue;

                if (sb.Length > 0) sb.Append(" || ");
                sb.Append("#").Append(e.Frame.ToString(CultureInfo.InvariantCulture));
                sb.Append(" ").Append(e.Kind);
                if (e.ElapsedMs >= 0.0f)
                    sb.Append(" ").Append(e.ElapsedMs.ToString("0.00", CultureInfo.InvariantCulture)).Append("ms");
                if (!string.IsNullOrEmpty(e.Detail))
                    sb.Append(" ").Append(e.Detail);
                written++;
            }

            return sb.Length > 0 ? sb.ToString() : "<none>";
        }

        public static void LogMemoryBreakdown(string source)
        {
            long gcManaged = 0L;
            long unityAllocated = 0L;
            long unityReserved = 0L;
            long monoUsed = 0L;
            long monoHeap = 0L;
            long graphicsDriver = 0L;
            long workingSet = 0L;
            long privateBytes = 0L;
            long virtualBytes = 0L;

            try { gcManaged = GC.GetTotalMemory(false); } catch { }
            try { unityAllocated = Profiler.GetTotalAllocatedMemoryLong(); } catch { }
            try { unityReserved = Profiler.GetTotalReservedMemoryLong(); } catch { }
            try { monoUsed = Profiler.GetMonoUsedSizeLong(); } catch { }
            try { monoHeap = Profiler.GetMonoHeapSizeLong(); } catch { }
            try { graphicsDriver = GetGraphicsDriverBytes(); } catch { }

            bool processApiOk = false;
            try
            {
                using (global::System.Diagnostics.Process p = global::System.Diagnostics.Process.GetCurrentProcess())
                {
                    workingSet = p.WorkingSet64;
                    privateBytes = p.PrivateMemorySize64;
                    virtualBytes = p.VirtualMemorySize64;
                    processApiOk = workingSet > 0L || privateBytes > 0L || virtualBytes > 0L;
                }
            }
            catch { }

            TextureSummary textures = BuildTextureSummary();
            ObjectSummary meshes = BuildObjectSummary<Mesh>();
            ObjectSummary materials = BuildObjectSummary<Material>();
            ObjectSummary renderTextures = BuildObjectSummary<RenderTexture>();
            ObjectSummary audioClips = BuildObjectSummary<AudioClip>();
            ObjectSummary animationClips = BuildObjectSummary<AnimationClip>();

            Debug.LogWarning("[C2:MEMORY BREAKDOWN V220] source='" + (source ?? string.Empty) + "'" +
                             " processApiOk=" + processApiOk.ToString() +
                             " processWorkingSetMB=" + MB(workingSet) +
                             " processPrivateMB=" + MB(privateBytes) +
                             " processVirtualMB=" + MB(virtualBytes) +
                             " gcManagedMB=" + MB(gcManaged) +
                             " unityAllocatedMB=" + MB(unityAllocated) +
                             " unityReservedMB=" + MB(unityReserved) +
                             " monoUsedMB=" + MB(monoUsed) +
                             " monoHeapMB=" + MB(monoHeap) +
                             " graphicsDriverMB=" + MB(graphicsDriver) +
                             " texture2D.count=" + textures.Total.Count.ToString(CultureInfo.InvariantCulture) +
                             " texture2D.runtimeMB=" + MB(textures.Total.Bytes) +
                             " mesh.count=" + meshes.Count.ToString(CultureInfo.InvariantCulture) +
                             " mesh.runtimeMB=" + MB(meshes.Bytes) +
                             " material.count=" + materials.Count.ToString(CultureInfo.InvariantCulture) +
                             " material.runtimeMB=" + MB(materials.Bytes) +
                             " renderTexture.count=" + renderTextures.Count.ToString(CultureInfo.InvariantCulture) +
                             " renderTexture.runtimeMB=" + MB(renderTextures.Bytes) +
                             " audioClip.count=" + audioClips.Count.ToString(CultureInfo.InvariantCulture) +
                             " audioClip.runtimeMB=" + MB(audioClips.Bytes) +
                             " animationClip.count=" + animationClips.Count.ToString(CultureInfo.InvariantCulture) +
                             " animationClip.runtimeMB=" + MB(animationClips.Bytes));

            Debug.LogWarning("[C2:MEMORY TEXTURE GROUPS V220] source='" + (source ?? string.Empty) + "'" +
                             " total.count=" + textures.Total.Count.ToString(CultureInfo.InvariantCulture) +
                             " totalMB=" + MB(textures.Total.Bytes) +
                             " terrainAtlas.count=" + textures.TerrainAtlas.Count.ToString(CultureInfo.InvariantCulture) +
                             " terrainAtlasMB=" + MB(textures.TerrainAtlas.Bytes) +
                             " terrainChunks.count=" + textures.TerrainChunks.Count.ToString(CultureInfo.InvariantCulture) +
                             " terrainChunksMB=" + MB(textures.TerrainChunks.Bytes) +
                             " terrainShadow.count=" + textures.TerrainShadow.Count.ToString(CultureInfo.InvariantCulture) +
                             " terrainShadowMB=" + MB(textures.TerrainShadow.Bytes) +
                             " buildings.count=" + textures.Buildings.Count.ToString(CultureInfo.InvariantCulture) +
                             " buildingsMB=" + MB(textures.Buildings.Bytes) +
                             " unit.count=" + textures.Unit.Count.ToString(CultureInfo.InvariantCulture) +
                             " unitMB=" + MB(textures.Unit.Bytes) +
                             " water.count=" + textures.Water.Count.ToString(CultureInfo.InvariantCulture) +
                             " waterMB=" + MB(textures.Water.Bytes) +
                             " walsWalls.count=" + textures.WalsWalls.Count.ToString(CultureInfo.InvariantCulture) +
                             " walsWallsMB=" + MB(textures.WalsWalls.Bytes) +
                             " ui.count=" + textures.UI.Count.ToString(CultureInfo.InvariantCulture) +
                             " uiMB=" + MB(textures.UI.Bytes) +
                             " other.count=" + textures.Other.Count.ToString(CultureInfo.InvariantCulture) +
                             " otherMB=" + MB(textures.Other.Bytes));

            if (textures.TerrainAtlas.Count > 0)
            {
                Debug.LogWarning("[C2:TERRAIN ATLAS MEMORY V220] pages=" + textures.TerrainAtlas.Count.ToString(CultureInfo.InvariantCulture) +
                                 " runtimeMB=" + MB(textures.TerrainAtlas.Bytes) +
                                 " expectedRGB24MB=" + MB(textures.TerrainAtlasExpectedRgb24Bytes) +
                                 " expectedRGBA32MB=" + MB(textures.TerrainAtlasExpectedRgba32Bytes) +
                                 " expectedRGB565MB=" + MB(textures.TerrainAtlasExpectedRgb565Bytes) +
                                 " expectedBC1MB=" + MB(textures.TerrainAtlasExpectedBc1Bytes) +
                                 " pageSizeMin=" + textures.TerrainAtlasMinWidth.ToString(CultureInfo.InvariantCulture) + "x" + textures.TerrainAtlasMinHeight.ToString(CultureInfo.InvariantCulture) +
                                 " pageSizeMax=" + textures.TerrainAtlasMaxWidth.ToString(CultureInfo.InvariantCulture) + "x" + textures.TerrainAtlasMaxHeight.ToString(CultureInfo.InvariantCulture) +
                                 " avgRuntimeMB=" + MB(textures.TerrainAtlas.Bytes / Math.Max(1, textures.TerrainAtlas.Count)) +
                                 " readable=" + textures.TerrainAtlasReadableCount.ToString(CultureInfo.InvariantCulture) + "/" + textures.TerrainAtlas.Count.ToString(CultureInfo.InvariantCulture) +
                                 " formats='" + textures.TerrainAtlasFormats + "'");

                Debug.LogWarning("[C2:TERRAIN ATLAS SAVINGS V220] currentRuntimeMB=" + MB(textures.TerrainAtlas.Bytes) +
                                 " keepVisible1PageMB~" + MB(textures.TerrainAtlas.Bytes / Math.Max(1, textures.TerrainAtlas.Count)) +
                                 " keepVisible4PagesMB~" + MB((textures.TerrainAtlas.Bytes / Math.Max(1, textures.TerrainAtlas.Count)) * 4L) +
                                 " keepVisible6PagesMB~" + MB((textures.TerrainAtlas.Bytes / Math.Max(1, textures.TerrainAtlas.Count)) * 6L) +
                                 " allPagesRGB565ExpectedMB=" + MB(textures.TerrainAtlasExpectedRgb565Bytes) +
                                 " allPagesBC1ExpectedMB=" + MB(textures.TerrainAtlasExpectedBc1Bytes) +
                                 " note='runtimeMB is Unity profiler size; expected* is raw format estimate'");
            }

            if (!string.IsNullOrEmpty(textures.Top))
                Debug.LogWarning("[C2:MEMORY TEXTURE TOP V220] " + textures.Top);

            if (!string.IsNullOrEmpty(textures.TopNonTerrain))
                Debug.LogWarning("[C2:MEMORY NON_TERRAIN TEXTURE TOP V220] " + textures.TopNonTerrain);

            if (!string.IsNullOrEmpty(textures.Duplicates))
                Debug.LogWarning("[C2:MEMORY DUPLICATE TEXTURES V220] " + textures.Duplicates);
        }

        private static long GetGraphicsDriverBytes()
        {
            try
            {
                var mi = typeof(Profiler).GetMethod("GetAllocatedMemoryForGraphicsDriver", Type.EmptyTypes);
                if (mi == null) return 0L;
                object v = mi.Invoke(null, null);
                if (v is long) return (long)v;
                if (v is ulong) return unchecked((long)(ulong)v);
                if (v is int) return (int)v;
                return 0L;
            }
            catch
            {
                return 0L;
            }
        }

        private static ObjectSummary BuildObjectSummary<T>() where T : UnityEngine.Object
        {
            ObjectSummary s = new ObjectSummary();
            try
            {
                T[] objects = Resources.FindObjectsOfTypeAll<T>();
                if (objects == null) return s;

                s.Count = objects.Length;
                for (int i = 0; i < objects.Length; i++)
                    s.Bytes += RuntimeSize(objects[i]);
            }
            catch { }
            return s;
        }

        private static TextureSummary BuildTextureSummary()
        {
            TextureSummary s = new TextureSummary();
            List<TopTextureEntry> topAll = new List<TopTextureEntry>(256);
            List<TopTextureEntry> topNonTerrain = new List<TopTextureEntry>(256);
            Dictionary<string, DuplicateTextureEntry> duplicates = new Dictionary<string, DuplicateTextureEntry>(StringComparer.Ordinal);

            try
            {
                Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
                if (textures == null) return s;

                s.TerrainAtlasMinWidth = int.MaxValue;
                s.TerrainAtlasMinHeight = int.MaxValue;
                Dictionary<string, int> atlasFormats = new Dictionary<string, int>(StringComparer.Ordinal);

                for (int i = 0; i < textures.Length; i++)
                {
                    Texture2D t = textures[i];
                    if (t == null) continue;

                    long bytes = RuntimeSize(t);
                    string cleanName = CleanName(t.name);
                    string fullName = t.name ?? string.Empty;
                    string category = ClassifyTextureName(fullName);
                    string format = SafeTextureFormat(t);
                    bool readable = SafeIsReadable(t);

                    AddGroup(ref s.Total, bytes);
                    if (category == "terrainAtlas")
                    {
                        AddGroup(ref s.TerrainAtlas, bytes);
                        AccumulateTerrainAtlasPage(ref s, t, bytes, format, readable, atlasFormats);
                    }
                    else if (category == "terrainChunk") AddGroup(ref s.TerrainChunks, bytes);
                    else if (category == "terrainShadow") AddGroup(ref s.TerrainShadow, bytes);
                    else if (category == "building") AddGroup(ref s.Buildings, bytes);
                    else if (category == "unit") AddGroup(ref s.Unit, bytes);
                    else if (category == "water") AddGroup(ref s.Water, bytes);
                    else if (category == "walsWall") AddGroup(ref s.WalsWalls, bytes);
                    else if (category == "ui") AddGroup(ref s.UI, bytes);
                    else AddGroup(ref s.Other, bytes);

                    TopTextureEntry e = new TopTextureEntry
                    {
                        Name = cleanName,
                        Width = t.width,
                        Height = t.height,
                        Format = format,
                        Bytes = bytes,
                        Category = category,
                        Readable = readable
                    };
                    topAll.Add(e);
                    if (category != "terrainAtlas")
                        topNonTerrain.Add(e);

                    string dupKey = cleanName;
                    DuplicateTextureEntry d;
                    if (!duplicates.TryGetValue(dupKey, out d))
                    {
                        d = new DuplicateTextureEntry { Name = cleanName, Count = 0, Bytes = 0L };
                    }
                    d.Count++;
                    d.Bytes += bytes;
                    duplicates[dupKey] = d;
                }

                if (s.TerrainAtlas.Count == 0)
                {
                    s.TerrainAtlasMinWidth = 0;
                    s.TerrainAtlasMinHeight = 0;
                    s.TerrainAtlasMaxWidth = 0;
                    s.TerrainAtlasMaxHeight = 0;
                }
                s.TerrainAtlasFormats = FormatCounts(atlasFormats);

                topAll.Sort((a, b) => b.Bytes.CompareTo(a.Bytes));
                topNonTerrain.Sort((a, b) => b.Bytes.CompareTo(a.Bytes));
                s.Top = BuildTopTextureString(topAll, TopTextureCount);
                s.TopNonTerrain = BuildTopTextureString(topNonTerrain, TopTextureCount);
                s.Duplicates = BuildDuplicateString(duplicates, TopDuplicateCount);
            }
            catch (Exception ex)
            {
                s.Top = "failed:" + ex.Message;
            }

            return s;
        }

        private static void AccumulateTerrainAtlasPage(ref TextureSummary s, Texture2D t, long runtimeBytes, string format, bool readable, Dictionary<string, int> atlasFormats)
        {
            int w = Math.Max(0, t.width);
            int h = Math.Max(0, t.height);
            s.TerrainAtlasMinWidth = Math.Min(s.TerrainAtlasMinWidth, w);
            s.TerrainAtlasMinHeight = Math.Min(s.TerrainAtlasMinHeight, h);
            s.TerrainAtlasMaxWidth = Math.Max(s.TerrainAtlasMaxWidth, w);
            s.TerrainAtlasMaxHeight = Math.Max(s.TerrainAtlasMaxHeight, h);
            if (readable) s.TerrainAtlasReadableCount++;

            long pixels = (long)w * (long)h;
            s.TerrainAtlasExpectedRgb24Bytes += pixels * 3L;
            s.TerrainAtlasExpectedRgba32Bytes += pixels * 4L;
            s.TerrainAtlasExpectedRgb565Bytes += pixels * 2L;
            s.TerrainAtlasExpectedBc1Bytes += EstimateBc1Bytes(w, h);

            int count;
            atlasFormats.TryGetValue(format, out count);
            atlasFormats[format] = count + 1;
        }

        private static long EstimateBc1Bytes(int width, int height)
        {
            int bw = Math.Max(1, (width + 3) / 4);
            int bh = Math.Max(1, (height + 3) / 4);
            return (long)bw * (long)bh * 8L;
        }

        private static string ClassifyTextureName(string rawName)
        {
            string n = (rawName ?? string.Empty).ToLowerInvariant();

            if (n.Contains("terrainsoftwareatlaspage"))
                return "terrainAtlas";
            if (n.Contains("terrainchunksoftware"))
                return "terrainChunk";
            if (n.Contains("terrainshadowoverlay") || n.Contains("shadowoverlay"))
                return "terrainShadow";
            if (n.StartsWith("c2_bld_", StringComparison.Ordinal) || n.Contains("_bld_") || n.Contains("building"))
                return "building";
            if (n.StartsWith("c2viewergpframe", StringComparison.Ordinal) ||
                n.StartsWith("c2_unit_natcolor_exact", StringComparison.Ordinal) ||
                n.Contains("unit_natcolor") ||
                n.Contains("viewer_gp"))
                return "unit";
            if (n.Contains("water") || n.Contains("oblaka") || n.Contains("cloud") || n.Contains("rgbw") || n.Contains("sea2") || n.Contains("riv1"))
                return "water";
            if (n.Contains("wals") || n.Contains("wallobj") || n.Contains("wall") || n.Contains("damba"))
                return "walsWall";
            if (n.Contains("interf") || n.Contains("cursor") || n.Contains("font") || n.Contains("ui") || n.Contains("button") || n.Contains("panel") || n.Contains("icon"))
                return "ui";
            return "other";
        }

        private static string SafeTextureFormat(Texture2D t)
        {
            try { return t.format.ToString(); }
            catch { return "<format?>"; }
        }

        private static bool SafeIsReadable(Texture2D t)
        {
            try { return t.isReadable; }
            catch { return false; }
        }

        private static void AddGroup(ref ObjectSummary s, long bytes)
        {
            s.Count++;
            s.Bytes += bytes;
        }

        private static string BuildTopTextureString(List<TopTextureEntry> top, int maxCount)
        {
            if (top == null || top.Count == 0) return string.Empty;

            StringBuilder sb = new StringBuilder(4096);
            int n = Math.Min(maxCount, top.Count);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(" | ");
                TopTextureEntry e = top[i];
                sb.Append('#').Append(i + 1).Append(' ');
                sb.Append("'").Append(e.Name).Append("'");
                sb.Append(' ').Append(e.Width.ToString(CultureInfo.InvariantCulture)).Append('x').Append(e.Height.ToString(CultureInfo.InvariantCulture));
                sb.Append(' ').Append(e.Format);
                sb.Append(" cat=").Append(e.Category);
                sb.Append(" readable=").Append(e.Readable ? "1" : "0");
                sb.Append(" runtimeMB=").Append(MB(e.Bytes));
            }
            return sb.ToString();
        }

        private static string BuildDuplicateString(Dictionary<string, DuplicateTextureEntry> duplicates, int maxCount)
        {
            if (duplicates == null || duplicates.Count == 0) return string.Empty;

            List<DuplicateTextureEntry> list = new List<DuplicateTextureEntry>(duplicates.Count);
            foreach (KeyValuePair<string, DuplicateTextureEntry> kv in duplicates)
            {
                if (kv.Value.Count > 1)
                    list.Add(kv.Value);
            }
            if (list.Count == 0) return "none";

            list.Sort((a, b) =>
            {
                int c = b.Bytes.CompareTo(a.Bytes);
                if (c != 0) return c;
                return b.Count.CompareTo(a.Count);
            });

            StringBuilder sb = new StringBuilder(4096);
            int n = Math.Min(maxCount, list.Count);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(" | ");
                DuplicateTextureEntry e = list[i];
                sb.Append('#').Append(i + 1).Append(' ');
                sb.Append("'").Append(e.Name).Append("'");
                sb.Append(" count=").Append(e.Count.ToString(CultureInfo.InvariantCulture));
                sb.Append(" totalMB=").Append(MB(e.Bytes));
                sb.Append(" avgMB=").Append(MB(e.Bytes / Math.Max(1, e.Count)));
            }
            return sb.ToString();
        }

        private static string FormatCounts(Dictionary<string, int> counts)
        {
            if (counts == null || counts.Count == 0) return string.Empty;
            List<string> keys = new List<string>(counts.Keys);
            keys.Sort(StringComparer.Ordinal);
            StringBuilder sb = new StringBuilder(256);
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0) sb.Append(',');
                string k = keys[i];
                sb.Append(k).Append(':').Append(counts[k].ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private static long RuntimeSize(UnityEngine.Object obj)
        {
            if (obj == null) return 0L;
            try { return Profiler.GetRuntimeMemorySizeLong(obj); }
            catch { return 0L; }
        }

        private static string CleanName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "<unnamed>";
            s = s.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
            if (s.Length > 96) s = s.Substring(0, 96) + "...";
            return s;
        }

        private static string MB(long bytes)
        {
            return (bytes / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture);
        }

        private struct ObjectSummary
        {
            public int Count;
            public long Bytes;
        }

        private struct TextureSummary
        {
            public ObjectSummary Total;
            public ObjectSummary TerrainAtlas;
            public ObjectSummary TerrainChunks;
            public ObjectSummary TerrainShadow;
            public ObjectSummary Buildings;
            public ObjectSummary Unit;
            public ObjectSummary Water;
            public ObjectSummary WalsWalls;
            public ObjectSummary UI;
            public ObjectSummary Other;

            public long TerrainAtlasExpectedRgb24Bytes;
            public long TerrainAtlasExpectedRgba32Bytes;
            public long TerrainAtlasExpectedRgb565Bytes;
            public long TerrainAtlasExpectedBc1Bytes;
            public int TerrainAtlasMinWidth;
            public int TerrainAtlasMinHeight;
            public int TerrainAtlasMaxWidth;
            public int TerrainAtlasMaxHeight;
            public int TerrainAtlasReadableCount;
            public string TerrainAtlasFormats;

            public string Top;
            public string TopNonTerrain;
            public string Duplicates;
        }

        private struct TopTextureEntry
        {
            public string Name;
            public int Width;
            public int Height;
            public string Format;
            public string Category;
            public bool Readable;
            public long Bytes;
        }

        private struct DuplicateTextureEntry
        {
            public string Name;
            public int Count;
            public long Bytes;
        }

        private struct PerfEvent
        {
            public int Frame;
            public float Realtime;
            public string Kind;
            public string Detail;
            public float ElapsedMs;
        }
    }

    internal sealed class C2RuntimeFpsMonitorV1 : MonoBehaviour
    {
        private const float WarmupSeconds = 5.0f;
        private const float SampleWindowSeconds = 1.0f;
        private const float SummaryPeriodSeconds = 15.0f;
        private const float RecordEpsilon = 0.05f;
        private const float SlowFrameMs = 45.0f;
        private const float SlowFrameLogMinIntervalSeconds = 0.75f;
        private const int MaxStoredSamples = 600;

        private int _frames;
        private float _accum;
        private float _minFps = float.PositiveInfinity;
        private float _maxFps = 0f;
        private float _minAtRealtime;
        private float _maxAtRealtime;
        private float _nextSummaryAt;
        private float _warmupEndAt;
        private long _loadTotalMs;
        private float _configuredAtRealtime;
        private string _mapPath = string.Empty;
        private string _selectedId = string.Empty;
        private bool _configured;
        private bool _samplingStarted;
        private double _sumFps;
        private int _sampleCount;
        private readonly List<float> _samples = new List<float>(256);
        private float _nextSlowFrameLogAt;
        private int _lastGc0;
        private int _lastGc1;
        private int _lastGc2;
        private long _lastManagedBytes;

        private readonly string[] _counterNames = {
            "Main Thread", "Render Thread", "Gfx.WaitForPresentOnGfxThread", "C2.Hud.Update",
            "GC Allocated In Frame", "Draw Calls Count"
        };
        private Unity.Profiling.ProfilerRecorder[] _frameCounters;
        private double[] _counterSums;
        private long[] _counterPeaks;
        private int _counterFrames;
        private int _cameraMovingFrames;
        private float _nextFrameCountersAt;

        private void StartFrameCounters()
        {
            StopFrameCounters();
            _frameCounters = new Unity.Profiling.ProfilerRecorder[_counterNames.Length];
            _counterSums = new double[_counterNames.Length];
            _counterPeaks = new long[_counterNames.Length];
            _counterFrames = 0;
            _cameraMovingFrames = 0;
            _nextFrameCountersAt = Time.realtimeSinceStartup + 5.0f;
            for (int index = 0; index < _frameCounters.Length; index++)
            {
                var category = index == 4 ? Unity.Profiling.ProfilerCategory.Memory
                    : index == 5 ? Unity.Profiling.ProfilerCategory.Render : Unity.Profiling.ProfilerCategory.Internal;
                if (index == 3) category = Unity.Profiling.ProfilerCategory.Scripts;
                _frameCounters[index] = Unity.Profiling.ProfilerRecorder.StartNew(category, _counterNames[index], 1);
            }
        }

        private void StopFrameCounters()
        {
            if (_frameCounters == null) return;
            for (int index = 0; index < _frameCounters.Length; index++) _frameCounters[index].Dispose();
            _frameCounters = null;
        }

        private void SampleFrameCounters(float now)
        {
            if (_frameCounters == null) return;
            _counterFrames++;
            if (C2RuntimeDiagnosticsV1.CameraMovingFrame >= Time.frameCount - 1) _cameraMovingFrames++;
            for (int index = 0; index < _frameCounters.Length; index++)
            {
                if (!_frameCounters[index].Valid) continue;
                long value = _frameCounters[index].LastValue;
                _counterSums[index] += value;
                _counterPeaks[index] = Math.Max(_counterPeaks[index], value);
            }
            if (now < _nextFrameCountersAt) return;
            _nextFrameCountersAt = now + 5.0f;
            var message = new StringBuilder("[C2:FRAME COST] cameraMovingPercent=");
            message.Append((100.0 * _cameraMovingFrames / Math.Max(1, _counterFrames)).ToString("0", CultureInfo.InvariantCulture));
            for (int index = 0; index < _frameCounters.Length; index++)
            {
                message.Append(" | ").Append(_counterNames[index]).Append('=');
                if (!_frameCounters[index].Valid) message.Append("unavailable");
                else
                {
                    double scale = index < 4 ? 0.000001 : 1.0;
                    message.Append((_counterSums[index] * scale / Math.Max(1, _counterFrames)).ToString("0.00", CultureInfo.InvariantCulture));
                    message.Append(" peak=").Append((_counterPeaks[index] * scale).ToString("0.00", CultureInfo.InvariantCulture));
                    message.Append(index < 4 ? "ms" : index == 4 ? "bytes" : "calls");
                }
                _counterSums[index] = 0;
                _counterPeaks[index] = 0;
            }
            Debug.Log(message.ToString());
            _counterFrames = 0;
            _cameraMovingFrames = 0;
        }

        public void Configure(long loadTotalMs, string mapPath, string selectedId)
        {
            _loadTotalMs = loadTotalMs;
            _mapPath = mapPath ?? string.Empty;
            _selectedId = selectedId ?? string.Empty;
            _frames = 0;
            _accum = 0f;
            _minFps = float.PositiveInfinity;
            _maxFps = 0f;
            _minAtRealtime = 0f;
            _maxAtRealtime = 0f;
            _configuredAtRealtime = Time.realtimeSinceStartup;
            _warmupEndAt = _configuredAtRealtime + WarmupSeconds;
            _nextSummaryAt = _warmupEndAt + SummaryPeriodSeconds;
            _configured = true;
            _samplingStarted = false;
            _sumFps = 0.0;
            _sampleCount = 0;
            _samples.Clear();
            _nextSlowFrameLogAt = 0.0f;
            _lastGc0 = SafeGcCollectionCount(0);
            _lastGc1 = SafeGcCollectionCount(1);
            _lastGc2 = SafeGcCollectionCount(2);
            _lastManagedBytes = SafeManagedBytes();
            StartFrameCounters();

            Debug.LogWarning("[C2:FPS V220 START] map='" + _mapPath + "'" +
                             " selected='" + _selectedId + "'" +
                             " afterLoadMs=" + _loadTotalMs.ToString(CultureInfo.InvariantCulture) +
                             " warmupSec=" + WarmupSeconds.ToString("0.0", CultureInfo.InvariantCulture) +
                             " sampleWindowSec=" + SampleWindowSeconds.ToString("0.0", CultureInfo.InvariantCulture) +
                             " targetFrameRate=" + Application.targetFrameRate.ToString(CultureInfo.InvariantCulture) +
                             " vSyncCount=" + QualitySettings.vSyncCount.ToString(CultureInfo.InvariantCulture) +
                             " fixedDeltaTime=" + Time.fixedDeltaTime.ToString("0.000000", CultureInfo.InvariantCulture) +
                             " fixedUpdateHz=" + (Time.fixedDeltaTime > 0f ? (1.0f / Time.fixedDeltaTime).ToString("0.0", CultureInfo.InvariantCulture) : "0.0"));
        }

        private void Update()
        {
            if (!_configured) return;

            float now = Time.realtimeSinceStartup;
            SampleFrameCounters(now);
            float dt = Time.unscaledDeltaTime;
            if (C2RuntimeDiagnosticsV1.DetailedPerfEventsEnabled && dt > 0f && dt <= 2.0f)
                MaybeLogSlowFrame(dt, now);

            if (now < _warmupEndAt)
                return;

            if (!_samplingStarted)
            {
                _samplingStarted = true;
                _frames = 0;
                _accum = 0f;
                Debug.LogWarning("[C2:FPS V220 SAMPLING START] realtimeSec=" + now.ToString("0.0", CultureInfo.InvariantCulture) +
                                 " skippedWarmupSec=" + WarmupSeconds.ToString("0.0", CultureInfo.InvariantCulture));
            }

            if (dt <= 0f || dt > 2.0f) return;

            _frames++;
            _accum += dt;

            if (_accum >= SampleWindowSeconds)
            {
                float fps = _frames / _accum;
                AddSample(fps);

                if (fps + RecordEpsilon < _minFps)
                {
                    _minFps = fps;
                    _minAtRealtime = now;
                    LogRecord("MIN", fps, now);
                }

                if (fps > _maxFps + RecordEpsilon)
                {
                    _maxFps = fps;
                    _maxAtRealtime = now;
                    LogRecord("MAX", fps, now);
                }

                _frames = 0;
                _accum = 0f;
            }

            if (Time.realtimeSinceStartup >= _nextSummaryAt)
            {
                _nextSummaryAt = Time.realtimeSinceStartup + SummaryPeriodSeconds;
                LogSummary();
            }
        }

        private void AddSample(float fps)
        {
            _sumFps += fps;
            _sampleCount++;

            if (_samples.Count >= MaxStoredSamples)
                _samples.RemoveAt(0);
            _samples.Add(fps);
        }

        private void LogSummary()
        {
            if (!_samplingStarted || _sampleCount <= 0 || float.IsInfinity(_minFps) || _maxFps <= 0f)
                return;

            float avg = (float)(_sumFps / Math.Max(1, _sampleCount));
            float low1 = CalculateLowPercent(0.01f);
            float low5 = CalculateLowPercent(0.05f);

            Debug.LogWarning("[C2:FPS V220 SUMMARY] uptimeSec=" + Time.realtimeSinceStartup.ToString("0.0", CultureInfo.InvariantCulture) +
                             " map='" + _mapPath + "'" +
                             " samples=" + _sampleCount.ToString(CultureInfo.InvariantCulture) +
                             " minFps=" + _minFps.ToString("0.0", CultureInfo.InvariantCulture) +
                             " minAtSec=" + _minAtRealtime.ToString("0.0", CultureInfo.InvariantCulture) +
                             " maxFps=" + _maxFps.ToString("0.0", CultureInfo.InvariantCulture) +
                             " maxAtSec=" + _maxAtRealtime.ToString("0.0", CultureInfo.InvariantCulture) +
                             " avgFps=" + avg.ToString("0.0", CultureInfo.InvariantCulture) +
                             " low1pctFps=" + low1.ToString("0.0", CultureInfo.InvariantCulture) +
                             " low5pctFps=" + low5.ToString("0.0", CultureInfo.InvariantCulture) +
                             " targetFrameRate=" + Application.targetFrameRate.ToString(CultureInfo.InvariantCulture) +
                             " vSyncCount=" + QualitySettings.vSyncCount.ToString(CultureInfo.InvariantCulture));
        }

        private float CalculateLowPercent(float fraction)
        {
            if (_samples.Count == 0) return 0f;

            List<float> sorted = new List<float>(_samples);
            sorted.Sort();

            int n = Math.Max(1, (int)Math.Ceiling(sorted.Count * Mathf.Clamp01(fraction)));
            double sum = 0.0;
            for (int i = 0; i < n && i < sorted.Count; i++)
                sum += sorted[i];

            return (float)(sum / Math.Max(1, n));
        }

        private void LogRecord(string kind, float fps, float now)
        {
            Debug.LogWarning("[C2:FPS V220 RECORD] kind=" + kind +
                             " fps=" + fps.ToString("0.0", CultureInfo.InvariantCulture) +
                             " atRealtimeSec=" + now.ToString("0.0", CultureInfo.InvariantCulture) +
                             " sinceSamplingStartSec=" + (now - _warmupEndAt).ToString("0.0", CultureInfo.InvariantCulture) +
                             " date='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "'" +
                             " recentPerf='" + C2RuntimeDiagnosticsV1.RecentPerfEvents(now, 3.0f) + "'");
        }

        private void MaybeLogSlowFrame(float dt, float now)
        {
            float dtMs = dt * 1000.0f;
            if (dtMs < SlowFrameMs || now < _nextSlowFrameLogAt)
                return;

            _nextSlowFrameLogAt = now + SlowFrameLogMinIntervalSeconds;

            int gc0 = SafeGcCollectionCount(0);
            int gc1 = SafeGcCollectionCount(1);
            int gc2 = SafeGcCollectionCount(2);
            long managed = SafeManagedBytes();
            long unityAllocated = SafeProfilerBytes(() => Profiler.GetTotalAllocatedMemoryLong());
            long graphics = SafeGraphicsDriverBytesLikeOriginal();

            // Do not turn one slow editor frame (for example PrintScreen/focus loss)
            // into a much longer stall by scanning every loaded Unity object.
            int rendererCount = -1;
            int cameraCount = Camera.allCamerasCount;
            int meshCount = -1;
            int textureCount = -1;

            Debug.LogWarning("[C2:PERF SPIKE V281] frame=" + Time.frameCount.ToString(CultureInfo.InvariantCulture) +
                             " dtMs=" + dtMs.ToString("0.0", CultureInfo.InvariantCulture) +
                             " approxFps=" + (1.0f / Mathf.Max(0.0001f, dt)).ToString("0.0", CultureInfo.InvariantCulture) +
                             " realtimeSec=" + now.ToString("0.0", CultureInfo.InvariantCulture) +
                             " map='" + _mapPath + "'" +
                             " gcDelta=(" + (gc0 - _lastGc0).ToString(CultureInfo.InvariantCulture) +
                             "," + (gc1 - _lastGc1).ToString(CultureInfo.InvariantCulture) +
                             "," + (gc2 - _lastGc2).ToString(CultureInfo.InvariantCulture) + ")" +
                             " managedMB=" + MBLikeOriginal(managed) +
                             " managedDeltaMB=" + MBLikeOriginal(managed - _lastManagedBytes) +
                             " unityAllocatedMB=" + MBLikeOriginal(unityAllocated) +
                             " graphicsDriverMB=" + MBLikeOriginal(graphics) +
                             " activeRenderers=" + rendererCount.ToString(CultureInfo.InvariantCulture) +
                             " cameras=" + cameraCount.ToString(CultureInfo.InvariantCulture) +
                             " meshes=" + meshCount.ToString(CultureInfo.InvariantCulture) +
                             " textures=" + textureCount.ToString(CultureInfo.InvariantCulture) +
                             " recentPerf='" + C2RuntimeDiagnosticsV1.RecentPerfEvents(now, 3.0f) + "'");

            _lastGc0 = gc0;
            _lastGc1 = gc1;
            _lastGc2 = gc2;
            _lastManagedBytes = managed;
        }

        private static int SafeGcCollectionCount(int generation)
        {
            try { return GC.CollectionCount(generation); } catch { return 0; }
        }

        private static long SafeManagedBytes()
        {
            try { return GC.GetTotalMemory(false); } catch { return 0L; }
        }

        private static long SafeProfilerBytes(Func<long> getter)
        {
            try { return getter != null ? getter() : 0L; } catch { return 0L; }
        }

        private static long SafeGraphicsDriverBytesLikeOriginal()
        {
            try
            {
                var mi = typeof(Profiler).GetMethod("GetAllocatedMemoryForGraphicsDriver", Type.EmptyTypes);
                if (mi == null) return 0L;
                object value = mi.Invoke(null, null);
                return value is long ? (long)value : 0L;
            }
            catch
            {
                return 0L;
            }
        }

        private static int SafeObjectCount<T>() where T : UnityEngine.Object
        {
            try { return UnityEngine.Object.FindObjectsOfType<T>().Length; } catch { return -1; }
        }

        private static string MBLikeOriginal(long bytes)
        {
            return (bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.InvariantCulture);
        }

        private void OnDisable()
        {
            StopFrameCounters();
            if (_configured && _samplingStarted)
                LogSummary();
        }
    }
}
