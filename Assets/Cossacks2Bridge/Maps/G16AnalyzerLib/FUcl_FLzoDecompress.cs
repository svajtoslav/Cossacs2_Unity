using System;

namespace G16AnalyzerLib
{
    /// <summary>
    /// LZO1X декомпрессор - точная копия lzo1x_decompress_asm_fast
    /// </summary>
    public static class Lzo1xDecompressor
    {
        // Коды ошибок LZO
        public const int LZO_E_OK = 0;
        public const int LZO_E_INPUT_OVERRUN = 4;
        public const int LZO_E_INPUT_NOT_CONSUMED = 8;

        /// <summary>
        /// lzo1x_decompress_asm_fast - точная копия логики из C++
        /// </summary>
        /// <param name="src">Входные сжатые данные</param>
        /// <param name="srcOffset">Смещение во входных данных</param>
        /// <param name="srcLen">Длина сжатых данных</param>
        /// <param name="dst">Выходной буфер</param>
        /// <param name="dstLen">Выходной параметр - длина распакованных данных</param>
        /// <param name="wrkmem">Рабочая память (не используется, для совместимости)</param>
        /// <returns>Код результата (0 = успех)</returns>
        public static int Decompress(
            byte[] src, int srcOffset, uint srcLen,
            byte[] dst, out uint dstLen,
            byte[] wrkmem = null)
        {
            dstLen = 0;

            int ip = srcOffset;                          // esi - указатель на входные данные
            int op = 0;                                   // edi - указатель на выходные данные
            int ipEnd = srcOffset + (int)srcLen;

            uint t;                                       // eax
            uint mLen;                                    // ecx - длина совпадения
            int mPos;                                     // edx - позиция совпадения

            try
            {
                t = src[ip++];

                if (t > 17)
                {
                    t -= 14;
                    goto copy_literal_run;
                }

            first_literal_run:
                if (t == 0)
                {
                    while (src[ip] == 0)
                    {
                        t += 255;
                        ip++;
                    }
                    t += (uint)(21 + src[ip++]);
                }
                else if (t < 16)
                {
                    t += 6;
                }
                else
                {
                    goto match;
                }

            copy_literal_run:
                {
                    // Копирование литералов по 4 байта
                    uint copyLen = t;
                    uint remainder = (t ^ 3) & 3;
                    uint dwords = copyLen >> 2;

                    while (dwords > 0)
                    {
                        // *(unsigned int*)op = *(unsigned int*)ip;
                        dst[op] = src[ip];
                        dst[op + 1] = src[ip + 1];
                        dst[op + 2] = src[ip + 2];
                        dst[op + 3] = src[ip + 3];
                        op += 4;
                        ip += 4;
                        dwords--;
                    }

                    ip -= (int)remainder;
                    op -= (int)remainder;
                }

                t = src[ip++];

                if (t < 16)
                {
                    // Специальный случай: короткое совпадение после литералов
                    uint offset = (t >> 2) + ((uint)src[ip++] << 2);
                    mPos = op - 0x801 - (int)offset;

                    // *(unsigned int*)op = *(unsigned int*)m_pos;
                    dst[op] = dst[mPos];
                    dst[op + 1] = dst[mPos + 1];
                    dst[op + 2] = dst[mPos + 2];
                    op += 3;
                    goto copy_match_done;
                }

            match:
                if (t >= 64)
                {
                    // M2 match
                    mLen = (t >> 5) + 4;
                    uint offset = ((t >> 2) & 7) + ((uint)src[ip++] << 3);
                    mPos = op - 1 - (int)offset;

                    if (offset < 3)
                    {
                        goto copy_match_byte;
                    }
                    goto copy_match_dword;
                }
                else if (t >= 32)
                {
                    // M3 match
                    mLen = t & 31;
                    if (mLen == 0)
                    {
                        while (src[ip] == 0)
                        {
                            mLen += 255;
                            ip++;
                        }
                        mLen += (uint)(36 + src[ip++]);
                    }
                    else
                    {
                        mLen += 5;
                    }

                    // unsigned int offset = *(unsigned short*)ip >> 2;
                    uint offset = (uint)(BitConverter.ToUInt16(src, ip) >> 2);
                    ip += 2;
                    mPos = op - 1 - (int)offset;

                    if (offset < 3)
                    {
                        goto copy_match_byte;
                    }
                    goto copy_match_dword;
                }
                else if (t >= 16)
                {
                    // M4 match
                    // Original x86 path ORs the high M4 bit into the 16-bit raw offset before SHR 2.
                    // Therefore raw>>2 == 0 is EOF only when the high bit is also zero.
                    // If we test the low word alone, FrnMel segment 16..31 is falsely cut as EOF.
                    uint highBit = (t & 8u) << 13;
                    mLen = t & 7;

                    if (mLen == 0)
                    {
                        while (src[ip] == 0)
                        {
                            mLen += 255;
                            ip++;
                        }
                        mLen += (uint)(12 + src[ip++]);
                    }
                    else
                    {
                        mLen += 5;
                    }

                    uint rawOffset16 = BitConverter.ToUInt16(src, ip);
                    ip += 2;
                    uint offset = (highBit | rawOffset16) >> 2;

                    if (offset == 0)
                    {
                        // Конец декомпрессии
                        int result = (mLen != 6) ? -1 : 0;

                        dstLen = (uint)op;

                        if (ip > ipEnd)
                        {
                            return -LZO_E_INPUT_OVERRUN;
                        }
                        else if (ip < ipEnd)
                        {
                            return -LZO_E_INPUT_NOT_CONSUMED;
                        }

                        return result;
                    }

                    mPos = op - 0x4000 - (int)offset;
                    goto copy_match_dword;
                }
                else
                {
                    // M1 match (t < 16)
                    uint offset = (t >> 2) + ((uint)src[ip++] << 2);
                    mPos = op - 1 - (int)offset;

                    dst[op++] = dst[mPos++];
                    dst[op++] = dst[mPos];
                    goto copy_match_done;
                }

            copy_match_dword:
                {
                    int endPos = op + (int)mLen - 3;
                    uint dwords = mLen >> 2;

                    while (dwords > 0)
                    {
                        // *(unsigned int*)op = *(unsigned int*)m_pos;
                        dst[op] = dst[mPos];
                        dst[op + 1] = dst[mPos + 1];
                        dst[op + 2] = dst[mPos + 2];
                        dst[op + 3] = dst[mPos + 3];
                        op += 4;
                        mPos += 4;
                        dwords--;
                    }

                    op = endPos;
                }
                goto copy_match_done;

            copy_match_byte:
                {
                    mLen -= 3;
                    while (mLen > 0)
                    {
                        dst[op++] = dst[mPos++];
                        mLen--;
                    }
                }
                // fall through

            copy_match_done:
                {
                    t = (uint)(src[ip - 2] & 3);
                    if (t == 0)
                    {
                        goto first_literal_run;
                    }

                    // Копируем остаточные байты
                    // *(unsigned int*)op = *(unsigned int*)ip;
                    for (uint i = 0; i < t; i++)
                    {
                        dst[op + i] = src[ip + i];
                    }
                    ip += (int)t;
                    op += (int)t;

                    t = src[ip++];
                    goto match;
                }
            }
            catch (IndexOutOfRangeException)
            {
                return -LZO_E_INPUT_OVERRUN;
            }
        }

