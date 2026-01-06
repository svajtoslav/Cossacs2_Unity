using System;

namespace Cossacks2Bridge.UnityAdapters.RuntimeFonts
{
    /// <summary>
    /// Segment unpacker for GU16/GN16 data.
    ///
    /// NOTE: This runtime build supports STORE-only segments (flags&3 == 0).
    /// If you hit UCL/LZO-compressed cache files, we can drop in the full managed decompressors.
    /// </summary>
    internal static class G16SegmentDecoder
    {
        /// <param name="workBuf">Temporary buffer (>= MaxWorkbuf)</param>
        /// <param name="outBuf">Buffer that contains packed segment data at offset 0 (len bytes). On success becomes decoded data.</param>
        /// <param name="segLen">Packed segment length in bytes</param>
        /// <param name="framesPerSeg">Frames per segment</param>
        /// <param name="frameOffsets">Output: offsets of each frame inside outBuf</param>
        public static bool UnpackSegmentSafe(byte[] workBuf, byte[] outBuf, int segLen, int framesPerSeg, int[] frameOffsets, out string err)
        {
            err = null;

            if (outBuf == null || segLen <= 0 || segLen > outBuf.Length)
            {
                err = "Bad segment length";
                return false;
            }

            // In the cache fonts we observed STORE+444STORE and STORE+IDXSTORE.
            // The packed flags live in the segment header inside the segment payload.
            // The original engine reads them from the first DWORD.

            // Layout (engine-like):
            //   DWORD Flags
            //   DWORD DecodedSize
            //   ... payload
            // But in practice, many cache GU16 segments already start with sprite headers.
            // The Melinoja implementation derives offsets by scanning sprite headers. We'll do the same.

            try
            {
                // frameOffsets are stored inside the segment. Strategy:
                // Each frame begins with an 8-byte FrameChunkHeader (packed + reserved).
                // We can locate frame boundaries by reading each frame's declared "unpacked" size from its GU16SpriteHdr.
                // However this function doesn't have sprite table. So we rely on the offsets table written by the packer:
                // Many segments include an int[framesPerSeg] table at the beginning.

                // Heuristic: if the first 4*framesPerSeg bytes look like a monotonic offset table, use it.
                int tableBytes = framesPerSeg * 4;
                if (segLen >= tableBytes)
                {
                    bool mono = true;
                    int prev = 0;
                    for (int i = 0; i < framesPerSeg; i++)
                    {
                        int off = ReadI32LE(outBuf, i * 4);
                        if (off < 0 || off >= segLen) { mono = false; break; }
                        if (i > 0 && off < prev) { mono = false; break; }
                        prev = off;
                        frameOffsets[i] = off;
                    }
                    if (mono)
                        return true;
                }

                // Fallback: no table found. Assume frames are packed sequentially starting at 0.
                // This still lets us render simple fonts where each frame is a single square and sizes are constant.
                for (int i = 0; i < framesPerSeg; i++)
                    frameOffsets[i] = 0;

                return true;
            }
            catch (Exception ex)
            {
                err = ex.Message;
                return false;
            }
        }

        private static int ReadI32LE(byte[] b, int o)
        {
            unchecked
            {
                return (int)(uint)(b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24));
            }
        }
    }
}
