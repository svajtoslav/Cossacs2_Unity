using System;
using System.Collections.Generic;
using System.IO;

namespace TemnyLessViewer
{
    /// <summary>
    /// Minimal C# analogue of original Cossacks II GP_System + ISM package layer for the MD viewer.
    ///
    /// Original path:
    ///   RLCRef[p1] = GPS.PreLoadGPImage(gy);
    ///   GPS/ISM owns package id, package path, lazy LoadPackage, DrawSprite/GetFrame cache.
    ///
    /// This class keeps the same separation for the viewer:
    ///   MD/UserLC -> PreLoadGPImage -> gpID/RLCRef
    ///   gpID+sprID -> lazy package load -> rendered-frame managed cache -> viewer renderer.
    ///
    /// It intentionally does NOT call G2DAnalyzer, frame_XXXX.tga, meta.txt, or any old export path.
    /// </summary>
    public sealed class C2GpSystem
    {
        private sealed class Package
        {
            public int Id;
            public string Name = "";
            public string NormalizedName = "";
            public string DataRoot = "";
            public string Path = "";
            public bool LoadAttempted;
            public bool Loaded;
            public string LastError = "";
            public C2DirectSpriteBank Bank;
            public long LastUseTick;
        }

        private struct FrameKey : IEquatable<FrameKey>
        {
            public readonly int GpId;
            public readonly int SprId;
            public readonly int NationRgb;

            public FrameKey(int gpId, int sprId)
                : this(gpId, sprId, 0)
            {
            }

            public FrameKey(int gpId, int sprId, int nationRgb)
            {
                GpId = gpId;
                SprId = sprId;
                NationRgb = nationRgb;
            }

            public bool Equals(FrameKey other)
            {
                return GpId == other.GpId && SprId == other.SprId && NationRgb == other.NationRgb;
            }

            public override bool Equals(object obj)
            {
                return obj is FrameKey && Equals((FrameKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((GpId * 397) ^ SprId) * 397 ^ NationRgb;
                }
            }

            public override string ToString()
            {
                return GpId + ":" + SprId;
            }
        }

        private sealed class CachedFrame
        {
            public FrameKey Key;
            public C2RenderedFrame Frame;
            public int Bytes;
            public LinkedListNode<FrameKey> Node;
            public long LastUseTick;
        }

        private readonly Dictionary<string, int> _idByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, Package> _packages = new Dictionary<int, Package>();
        private readonly Dictionary<FrameKey, CachedFrame> _frames = new Dictionary<FrameKey, CachedFrame>();
        private readonly LinkedList<FrameKey> _lru = new LinkedList<FrameKey>();

        private int _nextId = 1;
        private long _tick;

        public int MaxCachedFrames { get; set; } = 192;
        public long MaxCachedBytes { get; set; } = 256L * 1024L * 1024L;

        public long CacheBytes { get; private set; }
        public long Hits { get; private set; }
        public long Misses { get; private set; }
        public long Evictions { get; private set; }
        public int FailedLoads { get; private set; }

        public int PackageCount => _packages.Count;
        public int LoadedPackageCount
        {
            get
            {
                int n = 0;
                foreach (Package p in _packages.Values)
                    if (p.Loaded) n++;
                return n;
            }
        }

        public int CachedFrameCount => _frames.Count;

        public void Reset()
        {
            foreach (Package p in _packages.Values)
            {
                if (p.Bank != null) p.Bank.Clear();
            }

            _idByName.Clear();
            _packages.Clear();
            _frames.Clear();
            _lru.Clear();

            _nextId = 1;
            _tick = 0;
            CacheBytes = 0;
            Hits = 0;
            Misses = 0;
            Evictions = 0;
            FailedLoads = 0;
        }

