using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Shared loader for original UI/cursor sprites.  It first tries the Melinoja G16 bridge,
    // then falls back to decoded *_frames folders or normal Resources textures.
    public static class C2GameplayOriginalSpriteCacheV1
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> SourceAudit = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static Type _bridgeType;
        private static MethodInfo _loadG16ToMemory;
        private static MethodInfo _tryGetG16FrameRgba;
        private static bool _bridgeScanned;
        private static Texture2D _fallbackTex;

        public static Sprite LoadSprite(string fileId, int spriteId, string debugName)
        {
            if (spriteId < 0) spriteId = 0;
            // V134: the cache key must preserve the row-orientation class.
            // V133 keyed only by fileId+spriteId; a sprite decoded with one row policy could be reused
            // by a UI-level flipped produce portrait and become upside down.
            string key = MakeSpriteCacheKeyV134(fileId, spriteId, debugName);
            Sprite cached;
            if (SpriteCache.TryGetValue(key, out cached)) return cached;

            Sprite sp = TryLoadG16Sprite(fileId, spriteId, debugName);
            if (sp == null) sp = TryLoadFrameFolderSprite(fileId, spriteId, debugName);
            if (sp == null) sp = TryLoadResourceTextureSprite(fileId, spriteId, debugName);
            if (sp == null)
            {
                RememberSource(fileId, spriteId, "FALLBACK debug='" + (debugName ?? string.Empty) + "'");
                sp = CreateFallbackSprite(debugName, spriteId);
            }

            SpriteCache[key] = sp;
            return sp;
        }

        public static Texture2D LoadTexture(string fileId, int spriteId, string debugName)
        {
            Sprite sp = LoadSprite(fileId, spriteId, debugName);
            return sp != null ? sp.texture : null;
        }

        public static string GetSourceAudit(string fileId, int spriteId)
        {
            string value;
            return SourceAudit.TryGetValue(MakeSourceKey(fileId, spriteId), out value) ? value : "unknown";
        }

        private static Sprite TryLoadG16Sprite(string fileId, int spriteId, string debugName)
        {
            if (!EnsureBridge()) return null;

            List<string> paths = EnumerateG16Candidates(fileId);
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;

                try
                {
                    object[] loadArgs = new object[] { path, null, false };
                    bool loaded = false;
                    if (_loadG16ToMemory != null)
                        loaded = Convert.ToBoolean(_loadG16ToMemory.Invoke(null, loadArgs));
                    if (!loaded) continue;

                    object[] frameArgs = new object[] { path, spriteId, 0, 0, null, null };
                    bool ok = Convert.ToBoolean(_tryGetG16FrameRgba.Invoke(null, frameArgs));
                    if (!ok) continue;

                    int w = Convert.ToInt32(frameArgs[2]);
                    int h = Convert.ToInt32(frameArgs[3]);
                    byte[] rgba = frameArgs[4] as byte[];
                    if (w <= 0 || h <= 0 || rgba == null || rgba.Length < w * h * 4) continue;

                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
                    tex.name = SafeName(debugName, Path.GetFileNameWithoutExtension(path) + "_" + spriteId.ToString("0000"));
                    // Most original UI frames/portraits need rowFlip=1 because TryGetG16FrameRGBA returns top-down rows.
                    // Produce building mini-icons are the exception in the current bridge/resource set:
                    // Interf3\\BldSmallIcons and Interf3\\Addon\\PatchIcons were already visually upright before the global V16 flip.
                    // Applying rowFlip=1 to them makes every building card upside down and visually shifted upward.
                    bool rowFlip = ShouldFlipRowsForUiG16(fileId, path, debugName);
                    if (rowFlip) FlipRgbaRowsInPlace(rgba, w, h);
                    tex.LoadRawTextureData(rgba);
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.Apply(false, false);
                    RememberSource(fileId, spriteId, "G16 rowFlip=" + (rowFlip ? "1" : "0") + " rule=split_ui_g16 path='" + path + "' size=" + w.ToString() + "x" + h.ToString() + " debug='" + (debugName ?? string.Empty) + "'");
                    return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100.0f);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[C2:GAMEPLAY SPRITE V1] G16 load failed fileId='" + fileId + "' spr=" + spriteId +
                                     " path='" + path + "' err=" + ex.GetType().Name + ": " + ex.Message);
                }
            }

            return null;
        }

        private static void FlipRgbaRowsInPlace(byte[] rgba, int width, int height)
        {
            if (rgba == null || width <= 0 || height <= 1) return;
            int stride = width * 4;
            if (rgba.Length < stride * height) return;
            byte[] tmp = new byte[stride];
            for (int y = 0; y < height / 2; y++)
            {
                int top = y * stride;
                int bottom = (height - 1 - y) * stride;
                Buffer.BlockCopy(rgba, top, tmp, 0, stride);
                Buffer.BlockCopy(rgba, bottom, rgba, top, stride);
                Buffer.BlockCopy(tmp, 0, rgba, bottom, stride);
            }
        }

        private static bool ShouldFlipRowsForUiG16(string fileId, string path, string debugName)
        {
            if (ForceNoRowFlipForUiG16V134(fileId, path, debugName))
                return false;

            return true;
        }

        private static bool ForceNoRowFlipForUiG16V134(string fileId, string path, string debugName)
        {
            string f = ((fileId ?? string.Empty) + "|" + (path ?? string.Empty) + "|" + (debugName ?? string.Empty)).Replace('/', '\\');

            // Correct split:
            // - selected-unit card/frame sprites need raw row flip.
            // - produce building cards from BldSmallIcons/PatchIcons are already in the orientation expected by Unity after bridge decode.
            // - Units_*_mini produce portraits are flipped at the UI RectTransform level (uiFlipY=true);
            //   flipping rows here as well makes them upside down.
            if (f.IndexOf("BldSmallIcons", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (f.IndexOf("PatchIcons", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (f.IndexOf("Units_", StringComparison.OrdinalIgnoreCase) >= 0 &&
                f.IndexOf("_mini", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            return false;
        }

        private static string MakeSpriteCacheKeyV134(string fileId, int spriteId, string debugName)
        {
            bool noRowFlip = ForceNoRowFlipForUiG16V134(fileId, string.Empty, debugName);
            return "V134_HUD_SPRITE_SHARED|" + (fileId ?? string.Empty).Trim() + "|" +
                   spriteId.ToString() + "|noflip=" + (noRowFlip ? "1" : "0");
        }


        private static string MakeSourceKey(string fileId, int spriteId)
        {
            return ((fileId ?? string.Empty).Trim() + "|" + spriteId.ToString()).ToUpperInvariant();
        }

        private static void RememberSource(string fileId, int spriteId, string source)
        {
            SourceAudit[MakeSourceKey(fileId, spriteId)] = source ?? string.Empty;
        }

        private static bool EnsureBridge()
        {
            if (_bridgeScanned) return _bridgeType != null && _tryGetG16FrameRgba != null;
            _bridgeScanned = true;

            _bridgeType = Type.GetType("TemnyLessCodec.MelinojaCodecBridge, TemnyLessCodec.Runtime", false)
                       ?? Type.GetType("TemnyLessCodec.MelinojaCodecBridge, Assembly-CSharp", false)
                       ?? Type.GetType("TemnyLessCodec.MelinojaCodecBridge, Melinoja", false)
                       ?? Type.GetType("MelinojaCodecBridge, Assembly-CSharp", false)
                       ?? Type.GetType("MelinojaCodecBridge, Melinoja", false);

            if (_bridgeType == null)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length && _bridgeType == null; i++)
                {
                    try
                    {
                        _bridgeType = assemblies[i].GetType("TemnyLessCodec.MelinojaCodecBridge", false)
                                   ?? assemblies[i].GetType("MelinojaCodecBridge", false);
                    }
                    catch { }
                }
            }

            if (_bridgeType == null) return false;

            _loadG16ToMemory = _bridgeType.GetMethod("LoadG16ToMemory", BindingFlags.Public | BindingFlags.Static);
            _tryGetG16FrameRgba = _bridgeType.GetMethod("TryGetG16FrameRGBA", BindingFlags.Public | BindingFlags.Static);
            return _loadG16ToMemory != null && _tryGetG16FrameRgba != null;
        }

        private static List<string> EnumerateG16Candidates(string fileId)
        {
            List<string> result = new List<string>(64);
            string raw = (fileId ?? string.Empty).Trim().Replace('/', '\\');
            string noExt = Path.ChangeExtension(raw, null) ?? raw;
            string bare = Path.GetFileName(noExt) ?? noExt;
            string flat = noExt.Replace('\\', '_').Replace('/', '_');
            string upperFlat = flat.ToUpperInvariant();
            string lowerFlat = flat.ToLowerInvariant();

            Action<string> add = delegate (string p)
            {
                if (string.IsNullOrWhiteSpace(p)) return;
                if (!result.Contains(p)) result.Add(p);
            };

            Action<string> addForRoot = delegate (string root)
            {
                if (string.IsNullOrWhiteSpace(root)) return;
                add(Path.Combine(root, noExt + ".g16"));
                add(Path.Combine(root, flat + ".g16"));
                add(Path.Combine(root, upperFlat + ".g16"));
                add(Path.Combine(root, lowerFlat + ".g16"));

                add(Path.Combine(root, "Interf3", bare + ".g16"));
                add(Path.Combine(root, "Interf3", "Interf3_" + bare + ".g16"));
                add(Path.Combine(root, "Interf3", "INTERF3_" + bare.ToUpperInvariant() + ".g16"));
                add(Path.Combine(root, "Interf3", flat + ".g16"));
                add(Path.Combine(root, "Interf3", upperFlat + ".g16"));
                add(Path.Combine(root, "Interf3", lowerFlat + ".g16"));

                if (noExt.StartsWith("Interf3\\", StringComparison.OrdinalIgnoreCase))
                {
                    string rest = noExt.Substring("Interf3\\".Length);
                    string restFlat = "Interf3_" + rest.Replace('\\', '_').Replace('/', '_');
                    add(Path.Combine(root, "Interf3", rest + ".g16"));
                    add(Path.Combine(root, "Interf3", restFlat + ".g16"));
                    add(Path.Combine(root, "Interf3", restFlat.ToUpperInvariant() + ".g16"));
                    add(Path.Combine(root, rest + ".g16"));
                }
            };

            // V14: active/modded game Data-root wins. Unity Resources are fallback copies only.
            string[] roots = OriginalDataRootsLikeOriginal();
            for (int i = 0; i < roots.Length; i++) addForRoot(roots[i]);
            return result;
        }

        private static string[] OriginalDataRootsLikeOriginal()
        {
            return new[]
            {
                @"C:\GSC Game World\Cossacks II\Data",
                @"C:\GSC Game World\Cossacks II\Data1",
                @"C:\Program Files (x86)\GSC Game World\Cossacks II\Data",
                @"C:\Games\Cossacks II\Data",
                Path.Combine(Application.dataPath, "..", "Data"),
                Path.Combine(Application.dataPath, "..", "Cossacks2", "Data"),
                Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data"),
                Path.Combine(Application.streamingAssetsPath, "Cossacks2"),
                Application.streamingAssetsPath,
                Path.Combine(Application.dataPath, "Resources"),
                Path.Combine(Application.dataPath, "Resources", "Data")
            };
        }

        private static Sprite TryLoadFrameFolderSprite(string fileId, int spriteId, string debugName)
        {
            string raw = (fileId ?? string.Empty).Trim().Replace('/', '_').Replace('\\', '_');
            if (string.IsNullOrWhiteSpace(raw)) return null;
            string frame = "frame_" + spriteId.ToString("0000");
            string[] folders =
            {
                raw + "_frames",
                raw.ToUpperInvariant() + "_frames",
                raw.ToLowerInvariant() + "_frames",
                raw.Replace("Interf3_", "INTERF3_") + "_frames"
            };

            for (int i = 0; i < folders.Length; i++)
            {
                Texture2D tex = Resources.Load<Texture2D>(folders[i] + "/" + frame);
                if (tex == null) continue;
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                RememberSource(fileId, spriteId, "FRAME folder='" + folders[i] + "' frame='" + frame + "' debug='" + (debugName ?? string.Empty) + "'");
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            }
            return null;
        }

        private static Sprite TryLoadResourceTextureSprite(string fileId, int spriteId, string debugName)
        {
            string raw = (fileId ?? string.Empty).Trim().Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(raw)) return null;
            string noExt = Path.ChangeExtension(raw, null) ?? raw;
            string[] candidates =
            {
                noExt,
                noExt.Replace("Interf3/", "Interf3/Interf3_"),
                noExt.Replace('/', '_'),
                "textures/" + Path.GetFileName(noExt)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                Texture2D tex = Resources.Load<Texture2D>(candidates[i]);
                if (tex == null) continue;
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                RememberSource(fileId, spriteId, "RESOURCE candidate='" + candidates[i] + "' debug='" + (debugName ?? string.Empty) + "'");
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            }
            return null;
        }

        private static Sprite CreateFallbackSprite(string debugName, int spriteId)
        {
            if (_fallbackTex == null)
            {
                _fallbackTex = new Texture2D(32, 32, TextureFormat.RGBA32, false, false);
                _fallbackTex.name = "C2_Gameplay_FallbackSprite_V1";
                Color32 a = new Color32(20, 20, 20, 220);
                Color32 b = new Color32(210, 160, 40, 255);
                Color32 c = new Color32(0, 0, 0, 0);
                Color32[] px = new Color32[32 * 32];
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        bool border = x == 0 || y == 0 || x == 31 || y == 31;
                        bool diag = Math.Abs(x - y) <= 1 || Math.Abs((31 - x) - y) <= 1;
                        px[y * 32 + x] = border ? b : (diag ? a : c);
                    }
                }
                _fallbackTex.SetPixels32(px);
                _fallbackTex.filterMode = FilterMode.Point;
                _fallbackTex.wrapMode = TextureWrapMode.Clamp;
                _fallbackTex.Apply(false, false);
            }

            Sprite sp = Sprite.Create(_fallbackTex, new Rect(0, 0, _fallbackTex.width, _fallbackTex.height), new Vector2(0.5f, 0.5f), 100.0f);
            sp.name = SafeName(debugName, "Fallback_" + spriteId.ToString());
            return sp;
        }

        private static string SafeName(string preferred, string fallback)
        {
            return string.IsNullOrWhiteSpace(preferred) ? fallback : preferred.Trim();
        }
    }
}
