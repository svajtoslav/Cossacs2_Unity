using System;
using System.Collections.Generic;
using System.IO;
using TemnyLessViewer;
using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // InitFonts.h: Special* = FontG12, Small* = FontC12. CP1251 frame numbers,
    // Fastdraw.cpp: advance = GP frame width; space = width of lowercase 'c'.
    // One atlas and one UI mesh per label; refreshing a number allocates no images.
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class C2HudBitmapTextV448 : MaskableGraphic
    {
        private sealed class FontBank
        {
            internal Texture2D Atlas;
            internal readonly Rect[] UV = new Rect[256];
            internal readonly Vector2[] Size = new Vector2[256];
        }
        private static readonly Dictionary<string, FontBank> Fonts = new Dictionary<string, FontBank>(StringComparer.OrdinalIgnoreCase);
        private FontBank font;
        private string value = "";
        internal bool Centered;
        internal string Value
        {
            get => value;
            set { if (this.value == value) return; this.value = value ?? ""; SetVerticesDirty(); }
        }
        public override Texture mainTexture => font?.Atlas != null ? font.Atlas : Texture2D.whiteTexture;

        internal void SetFont(string name)
        {
            font=LoadFontV453(name);
            SetAllDirty();
        }
        private static FontBank LoadFontV453(string name)
        {
            if (!Fonts.TryGetValue(name, out var font))
            {
                string path = null;
                foreach (var root in C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal())
                {
                    string file = Path.Combine(root, "Cash", "interf3_Fonts_" + name + ".g16");
                    if (!File.Exists(file)) file = Path.Combine(root, "Interf3", "interf3_Fonts_" + name + ".g16");
                    if (File.Exists(file)) { path = file; break; }
                }
                var bank = new C2DirectSpriteBank();
                if (path == null || !bank.Load(path, out _))
                {
                    Debug.LogError("[C2:HUD FONT] Missing original font " + name);
                    return null;
                }
                font = new FontBank();
                var frames = new C2RenderedFrame[256];
                int cellW = 1, cellH = 1;
                for (int i = 33; i < Math.Min(256, bank.FrameCount); i++)
                    if (bank.RenderFrame(i, out frames[i], out _) && frames[i] != null)
                    { cellW = Math.Max(cellW, frames[i].Width + 2); cellH = Math.Max(cellH, frames[i].Height + 2); }
                int width = cellW * 16, height = cellH * 16;
                var pixels = new byte[width * height * 4];
                for (int i = 0; i < 256; i++)
                {
                    var f = frames[i]; if (f == null || f.Rgba == null) continue;
                    int x = i % 16 * cellW + 1, y = i / 16 * cellH + 1;
                    for (int row = 0; row < f.Height; row++)
                        Buffer.BlockCopy(f.Rgba, row * f.Width * 4, pixels, ((y + f.Height - row - 1) * width + x) * 4, f.Width * 4);
                    font.Size[i] = new Vector2(f.Width, f.Height);
                    font.UV[i] = new Rect((float)x / width, (float)y / height, (float)f.Width / width, (float)f.Height / height);
                }
                font.Atlas = new Texture2D(width, height, TextureFormat.RGBA32, false);
                font.Atlas.name = "C2_Original_" + name;
                font.Atlas.filterMode = FilterMode.Point;
                font.Atlas.wrapMode = TextureWrapMode.Clamp;
                font.Atlas.LoadRawTextureData(pixels); font.Atlas.Apply(false, true);
                Fonts[name] = font;
            }
            return font;
        }
        // IMGUI map hints share the native font atlas used by the HUD.
        internal static float MeasureGuiV453(string value)
        {
            var f=LoadFontV453("FontC12"); if(f==null)return 0;
            float w=0;
            foreach(char c in value){C2LegacyText14.TryEncodeChar(c,out byte b);w+=f.Size[b==32||b==160?99:b].x;}
            return w;
        }
        internal static void DrawGuiV453(Vector2 origin,string value)
        {
            var f=LoadFontV453("FontC12"); if(f==null)return;
            float x=origin.x;
            foreach(char c in value)
            {
                C2LegacyText14.TryEncodeChar(c,out byte b);
                if(b==32||b==160){x+=f.Size[99].x;continue;}
                var size=f.Size[b];
                GUI.DrawTextureWithTexCoords(new Rect(x,origin.y-3,size.x,size.y),f.Atlas,f.UV[b],true);
                x+=size.x;
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (font == null || string.IsNullOrEmpty(value)) return;
            float total = 0;
            foreach (char c in value) { C2LegacyText14.TryEncodeChar(c, out byte b); total += font.Size[b == 32 || b == 160 ? 99 : b].x; }
            float x = Centered ? -Mathf.Floor(total / 2) : 0;
            // DrawMultilineText starts the glyph at lineTop-Top (fonts.xml Top=3).
            const float top = 3;
            foreach (char c in value)
            {
                C2LegacyText14.TryEncodeChar(c, out byte b);
                if (b == 32 || b == 160) { x += font.Size[99].x; continue; }
                Vector2 size = font.Size[b]; Rect uv = font.UV[b]; int n = vh.currentVertCount;
                vh.AddVert(new Vector3(x, top), color, new Vector2(uv.xMin, uv.yMax));
                vh.AddVert(new Vector3(x + size.x, top), color, new Vector2(uv.xMax, uv.yMax));
                vh.AddVert(new Vector3(x + size.x, top - size.y), color, new Vector2(uv.xMax, uv.yMin));
                vh.AddVert(new Vector3(x, top - size.y), color, new Vector2(uv.xMin, uv.yMin));
                vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
                x += size.x;
            }
        }
    }
}