        public int PreLoadGPImage(string gpName, string dataRoot, out string error)
        {
            error = "";
            string name = NormalizeGpName(gpName);
            if (name.Length == 0)
            {
                error = "empty gpName";
                return -1;
            }

            int existing;
            if (_idByName.TryGetValue(name, out existing))
                return existing;

            int id = _nextId++;
            Package p = new Package();
            p.Id = id;
            p.Name = gpName ?? "";
            p.NormalizedName = name;
            p.DataRoot = dataRoot ?? "";
            p.Path = ResolvePackagePath(gpName, dataRoot);

            _packages[id] = p;
            _idByName[name] = id;

            if (string.IsNullOrEmpty(p.Path))
                error = "package path not found";

            return id;
        }

        public bool LoadGP(int gpID, out string error)
        {
            error = "";
            Package p;
            if (!_packages.TryGetValue(gpID, out p))
            {
                error = "unknown gpID " + gpID;
                return false;
            }

            p.LastUseTick = ++_tick;

            if (p.Loaded && p.Bank != null)
                return true;

            if (p.LoadAttempted && !string.IsNullOrEmpty(p.LastError))
            {
                error = p.LastError;
                return false;
            }

            p.LoadAttempted = true;

            if (string.IsNullOrEmpty(p.Path) || !File.Exists(p.Path))
            {
                p.LastError = "package file not found: " + p.Name;
                FailedLoads++;
                error = p.LastError;
                return false;
            }

            C2DirectSpriteBank bank = new C2DirectSpriteBank();
            string err;
            if (!bank.Load(p.Path, out err))
            {
                p.LastError = err;
                FailedLoads++;
                error = err;
                return false;
            }

            p.Bank = bank;
            p.Loaded = true;
            p.LastError = "";
            return true;
        }

        public bool GetRenderedFrameNationColor(
            int gpID, int sprID, byte r, byte g, byte b,
            out C2RenderedFrame frame, out string error)
        {
            frame = null;
            error = "";
            bool mirrorX = sprID >= 4095;
            int realSprID = mirrorX ? (sprID & 4095) : sprID;
            int rgbKey = unchecked((int)0x80000000) | (r << 16) | (g << 8) | b;
            FrameKey key = new FrameKey(gpID, mirrorX ? realSprID + 4096 : realSprID, rgbKey);
            CachedFrame cached;
            if (_frames.TryGetValue(key, out cached))
            {
                Hits++;
                Touch(cached);
                frame = cached.Frame;
                return true;
            }
            Misses++;
            if (!LoadGP(gpID, out error)) return false;
            Package p = _packages[gpID];
            if (p.Bank == null) { error = "package bank is null"; return false; }
            // sgSpriteManager.inl::GetFrameInstance -> UnswizzleFrameIndex.
            // MD requests interleaved directions; G17 stores a full animation
            // for each direction. Convert once, after stripping the mirror flag.
            int bankFrame = p.Bank.UnswizzleFrameIndexLikeOriginal(realSprID);
            if (!p.Bank.RenderFrameNationColor(bankFrame, mirrorX, r, g, b, out frame, out error))
                return false;
            AddFrameToCache(key, frame);
            return true;
        }

        public bool GetRenderedFrame(int gpID, int sprID, out C2RenderedFrame frame, out string error)
        {
            frame = null;
            error = "";

            if (sprID < 0)
            {
                error = "negative sprID";
                return false;
            }

            // Original GP_System::ShowGP convention:
            //   sprID >= 4095 means "draw mirrored", real sprite is sprID & 4095.
            // Do not mirror the already composited viewer bitmap. The mirror flag must
            // reach the package renderer, because ISM flips the sprite geometry itself.
            bool mirrorX = sprID >= 4095;
            int realSprID = mirrorX ? (sprID & 4095) : sprID;

            FrameKey key = new FrameKey(gpID, mirrorX ? (realSprID + 4096) : realSprID);
            CachedFrame cf;
            if (_frames.TryGetValue(key, out cf))
            {
                Hits++;
                Touch(cf);
                frame = cf.Frame;
                return true;
            }

            Misses++;

            if (!LoadGP(gpID, out error))
                return false;

            Package p = _packages[gpID];
            if (p.Bank == null)
            {
                error = "package bank is null";
                return false;
            }

            int bankFrame = p.Bank.UnswizzleFrameIndexLikeOriginal(realSprID);
            if (!p.Bank.RenderFrame(bankFrame, mirrorX, out frame, out error))
                return false;

            AddFrameToCache(key, frame);
            return true;
        }

