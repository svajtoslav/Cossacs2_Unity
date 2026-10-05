using System;
using System.Collections.Generic;
using Cossacks2Bridge.Core;
using Cossacks2Bridge.UnityAdapters;
using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Renderers
{
    /// <summary>
    /// V396A7R6: single-surface GP compositor for the profile-delete modal.
    ///
    /// IMPORTANT:
    /// - no XML is parsed here;
    /// - the sole source model is UiDesk/Menu14UnifiedLoader;
    /// - source coordinates/frame ids are not adjusted to hide seams;
    /// - seam-prone GP pieces are drawn into ONE 1024x768 render target under
    ///   inclusive software clipping equivalent to IntersectWindows.
    ///
    /// Only graphics that the original engine composed through DrawFilledRect3 /
    /// DrawRect4 and VitButton::_Draw / DrawHeaderEx2 are moved to this surface.
    /// Text, portraits, transformed ornaments and actions remain on their existing
    /// source-driven runtime path.
    /// </summary>
    internal sealed class ProfileDeleteSingleSurfaceV396A7R6 : MonoBehaviour
    {
        private const int SurfaceW = 1024;
        private const int SurfaceH = 768;

        private readonly Dictionary<int, bool> _hover = new Dictionary<int, bool>();
        private readonly List<UiDialogsDesk> _borderDesks = new List<UiDialogsDesk>();
        private readonly List<UiVitButton> _buttons = new List<UiVitButton>();

        private RenderTexture _surface;
        private RawImage _image;

        internal static ProfileDeleteSingleSurfaceV396A7R6 Create(
            RectTransform root,
            UiDesk desk,
            HashSet<int> subtree,
            UiNode modalRoot)
        {
            if (root == null || desk?.Children == null || subtree == null || modalRoot == null)
                return null;

            var go = new GameObject("ProfileDelete_GP_SingleSurface_V396A7R6", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(root, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(SurfaceW, SurfaceH);

            var comp = go.AddComponent<ProfileDeleteSingleSurfaceV396A7R6>();
            comp._image = go.GetComponent<RawImage>();
            comp._image.raycastTarget = false;

            comp._surface = new RenderTexture(SurfaceW, SurfaceH, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
            {
                name = "C2_ProfileDelete_GP_Surface_V396A7R6",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            comp._surface.Create();
            comp._image.texture = comp._surface;
            comp._image.color = Color.white;

            for (int i = 0; i < desk.Children.Count; i++)
            {
                UiNode n = desk.Children[i];
                if (n == null || !subtree.Contains(n.SourceId) || !n.Visible) continue;
                if (n is UiDialogsDesk dd && n.SourceId != modalRoot.SourceId &&
                    !string.IsNullOrWhiteSpace(dd.Border) &&
                    !dd.Border.Equals("NullBorder", StringComparison.OrdinalIgnoreCase))
                {
                    comp._borderDesks.Add(dd);
                }
                else if (n is UiVitButton vb)
                {
                    comp._buttons.Add(vb);
                }
            }

            comp.Recompose();
            Debug.Log(
                $"[C2:UI COMPOSITOR V396A7R6] surface=1024x768 parser=Menu14UnifiedLoader " +
                $"borders={comp._borderDesks.Count} vitButtons={comp._buttons.Count} " +
                "clip=IntersectWindows_inclusive draw=one_RenderTexture RectMask2D_for_GP=0");
            return comp;
        }

        internal void SetHovered(int sourceId, bool hovered)
        {
            if (sourceId < 0) return;
            if (_hover.TryGetValue(sourceId, out bool old) && old == hovered) return;
            _hover[sourceId] = hovered;
            Recompose();
        }

        private void OnDestroy()
        {
            if (_surface != null)
            {
                _surface.Release();
                Destroy(_surface);
                _surface = null;
            }
        }

        private void Recompose()
        {
            if (_surface == null) return;

            RenderTexture prev = RenderTexture.active;
            bool pushed = false;
            try
            {
                RenderTexture.active = _surface;
                GL.Clear(true, true, Color.clear);
                GL.PushMatrix();
                pushed = true;
                // Original dialog coordinates are top-left, +Y downward.
                GL.LoadPixelMatrix(0, SurfaceW, SurfaceH, 0);

                // In this modal the Main border/fill is source-before the header
                // and action buttons. Keep that exact graphics ordering.
                for (int i = 0; i < _borderDesks.Count; i++)
                    DrawDialogsDeskBorder(_borderDesks[i]);

                for (int i = 0; i < _buttons.Count; i++)
                {
                    UiVitButton vb = _buttons[i];
                    bool hovered = vb != null && _hover.TryGetValue(vb.SourceId, out bool h) && h;
                    DrawVitButton(vb, hovered);
                }

                GL.PopMatrix();
                pushed = false;
            }
            finally
            {
                if (pushed) GL.PopMatrix();
                RenderTexture.active = prev;
            }
        }

        private void DrawDialogsDeskBorder(UiDialogsDesk dd)
        {
            if (dd == null) return;
            if (!ListDeskSourceRuntime14.TryGetBorderTemplate(dd.Border, out ListDeskSourceRuntime14.TemplateSpec spec) || spec == null)
                return;

            int x0 = dd.X;
            int y0 = dd.Y;
            int x1 = x0 + Math.Max(1, dd.Width) - 1;
            int y1 = y0 + Math.Max(1, dd.Height) - 1;

            if (spec.BorderNFillers > 0 && spec.BorderStartFiller >= 0)
                DrawFilledRect3(x0, y0, x1, y1, spec);
            else
                DrawRect4(x0, y0, x1, y1, spec);
        }

        // DrawForms.cpp::DrawFilledRect3, unchanged integer formulas.
        private void DrawFilledRect3(int x0, int y0, int x1, int y1, ListDeskSourceRuntime14.TemplateSpec spec)
        {
            Sprite first = Frame(spec.BorderGPFile, spec.BorderStartFiller);
            int lx = SpriteW(first);
            int ly = SpriteH(first);
            if (lx > 0 && ly > 0)
            {
                ClipRect clip = I(new ClipRect(x0, y0, x1, y1));
                int nx = (x1 - x0) / lx;
                int ny = (y1 - y0) / ly;
                for (int ix = 0; ix <= nx; ix++)
                {
                    for (int iy = 0; iy <= ny; iy++)
                    {
                        int nfill = Math.Max(1, spec.BorderNFillers);
                        int frame = spec.BorderStartFiller + ((ix * ix + iy * iy * iy) % nfill);
                        Blit(Frame(spec.BorderGPFile, frame), x0 + ix * lx, y0 + iy * ly, clip);
                    }
                }
            }
            DrawRect4(x0, y0, x1, y1, spec);
        }

        // DrawForms.cpp::DrawRect4, including the original LDLY=width(CLD) quirk.
        private void DrawRect4(int x0, int y0, int x1, int y1, ListDeskSourceRuntime14.TemplateSpec spec)
        {
            Sprite clu = Frame(spec.BorderGPFile, spec.BorderLeftTop);
            Sprite cru = Frame(spec.BorderGPFile, spec.BorderRightTop);
            Sprite cld = Frame(spec.BorderGPFile, spec.BorderLeftBottom);
            Sprite crd = Frame(spec.BorderGPFile, spec.BorderRightBottom);
            Sprite lu = Frame(spec.BorderGPFile, spec.BorderTopLine);
            Sprite ld = Frame(spec.BorderGPFile, spec.BorderBottomLine);
            Sprite ll = Frame(spec.BorderGPFile, spec.BorderLeftLine);
            Sprite lr = Frame(spec.BorderGPFile, spec.BorderRightLine);

            int ullx = SpriteW(clu); if (ullx == 0) ullx = 32;
            int lx2 = ullx >> 1;
            int dllx = cld != null ? SpriteW(cld) : SpriteW(clu); if (dllx == 0) dllx = 32;
            int lx3 = dllx >> 1;
            int uplx = SpriteW(lu); if (uplx == 0) uplx = 32;
            int dnlx = SpriteW(ld); if (dnlx == 0) dnlx = 32;
            int lsly = SpriteH(clu);
            int lly2 = lsly >> 1;
            int ldly = SpriteW(cld); if (ldly > 1000) ldly = 0;
            int ldy2 = ldly >> 1;
            int leftly = SpriteH(ll); if (leftly == 0) leftly = 32;
            int rightly = SpriteH(lr); if (rightly == 0) rightly = 32;

            int midY = (y1 + y0) / 2;

            ClipRect topClip = I(new ClipRect(x0 + lx2, y0 - lly2, x1 - lx2 - 1, midY));
            int n = (x1 - x0 + 1 - ullx) / uplx;
            for (int i = 0; i <= n + 2; i++)
                if (lu != null) Blit(lu, x0 + i * uplx + lx2, y0 - lly2, topClip);

            ClipRect bottomClip = I(new ClipRect(x0 + lx3, midY + 1, x1 - lx3 - 1, y1 + lly2));
            n = (x1 - x0 + 1 - dllx) / dnlx;
            for (int i = 0; i <= n + 2; i++)
                if (ld != null) Blit(ld, x0 + i * dnlx + lx3, y1 - ldy2, bottomClip);

            ClipRect verticalClip = I(new ClipRect(x0 - lx3, y0 + lly2, x1 + lx3, y1 - ldy2));
            n = (y1 - y0 + 1 - lly2 - ldy2) / leftly;
            for (int i = 0; i <= n + 1; i++)
                if (ll != null) Blit(ll, x0 - lx3, y0 + i * leftly + lly2, verticalClip);
            n = (y1 - y0 + 1 - lly2 - ldy2) / rightly;
            for (int i = 0; i <= n + 1; i++)
                if (lr != null) Blit(lr, x1 - lx3, y0 + i * rightly + lly2, verticalClip);

            int xMid = (x0 + x1) / 2;
            int cornerMid = (y1 - ldy2 + y0 + lly2) / 2;
            if (clu != null) Blit(clu, x0 - lx2, y0 - lly2,
                I(new ClipRect(x0 - lx2, y0 - lly2, xMid - 1, cornerMid - 1)));
            if (cru != null) Blit(cru, x1 - lx2, y0 - lly2,
                I(new ClipRect(xMid, y0 - lly2, x1 + lx2, cornerMid - 1)));
            if (cld != null) Blit(cld, x0 - lx3, y1 - ldy2,
                I(new ClipRect(x0 - lx3, cornerMid, xMid - 1, y1 + lly2)));
            if (crd != null) Blit(crd, x1 - lx3, y1 - ldy2,
                I(new ClipRect(xMid, cornerMid, x1 + lx3, y1 + lly2)));
        }

        // Dialogs.cpp::VitButton::_Draw + DrawForms.cpp::DrawHeaderEx2.
        private void DrawVitButton(UiVitButton vb, bool hovered)
        {
            if (vb == null || !vb.Visible) return;
            int cspr = hovered && vb.Enabled ? vb.SpriteActive : vb.SpritePassive;
            if (cspr < 0) return;

            int x0 = vb.X;
            int y0 = vb.Y;
            int lx = Math.Max(1, vb.Width);
            int ly = Math.Max(1, vb.Height);
            ClipRect outer = I(new ClipRect(x0, y0, x0 + lx - 1, y0 + ly - 1));

            if (vb.DisableCycling)
            {
                Blit(Frame(vb.GP_File, cspr), x0, y0, outer);
                return;
            }

            if (vb.OneSprited)
                DrawHeaderEx2(x0, y0, lx, vb.GP_File, -1, -1, cspr, cspr, cspr, outer);
            else
                DrawHeaderEx2(x0, y0, lx, vb.GP_File, cspr, cspr + 1, cspr + 2, cspr + 3, cspr + 4, outer);
        }

        private void DrawHeaderEx2(
            int x0, int y0, int lx, string gpFile,
            int frameL, int frameR, int frameC1, int frameC2, int frameC3,
            ClipRect parentClip)
        {
            if (lx < 24) lx = 24;
            Sprite spL = Frame(gpFile, frameL);
            Sprite spR = Frame(gpFile, frameR);
            int frWidthL = SpriteW(spL); if (frWidthL > 2048) frWidthL = 0;
            int frWidthR = SpriteW(spR); if (frWidthR > 2048) frWidthR = 0;

            ClipRect centerClip = ClipRect.Intersect(parentClip,
                I(new ClipRect(x0 + frWidthL, y0 - 32, x0 + lx - frWidthR, y0 + 128)));

            int count = 0;
            for (int i = 0; i < lx && count < 300;)
            {
                count++;
                int spr = (i % 3) == 0 ? frameC1 : ((i % 3) == 1 ? frameC2 : frameC3);
                Sprite sp = Frame(gpFile, spr);
                int w = SpriteW(sp);
                if (sp == null || w <= 0) break;
                Blit(sp, x0 + i, y0, centerClip);
                i += w;
            }

            if (spL != null) Blit(spL, x0, y0, parentClip);
            if (spR != null) Blit(spR, x0 + lx - frWidthR, y0, parentClip);
        }

        private static Sprite Frame(string gpFile, int frame)
        {
            if (string.IsNullOrWhiteSpace(gpFile) || frame < 0) return null;
            Sprite sp = OptionsRenderer.LoadSourceGpFrameV396A7R5(gpFile, frame);
            if (sp != null && sp.texture != null)
            {
                sp.texture.filterMode = FilterMode.Point;
                sp.texture.wrapMode = TextureWrapMode.Clamp;
            }
            return sp;
        }

        private static int SpriteW(Sprite sp) => sp == null ? 0 : Mathf.RoundToInt(sp.rect.width);
        private static int SpriteH(Sprite sp) => sp == null ? 0 : Mathf.RoundToInt(sp.rect.height);

        private static readonly ClipRect ScreenClip = new ClipRect(0, 0, SurfaceW - 1, SurfaceH - 1);
        private static ClipRect I(ClipRect r) => ClipRect.Intersect(ScreenClip, r);

        private struct ClipRect
        {
            public int X0, Y0, X1, Y1;
            public ClipRect(int x0, int y0, int x1, int y1)
            {
                X0 = x0; Y0 = y0; X1 = x1; Y1 = y1;
            }
            public bool Valid => X1 >= X0 && Y1 >= Y0;
            public static ClipRect Intersect(ClipRect a, ClipRect b)
            {
                return new ClipRect(
                    Math.Max(a.X0, b.X0), Math.Max(a.Y0, b.Y0),
                    Math.Min(a.X1, b.X1), Math.Min(a.Y1, b.Y1));
            }
        }

        /// <summary>
        /// Draws one source sprite into the common RT and applies clipping by
        /// cropping BOTH destination and source UVs. No RectMask2D is involved.
        /// </summary>
        private static void Blit(Sprite sp, int dstX, int dstY, ClipRect clip)
        {
            if (sp == null || sp.texture == null || !clip.Valid) return;
            int w = SpriteW(sp);
            int h = SpriteH(sp);
            if (w <= 0 || h <= 0) return;

            int sx0 = Math.Max(dstX, clip.X0);
            int sy0 = Math.Max(dstY, clip.Y0);
            int sx1 = Math.Min(dstX + w - 1, clip.X1);
            int sy1 = Math.Min(dstY + h - 1, clip.Y1);
            if (sx1 < sx0 || sy1 < sy0) return;

            float fx0 = (float)(sx0 - dstX) / w;
            float fx1 = (float)(sx1 - dstX + 1) / w;
            float fy0 = (float)(sy0 - dstY) / h;       // fraction removed from top
            float fy1 = (float)(sy1 - dstY + 1) / h;  // visible bottom edge in top-down space

            Rect tr;
            try { tr = sp.textureRect; }
            catch { tr = sp.rect; }

            float u0 = tr.x / sp.texture.width;
            float v0 = tr.y / sp.texture.height;
            float uw = tr.width / sp.texture.width;
            float vh = tr.height / sp.texture.height;

            // Destination uses top-left coordinates. Unity texture V=0 is bottom,
            // so top-down Y cropping maps to the inverse interval in UV space.
            Rect uv = new Rect(
                u0 + uw * fx0,
                v0 + vh * (1f - fy1),
                uw * (fx1 - fx0),
                vh * (fy1 - fy0));

            Rect dst = new Rect(sx0, sy0, sx1 - sx0 + 1, sy1 - sy0 + 1);
            Graphics.DrawTexture(dst, sp.texture, uv, 0, 0, 0, 0, Color.white);
        }
    }
}
