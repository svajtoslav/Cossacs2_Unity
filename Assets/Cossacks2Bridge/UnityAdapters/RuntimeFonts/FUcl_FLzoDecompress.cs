// Source adapted from the user's Melinoja project (pure managed).
// Provides UCL/LZO decompression stubs used by G16SegmentDecoder.

using System;

namespace Cossacks2Bridge.UnityAdapters.RuntimeFonts
{
    internal static class FUcl_FLzoDecompress
    {
        // NOTE: For the Cossacks2 cache fonts most segments are STORE or use a small subset.
        // This implementation is intentionally minimal and safe. If a particular file uses an
        // unsupported compression, a clear error is returned.

        public static bool TryDecompressUcl(byte[] src, int srcLen, byte[] dst, ref int dstLen, out string err)
        {
            // The Melinoja project contains a full port; if your cache uses UCL, we can paste it here.
            // For now, fail clearly instead of crashing.
            err = "UCL decompression is not implemented in this Unity runtime build";
            return false;
        }

        public static bool TryDecompressLzo(byte[] src, int srcLen, byte[] dst, ref int dstLen, out string err)
        {
            err = "LZO decompression is not implemented in this Unity runtime build";
            return false;
        }
    }
}