        public int GetFrameCount(int gpID)
        {
            string err;
            if (!LoadGP(gpID, out err))
                return 0;

            Package p;
            if (!_packages.TryGetValue(gpID, out p) || p.Bank == null)
                return 0;

            return p.Bank.FrameCount;
        }

        public string GetPackagePath(int gpID)
        {
            Package p;
            return _packages.TryGetValue(gpID, out p) ? p.Path : "";
        }

        public string GetPackageName(int gpID)
        {
            Package p;
            return _packages.TryGetValue(gpID, out p) ? p.Name : "";
        }

        public string GetPackageKind(int gpID)
        {
            Package p;
            if (!_packages.TryGetValue(gpID, out p) || p.Bank == null) return "not-loaded";
            return p.Bank.Kind.ToString();
        }

        public string MakeDebugBlock(int currentGpId, int currentSprId, string anim, int animFrame, int animFrames,
            bool selected, bool moving, float unitX, float unitY, float targetX, float targetY)
        {
            string curPath = currentGpId >= 0 ? GetPackagePath(currentGpId) : "";
            string curName = currentGpId >= 0 ? GetPackageName(currentGpId) : "";
            string shortPath = curPath;
            if (!string.IsNullOrEmpty(shortPath) && shortPath.Length > 58)
                shortPath = "..." + shortPath.Substring(shortPath.Length - 58);

            return
                "GPS/ISM managed cache V4\n" +
                "RLCRef gpID: " + currentGpId + "  sprID: " + currentSprId + "\n" +
                "package: " + (string.IsNullOrEmpty(curName) ? "-" : curName) + "\n" +
                "kind: " + (currentGpId >= 0 ? GetPackageKind(currentGpId) : "-") + "\n" +
                "path: " + (string.IsNullOrEmpty(shortPath) ? "-" : shortPath) + "\n" +
                "packages: " + PackageCount + "  loaded: " + LoadedPackageCount + "  failed: " + FailedLoads + "\n" +
                "frames cache: " + CachedFrameCount + "/" + MaxCachedFrames + "\n" +
                "cache MB: " + (CacheBytes / 1024.0 / 1024.0).ToString("0.0") + " / " + (MaxCachedBytes / 1024.0 / 1024.0).ToString("0") + "\n" +
                "hits/miss/evict: " + Hits + "/" + Misses + "/" + Evictions + "\n" +
                "anim: " + anim + "  " + animFrame + "/" + animFrames + "\n" +
                "selected: " + selected + "  moving: " + moving + "\n" +
                "unit: " + unitX.ToString("0.0") + "," + unitY.ToString("0.0") +
                "  target: " + targetX.ToString("0.0") + "," + targetY.ToString("0.0");
        }

        private void AddFrameToCache(FrameKey key, C2RenderedFrame frame)
        {
            int bytes = EstimateFrameBytes(frame);
            LinkedListNode<FrameKey> node = _lru.AddFirst(key);

            CachedFrame cf = new CachedFrame();
            cf.Key = key;
            cf.Frame = frame;
            cf.Bytes = bytes;
            cf.Node = node;
            cf.LastUseTick = ++_tick;

            _frames[key] = cf;
            CacheBytes += bytes;

            EvictIfNeeded();
        }

        private void Touch(CachedFrame cf)
        {
            cf.LastUseTick = ++_tick;
            if (cf.Node != null)
            {
                _lru.Remove(cf.Node);
                cf.Node = _lru.AddFirst(cf.Key);
            }
        }

