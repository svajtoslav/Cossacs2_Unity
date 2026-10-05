using System;

namespace TemnyLessViewer
{
    /// <summary>
    /// Direct in-memory frame. No TGA/frame_XXXX cache.
    /// RGBA is byte order R,G,B,A, top-left origin.
    /// </summary>
    public sealed class C2RenderedFrame
    {
        public int Width;
        public int Height;

        // Pixel coordinates of original (0,0) inside RGBA for GU2D frames.
        // For GU16/GN16 this is 0,0 because MD/USERLC offsets are applied outside.
        public int OriginX;
        public int OriginY;

        public byte[] Rgba = Array.Empty<byte>();
    }
}
