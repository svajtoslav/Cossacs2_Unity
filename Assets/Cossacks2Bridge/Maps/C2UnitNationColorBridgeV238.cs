// C2UnitNationColorBridgeV238.cs
// V239 replacement: exact Melinoja/CodecFacade nation-color bridge only.
// No heuristic recolor. If exact nation-color decode is unavailable, returns null;
// unit runtime falls back to plain frame instead of overpainting half the sprite.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        public static Texture2D TryLoadG2DFrameViaMelinojaNationColorV1LikeOriginal(
            string absPath,
            int frame,
            Color32 nationColor,
            out string source)
        {
            source = string.Empty;
            Texture2D tex = TryLoadG16FrameViaMelinojaNationColorV239(absPath, frame, nationColor, out source);
            if (tex != null) return tex;

            Texture2D g2d = TryLoadG2DFrameViaCodecFacadeNationColorV239(absPath, frame, nationColor, out string g2dSource);
            source = (source ?? string.Empty) + " | g2d=[" + (g2dSource ?? string.Empty) + "]";
            return g2d;
        }

        public static Texture2D TryLoadBuildingGpFrameViaMelinojaNationColorV1LikeOriginal(
            string absPath,
            int frame,
            Color32 nationColor,
            out string source,
            string logicalPackage)
        {
            return TryLoadG16FrameViaMelinojaNationColorV239(absPath, frame, nationColor, out source);
        }

        private static Texture2D TryLoadG16FrameViaMelinojaNationColorV239(string abs, int frameIndex, Color32 nationColor, out string source)
        {
            source = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                {
                    source = "v239_g16_nat_path_not_found:" + (abs ?? string.Empty);
                    return null;
                }

                MethodInfo load = ResolveCodecFacadeNationColorMethodV239("LoadG16ToMemoryNationColor");
                if (load != null)
                {
                    ParameterInfo[] lps = load.GetParameters();
                    object[] largs = lps.Length == 6
                        ? new object[] { abs, nationColor.r, nationColor.g, nationColor.b, null, false }
                        : null;
                    if (largs != null)
                    {
                        try { load.Invoke(null, largs); } catch { }
                    }
                }

                MethodInfo mi = ResolveCodecFacadeNationColorMethodV239("TryGetG16FrameRGBANationColor");
                if (mi == null)
                {
                    source = "v239_exact_method_missing TryGetG16FrameRGBANationColor";
                    return null;
                }

                object[] args = { abs, frameIndex, nationColor.r, nationColor.g, nationColor.b, 0, 0, null, null };
                object result = mi.Invoke(null, args);
                if (!(result is bool) || !(bool)result)
                {
                    source = "v239_TryGetG16FrameRGBANationColor_false: " + (args[8] as string ?? string.Empty);
                    return null;
                }

                int w = args[5] is int ? (int)args[5] : 0;
                int h = args[6] is int ? (int)args[6] : 0;
                byte[] rgba = args[7] as byte[];
                if (w <= 0 || h <= 0 || rgba == null || rgba.Length < w * h * 4)
                {
                    source = "v239_bad_rgba w=" + w.ToString(CultureInfo.InvariantCulture) + " h=" + h.ToString(CultureInfo.InvariantCulture);
                    return null;
                }

                Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
                tex.name = "C2_UNIT_NATCOLOR_EXACT_V239_" + Path.GetFileNameWithoutExtension(abs) + "_frame_" +
                           frameIndex.ToString(CultureInfo.InvariantCulture) + "_" +
                           nationColor.r.ToString(CultureInfo.InvariantCulture) + "_" +
                           nationColor.g.ToString(CultureInfo.InvariantCulture) + "_" +
                           nationColor.b.ToString(CultureInfo.InvariantCulture);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                // CodecFacade returns conventional top-left RGBA (the same layout as
                // C2RenderedFrame). Unity Texture2D raw rows start at the bottom.
                // Leaving it unflipped put the nation mask on the vertically mirrored
                // part of the unit, so black team-colour sleeves remained untouched.
                tex.LoadRawTextureData(FlipTopLeftRgbaToUnityV334(rgba, w, h));
                tex.Apply(false, false);
                source = "v239_g16_nat_exact_ok " + tex.name;
                return tex;
            }
            catch (Exception ex)
            {
                source = "v239_g16_nat_exception " + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }

        private static Texture2D TryLoadG2DFrameViaCodecFacadeNationColorV239(string abs, int frameIndex, Color32 nationColor, out string source)
        {
            source = string.Empty;
            try
            {
                MethodInfo mi = ResolveCodecFacadeNationColorMethodV239("TryGetG2DFrameRGBANationColor");
                if (mi == null)
                {
                    source = "v239_exact_method_missing TryGetG2DFrameRGBANationColor";
                    return null;
                }

                object[] args = { abs, frameIndex, nationColor.r, nationColor.g, nationColor.b, 0, 0, null, null };
                object result = mi.Invoke(null, args);
                if (!(result is bool) || !(bool)result)
                {
                    source = "v239_TryGetG2DFrameRGBANationColor_false: " + (args[8] as string ?? string.Empty);
                    return null;
                }

                int w = args[5] is int ? (int)args[5] : 0;
                int h = args[6] is int ? (int)args[6] : 0;
                byte[] rgba = args[7] as byte[];
                if (w <= 0 || h <= 0 || rgba == null || rgba.Length < w * h * 4)
                {
                    source = "v239_bad_g2d_rgba";
                    return null;
                }

                Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
                tex.name = "C2_UNIT_NATCOLOR_EXACT_G2D_V239_" + Path.GetFileNameWithoutExtension(abs) + "_frame_" +
                           frameIndex.ToString(CultureInfo.InvariantCulture);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.LoadRawTextureData(FlipTopLeftRgbaToUnityV334(rgba, w, h));
                tex.Apply(false, false);
                source = "v239_g2d_nat_exact_ok " + tex.name;
                return tex;
            }
            catch (Exception ex)
            {
                source = "v239_g2d_nat_exception " + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }

        private static MethodInfo ResolveCodecFacadeNationColorMethodV239(string methodName)
        {
            Type facadeType = Type.GetType("TemnyLessCodec.CodecFacade, Melinoja");
            if (facadeType == null) facadeType = Type.GetType("TemnyLessCodec.CodecFacade");
            if (facadeType != null)
            {
                MethodInfo mi = facadeType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (mi != null) return mi;
            }

            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Assembly asm = assemblies[i];
                    if (asm == null) continue;
                    Type t = asm.GetType("TemnyLessCodec.CodecFacade", false);
                    if (t == null) continue;
                    MethodInfo mi = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                    if (mi != null) return mi;
                }
            }
            catch { }

            return null;
        }

        private static byte[] FlipTopLeftRgbaToUnityV334(byte[] rgba, int width, int height)
        {
            if (rgba == null || width <= 0 || height <= 0 || rgba.Length < width * height * 4)
                return rgba;
            int stride = width * 4;
            byte[] flipped = new byte[height * stride];
            for (int y = 0; y < height; y++)
                Buffer.BlockCopy(rgba, y * stride, flipped, (height - 1 - y) * stride, stride);
            return flipped;
        }
    }
}