        private void EvictIfNeeded()
        {
            while ((_frames.Count > MaxCachedFrames || CacheBytes > MaxCachedBytes) && _lru.Last != null)
            {
                FrameKey victimKey = _lru.Last.Value;
                _lru.RemoveLast();

                CachedFrame victim;
                if (_frames.TryGetValue(victimKey, out victim))
                {
                    CacheBytes -= victim.Bytes;
                    _frames.Remove(victimKey);
                    Evictions++;
                }
            }
        }

        private static int EstimateFrameBytes(C2RenderedFrame frame)
        {
            if (frame == null || frame.Rgba == null) return 0;
            return frame.Rgba.Length;
        }

        private static string NormalizeGpName(string gpName)
        {
            string s = (gpName ?? "").Trim().Replace('/', '\\');
            while (s.Contains("\\\\"))
                s = s.Replace("\\\\", "\\");
            if (s.EndsWith(".g2d", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".g16", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".d16", StringComparison.OrdinalIgnoreCase) ||
                s.EndsWith(".p16", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(0, s.Length - 4);
            }
            return s.ToLowerInvariant();
        }

        private static string ResolvePackagePath(string gpName, string dataRoot)
        {
            string rel = (gpName ?? "").Trim().Replace('/', '\\');
            if (rel.Length == 0) return "";

            string mdRoot = dataRoot ?? "";
            string[] exts = { ".g2d", ".G2D", ".g16", ".G16", ".d16", ".D16", ".p16", ".P16" };
            List<string> tries = new List<string>();

            bool hasExt = Path.HasExtension(rel);
            if (Path.IsPathRooted(rel))
            {
                if (hasExt) tries.Add(rel);
                else
                {
                    foreach (string e in exts) tries.Add(rel + e);
                }
            }
            else
            {
                if (hasExt)
                    tries.Add(Path.Combine(mdRoot, rel));
                else
                {
                    foreach (string e in exts)
                    {
                        tries.Add(Path.Combine(mdRoot, rel + e));
                        tries.Add(Path.Combine(mdRoot, "Data", rel + e));
                    }
                }
            }

            string cashStem = CashStemForRel(rel);
            string cashDir = Path.Combine(mdRoot, "Cash");
            string cashDir2 = Path.Combine(mdRoot, "Data", "Cash");

            foreach (string cd in new[] { cashDir, cashDir2 })
            {
                foreach (string e in exts)
                {
                    tries.Add(Path.Combine(cd, cashStem + e));
                    tries.Add(Path.Combine(cd, cashStem.ToUpperInvariant() + e));
                    tries.Add(Path.Combine(cd, cashStem.ToLowerInvariant() + e));
                }
            }

            foreach (string p in tries)
            {
                try
                {
                    if (File.Exists(p)) return p;
                }
                catch
                {
                }
            }

            foreach (string cd in new[] { cashDir, cashDir2 })
            {
                if (!Directory.Exists(cd)) continue;
                string wanted = Path.GetFileNameWithoutExtension(cashStem).ToLowerInvariant();
                try
                {
                    foreach (string f in Directory.GetFiles(cd))
                    {
                        string stem = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        if (stem == wanted && (ext == ".g2d" || ext == ".g16" || ext == ".d16" || ext == ".p16"))
                            return f;
                    }
                }
                catch
                {
                }
            }

            return "";
        }

        private static string CashStemForRel(string rel)
        {
            string noExt = rel ?? "";
            string ext = Path.GetExtension(noExt);
            if (!string.IsNullOrEmpty(ext))
                noExt = noExt.Substring(0, noExt.Length - ext.Length);

            char[] a = noExt.Replace('/', '\\').ToCharArray();
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] == '\\' || a[i] == '/' || a[i] == ':' || a[i] == ' ')
                    a[i] = '_';
            }
            return new string(a);
        }
    }
}