        /// <summary>
        /// Упрощённая обёртка для декомпрессии
        /// </summary>
        public static bool DecompressSafe(
            byte[] src, int srcOffset, uint srcLen,
            byte[] dst, out uint dstLen)
        {
            int result = Decompress(src, srcOffset, srcLen, dst, out dstLen, null);
            return result == LZO_E_OK;
        }
    }


    /// <summary>
    /// Strict safe LZO1X fallback based on the public miniLZO stream layout.
    /// It is used only when the old gMotor fast-port output does not validate
    /// as a G16 segment. This prevents partial/corrupt LZO output from being
    /// parsed later as squares/alpha and producing misleading "Alpha buffer overrun".
    /// </summary>
    public static class Lzo1xStrictSafeDecompressor
    {
        public static bool Decompress(
            byte[] src, int srcOffset, uint srcLen,
            byte[] dst, out uint dstLen)
        {
            dstLen = 0;
            if (src == null || dst == null || srcLen == 0)
                return false;

            int ip = srcOffset;
            int ipEnd = srcOffset + (int)srcLen;
            int op = 0;
            int opEnd = dst.Length;

            try
            {
                if (ip >= ipEnd)
                    return false;

                uint t = src[ip++];

                if (t > 17)
                {
                    t -= 17;
                    if (!CopyLiteral(src, ref ip, ipEnd, dst, ref op, opEnd, t))
                        return false;
                    if (ip >= ipEnd)
                    {
                        dstLen = (uint)op;
                        return true;
                    }
                    t = src[ip++];
                }

                while (true)
                {
                    if (t < 16)
                    {
                        if (t == 0)
                        {
                            uint n = 0;
                            while (ip < ipEnd && src[ip] == 0)
                            {
                                n += 255;
                                ip++;
                            }
                            if (ip >= ipEnd)
                                return false;
                            t = n + (uint)src[ip++] + 15;
                        }

                        t += 3;
                        if (!CopyLiteral(src, ref ip, ipEnd, dst, ref op, opEnd, t))
                            return false;

                        if (ip >= ipEnd)
                        {
                            dstLen = (uint)op;
                            return true;
                        }

                        t = src[ip++];
                        if (t < 16)
                        {
                            if (ip >= ipEnd)
                                return false;

                            uint off = (t >> 2) + ((uint)src[ip++] << 2) + 0x801;
                            uint len = 3;
                            if (!CopyMatch(dst, ref op, opEnd, off, len))
                                return false;

                            uint lit = t & 3;
                            if (lit != 0 && !CopyLiteral(src, ref ip, ipEnd, dst, ref op, opEnd, lit))
                                return false;

                            if (ip >= ipEnd)
                            {
                                dstLen = (uint)op;
                                return true;
                            }
                            t = src[ip++];
                        }
                    }

                    if (t >= 64)
                    {
                        if (ip >= ipEnd)
                            return false;
                        uint len = (t >> 5) + 1;
                        uint off = ((t >> 2) & 7) + ((uint)src[ip++] << 3) + 1;
                        if (!CopyMatch(dst, ref op, opEnd, off, len))
                            return false;
                    }
                    else if (t >= 32)
                    {
                        uint len = t & 31;
                        if (len == 0)
                        {
                            while (ip < ipEnd && src[ip] == 0)
                            {
                                len += 255;
                                ip++;
                            }
                            if (ip >= ipEnd)
                                return false;
                            len += 31 + (uint)src[ip++];
                        }
                        len += 2;

                        if (ip + 2 > ipEnd)
                            return false;
                        uint raw = (uint)(src[ip] | (src[ip + 1] << 8));
                        uint off = (raw >> 2) + 1;
                        uint lit = raw & 3;
                        ip += 2;

                        if (!CopyMatch(dst, ref op, opEnd, off, len))
                            return false;
                        if (lit != 0 && !CopyLiteral(src, ref ip, ipEnd, dst, ref op, opEnd, lit))
                            return false;

                        if (ip >= ipEnd)
                        {
                            dstLen = (uint)op;
                            return true;
                        }
                        t = src[ip++];
                        continue;
                    }
                    else if (t >= 16)
                    {
                        uint len = t & 7;
                        uint high = (t & 8) << 11;
                        if (len == 0)
                        {
                            while (ip < ipEnd && src[ip] == 0)
                            {
                                len += 255;
                                ip++;
                            }
                            if (ip >= ipEnd)
                                return false;
                            len += 7 + (uint)src[ip++];
                        }
                        len += 2;

                        if (ip + 2 > ipEnd)
                            return false;
                        uint raw = (uint)(src[ip] | (src[ip + 1] << 8));
                        uint offLow = raw >> 2;
                        uint lit = raw & 3;
                        ip += 2;

                        // Original M4 EOF test uses the combined high+low offset.
                        // raw>>2 == 0 alone is not EOF when the high M4 bit is set.
                        uint combinedOff = high + offLow;
                        if (combinedOff == 0)
                        {
                            dstLen = (uint)op;
                            return ip == ipEnd;
                        }

                        uint off = combinedOff + 0x4000;
                        if (!CopyMatch(dst, ref op, opEnd, off, len))
                            return false;
                        if (lit != 0 && !CopyLiteral(src, ref ip, ipEnd, dst, ref op, opEnd, lit))
                            return false;

                        if (ip >= ipEnd)
                        {
                            dstLen = (uint)op;
                            return true;
                        }
                        t = src[ip++];
                        continue;
                    }
                    else
                    {
                        if (ip >= ipEnd)
                            return false;
                        uint off = (t >> 2) + ((uint)src[ip++] << 2) + 1;
                        uint len = 2;
                        if (!CopyMatch(dst, ref op, opEnd, off, len))
                            return false;
                    }

                    uint trailing = t & 3;
                    if (trailing != 0 && !CopyLiteral(src, ref ip, ipEnd, dst, ref op, opEnd, trailing))
                        return false;

                    if (ip >= ipEnd)
                    {
                        dstLen = (uint)op;
                        return true;
                    }
                    t = src[ip++];
                }
            }
            catch
            {
                dstLen = 0;
                return false;
            }
        }

