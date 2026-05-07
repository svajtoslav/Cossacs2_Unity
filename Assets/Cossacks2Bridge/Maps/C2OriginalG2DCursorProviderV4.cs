using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Original Cossacks 2 cursor provider:
    // NewMon.cpp: GPS.PreLoadGPImage("Cursor_00"), "Cursor_01", "Cursor_02"
    // Interface.cpp: SetCurPtr(v) => curptr=v
    // Mouse_X.cpp: GPS.ShowGP(MX, MY, CurrentCursorGP, curptr, 0)
    public static class C2OriginalG2DCursorProviderV4
    {
        public const string Contract = "V4_ORIGINAL_CURSOR_G2D_CURPTR_CURSOR00_01_02";

        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _loadedG2D = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _decodedDirs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _loggedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static Type _bridgeType;
        private static bool _bridgeScanned;
        private static Texture2D _lastFallbackAtlas;

        public static Sprite LoadCursorFrame(int curptr, out string source)
        {
            source = string.Empty;
            int frame = Mathf.Clamp(curptr, 0, 255);
            string key = "cursor|" + frame.ToString(CultureInfo.InvariantCulture);
            if (_spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                source = "cache Cursor_00.g2d curptr=" + frame.ToString(CultureInfo.InvariantCulture);
                return cached;
            }

            // Original game preloads all three packages. CurrentCursorGP normally points to Cursor_00.
            // If a frame is missing in Cursor_00, we still probe 01/02 because the clean data set has all three files.
            string[] packages = { "Cursor_00", "Cursor_01", "Cursor_02", "CursGo" };
            for (int i = 0; i < packages.Length; i++)
            {
                string abs = FindOriginalG2D(packages[i]);
                if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                    continue;

                Texture2D tex = TryLoadG2DFrame(abs, frame, out string audit);
                if (tex != null)
                {
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.0f, 1.0f), 100.0f, 0, SpriteMeshType.FullRect);
                    sp.name = "C2_OriginalCursor_" + packages[i] + "_curptr_" + frame.ToString(CultureInfo.InvariantCulture);
                    _spriteCache[key] = sp;
                    source = packages[i] + ".g2d curptr=" + frame.ToString(CultureInfo.InvariantCulture) + " " + audit;
                    return sp;
                }

                source = packages[i] + ".g2d failed: " + audit;
            }

            // Non-original last resort only: keeps debugging visible if the G2D decoder is absent.
            Sprite fallback = TryLoadLegacyAtlasFallback(frame, out string fallbackAudit);
            if (fallback == null)
                fallback = CreateVisibleFallbackCursor(frame, "C2_OriginalCursor_Fallback_curptr_" + frame.ToString(CultureInfo.InvariantCulture));
            _spriteCache[key] = fallback;
            source = "fallback_only_g2d_not_decoded curptr=" + frame.ToString(CultureInfo.InvariantCulture) + " " + source + " " + fallbackAudit;
            return fallback;
        }

        public static Sprite LoadMovePulseFrame(int frame, out string source)
        {
            int f = Mathf.Clamp(frame, 0, 255);
            source = "Interf3\\moveon frame=" + f.ToString(CultureInfo.InvariantCulture);
            return C2GameplayOriginalSpriteCacheV1.LoadSprite("Interf3\\moveon", f, "C2_Original_MoveOn_Pulse");
        }

        public static Sprite LoadExitPointFrame(int frame, out string source)
        {
            int f = Mathf.Clamp(frame, 0, 255);
            source = "Interf3\\exitpoint frame=" + f.ToString(CultureInfo.InvariantCulture);
            return C2GameplayOriginalSpriteCacheV1.LoadSprite("Interf3\\exitpoint", f, "C2_Original_ExitPoint_Pulse");
        }

        private static string FindOriginalG2D(string nameNoExt)
        {
            string data = Application.dataPath;
            string resources = Path.Combine(data, "Resources");
            string streaming = Application.streamingAssetsPath;
            string[] roots =
            {
                resources,
                Path.Combine(resources, "Cash"),
                Path.Combine(resources, "Interf3"),
                Path.Combine(streaming, "Cossacks2", "Data"),
                Path.Combine(streaming, "Cossacks2", "Data", "Cash"),
                Path.Combine(data, "..", "Data")
            };

            for (int i = 0; i < roots.Length; i++)
            {
                string p = Path.Combine(roots[i], nameNoExt + ".g2d");
                if (File.Exists(p)) return p;
                p = Path.Combine(roots[i], nameNoExt + ".G2D");
                if (File.Exists(p)) return p;
            }
            return string.Empty;
        }

        private static Texture2D TryLoadG2DFrame(string abs, int frameIndex, out string source)
        {
            source = string.Empty;
            try
            {
                Texture2D decoded = TryLoadG2DFrameViaDecodeToFrames(abs, frameIndex, out string decodeAudit);
                if (decoded != null)
                {
                    source = decodeAudit;
                    return decoded;
                }

                Type bridge = ResolveBridgeType();
                if (bridge == null)
                {
                    source = "DecodeToFrames failed: " + decodeAudit + "; bridge_not_found";
                    return null;
                }

                if (!_loadedG2D.Contains(abs))
                {
                    TryInvokeG2DLoad(bridge, abs, out string loadAudit);
                    _loadedG2D.Add(abs);
                    source = loadAudit;
                }

                Texture2D tex = TryInvokeG2DFrameTexture(bridge, abs, frameIndex, out string texAudit);
                if (tex != null)
                {
                    source = (string.IsNullOrWhiteSpace(source) ? string.Empty : source + " ") + texAudit;
                    return tex;
                }

                byte[] rgba = TryInvokeG2DFrameRgba(bridge, abs, frameIndex, out int w, out int h, out string rgbaAudit);
                if (rgba != null && w > 0 && h > 0 && rgba.Length >= w * h * 4)
                {
                    tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
                    tex.name = "C2_G2D_Cursor_RGBA_" + Path.GetFileNameWithoutExtension(abs) + "_" + frameIndex.ToString(CultureInfo.InvariantCulture);
                    tex.LoadRawTextureData(rgba);
                    tex.Apply(false, false);
                    source = (string.IsNullOrWhiteSpace(source) ? string.Empty : source + " ") + rgbaAudit;
                    return tex;
                }

                source = (string.IsNullOrWhiteSpace(source) ? string.Empty : source + " ") + texAudit + " " + rgbaAudit + " " + decodeAudit;
                return null;
            }
            catch (Exception ex)
            {
                source = "G2D exception=" + ex.GetType().Name + ":" + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return null;
            }
        }

        private static Type ResolveBridgeType()
        {
            if (_bridgeScanned) return _bridgeType;
            _bridgeScanned = true;

            _bridgeType = Type.GetType("TemnyLessCodec.MelinojaCodecBridge, TemnyLessCodec.Runtime", false)
                       ?? Type.GetType("TemnyLessCodec.MelinojaCodecBridge, Assembly-CSharp", false)
                       ?? Type.GetType("TemnyLessCodec.MelinojaCodecBridge, Melinoja", false)
                       ?? Type.GetType("MelinojaCodecBridge, Assembly-CSharp", false)
                       ?? Type.GetType("MelinojaCodecBridge, Melinoja", false);

            if (_bridgeType != null) return _bridgeType;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int ai = 0; assemblies != null && ai < assemblies.Length; ai++)
            {
                Type[] types = null;
                try { types = assemblies[ai].GetTypes(); } catch { continue; }
                for (int ti = 0; types != null && ti < types.Length; ti++)
                {
                    Type t = types[ti];
                    if (t == null) continue;
                    string n = t.FullName ?? t.Name ?? string.Empty;
                    if (n.IndexOf("MelinojaCodecBridge", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("CodecBridge", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        MethodInfo m = t.GetMethod("LoadG2DToMemory", BindingFlags.Public | BindingFlags.Static)
                                    ?? t.GetMethod("LoadG2D", BindingFlags.Public | BindingFlags.Static)
                                    ?? t.GetMethod("DecodeG2DToLogAndFrames", BindingFlags.Public | BindingFlags.Static);
                        if (m != null)
                        {
                            _bridgeType = t;
                            return _bridgeType;
                        }
                    }
                }
            }
            return null;
        }

        private static bool TryInvokeG2DLoad(Type bridgeType, string abs, out string audit)
        {
            audit = "G2DLoad:none";
            if (bridgeType == null) return false;

            string[] names = { "LoadG2DToMemory", "LoadG2D", "LoadGP2DToMemory", "LoadSpritePackageToMemory" };
            for (int i = 0; i < names.Length; i++)
            {
                MethodInfo mi = bridgeType.GetMethod(names[i], BindingFlags.Public | BindingFlags.Static);
                if (mi == null) continue;

                try
                {
                    ParameterInfo[] ps = mi.GetParameters();
                    object result = null;
                    if (ps.Length == 3)
                    {
                        object[] args = { abs, null, false };
                        result = mi.Invoke(null, args);
                        bool ok = !(result is bool b) || b;
                        audit = names[i] + " ok=" + ok + " err=" + (args[1] as string ?? string.Empty);
                        return ok;
                    }
                    if (ps.Length == 2)
                    {
                        object[] args = { abs, null };
                        result = mi.Invoke(null, args);
                        bool ok = !(result is bool b) || b;
                        audit = names[i] + " ok=" + ok + " err=" + (args[1] as string ?? string.Empty);
                        return ok;
                    }
                    if (ps.Length == 1)
                    {
                        object[] args = { abs };
                        result = mi.Invoke(null, args);
                        bool ok = !(result is bool b) || b;
                        audit = names[i] + " ok=" + ok;
                        return ok;
                    }
                }
                catch (Exception ex)
                {
                    audit = names[i] + " failed=" + ex.GetType().Name + ":" + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                }
            }

            return false;
        }

        private static Texture2D TryInvokeG2DFrameTexture(Type bridgeType, string abs, int frameIndex, out string audit)
        {
            audit = "G2DTexture:none";
            if (bridgeType == null) return null;

            string[] names = { "TryGetG2DFrameTexture", "TryGetG2DFrameTexture2D", "GetG2DFrameTexture", "GetG2DFrameTexture2D" };
            for (int i = 0; i < names.Length; i++)
            {
                MethodInfo mi = bridgeType.GetMethod(names[i], BindingFlags.Public | BindingFlags.Static);
                if (mi == null) continue;

                try
                {
                    ParameterInfo[] ps = mi.GetParameters();
                    object result = null;
                    if (ps.Length == 4)
                    {
                        object[] args = { abs, frameIndex, null, null };
                        result = mi.Invoke(null, args);
                        Texture2D tex = args[2] as Texture2D ?? result as Texture2D;
                        bool ok = result is bool b ? b : tex != null;
                        audit = names[i] + " ok=" + ok + " err=" + (args[3] as string ?? string.Empty);
                        if (ok && tex != null) return tex;
                    }
                    else if (ps.Length == 3)
                    {
                        object[] args = { abs, frameIndex, null };
                        result = mi.Invoke(null, args);
                        Texture2D tex = args[2] as Texture2D ?? result as Texture2D;
                        bool ok = result is bool b ? b : tex != null;
                        audit = names[i] + " ok=" + ok;
                        if (ok && tex != null) return tex;
                    }
                    else if (ps.Length == 2)
                    {
                        object[] args = { abs, frameIndex };
                        result = mi.Invoke(null, args);
                        Texture2D tex = result as Texture2D;
                        audit = names[i] + " tex=" + (tex != null);
                        if (tex != null) return tex;
                    }
                }
                catch (Exception ex)
                {
                    audit = names[i] + " failed=" + ex.GetType().Name + ":" + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                }
            }

            return null;
        }

        private static byte[] TryInvokeG2DFrameRgba(Type bridgeType, string abs, int frameIndex, out int w, out int h, out string audit)
        {
            w = 0; h = 0;
            audit = "G2DRGBA:none";
            if (bridgeType == null) return null;

            string[] names = { "TryGetG2DFrameRGBA", "TryGetG2DFrameRgba", "TryGetG2DFramePixelsRGBA", "TryGetSpritePackageFrameRGBA" };
            for (int i = 0; i < names.Length; i++)
            {
                MethodInfo mi = bridgeType.GetMethod(names[i], BindingFlags.Public | BindingFlags.Static);
                if (mi == null) continue;

                try
                {
                    ParameterInfo[] ps = mi.GetParameters();
                    object result = null;
                    if (ps.Length == 6)
                    {
                        object[] args = { abs, frameIndex, 0, 0, null, null };
                        result = mi.Invoke(null, args);
                        bool ok = result is bool b && b;
                        w = args[2] is int iw ? iw : 0;
                        h = args[3] is int ih ? ih : 0;
                        byte[] rgba = args[4] as byte[];
                        audit = names[i] + " ok=" + ok + " size=" + w + "x" + h + " err=" + (args[5] as string ?? string.Empty);
                        if (ok && rgba != null) return rgba;
                    }
                    else if (ps.Length == 5)
                    {
                        object[] args = { abs, frameIndex, 0, 0, null };
                        result = mi.Invoke(null, args);
                        bool ok = result is bool b && b;
                        w = args[2] is int iw ? iw : 0;
                        h = args[3] is int ih ? ih : 0;
                        byte[] rgba = args[4] as byte[];
                        audit = names[i] + " ok=" + ok + " size=" + w + "x" + h;
                        if (ok && rgba != null) return rgba;
                    }
                }
                catch (Exception ex)
                {
                    audit = names[i] + " failed=" + ex.GetType().Name + ":" + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                }
            }

            return null;
        }

        private static Texture2D TryLoadG2DFrameViaDecodeToFrames(string abs, int frameIndex, out string source)
        {
            source = "DecodeG2DToLogAndFrames:none";
            try
            {
                string outDir = GetDecodeCacheDir(abs);
                if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                string joinedDirs;
                if (!_decodedDirs.TryGetValue(abs, out joinedDirs) || string.IsNullOrWhiteSpace(joinedDirs))
                {
                    bool decoded = TryInvokeAnyG2DDecodeToFrames(abs, outDir, out string audit);
                    List<string> dirs = BuildFrameSearchDirs(abs, outDir);
                    joinedDirs = string.Join("|", dirs.ToArray());
                    _decodedDirs[abs] = joinedDirs;
                    source = "DecodeG2DToLogAndFrames decoded=" + decoded + " dirs=" + joinedDirs + " audit=" + audit;
                }
                else
                {
                    source = "DecodeG2DToLogAndFrames cached_dirs=" + joinedDirs;
                }

                string file = FindDecodedFrameFileInDirs(joinedDirs, frameIndex);
                if (string.IsNullOrWhiteSpace(file))
                {
                    source += " no_frame_file frame=" + frameIndex.ToString(CultureInfo.InvariantCulture);
                    return null;
                }

                Texture2D tex = LoadDecodedFrameTexture(file, out string loadAudit);
                source += " frameFile=" + file + " " + loadAudit;
                return tex;
            }
            catch (Exception ex)
            {
                source = "DecodeG2DToLogAndFrames exception=" + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }

        private static bool TryInvokeAnyG2DDecodeToFrames(string abs, string outDir, out string audit)
        {
            audit = "not_found";
            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); } catch { continue; }
                    for (int ti = 0; ti < types.Length; ti++)
                    {
                        Type t = types[ti];
                        if (t == null) continue;
                        MethodInfo[] methods;
                        try { methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static); } catch { continue; }
                        for (int mi = 0; mi < methods.Length; mi++)
                        {
                            MethodInfo m = methods[mi];
                            if (m == null || !string.Equals(m.Name, "DecodeG2DToLogAndFrames", StringComparison.Ordinal))
                                continue;

                            string sig = (t.FullName ?? t.Name) + "." + m.Name;
                            if (_loggedMethods.Add(sig))
                                Debug.Log("[C2:ORIGINAL CURSOR G2D API V4] found " + sig);

                            if (TryInvokeDecodeMethod(m, abs, outDir, out string methodAudit))
                            {
                                audit = sig + " -> " + methodAudit;
                                return true;
                            }

                            audit = sig + " failed: " + methodAudit;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                audit = "exception=" + ex.GetType().Name + ":" + ex.Message;
            }
            return false;
        }

        private static bool TryInvokeDecodeMethod(MethodInfo m, string abs, string outDir, out string audit)
        {
            audit = "invoke:none";
            try
            {
                ParameterInfo[] ps = m.GetParameters();
                object[] args = new object[ps.Length];
                int stringInCount = 0;

                for (int i = 0; i < ps.Length; i++)
                {
                    Type pt = ps[i].ParameterType;
                    bool byRef = pt.IsByRef;
                    Type et = byRef ? pt.GetElementType() : pt;
                    string pn = (ps[i].Name ?? string.Empty).ToLowerInvariant();

                    if (byRef)
                    {
                        if (et == typeof(string)) args[i] = null;
                        else if (et == typeof(int)) args[i] = 0;
                        else if (et == typeof(bool)) args[i] = false;
                        else args[i] = null;
                        continue;
                    }

                    if (et == typeof(string))
                    {
                        if (pn.Contains("out") || pn.Contains("dir") || pn.Contains("folder") || pn.Contains("frame"))
                            args[i] = outDir;
                        else if (stringInCount == 0)
                            args[i] = abs;
                        else
                            args[i] = outDir;
                        stringInCount++;
                    }
                    else if (et == typeof(bool)) args[i] = true;
                    else if (et == typeof(int)) args[i] = 0;
                    else args[i] = null;
                }

                object result = m.Invoke(null, args);
                bool ok = true;
                if (result is bool rb) ok = rb;
                else if (result is int ri) ok = ri >= 0;

                string outs = string.Empty;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i].ParameterType.IsByRef)
                        outs += " out" + i.ToString(CultureInfo.InvariantCulture) + "=" + (args[i] == null ? "null" : args[i].ToString());
                }

                audit = "ok=" + ok + " result=" + (result == null ? "null" : result.ToString()) + outs;
                return ok;
            }
            catch (Exception ex)
            {
                audit = "exception=" + ex.GetType().Name + ":" + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return false;
            }
        }

        private static string GetDecodeCacheDir(string abs)
        {
            string root = Application.temporaryCachePath;
            if (string.IsNullOrWhiteSpace(root)) root = Path.Combine(Application.dataPath, "..", "Library");
            return Path.Combine(root, "C2OriginalCursorG2DFrames", MakeSafeFileName(Path.GetFileNameWithoutExtension(abs) ?? "cursor"));
        }

        private static List<string> BuildFrameSearchDirs(string abs, string requestedOutDir)
        {
            List<string> dirs = new List<string>();
            AddDir(dirs, requestedOutDir);
            string root = Path.GetDirectoryName(abs) ?? string.Empty;
            string baseNoExt = Path.GetFileNameWithoutExtension(abs) ?? string.Empty;
            AddDir(dirs, Path.Combine(root, baseNoExt + "_frames"));
            AddDir(dirs, Path.Combine(root, baseNoExt + ".g2d_frames"));
            AddDir(dirs, Path.Combine(root, baseNoExt + "_G2D_frames"));
            AddDir(dirs, Path.Combine(Application.dataPath, "Resources", baseNoExt + "_frames"));
            return dirs;
        }

        private static void AddDir(List<string> dirs, string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return;
            if (!dirs.Contains(dir)) dirs.Add(dir);
        }

        private static string FindDecodedFrameFileInDirs(string joinedDirs, int frameIndex)
        {
            if (string.IsNullOrWhiteSpace(joinedDirs)) return string.Empty;
            string[] dirs = joinedDirs.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < dirs.Length; i++)
            {
                string file = FindDecodedFrameFile(dirs[i], frameIndex);
                if (!string.IsNullOrWhiteSpace(file)) return file;
            }
            return string.Empty;
        }

        private static string FindDecodedFrameFile(string outDir, int frameIndex)
        {
            if (string.IsNullOrWhiteSpace(outDir) || !Directory.Exists(outDir)) return string.Empty;

            string f4 = "frame_" + frameIndex.ToString("D4", CultureInfo.InvariantCulture);
            string f0 = "frame_" + frameIndex.ToString(CultureInfo.InvariantCulture);
            string n4 = frameIndex.ToString("D4", CultureInfo.InvariantCulture);
            string n0 = frameIndex.ToString(CultureInfo.InvariantCulture);
            string[] exact =
            {
                Path.Combine(outDir, f4 + ".tga"), Path.Combine(outDir, f4 + ".png"),
                Path.Combine(outDir, f0 + ".tga"), Path.Combine(outDir, f0 + ".png"),
                Path.Combine(outDir, n4 + ".tga"), Path.Combine(outDir, n4 + ".png"),
                Path.Combine(outDir, n0 + ".tga"), Path.Combine(outDir, n0 + ".png")
            };
            for (int i = 0; i < exact.Length; i++)
                if (File.Exists(exact[i])) return exact[i];

            try
            {
                string[] files = Directory.GetFiles(outDir, "*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    string ext = Path.GetExtension(files[i]) ?? string.Empty;
                    if (!ext.Equals(".tga", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
                        continue;
                    string name = Path.GetFileNameWithoutExtension(files[i]) ?? string.Empty;
                    if (name.Equals(f4, StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(f0, StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(n4, StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(n0, StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("_" + n4, StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("_" + n0, StringComparison.OrdinalIgnoreCase) ||
                        name.IndexOf(f4, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf(f0, StringComparison.OrdinalIgnoreCase) >= 0)
                        return files[i];
                }
            }
            catch { }

            return string.Empty;
        }

        private static Texture2D LoadDecodedFrameTexture(string path, out string audit)
        {
            audit = string.Empty;
            try
            {
                string ext = Path.GetExtension(path) ?? string.Empty;
                if (ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                    if (tex.LoadImage(bytes, false))
                    {
                        audit = "png " + tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture);
                        return tex;
                    }
                    audit = "png LoadImage false";
                    return null;
                }

                if (ext.Equals(".tga", StringComparison.OrdinalIgnoreCase))
                    return LoadTgaTexture(path, out audit);

                audit = "unsupported ext=" + ext;
                return null;
            }
            catch (Exception ex)
            {
                audit = "load exception=" + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }

        private static Texture2D LoadTgaTexture(string path, out string audit)
        {
            audit = string.Empty;
            try
            {
                byte[] d = File.ReadAllBytes(path);
                if (d == null || d.Length < 18)
                {
                    audit = "tga too small";
                    return null;
                }

                int idLen = d[0];
                int colorMapType = d[1];
                int imageType = d[2];
                int w = d[12] | (d[13] << 8);
                int h = d[14] | (d[15] << 8);
                int bpp = d[16];
                int desc = d[17];

                if (w <= 0 || h <= 0 || colorMapType != 0 || (imageType != 2 && imageType != 3 && imageType != 10) || (bpp != 24 && bpp != 32 && bpp != 8))
                {
                    audit = "unsupported tga type=" + imageType + " bpp=" + bpp + " cmap=" + colorMapType + " size=" + w + "x" + h;
                    return null;
                }

                Color32[] pix = new Color32[w * h];
                int p = 18 + idLen;
                bool topOrigin = (desc & 0x20) != 0;

                Action<int, byte, byte, byte, byte> put = (idx, r, g, b, a) =>
                {
                    int x = idx % w;
                    int y = idx / w;
                    int dy = topOrigin ? (h - 1 - y) : y;
                    if (x >= 0 && x < w && dy >= 0 && dy < h)
                        pix[dy * w + x] = new Color32(r, g, b, a);
                };

                int outIdx = 0;
                if (imageType == 2 || imageType == 3)
                {
                    while (outIdx < pix.Length && p < d.Length)
                    {
                        byte b, g, r, a;
                        if (bpp == 8)
                        {
                            byte v = d[p++]; b = g = r = v; a = 255;
                        }
                        else
                        {
                            b = d[p++]; g = d[p++]; r = d[p++]; a = (bpp == 32 && p < d.Length) ? d[p++] : (byte)255;
                        }
                        put(outIdx++, r, g, b, a);
                    }
                }
                else
                {
                    while (outIdx < pix.Length && p < d.Length)
                    {
                        byte packet = d[p++];
                        int count = (packet & 0x7F) + 1;
                        bool rle = (packet & 0x80) != 0;
                        if (rle)
                        {
                            byte b, g, r, a;
                            if (bpp == 8)
                            {
                                byte v = d[p++]; b = g = r = v; a = 255;
                            }
                            else
                            {
                                b = d[p++]; g = d[p++]; r = d[p++]; a = (bpp == 32 && p < d.Length) ? d[p++] : (byte)255;
                            }
                            for (int i = 0; i < count && outIdx < pix.Length; i++)
                                put(outIdx++, r, g, b, a);
                        }
                        else
                        {
                            for (int i = 0; i < count && outIdx < pix.Length && p < d.Length; i++)
                            {
                                byte b, g, r, a;
                                if (bpp == 8)
                                {
                                    byte v = d[p++]; b = g = r = v; a = 255;
                                }
                                else
                                {
                                    b = d[p++]; g = d[p++]; r = d[p++]; a = (bpp == 32 && p < d.Length) ? d[p++] : (byte)255;
                                }
                                put(outIdx++, r, g, b, a);
                            }
                        }
                    }
                }

                Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
                tex.SetPixels32(pix);
                tex.Apply(false, false);
                audit = "tga " + w.ToString(CultureInfo.InvariantCulture) + "x" + h.ToString(CultureInfo.InvariantCulture) + " type=" + imageType + " bpp=" + bpp;
                return tex;
            }
            catch (Exception ex)
            {
                audit = "tga exception=" + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }

        private static Sprite TryLoadLegacyAtlasFallback(int frame, out string audit)
        {
            audit = string.Empty;
            Texture2D atlas = Resources.Load<Texture2D>("textures/Cursor_00");
            _lastFallbackAtlas = atlas;
            if (atlas == null)
            {
                audit = "no textures/Cursor_00 fallback atlas";
                return null;
            }

            atlas.filterMode = FilterMode.Point;
            const int cell = 32;
            int cols = Mathf.Max(1, atlas.width / cell);
            int rows = Mathf.Max(1, atlas.height / cell);
            int col = Mathf.Clamp(frame % cols, 0, cols - 1);
            int rowTop = Mathf.Clamp(frame / cols, 0, rows - 1);
            int x = col * cell;
            int y = atlas.height - ((rowTop + 1) * cell);
            if (x < 0 || y < 0 || x + cell > atlas.width || y + cell > atlas.height)
            {
                audit = "legacy atlas invalid rect";
                return null;
            }

            Sprite sp = Sprite.Create(atlas, new Rect(x, y, cell, cell), new Vector2(0.0f, 1.0f), 100.0f, 0, SpriteMeshType.FullRect);
            sp.name = "C2_Cursor_LAST_RESORT_TGA_frame_" + frame.ToString(CultureInfo.InvariantCulture);
            audit = "LAST_RESORT textures/Cursor_00 frameGrid=4x4";
            return sp;
        }

        private static Sprite CreateVisibleFallbackCursor(int frame, string name)
        {
            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.name = name + "_generated";
            Color clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, clear);

            Color white = Color.white;
            Color black = Color.black;
            DrawLine(tex, 2, 30, 2, 2, black);
            DrawLine(tex, 2, 30, 19, 13, black);
            DrawLine(tex, 2, 2, 9, 9, black);
            DrawLine(tex, 9, 9, 13, 3, black);
            DrawLine(tex, 4, 27, 4, 7, white);
            DrawLine(tex, 4, 27, 16, 14, white);
            DrawLine(tex, 4, 7, 10, 12, white);
            tex.Apply(false, false);
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.0f, 1.0f), 100.0f, 0, SpriteMeshType.FullRect);
            sp.name = name;
            return sp;
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                if (x0 >= 0 && y0 >= 0 && x0 < tex.width && y0 < tex.height)
                    tex.SetPixel(x0, y0, c);

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        private static string MakeSafeFileName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "cursor";
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s;
        }
    }
}
