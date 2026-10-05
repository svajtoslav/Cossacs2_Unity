// MIT-derived BZip2 runtime decoder adapted for this project.
// Source basis: SharpZipLib BZip2InputStream / BZip2Constants (icsharpcode/SharpZipLib, MIT).
// Trimmed to decompression-only pieces required for .m3d map loading.

using System;
using System.IO;

namespace Cossacks2Bridge.UnityAdapters.Maps.InternalBZip2
{
    internal interface IChecksum
    {
        long Value { get; }
        void Reset();
        void Update(int value);
    }

    internal sealed class BZip2Crc : IChecksum
    {
        private const uint CrcInit = 0xFFFFFFFFu;
        private const uint Polynomial = 0x04C11DB7u;
        private static readonly uint[] CrcTable = CreateTable();
        private uint _checkValue;

        public BZip2Crc()
        {
            Reset();
        }

        public long Value => unchecked((long)(~_checkValue));

        public void Reset()
        {
            _checkValue = CrcInit;
        }

        public void Update(int value)
        {
            _checkValue = unchecked(CrcTable[((_checkValue >> 24) ^ (byte)value) & 0xFF] ^ (_checkValue << 8));
        }

        private static uint[] CreateTable()
        {
            var table = new uint[256];
            for (int i = 0; i < table.Length; i++)
            {
                uint r = (uint)i << 24;
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((r & 0x80000000u) != 0)
                        r = unchecked((r << 1) ^ Polynomial);
                    else
                        r <<= 1;
                }
                table[i] = r;
            }
            return table;
        }
    }

    internal static class BZip2Constants
    {
        public static readonly int[] RandomNumbers = {
            619, 720, 127, 481, 931, 816, 813, 233, 566, 247,
            985, 724, 205, 454, 863, 491, 741, 242, 949, 214,
            733, 859, 335, 708, 621, 574, 73, 654, 730, 472,
            419, 436, 278, 496, 867, 210, 399, 680, 480, 51,
            878, 465, 811, 169, 869, 675, 611, 697, 867, 561,
            862, 687, 507, 283, 482, 129, 807, 591, 733, 623,
            150, 238, 59, 379, 684, 877, 625, 169, 643, 105,
            170, 607, 520, 932, 727, 476, 693, 425, 174, 647,
            73, 122, 335, 530, 442, 853, 695, 249, 445, 515,
            909, 545, 703, 919, 874, 474, 882, 500, 594, 612,
            641, 801, 220, 162, 819, 984, 589, 513, 495, 799,
            161, 604, 958, 533, 221, 400, 386, 867, 600, 782,
            382, 596, 414, 171, 516, 375, 682, 485, 911, 276,
            98, 553, 163, 354, 666, 933, 424, 341, 533, 870,
            227, 730, 475, 186, 263, 647, 537, 686, 600, 224,
            469, 68, 770, 919, 190, 373, 294, 822, 808, 206,
            184, 943, 795, 384, 383, 461, 404, 758, 839, 887,
            715, 67, 618, 276, 204, 918, 873, 777, 604, 560,
            951, 160, 578, 722, 79, 804, 96, 409, 713, 940,
            652, 934, 970, 447, 318, 353, 859, 672, 112, 785,
            645, 863, 803, 350, 139, 93, 354, 99, 820, 908,
            609, 772, 154, 274, 580, 184, 79, 626, 630, 742,
            653, 282, 762, 623, 680, 81, 927, 626, 789, 125,
            411, 521, 938, 300, 821, 78, 343, 175, 128, 250,
            170, 774, 972, 275, 999, 639, 495, 78, 352, 126,
            857, 956, 358, 619, 580, 124, 737, 594, 701, 612,
            669, 112, 134, 694, 363, 992, 809, 743, 168, 974,
            944, 375, 748, 52, 600, 747, 642, 182, 862, 81,
            344, 805, 988, 739, 511, 655, 814, 334, 249, 515,
            897, 955, 664, 981, 649, 113, 974, 459, 893, 228,
            433, 837, 553, 268, 926, 240, 102, 654, 459, 51,
            686, 754, 806, 760, 493, 403, 415, 394, 687, 700,
            946, 670, 656, 610, 738, 392, 760, 799, 887, 653,
            978, 321, 576, 617, 626, 502, 894, 679, 243, 440,
            680, 879, 194, 572, 640, 724, 926, 56, 204, 700,
            707, 151, 457, 449, 797, 195, 791, 558, 945, 679,
            297, 59, 87, 824, 713, 663, 412, 693, 342, 606,
            134, 108, 571, 364, 631, 212, 174, 643, 304, 329,
            343, 97, 430, 751, 497, 314, 983, 374, 822, 928,
            140, 206, 73, 263, 980, 736, 876, 478, 430, 305,
            170, 514, 364, 692, 829, 82, 855, 953, 676, 246,
            369, 970, 294, 750, 807, 827, 150, 790, 288, 923,
            804, 378, 215, 828, 592, 281, 565, 555, 710, 82,
            896, 831, 547, 261, 524, 462, 293, 465, 502, 56,
            661, 821, 976, 991, 658, 869, 905, 758, 745, 193,
            768, 550, 608, 933, 378, 286, 215, 979, 792, 961,
            61, 688, 793, 644, 986, 403, 106, 366, 905, 644,
            372, 567, 466, 434, 645, 210, 389, 550, 919, 135,
            780, 773, 635, 389, 707, 100, 626, 958, 165, 504,
            920, 176, 193, 713, 857, 265, 203, 50, 668, 108,
            645, 990, 626, 197, 510, 357, 358, 850, 858, 364,
            936, 638
        };

        public const int BaseBlockSize = 100000;
        public const int MaximumAlphaSize = 258;
        public const int MaximumCodeLength = 23;
        public const int RunA = 0;
        public const int RunB = 1;
        public const int GroupCount = 6;
        public const int GroupSize = 50;
        public const int MaximumSelectors = 2 + (900000 / GroupSize);
    }

    internal sealed class BZip2Exception : Exception
    {
        public BZip2Exception(string message) : base(message) { }
    }

    internal sealed class BZip2InputStream : Stream
    {
        private const int StartBlockState = 1;
        private const int RandPartAState = 2;
        private const int RandPartBState = 3;
        private const int RandPartCState = 4;
        private const int NoRandPartAState = 5;
        private const int NoRandPartBState = 6;
        private const int NoRandPartCState = 7;

        private int last;
        private int origPtr;
        private int blockSize100k;
        private bool blockRandomised;
        private int bsBuff;
        private int bsLive;
        private IChecksum mCrc = new BZip2Crc();
        private readonly bool[] inUse = new bool[256];
        private int nInUse;
        private readonly byte[] seqToUnseq = new byte[256];
        private readonly byte[] unseqToSeq = new byte[256];
        private readonly byte[] selector = new byte[BZip2Constants.MaximumSelectors];
        private readonly byte[] selectorMtf = new byte[BZip2Constants.MaximumSelectors];
        private int[] tt;
        private byte[] ll8;
        private readonly int[] unzftab = new int[256];
        private readonly int[][] limit = new int[BZip2Constants.GroupCount][];
        private readonly int[][] baseArray = new int[BZip2Constants.GroupCount][];
        private readonly int[][] perm = new int[BZip2Constants.GroupCount][];
        private readonly int[] minLens = new int[BZip2Constants.GroupCount];
        private readonly Stream baseStream;
        private bool streamEnd;
        private int currentChar = -1;
        private int currentState = StartBlockState;
        private int storedBlockCRC, storedCombinedCRC;
        private int computedBlockCRC;
        private uint computedCombinedCRC;
        private int count, chPrev, ch2;
        private int tPos;
        private int rNToGo;
        private int rTPos;
        private int i2, j2;
        private byte z;

        public BZip2InputStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            for (int i = 0; i < BZip2Constants.GroupCount; ++i)
            {
                limit[i] = new int[BZip2Constants.MaximumAlphaSize];
                baseArray[i] = new int[BZip2Constants.MaximumAlphaSize];
                perm[i] = new int[BZip2Constants.MaximumAlphaSize];
            }

            baseStream = stream;
            bsLive = 0;
            bsBuff = 0;
            Initialize();
            InitBlock();
            if (!streamEnd)
                SetupBlock();
        }

        public bool IsStreamOwner { get; set; } = true;
        public override bool CanRead => baseStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => baseStream.Length;
        public override long Position
        {
            get => baseStream.Position;
            set => throw new NotSupportedException("BZip2InputStream position cannot be set");
        }

        public override void Flush() => baseStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("BZip2InputStream Seek not supported");
        public override void SetLength(long value) => throw new NotSupportedException("BZip2InputStream SetLength not supported");
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("BZip2InputStream Write not supported");
        public override void WriteByte(byte value) => throw new NotSupportedException("BZip2InputStream WriteByte not supported");

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            for (int i = 0; i < count; ++i)
            {
                int rb = ReadByte();
                if (rb == -1)
                    return i;
                buffer[offset + i] = (byte)rb;
            }
            return count;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && IsStreamOwner)
                baseStream.Dispose();
            base.Dispose(disposing);
        }

        public override int ReadByte()
        {
            if (streamEnd) return -1;

            int retChar = currentChar;
            switch (currentState)
            {
                case RandPartBState: SetupRandPartB(); break;
                case RandPartCState: SetupRandPartC(); break;
                case NoRandPartBState: SetupNoRandPartB(); break;
                case NoRandPartCState: SetupNoRandPartC(); break;
            }
            return retChar;
        }

        private void MakeMaps()
        {
            nInUse = 0;
            for (int i = 0; i < 256; ++i)
            {
                if (inUse[i])
                {
                    seqToUnseq[nInUse] = (byte)i;
                    unseqToSeq[i] = (byte)nInUse;
                    nInUse++;
                }
            }
        }

        private void Initialize()
        {
            char magic1 = BsGetUChar();
            char magic2 = BsGetUChar();
            char magic3 = BsGetUChar();
            char magic4 = BsGetUChar();

            if (magic1 != 'B' || magic2 != 'Z' || magic3 != 'h' || magic4 < '1' || magic4 > '9')
            {
                streamEnd = true;
                return;
            }

            SetDecompressStructureSizes(magic4 - '0');
            computedCombinedCRC = 0;
        }

        private void InitBlock()
        {
            char magic1 = BsGetUChar();
            char magic2 = BsGetUChar();
            char magic3 = BsGetUChar();
            char magic4 = BsGetUChar();
            char magic5 = BsGetUChar();
            char magic6 = BsGetUChar();

            if (magic1 == 0x17 && magic2 == 0x72 && magic3 == 0x45 && magic4 == 0x38 && magic5 == 0x50 && magic6 == 0x90)
            {
                Complete();
                return;
            }

            if (magic1 != 0x31 || magic2 != 0x41 || magic3 != 0x59 || magic4 != 0x26 || magic5 != 0x53 || magic6 != 0x59)
            {
                BadBlockHeader();
                streamEnd = true;
                return;
            }

            storedBlockCRC = BsGetInt32();
            blockRandomised = (BsR(1) == 1);
            GetAndMoveToFrontDecode();
            mCrc.Reset();
            currentState = StartBlockState;
        }

        private void EndBlock()
        {
            computedBlockCRC = (int)mCrc.Value;
            if (storedBlockCRC != computedBlockCRC)
                CrcError();

            computedCombinedCRC = ((computedCombinedCRC << 1) & 0xFFFFFFFFu) | (computedCombinedCRC >> 31);
            computedCombinedCRC ^= (uint)computedBlockCRC;
        }

        private void Complete()
        {
            storedCombinedCRC = BsGetInt32();
            if (storedCombinedCRC != (int)computedCombinedCRC)
                CrcError();
            streamEnd = true;
        }

        private void FillBuffer()
        {
            int thech;
            try
            {
                thech = baseStream.ReadByte();
            }
            catch (Exception)
            {
                CompressedStreamEOF();
                return;
            }

            if (thech == -1)
                CompressedStreamEOF();

            bsBuff = (bsBuff << 8) | (thech & 0xFF);
            bsLive += 8;
        }

        private int BsR(int n)
        {
            while (bsLive < n)
                FillBuffer();
            int v = (bsBuff >> (bsLive - n)) & ((1 << n) - 1);
            bsLive -= n;
            return v;
        }

        private char BsGetUChar() => (char)BsR(8);
        private int BsGetIntVS(int numBits) => BsR(numBits);

        private int BsGetInt32()
        {
            int result = BsR(8);
            result = (result << 8) | BsR(8);
            result = (result << 8) | BsR(8);
            result = (result << 8) | BsR(8);
            return result;
        }

        private void RecvDecodingTables()
        {
            char[][] len = new char[BZip2Constants.GroupCount][];
            for (int i = 0; i < BZip2Constants.GroupCount; ++i)
                len[i] = new char[BZip2Constants.MaximumAlphaSize];

            bool[] inUse16 = new bool[16];
            for (int i = 0; i < 16; i++)
                inUse16[i] = (BsR(1) == 1);

            for (int i = 0; i < 16; i++)
            {
                if (inUse16[i])
                {
                    for (int j = 0; j < 16; j++)
                        inUse[i * 16 + j] = (BsR(1) == 1);
                }
                else
                {
                    for (int j = 0; j < 16; j++)
                        inUse[i * 16 + j] = false;
                }
            }

            MakeMaps();
            int alphaSize = nInUse + 2;
            int nGroups = BsR(3);
            int nSelectors = BsR(15);

            for (int i = 0; i < nSelectors; i++)
            {
                int j = 0;
                while (BsR(1) == 1) j++;
                selectorMtf[i] = (byte)j;
            }

            byte[] pos = new byte[BZip2Constants.GroupCount];
            for (int v = 0; v < nGroups; v++)
                pos[v] = (byte)v;

            for (int i = 0; i < nSelectors; i++)
            {
                int v = selectorMtf[i];
                byte tmp = pos[v];
                while (v > 0)
                {
                    pos[v] = pos[v - 1];
                    v--;
                }
                pos[0] = tmp;
                selector[i] = tmp;
            }

            for (int t = 0; t < nGroups; t++)
            {
                int curr = BsR(5);
                for (int i = 0; i < alphaSize; i++)
                {
                    while (BsR(1) == 1)
                    {
                        curr += BsR(1) == 0 ? 1 : -1;
                    }
                    len[t][i] = (char)curr;
                }
            }

            for (int t = 0; t < nGroups; t++)
            {
                int minLen = 32;
                int maxLen = 0;
                for (int i = 0; i < alphaSize; i++)
                {
                    maxLen = Math.Max(maxLen, len[t][i]);
                    minLen = Math.Min(minLen, len[t][i]);
                }
                HbCreateDecodeTables(limit[t], baseArray[t], perm[t], len[t], minLen, maxLen, alphaSize);
                minLens[t] = minLen;
            }
        }

        private void GetAndMoveToFrontDecode()
        {
            byte[] yy = new byte[256];
            int nextSym;
            int limitLast = BZip2Constants.BaseBlockSize * blockSize100k;
            origPtr = BsGetIntVS(24);

            RecvDecodingTables();
            int eob = nInUse + 1;
            int groupNo = -1;
            int groupPos = 0;

            for (int i = 0; i <= 255; i++)
                unzftab[i] = 0;
            for (int i = 0; i <= 255; i++)
                yy[i] = (byte)i;

            last = -1;

            if (groupPos == 0)
            {
                groupNo++;
                groupPos = BZip2Constants.GroupSize;
            }

            groupPos--;
            int zt = selector[groupNo];
            int zn = minLens[zt];
            int zvec = BsR(zn);
            int zj;

            while (zvec > limit[zt][zn])
            {
                if (zn > 20)
                    throw new BZip2Exception("Bzip data error");
                zn++;
                while (bsLive < 1) FillBuffer();
                zj = (bsBuff >> (bsLive - 1)) & 1;
                bsLive--;
                zvec = (zvec << 1) | zj;
            }

            int permIndex = zvec - baseArray[zt][zn];
            if (permIndex < 0 || permIndex >= BZip2Constants.MaximumAlphaSize)
                throw new BZip2Exception("Bzip data error");

            nextSym = perm[zt][permIndex];

            while (true)
            {
                if (nextSym == eob)
                    break;

                if (nextSym == BZip2Constants.RunA || nextSym == BZip2Constants.RunB)
                {
                    int s = -1;
                    int n = 1;
                    do
                    {
                        if (nextSym == BZip2Constants.RunA) s += n;
                        else if (nextSym == BZip2Constants.RunB) s += 2 * n;
                        n <<= 1;

                        if (groupPos == 0)
                        {
                            groupNo++;
                            groupPos = BZip2Constants.GroupSize;
                        }

                        groupPos--;
                        zt = selector[groupNo];
                        zn = minLens[zt];
                        zvec = BsR(zn);

                        while (zvec > limit[zt][zn])
                        {
                            zn++;
                            while (bsLive < 1) FillBuffer();
                            zj = (bsBuff >> (bsLive - 1)) & 1;
                            bsLive--;
                            zvec = (zvec << 1) | zj;
                        }

                        nextSym = perm[zt][zvec - baseArray[zt][zn]];
                    }
                    while (nextSym == BZip2Constants.RunA || nextSym == BZip2Constants.RunB);

                    s++;
                    byte ch = seqToUnseq[yy[0]];
                    unzftab[ch] += s;

                    while (s > 0)
                    {
                        last++;
                        ll8[last] = ch;
                        s--;
                    }

                    if (last >= limitLast)
                        BlockOverrun();
                    continue;
                }

                last++;
                if (last >= limitLast)
                    BlockOverrun();

                byte tmp = yy[nextSym - 1];
                unzftab[seqToUnseq[tmp]]++;
                ll8[last] = seqToUnseq[tmp];

                int j = nextSym - 1;
                while (j > 0)
                {
                    yy[j] = yy[j - 1];
                    j--;
                }
                yy[0] = tmp;

                if (groupPos == 0)
                {
                    groupNo++;
                    groupPos = BZip2Constants.GroupSize;
                }

                groupPos--;
                zt = selector[groupNo];
                zn = minLens[zt];
                zvec = BsR(zn);
                while (zvec > limit[zt][zn])
                {
                    zn++;
                    while (bsLive < 1) FillBuffer();
                    zj = (bsBuff >> (bsLive - 1)) & 1;
                    bsLive--;
                    zvec = (zvec << 1) | zj;
                }
                nextSym = perm[zt][zvec - baseArray[zt][zn]];
            }
        }

        private void SetupBlock()
        {
            int[] cftab = new int[257];
            cftab[0] = 0;
            Array.Copy(unzftab, 0, cftab, 1, 256);

            for (int i = 1; i <= 256; i++)
                cftab[i] += cftab[i - 1];

            for (int i = 0; i <= last; i++)
            {
                byte ch = ll8[i];
                tt[cftab[ch]] = i;
                cftab[ch]++;
            }

            tPos = tt[origPtr];
            count = 0;
            i2 = 0;
            ch2 = 256;

            if (blockRandomised)
            {
                rNToGo = 0;
                rTPos = 0;
                SetupRandPartA();
            }
            else
            {
                SetupNoRandPartA();
            }
        }

        private void SetupRandPartA()
        {
            if (i2 <= last)
            {
                chPrev = ch2;
                ch2 = ll8[tPos];
                tPos = tt[tPos];
                if (rNToGo == 0)
                {
                    rNToGo = BZip2Constants.RandomNumbers[rTPos];
                    rTPos++;
                    if (rTPos == 512) rTPos = 0;
                }
                rNToGo--;
                ch2 ^= (rNToGo == 1) ? 1 : 0;
                i2++;

                currentChar = ch2;
                currentState = RandPartBState;
                mCrc.Update(ch2);
            }
            else
            {
                EndBlock();
                InitBlock();
                if (!streamEnd) SetupBlock();
            }
        }

        private void SetupNoRandPartA()
        {
            if (i2 <= last)
            {
                chPrev = ch2;
                ch2 = ll8[tPos];
                tPos = tt[tPos];
                i2++;

                currentChar = ch2;
                currentState = NoRandPartBState;
                mCrc.Update(ch2);
            }
            else
            {
                EndBlock();
                InitBlock();
                if (!streamEnd) SetupBlock();
            }
        }

        private void SetupRandPartB()
        {
            if (ch2 != chPrev)
            {
                currentState = RandPartAState;
                count = 1;
                SetupRandPartA();
            }
            else
            {
                count++;
                if (count >= 4)
                {
                    z = ll8[tPos];
                    tPos = tt[tPos];
                    if (rNToGo == 0)
                    {
                        rNToGo = BZip2Constants.RandomNumbers[rTPos];
                        rTPos++;
                        if (rTPos == 512) rTPos = 0;
                    }
                    rNToGo--;
                    z ^= (byte)((rNToGo == 1) ? 1 : 0);
                    j2 = 0;
                    currentState = RandPartCState;
                    SetupRandPartC();
                }
                else
                {
                    currentState = RandPartAState;
                    SetupRandPartA();
                }
            }
        }

        private void SetupRandPartC()
        {
            if (j2 < z)
            {
                currentChar = ch2;
                mCrc.Update(ch2);
                j2++;
            }
            else
            {
                currentState = RandPartAState;
                i2++;
                count = 0;
                SetupRandPartA();
            }
        }

        private void SetupNoRandPartB()
        {
            if (ch2 != chPrev)
            {
                currentState = NoRandPartAState;
                count = 1;
                SetupNoRandPartA();
            }
            else
            {
                count++;
                if (count >= 4)
                {
                    z = ll8[tPos];
                    tPos = tt[tPos];
                    currentState = NoRandPartCState;
                    j2 = 0;
                    SetupNoRandPartC();
                }
                else
                {
                    currentState = NoRandPartAState;
                    SetupNoRandPartA();
                }
            }
        }

        private void SetupNoRandPartC()
        {
            if (j2 < z)
            {
                currentChar = ch2;
                mCrc.Update(ch2);
                j2++;
            }
            else
            {
                currentState = NoRandPartAState;
                i2++;
                count = 0;
                SetupNoRandPartA();
            }
        }

        private void SetDecompressStructureSizes(int newSize100k)
        {
            if (!(0 <= newSize100k && newSize100k <= 9))
                throw new BZip2Exception("Invalid block size");

            blockSize100k = newSize100k;
            if (newSize100k == 0)
                return;

            int n = BZip2Constants.BaseBlockSize * newSize100k;
            ll8 = new byte[n];
            tt = new int[n];
        }

        private static void CompressedStreamEOF() => throw new EndOfStreamException("BZip2 input stream end of compressed stream");
        private static void BlockOverrun() => throw new BZip2Exception("BZip2 input stream block overrun");
        private static void BadBlockHeader() => throw new BZip2Exception("BZip2 input stream bad block header");
        private static void CrcError() => throw new BZip2Exception("BZip2 input stream crc error");

        private static void HbCreateDecodeTables(int[] limit, int[] baseArray, int[] perm, char[] length, int minLen, int maxLen, int alphaSize)
        {
            int pp = 0;

            for (int i = minLen; i <= maxLen; ++i)
                for (int j = 0; j < alphaSize; ++j)
                    if (length[j] == i)
                        perm[pp++] = j;

            for (int i = 0; i < BZip2Constants.MaximumCodeLength; i++)
                baseArray[i] = 0;

            for (int i = 0; i < alphaSize; i++)
                ++baseArray[length[i] + 1];

            for (int i = 1; i < BZip2Constants.MaximumCodeLength; i++)
                baseArray[i] += baseArray[i - 1];

            for (int i = 0; i < BZip2Constants.MaximumCodeLength; i++)
                limit[i] = 0;

            int vec = 0;
            for (int i = minLen; i <= maxLen; i++)
            {
                vec += (baseArray[i + 1] - baseArray[i]);
                limit[i] = vec - 1;
                vec <<= 1;
            }

            for (int i = minLen + 1; i <= maxLen; i++)
                baseArray[i] = ((limit[i - 1] + 1) << 1) - baseArray[i];
        }
    }

    internal static class SharpBZip2Compat
    {
        public static byte[] Decompress(byte[] payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            using (var src = new MemoryStream(payload, false))
            using (var bz = new BZip2InputStream(src) { IsStreamOwner = false })
            using (var dst = new MemoryStream(Math.Max(1024, payload.Length * 4)))
            {
                byte[] buffer = new byte[8192];
                while (true)
                {
                    int read = bz.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;
                    dst.Write(buffer, 0, read);
                }
                return dst.ToArray();
            }
        }
    }
}