        private static bool CopyLiteral(byte[] src, ref int ip, int ipEnd,
            byte[] dst, ref int op, int opEnd, uint count)
        {
            if (count == 0)
                return true;
            if (ip + (int)count > ipEnd || op + (int)count > opEnd)
                return false;
            for (uint i = 0; i < count; i++)
                dst[op++] = src[ip++];
            return true;
        }

        private static bool CopyMatch(byte[] dst, ref int op, int opEnd,
            uint offset, uint count)
        {
            if (offset == 0 || offset > op)
                return false;
            if (op + (int)count > opEnd)
                return false;

            int mp = op - (int)offset;
            for (uint i = 0; i < count; i++)
                dst[op++] = dst[mp++];
            return true;
        }
    }

    /// <summary>
    /// UCL декомпрессор - точная копия ucl_decompress
    /// </summary>
    public static class UclDecompressor
    {
        /// <summary>
        /// ucl_decompress - точная копия логики из C++
        /// </summary>
        /// <param name="src">Входные сжатые данные</param>
        /// <param name="srcOffset">Смещение во входных данных</param>
        /// <param name="srcLen">Длина сжатых данных</param>
        /// <param name="dst">Выходной буфер</param>
        /// <param name="dstLen">Выходной параметр - длина распакованных данных</param>
        /// <returns>true при успехе</returns>
        public static bool Decompress(
            byte[] src, int srcOffset, uint srcLen,
            byte[] dst, out uint dstLen)
        {
            dstLen = 0;
            uint bb = 0;
            int ilen = srcOffset;
            int ilenEnd = srcOffset + (int)srcLen;
            int olen = 0;
            uint lastMOff = 1;

            // Локальная функция getbit - точная копия макроса
            // #define getbit(bb) (bb<<=1, bb&0xff ? (bb>>8)&1 : ((bb=(src[ilen++]<<1)+1)>>8)&1)
            int GetBit()
            {
                bb <<= 1;
                if ((bb & 0xff) != 0)
                {
                    return (int)((bb >> 8) & 1);
                }
                else
                {
                    if (ilen >= ilenEnd)
                        return 0; // защита от выхода за границы
                    bb = (uint)((src[ilen++] << 1) + 1);
                    return (int)((bb >> 8) & 1);
                }
            }

            try
            {
                for (; ; )
                {
                    uint mOff, mLen;

                    if (GetBit() != 0)
                    {
                        if (ilen >= ilenEnd)
                            return false;
                        dst[olen++] = src[ilen++];
                        continue;
                    }

                    mOff = 1;
                    do
                    {
                        mOff = (mOff << 1) + (uint)GetBit();
                    } while (GetBit() == 0);

                    if (mOff == 2)
                    {
                        mOff = lastMOff;
                    }
                    else
                    {
                        if (ilen >= ilenEnd)
                            return false;
                        mOff = ((mOff - 3) << 8) + src[ilen++];
                        if (mOff == 0xffffffff)
                            break;
                        lastMOff = ++mOff;
                    }

                    mLen = (uint)GetBit();
                    mLen = (mLen << 1) + (uint)GetBit();

                    if (mLen == 0)
                    {
                        mLen++;
                        do
                        {
                            mLen = (mLen << 1) + (uint)GetBit();
                        } while (GetBit() == 0);
                        mLen += 2;
                    }

                    // m_len += (m_off > 0xd00);
                    mLen += (mOff > 0xd00) ? 1u : 0u;

                    {
                        if (olen < (int)mOff)
                            return false;

                        int mPos = olen - (int)mOff;
                        dst[olen++] = dst[mPos++];

                        while (mLen-- > 0)
                        {
                            dst[olen++] = dst[mPos++];
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }

            dstLen = (uint)olen;

            // if(ilen != src_len) return false;
            // Проверяем, что прочитали ровно столько, сколько было во входных данных
            if (ilen != ilenEnd)
                return false;

            return true;
        }
    }
}