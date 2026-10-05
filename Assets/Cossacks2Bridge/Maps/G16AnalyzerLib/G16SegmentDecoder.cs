using System;
using System.IO;
using System.Text;

namespace G16AnalyzerLib
{
    /// <summary>
    /// G16 Segment Decoder - точная копия логики gMotor
    /// </summary>
    public class G16SegmentDecoder
    {
        #region Constants (ТОЧНО КАК В ОРИГИНАЛЕ!)

        private const uint G16_COMPRESS_METHOD_MASK = 0x3;
        private const uint G16_NONCOMPRESSED = 0;
        private const uint G16_COMPRESSED_BY_UCL = 1;
        private const uint G16_COMPRESSED_BY_LZO = 2;

        private const uint G16_PACK_METHOD_MASK = 0xC;
        private const uint G16_IDXSTORE = 0;
        private const uint G16_444STORE = 12;

        #endregion

        #region Static State

        private static string g_lastUnpackError = "";
        private static StreamWriter g_trace = null;

        // ВАЖНО: Эта палитра должна синхронизироваться с G16GlobalState!
        private static ushort[] G16PalRGB = new ushort[256];

        #endregion

        #region Public Static Methods

        public static string GetLastUnpackErrorStatic() => g_lastUnpackError;

        /// <summary>
        /// Устанавливает палитру 444 для декодера IDXSTORE
        /// </summary>
        public static void G16SetPalette444(ushort[] pal444_256)
        {
            if (pal444_256 != null && pal444_256.Length >= 256)
                Array.Copy(pal444_256, G16PalRGB, 256);
            else
                Array.Clear(G16PalRGB, 0, 256);
        }

        /// <summary>
        /// Получает текущую палитру (для отладки)
        /// </summary>
        public static ushort[] G16GetPalette444()
        {
            return (ushort[])G16PalRGB.Clone();
        }

