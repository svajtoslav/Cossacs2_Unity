using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
// Melinoja.dll lives in com.temnyless.codec/Plugins and exposes TemnyLessCodec.CodecFacade.
// We use it only to decode proprietary .g16 (GU16/GN16) into plain TGA frames on disk.
using TemnyLessCodec;
#endif

namespace Cossacks2Bridge.UnityAdapters.RuntimeFonts
{
    /// <summary>
    /// Loads Cossacks2 bitmap fonts from the game's Cash (*.g16).
    /// Reality: in Cash, "*.g16" can be GU16 or GN16. GN16 is a container.
    /// We rely on Melinoja (TemnyLessCodec) to decode into <name>_frames/frame_XXXX.tga once,
    /// then pack those frames into a Unity atlas and create per-glyph sprites.
    /// </summary>
    public static class ManagedG16FontLoader
    {
        public struct AtlasResult
        {
            public Texture2D atlas;
            public Rect[] rects;      // per frame UV rect in atlas pixels
            public int[] advances;    // suggested advance in pixels
            public int[] widths;
            public int[] heights;
        }

        public static AtlasResult LoadAsAtlas(string absoluteG16Path, int expectedFrames = 256)
        {
            if (string.IsNullOrWhiteSpace(absoluteG16Path))
                throw new ArgumentException("Path is empty", nameof(absoluteG16Path));

            string framesDir = GetFramesDir(absoluteG16Path);

            if (!Directory.Exists(framesDir) || Directory.GetFiles(framesDir, "*.tga").Length == 0)
            {
                // decode using Melinoja (package com.temnyless.codec)
                string logPath, err;
                bool ok = CodecFacade.DecodeG16ToLogAndFrames(absoluteG16Path, out logPath, out err, doubleOverlay: false);
                if (!ok)
                    throw new InvalidDataException("Failed to decode G16 via Melinoja: " + err);
            }

            // Load frames present on disk.
            // Some font packs start at 33 (space) etc. We map by id from filename.
            var frames = new Color32[expectedFrames][];
            var w = new int[expectedFrames];
            var h = new int[expectedFrames];

            int maxW = 1, maxH = 1;
            foreach (var file in Directory.GetFiles(framesDir, "frame_*.tga"))
            {
                if (!TryParseFrameId(Path.GetFileNameWithoutExtension(file), out int id)) continue;
                if (id < 0 || id >= expectedFrames) continue;

                var (fw, fh, pix) = ReadTga32(file);
                frames[id] = pix;
                w[id] = fw;
                h[id] = fh;
                if (fw > maxW) maxW = fw;
                if (fh > maxH) maxH = fh;
            }

            // Create a simple 16x16 grid atlas (wasteful but robust, fast).
            int cols = 16;
            int rows = Mathf.CeilToInt(expectedFrames / (float)cols);
            int atlasW = maxW * cols;
            int atlasH = maxH * rows;

            var atlas = new Texture2D(atlasW, atlasH, TextureFormat.RGBA32, mipChain: false, linear: true);
            atlas.wrapMode = TextureWrapMode.Clamp;
            atlas.filterMode = FilterMode.Bilinear;

            // Fill transparent
            var clear = new Color32[atlasW * atlasH];
            for (int i = 0; i < clear.Length; i++) clear[i] = new Color32(0, 0, 0, 0);
            atlas.SetPixels32(clear);

            var rects = new Rect[expectedFrames];
            var adv = new int[expectedFrames];

            for (int id = 0; id < expectedFrames; id++)
            {
                int col = id % cols;
                int row = id / cols;

                int x = col * maxW;
                int y = atlasH - (row + 1) * maxH; // Unity texture y=bottom; we place row from top

                int fw = w[id];
                int fh = h[id];

                if (frames[id] != null && fw > 0 && fh > 0)
                {
                    // Blit into top-left of cell
                    Blit(atlas, x, y + (maxH - fh), fw, fh, frames[id]);
                }

                rects[id] = new Rect(x, y, maxW, maxH);

                // Conservative advance: glyph width + 1px spacing
                // If glyph is missing, use half-cell.
                adv[id] = (fw > 0) ? (fw + 1) : Mathf.Max(1, maxW / 2);
            }

            atlas.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            return new AtlasResult
            {
                atlas = atlas,
                rects = rects,
                advances = adv,
                widths = w,
                heights = h
            };
        }

        public static string GetFramesDir(string g16Path)
        {
            return Path.Combine(Path.GetDirectoryName(g16Path) ?? "", Path.GetFileNameWithoutExtension(g16Path) + "_frames");
        }

        private static bool TryParseFrameId(string stem, out int id)
        {
            id = -1;
            // stem like "frame_0033"
            if (!stem.StartsWith("frame_", StringComparison.OrdinalIgnoreCase)) return false;
            var s = stem.Substring("frame_".Length);
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
        }

        private static void Blit(Texture2D dst, int x, int y, int w, int h, Color32[] src)
        {
            // dst coords origin bottom-left
            // src is row-major top-left? Our TGA reader returns pixels in Unity bottom-left order already.
            dst.SetPixels32(x, y, w, h, src);
        }

        private static (int w, int h, Color32[] pixels) ReadTga32(string path)
        {
            var data = File.ReadAllBytes(path);
            if (data.Length < 18) throw new InvalidDataException("Bad TGA: " + path);

            int idLen = data[0];
            int colorMapType = data[1];
            int imageType = data[2]; // 2 = uncompressed truecolor
            int w = data[12] | (data[13] << 8);
            int h = data[14] | (data[15] << 8);
            int bpp = data[16];
            int desc = data[17];

            if (colorMapType != 0) throw new InvalidDataException("TGA colormap not supported: " + path);
            if (imageType != 2) throw new InvalidDataException("TGA type not supported: " + imageType + " in " + path);
            if (bpp != 32) throw new InvalidDataException("TGA bpp not supported: " + bpp + " in " + path);

            bool originTop = (desc & 0x20) != 0;

            int header = 18 + idLen;
            int expected = w * h * 4;
            if (data.Length < header + expected) throw new InvalidDataException("TGA truncated: " + path);

            var pixels = new Color32[w * h];

            // TGA is BGRA
            // Convert to Unity Color32 RGBA.
            // Also normalize to Unity bottom-left origin for Texture2D.SetPixels32(x,y,w,h,...)
            for (int yy = 0; yy < h; yy++)
            {
                int srcY = originTop ? yy : (h - 1 - yy);
                int dstY = h - 1 - yy; // bottom-left
                for (int xx = 0; xx < w; xx++)
                {
                    int srcIndex = header + (srcY * w + xx) * 4;
                    byte b = data[srcIndex + 0];
                    byte g = data[srcIndex + 1];
                    byte r = data[srcIndex + 2];
                    byte a = data[srcIndex + 3];

                    int dstIndex = dstY * w + xx;
                    pixels[dstIndex] = new Color32(r, g, b, a);
                }
            }

            return (w, h, pixels);
        }
    }
}