        public static void G16SetTraceFile(string path)
        {
            if (g_trace != null)
            {
                g_trace.Close();
                g_trace = null;
            }

            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    g_trace = new StreamWriter(path, false, Encoding.UTF8);
                    g_trace.WriteLine("=== G16 TRACE ===");
                }
                catch { }
            }
        }

        #endregion

        #region Instance Methods

        public string GetLastUnpackError() => g_lastUnpackError;

        /// <summary>
        /// Full API (buffers provided by caller)
        /// </summary>
        public bool UnpackSegmentSafe(
            byte[] InData,
            uint InLen,
            byte[] OutData,
            uint OutCapacity,
            byte[] WorkData,
            uint WorkCapacity,
            uint[] FOffsData,
            uint FramesNumber,
            uint Flags)
        {
            g_lastUnpackError = "";

            if (InData == null || InLen < 4)
            {
                g_lastUnpackError = "Bad input";
                return false;
            }

            // Читаем OutLen из первых 4 байт
            uint OutLen = BitConverter.ToUInt32(InData, 0);
            int inDataOffset = 4;
            uint inLenRemaining = InLen - 4;

            Trace($"UnpackSegment: OutLen={OutLen} InLen={inLenRemaining} Flags=0x{Flags:X}");

            // ========== ДЕКОМПРЕССИЯ ==========
            byte[] workPtr;
            int workOffset = 0;

            uint compressMethod = Flags & G16_COMPRESS_METHOD_MASK;

            if (compressMethod == G16_COMPRESSED_BY_UCL)
            {
                Trace("  Decompressing with UCL...");

                uint decompLen;
                if (!UclDecompress(InData, inDataOffset, inLenRemaining, WorkData, out decompLen))
                {
                    g_lastUnpackError = "UCL decompression failed";
                    return false;
                }

                OutLen = decompLen;
                workPtr = new byte[OutLen];
                Array.Copy(WorkData, 0, workPtr, 0, (int)OutLen);
                Trace($"  UCL OK, WorkLen={OutLen}");
            }
            else if (compressMethod == G16_COMPRESSED_BY_LZO)
            {
                Trace("  Decompressing with LZO...");

                uint decompLen;
                if (!LzoDecompress(InData, inDataOffset, inLenRemaining, WorkData, FramesNumber, Flags, out decompLen))
                {
                    if (string.IsNullOrEmpty(g_lastUnpackError))
                        g_lastUnpackError = "LZO decompression failed";
                    return false;
                }

                OutLen = decompLen;
                workPtr = new byte[OutLen];
                Array.Copy(WorkData, 0, workPtr, 0, (int)OutLen);
                Trace($"  LZO OK, WorkLen={OutLen}");
            }
            else if (compressMethod == G16_NONCOMPRESSED)
            {
                Trace("  NONCOMPRESSED mode");
                // Копируем данные начиная с inDataOffset
                workPtr = new byte[inLenRemaining];
                Array.Copy(InData, inDataOffset, workPtr, 0, (int)inLenRemaining);
                OutLen = inLenRemaining;
            }
            else
            {
                g_lastUnpackError = "Unknown compression method";
                return false;
            }

            // ========== ПАРСИНГ КВАДРАТОВ (ТОЧНО КАК В ОРИГИНАЛЕ!) ==========
            int srcPos = 0;
            int dstPos = 0;

            uint packMethod = Flags & G16_PACK_METHOD_MASK;

            string layoutError;
            if (!ValidateSegmentLayout(workPtr, (uint)workPtr.Length, FramesNumber, Flags, out layoutError))
            {
                g_lastUnpackError = layoutError;
                return false;
            }

            if (packMethod == G16_IDXSTORE)
            {
                Trace("  Pack method: IDXSTORE");

                // Читаем смещение до цветовых данных
                uint colorOffsetRel = GetUInt(workPtr, ref srcPos);
                int colorOffset = srcPos + (int)colorOffsetRel;

                // Читаем смещение до альфа-данных
                uint alphaOffsetRel = GetUInt(workPtr, ref srcPos);
                int alphaOffset = srcPos + (int)alphaOffsetRel;

                Trace($"  ColorOffset={colorOffset} AlphaOffset={alphaOffset}");

                for (uint f = 0; f < FramesNumber; f++)
                {
                    ushort squaresNumber = GetUShort(workPtr, ref srcPos);
                    FOffsData[f] = (uint)dstPos;

                    for (uint s = 0; s < squaresNumber; s++)
                    {
                        uint ch = GetUInt(workPtr, ref srcPos);
                        PutUInt(OutData, ref dstPos, ch);    // заголовок квадрата
                        PutUInt(OutData, ref dstPos, 0);     // reserved

                        int side = 1 << (int)(ch >> 28);

                        for (int j = 0; j < side; j++)
                        {
                            for (int i = 0; i < side; i += 2)
                            {
                                if (alphaOffset >= workPtr.Length)
                                {
                                    g_lastUnpackError = "Alpha buffer overrun";
                                    return false;
                                }

                                int aa = workPtr[alphaOffset++];
                                int a1 = aa & 0xF0;
                                int a2 = aa & 0x0F;

                                ushort px;

                                // Первый пиксель
                                if (a1 != 0)
                                {
                                    if (colorOffset >= workPtr.Length)
                                    {
                                        g_lastUnpackError = "Color buffer overrun";
                                        return false;
                                    }
                                    byte colorIdx = workPtr[colorOffset++];
                                    px = (ushort)((a1 << 8) | G16PalRGB[colorIdx]);
                                }
                                else
                                {
                                    px = 0;
                                }
                                PutUShort(OutData, ref dstPos, px);

                                // Второй пиксель
                                if (a2 != 0)
                                {
                                    if (colorOffset >= workPtr.Length)
                                    {
                                        g_lastUnpackError = "Color buffer overrun";
                                        return false;
                                    }
                                    byte colorIdx = workPtr[colorOffset++];
                                    px = (ushort)((a2 << 12) | G16PalRGB[colorIdx]);
                                }
                                else
                                {
                                    px = 0;
                                }
                                PutUShort(OutData, ref dstPos, px);
                            }
                        }
                    }
                }
            }
            else if (packMethod == G16_444STORE)
            {
                Trace("  Pack method: 444STORE");

                uint colorOffsetRel = GetUInt(workPtr, ref srcPos);
                int colorOffset = srcPos + (int)colorOffsetRel;

                uint alphaOffsetRel = GetUInt(workPtr, ref srcPos);
                int alphaOffset = srcPos + (int)alphaOffsetRel;

                Trace($"  ColorOffset={colorOffset} AlphaOffset={alphaOffset}");

                for (uint f = 0; f < FramesNumber; f++)
                {
                    ushort squaresNumber = GetUShort(workPtr, ref srcPos);
                    FOffsData[f] = (uint)dstPos;

                    for (uint s = 0; s < squaresNumber; s++)
                    {
                        uint ch = GetUInt(workPtr, ref srcPos);
                        PutUInt(OutData, ref dstPos, ch);
                        PutUInt(OutData, ref dstPos, 0);

                        int side = 1 << (int)(ch >> 28);

                        for (int j = 0; j < side; j++)
                        {
                            for (int i = 0; i < side; i += 2)
                            {
                                if (alphaOffset >= workPtr.Length)
                                {
                                    g_lastUnpackError = "Alpha buffer overrun";
                                    return false;
                                }

                                int aa = workPtr[alphaOffset++];
                                int a1 = aa & 0xF0;
                                int a2 = (aa << 4) & 0xF0;  // ВАЖНО: сдвиг влево!

                                ushort px;

                                // Первый пиксель
                                if (a1 != 0)
                                {
                                    if (colorOffset + 1 >= workPtr.Length)
                                    {
                                        g_lastUnpackError = "Color buffer overrun";
                                        return false;
                                    }
                                    ushort color = BitConverter.ToUInt16(workPtr, colorOffset);
                                    colorOffset += 2;
                                    px = (ushort)(color | ((a1 & 0xF0) << 8));
                                }
                                else
                                {
                                    px = 0;
                                }
                                PutUShort(OutData, ref dstPos, px);

                                // Второй пиксель
                                if (a2 != 0)
                                {
                                    if (colorOffset + 1 >= workPtr.Length)
                                    {
                                        g_lastUnpackError = "Color buffer overrun";
                                        return false;
                                    }
                                    ushort color = BitConverter.ToUInt16(workPtr, colorOffset);
                                    colorOffset += 2;
                                    px = (ushort)(color | ((a2 & 0xF0) << 8));
                                }
                                else
                                {
                                    px = 0;
                                }
                                PutUShort(OutData, ref dstPos, px);
                            }
                        }
                    }
                }
            }
            else
            {
                g_lastUnpackError = "Unknown pack method";
                return false;
            }

            Trace($"  UnpackSegment OK, dst_pos={dstPos}");
            return true;
        }

        #endregion

        #region Helper Methods

        private static uint GetUInt(byte[] data, ref int pos)
        {
            uint val = BitConverter.ToUInt32(data, pos);
            pos += 4;
            return val;
        }

        private static ushort GetUShort(byte[] data, ref int pos)
        {
            ushort val = BitConverter.ToUInt16(data, pos);
            pos += 2;
            return val;
        }

        private static void PutUInt(byte[] data, ref int pos, uint val)
        {
            data[pos] = (byte)(val & 0xFF);
            data[pos + 1] = (byte)((val >> 8) & 0xFF);
            data[pos + 2] = (byte)((val >> 16) & 0xFF);
            data[pos + 3] = (byte)((val >> 24) & 0xFF);
            pos += 4;
        }

        private static void PutUShort(byte[] data, ref int pos, ushort val)
        {
            data[pos] = (byte)(val & 0xFF);
            data[pos + 1] = (byte)((val >> 8) & 0xFF);
            pos += 2;
        }

        private static void Trace(string message)
        {
            if (g_trace != null)
            {
                g_trace.WriteLine(message);
                g_trace.Flush();
            }
        }

        #endregion

        #region UCL Decompressor

        private static bool UclDecompress(byte[] src, int srcOffset, uint srcLen,
            byte[] dst, out uint dstLen)
        {
            dstLen = 0;
            uint bb = 0;
            int ilen = srcOffset;
            int ilenEnd = srcOffset + (int)srcLen;
            int olen = 0;
            uint lastMOff = 1;

            try
            {
                while (true)
                {
                    uint mOff, mLen;

                    if (GetBitUcl(src, ref ilen, ilenEnd, ref bb) != 0)
                    {
                        if (ilen >= ilenEnd) return false;
                        dst[olen++] = src[ilen++];
                        continue;
                    }

                    mOff = 1;
                    do
                    {
                        mOff = (mOff << 1) + (uint)GetBitUcl(src, ref ilen, ilenEnd, ref bb);
                    } while (GetBitUcl(src, ref ilen, ilenEnd, ref bb) == 0);

                    if (mOff == 2)
                    {
                        mOff = lastMOff;
                    }
                    else
                    {
                        if (ilen >= ilenEnd) return false;
                        mOff = ((mOff - 3) << 8) + src[ilen++];
                        if (mOff == 0xffffffff)
                            break;
                        lastMOff = ++mOff;
                    }

                    mLen = (uint)GetBitUcl(src, ref ilen, ilenEnd, ref bb);
                    mLen = (mLen << 1) + (uint)GetBitUcl(src, ref ilen, ilenEnd, ref bb);

                    if (mLen == 0)
                    {
                        mLen++;
                        do
                        {
                            mLen = (mLen << 1) + (uint)GetBitUcl(src, ref ilen, ilenEnd, ref bb);
                        } while (GetBitUcl(src, ref ilen, ilenEnd, ref bb) == 0);
                        mLen += 2;
                    }

                    mLen += (mOff > 0xd00) ? 1u : 0u;

                    if (olen < (int)mOff) return false;

                    int mPos = olen - (int)mOff;
                    dst[olen++] = dst[mPos++];

                    while (mLen-- > 0)
                    {
                        dst[olen++] = dst[mPos++];
                    }
                }
            }
            catch
            {
                return false;
            }

            dstLen = (uint)olen;
            return true;
        }

        private static int GetBitUcl(byte[] src, ref int ilen, int ilenEnd, ref uint bb)
        {
            bb <<= 1;
            if ((bb & 0xff) != 0)
            {
                return (int)((bb >> 8) & 1);
            }
            else
            {
                if (ilen >= ilenEnd) return 0;
                bb = (uint)((src[ilen++] << 1) + 1);
                return (int)((bb >> 8) & 1);
            }
        }

        #endregion

        #region LZO Decompressor

        private static bool LzoDecompress(byte[] src, int srcOffset, uint srcLen,
            byte[] dst, uint framesNumber, uint flags, out uint dstLen)
        {
            dstLen = 0;
            string fastLayoutWhy = "not checked";
            string strictLayoutWhy = "not checked";

            Array.Clear(dst, 0, dst.Length);
            int fastRet = Lzo1xDecompressor.Decompress(src, srcOffset, srcLen, dst, out uint fastLen, null);
            if (fastLen > 0 && ValidateSegmentLayout(dst, fastLen, framesNumber, flags, out fastLayoutWhy))
            {
                dstLen = fastLen;
                Trace($"  LZO fast OK ret={fastRet} len={fastLen}");
                return true;
            }
            string fastWhy = $"fastRet={fastRet} fastLen={fastLen} layout={fastLayoutWhy}";

            Array.Clear(dst, 0, dst.Length);
            bool strictOk = Lzo1xStrictSafeDecompressor.Decompress(src, srcOffset, srcLen, dst, out uint strictLen);
            if (strictOk && strictLen > 0 &&
                ValidateSegmentLayout(dst, strictLen, framesNumber, flags, out strictLayoutWhy))
            {
                dstLen = strictLen;
                Trace($"  LZO strict-safe OK len={strictLen}");
                return true;
            }

            g_lastUnpackError = "LZO decompression failed or produced invalid G16 segment. " +
                                fastWhy + $"; strictOk={strictOk} strictLen={strictLen} strictLayout={strictLayoutWhy}";
            return false;
        }

        /// <summary>
        /// Cheap structural validation of the decompressed segment stream.
        /// This catches corrupt/partial LZO output before the real unpack loop starts.
        /// </summary>
        private static bool ValidateSegmentLayout(byte[] work, uint workLen, uint framesNumber,
            uint flags, out string error)
        {
            error = "";
            if (work == null || workLen < 8 || workLen > work.Length)
            {
                error = $"Bad decompressed segment length: {workLen}";
                return false;
            }

            int src = 0;
            uint colorRel = ReadUInt(work, src); src += 4;
            long colorOff = src + (long)colorRel;
            uint alphaRel = ReadUInt(work, src); src += 4;
            long alphaOff = src + (long)alphaRel;

            if (colorOff < 8 || alphaOff < 8 || colorOff > workLen || alphaOff > workLen || colorOff > alphaOff)
            {
                error = $"Bad segment offsets: color={colorOff} alpha={alphaOff} workLen={workLen}";
                return false;
            }

            long alphaNeed = 0;
            long maxHeaderEnd = colorOff;
            for (uint f = 0; f < framesNumber; f++)
            {
                if (src + 2 > maxHeaderEnd)
                {
                    error = $"Frame header overrun at frame={f} src={src} colorOff={colorOff}";
                    return false;
                }

                ushort squares = ReadUShort(work, src); src += 2;
                if (squares > 4096)
                {
                    error = $"Unrealistic square count {squares} at frame={f}";
                    return false;
                }

                for (ushort s = 0; s < squares; s++)
                {
                    if (src + 4 > maxHeaderEnd)
                    {
                        error = $"Square header overrun at frame={f} square={s} src={src} colorOff={colorOff}";
                        return false;
                    }

                    uint ch = ReadUInt(work, src); src += 4;
                    uint pow = ch >> 28;
                    if (pow > 10)
                    {
                        error = $"Bad square pow={pow} at frame={f} square={s}";
                        return false;
                    }

                    long side = 1L << (int)pow;
                    alphaNeed += (side * side) / 2;
                    if (alphaOff + alphaNeed > workLen)
                    {
                        error = $"Alpha stream too short: needEnd={alphaOff + alphaNeed} workLen={workLen} frame={f} square={s}";
                        return false;
                    }
                }
            }

            if (src != colorOff)
            {
                error = $"Header/color boundary mismatch: headerEnd={src} colorOff={colorOff}";
                return false;
            }

            uint packMethod = flags & G16_PACK_METHOD_MASK;
            int colorStep = (packMethod == G16_444STORE) ? 2 : 1;
            if (packMethod != G16_IDXSTORE && packMethod != G16_444STORE)
            {
                error = $"Unknown pack method: 0x{packMethod:X}";
                return false;
            }

            long c = colorOff;
            long aEnd = alphaOff + alphaNeed;
            for (long a = alphaOff; a < aEnd; a++)
            {
                byte aa = work[(int)a];
                if ((aa & 0xF0) != 0) c += colorStep;
                if ((aa & 0x0F) != 0) c += colorStep;
                if (c > alphaOff)
                {
                    error = $"Color stream overrun: colorEnd>{alphaOff} at alpha={a}";
                    return false;
                }
            }

            if (c != alphaOff)
            {
                error = $"Color/alpha boundary mismatch: colorEnd={c} alphaOff={alphaOff}";
                return false;
            }

            if (aEnd != workLen)
            {
                error = $"Alpha/end boundary mismatch: alphaEnd={aEnd} workLen={workLen}";
                return false;
            }

            return true;
        }

        private static uint ReadUInt(byte[] data, int pos)
        {
            return (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
        }

        private static ushort ReadUShort(byte[] data, int pos)
        {
            return (ushort)(data[pos] | (data[pos + 1] << 8));
        }

        #endregion
    }
}