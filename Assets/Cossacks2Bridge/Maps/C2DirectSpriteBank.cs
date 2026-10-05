using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using G16AnalyzerLib;
using BZip2InputStream = Cossacks2Bridge.UnityAdapters.Maps.InternalBZip2.BZip2InputStream;

namespace TemnyLessViewer
{
    /// <summary>
    /// Direct MD-viewer bank reader.
    /// MD path MUST NOT call старые анализаторы and MUST NOT create TGA-кэш/anchor-cache.
    /// G2D frames are rasterized from original mesh triangles/UV directly in memory.
    /// G16/GN16 frames are decoded from segments directly in memory.
    /// </summary>
    public sealed class C2DirectSpriteBank
    {
        public enum BankKind
        {
            None,
            G2D,
            G16
        }

        private BankKind _kind = BankKind.None;
        private string _path = "";
        private int _frameCount;
        private G2DImpl _g2d;
        private G16Impl _g16;

        public BankKind Kind => _kind;
        public string Path => _path;
        public int FrameCount => _frameCount;

        // sgG2D.cpp::GU2DPackage::Init supplies this to SpriteManager.
        // RenderFrame deliberately accepts a physical bank index (also used by
        // raw-bank viewers); the GP/ISM facade performs the logical conversion.
        public int DirectionCount => _g2d != null ? _g2d.DirectionCount : 1;

        public int UnswizzleFrameIndexLikeOriginal(int spriteId)
        {
            int directions = DirectionCount;
            return directions > 1
                ? spriteId / directions + (FrameCount / directions) * (spriteId % directions)
                : spriteId;
        }

        public void Clear()
        {
            _kind = BankKind.None;
            _path = "";
            _frameCount = 0;
            _g2d = null;
            _g16 = null;
        }

        public bool Load(string path, out string error)
        {
            Clear();
            error = "";

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                error = "bank file not found";
                return false;
            }

            byte[] file;
            try
            {
                file = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                error = "ReadAllBytes failed: " + ex.Message;
                return false;
            }

            if (file.Length < 4)
            {
                error = "file too small";
                return false;
            }

            uint magic = BitConverter.ToUInt32(file, 0);
            if (magic == 0x44325547u) // GU2D
            {
                G2DImpl impl = new G2DImpl();
                if (!impl.Load(file, out error))
                    return false;

                _kind = BankKind.G2D;
                _path = path;
                _frameCount = impl.FrameCount;
                _g2d = impl;
                return true;
            }

            G16Impl g16 = new G16Impl();
            if (g16.Load(file, out error))
            {
                _kind = BankKind.G16;
                _path = path;
                _frameCount = g16.FrameCount;
                _g16 = g16;
                return true;
            }

            return false;
        }

        public bool RenderFrame(int frameIndex, out C2RenderedFrame frame, out string error)
        {
            return RenderFrame(frameIndex, false, out frame, out error);
        }

        public bool RenderFrame(int frameIndex, bool mirrorX, out C2RenderedFrame frame, out string error)
        {
            frame = null;
            error = "";

            if (frameIndex < 0 || frameIndex >= _frameCount)
            {
                error = $"frame index out of range: {frameIndex}/{_frameCount}";
                return false;
            }

            if (_kind == BankKind.G2D && _g2d != null)
                return _g2d.RenderFrame(frameIndex, mirrorX, out frame, out error);

            if (_kind == BankKind.G16 && _g16 != null)
                return _g16.RenderFrame(frameIndex, mirrorX, out frame, out error);

            error = "bank is not loaded";
            return false;
        }

        public bool RenderFrameNationColor(
            int frameIndex, bool mirrorX, byte r, byte g, byte b,
            out C2RenderedFrame frame, out string error)
        {
            if (_kind == BankKind.G2D && _g2d != null)
                return _g2d.RenderFrame(frameIndex, mirrorX, true, r, g, b, out frame, out error);
            if (_kind == BankKind.G16 && _g16 != null)
                return _g16.RenderFrameNationColor(frameIndex, mirrorX, r, g, b, out frame, out error);
            return RenderFrame(frameIndex, mirrorX, out frame, out error);
        }

        // ================================================================
        // GU2D direct renderer: mesh + triangles + UV -> RGBA memory
        // ================================================================

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DGU2DHeaderCore
        {
            public uint m_Magic;
            public uint m_FileSize;
            public uint m_FramesNumber;
            public uint m_VerticesOffset;
            public uint m_VerticesNumber;
            public uint m_IndicesOffset;
            public uint m_IndicesNumber;
            public uint m_MeshesOffset;
            public uint m_MeshesNumber;
            public uint m_TexturesInfoOffset;
            public uint m_TexturesOffset;
            public uint m_TexturesNumber;
            public uint m_InfoLen;
            public ushort m_Width;
            public ushort m_Height;
            public byte m_Directions;
        }

        private struct DGU2DHeaderParsed
        {
            public DGU2DHeaderCore Core;
            public byte VertexFormat;
            public uint HeaderSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DGU2DFrameInfo
        {
            public uint m_MeshesOffset;
            public uint m_MeshesNumber;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DGU2DTextureInfo
        {
            public ushort m_Width;
            public ushort m_Height;
            public ushort m_Flags;
            public ushort m_Reserved;
            public uint m_TextureOffset;
            public uint m_TextureSize;
            public uint m_VerticesOffset;
            public uint m_VerticesNumber;
            public uint m_IndicesOffset;
            public uint m_IndicesNumber;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DGU2DMesh
        {
            public ushort m_TextureIdx;
            public uint m_VerticesOffset;
            public uint m_VerticesNumber;
            public uint m_TrianglesOffset;
            public uint m_TrianglesNumber;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DGU2DVertex
        {
            public short x, y, z, u, v;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DGU2DVertexC
        {
            public short x, y, z, u, v;
            public uint color;
        }

        private struct DDrawVertex
        {
            public float X, Y;
            public float U, V;
            public uint Color;
        }

        private struct PaletteColor
        {
            public byte R, G, B;
        }

        private sealed class G2DImpl
        {
            private byte[] _file;
            private DGU2DHeaderCore _hdr;
            private byte _vertexFormat;
            private uint _headerSize;
            private DGU2DFrameInfo[] _frames;
            private DGU2DTextureInfo[] _texInfos;
            private uint[][] _textures;
            private int[] _texW;
            private int[] _texH;
            private int _globalW;
            private int _globalH;
            private int _originX;
            private int _originY;
            private float _tx;
            private float _ty;

            public int FrameCount => _frames != null ? _frames.Length : 0;
            public int DirectionCount => Math.Max(1, (int)_hdr.m_Directions);

            public bool Load(byte[] file, out string error)
            {
                error = "";
                _file = file;

                DGU2DHeaderParsed hp;
                if (!ParseHeader(file, out hp, out error))
                    return false;

                _hdr = hp.Core;
                _vertexFormat = hp.VertexFormat;
                _headerSize = hp.HeaderSize;

                if (_hdr.m_FramesNumber > 200000u || _hdr.m_TexturesNumber > 200000u || _hdr.m_MeshesNumber > 500000u)
                {
                    error = "GU2D header values are insane";
                    return false;
                }

                int sizeFrame = Marshal.SizeOf(typeof(DGU2DFrameInfo));
                int sizeTex = Marshal.SizeOf(typeof(DGU2DTextureInfo));
                int sizeMesh = Marshal.SizeOf(typeof(DGU2DMesh));
                int sizeVertex = Marshal.SizeOf(typeof(DGU2DVertex));
                int sizeVertexC = Marshal.SizeOf(typeof(DGU2DVertexC));
                int vertexSize = ((_vertexFormat & 1) != 0) ? sizeVertexC : sizeVertex;

                if (!InRange(file, _headerSize, _hdr.m_FramesNumber * (uint)sizeFrame))
                {
                    error = "FrameInfo table is out of bounds";
                    return false;
                }

                if (!InRange(file, _hdr.m_TexturesInfoOffset, _hdr.m_TexturesNumber * (uint)sizeTex))
                {
                    error = "TexturesInfo table is out of bounds";
                    return false;
                }

                if (_hdr.m_TexturesOffset >= file.Length)
                {
                    error = "Textures blob offset is out of bounds";
                    return false;
                }

                _frames = new DGU2DFrameInfo[(int)_hdr.m_FramesNumber];
                for (uint i = 0; i < _hdr.m_FramesNumber; i++)
                    _frames[(int)i] = ReadStruct<DGU2DFrameInfo>(file, (int)(_headerSize + i * sizeFrame));

                _texInfos = new DGU2DTextureInfo[(int)_hdr.m_TexturesNumber];
                for (uint i = 0; i < _hdr.m_TexturesNumber; i++)
                    _texInfos[(int)i] = ReadStruct<DGU2DTextureInfo>(file, (int)(_hdr.m_TexturesInfoOffset + i * sizeTex));

                _textures = new uint[(int)_hdr.m_TexturesNumber][];
                _texW = new int[(int)_hdr.m_TexturesNumber];
                _texH = new int[(int)_hdr.m_TexturesNumber];

                uint texBlobOffset = _hdr.m_TexturesOffset;
                uint texBlobSize = (uint)(file.Length - _hdr.m_TexturesOffset);
                for (uint ti = 0; ti < _hdr.m_TexturesNumber; ti++)
                {
                    uint[] decoded;
                    string terr;
                    if (DecodeTextureARGB4444(file, texBlobOffset, texBlobSize, _texInfos[(int)ti], out decoded, out terr))
                    {
                        _textures[(int)ti] = decoded;
                        _texW[(int)ti] = _texInfos[(int)ti].m_Width;
                        _texH[(int)ti] = _texInfos[(int)ti].m_Height;
                    }
                    else
                    {
                        _textures[(int)ti] = Array.Empty<uint>();
                    }
                }

                // Global bounds are exactly the old analyzer's meta origin, but kept in memory.
                float minX = float.MaxValue, minY = float.MaxValue;
                float maxX = float.MinValue, maxY = float.MinValue;

                for (uint fi = 0; fi < _hdr.m_FramesNumber; fi++)
                {
                    DGU2DFrameInfo fr = _frames[(int)fi];
                    ulong meshesOff = (ulong)_hdr.m_MeshesOffset + (ulong)fr.m_MeshesOffset * (ulong)sizeMesh;
                    ulong meshesBytes = (ulong)fr.m_MeshesNumber * (ulong)sizeMesh;
                    if (!InRangeUlong(file, meshesOff, meshesBytes)) continue;

                    for (uint mi = 0; mi < fr.m_MeshesNumber; mi++)
                    {
                        DGU2DMesh m = ReadStruct<DGU2DMesh>(file, (int)(meshesOff + (ulong)mi * (ulong)sizeMesh));
                        ulong voff = (ulong)_hdr.m_VerticesOffset + (ulong)m.m_VerticesOffset * (ulong)vertexSize;
                        ulong vbytes = (ulong)m.m_VerticesNumber * (ulong)vertexSize;
                        if (!InRangeUlong(file, voff, vbytes)) continue;

                        for (uint k = 0; k < m.m_VerticesNumber; k++)
                        {
                            int vertexOffset = (int)(voff + (ulong)k * (ulong)vertexSize);
                            float vx, vy;
                            if ((_vertexFormat & 1) != 0)
                            {
                                DGU2DVertexC v = ReadStruct<DGU2DVertexC>(file, vertexOffset);
                                vx = v.x; vy = v.y;
                            }
                            else
                            {
                                DGU2DVertex v = ReadStruct<DGU2DVertex>(file, vertexOffset);
                                vx = v.x; vy = v.y;
                            }

                            if (vx < minX) minX = vx;
                            if (vy < minY) minY = vy;
                            if (vx > maxX) maxX = vx;
                            if (vy > maxY) maxY = vy;
                        }
                    }
                }

                if (minX > maxX || minY > maxY)
                {
                    error = "No vertex data in G2D";
                    return false;
                }

                _globalW = Math.Max(1, (int)Math.Ceiling(maxX - minX + 2.0f));
                _globalH = Math.Max(1, (int)Math.Ceiling(maxY - minY + 2.0f));
                _tx = -minX + 1.0f;
                _ty = -minY + 1.0f;
                _originX = (int)Math.Round(_tx);
                _originY = (int)Math.Round(_ty);
                return true;
            }

            public bool RenderFrame(int frameIndex, out C2RenderedFrame frame, out string error)
            {
                return RenderFrame(frameIndex, false, out frame, out error);
            }

            public bool RenderFrame(int frameIndex, bool mirrorX, out C2RenderedFrame frame, out string error)
            {
                return RenderFrame(frameIndex, mirrorX, false, 0, 0, 0, out frame, out error);
            }

            public bool RenderFrame(
                int frameIndex, bool mirrorX, bool useNationColor,
                byte nationR, byte nationG, byte nationB,
                out C2RenderedFrame frame, out string error)
            {
                frame = null;
                error = "";

                if (frameIndex < 0 || frameIndex >= FrameCount)
                {
                    error = "G2D frame index out of range";
                    return false;
                }

                int sizeMesh = Marshal.SizeOf(typeof(DGU2DMesh));
                int sizeVertex = Marshal.SizeOf(typeof(DGU2DVertex));
                int sizeVertexC = Marshal.SizeOf(typeof(DGU2DVertexC));
                int vertexSize = ((_vertexFormat & 1) != 0) ? sizeVertexC : sizeVertex;

                DGU2DFrameInfo fr = _frames[frameIndex];
                ulong meshesOff = (ulong)_hdr.m_MeshesOffset + (ulong)fr.m_MeshesOffset * (ulong)sizeMesh;
                ulong meshesBytes = (ulong)fr.m_MeshesNumber * (ulong)sizeMesh;
                if (!InRangeUlong(_file, meshesOff, meshesBytes))
                {
                    error = "G2D frame mesh table is out of bounds";
                    return false;
                }

                // Original ISM mirror is NOT "flip already rasterized bitmap".
                // GP_System::ShowGP passes sprID>=4095 to ISM::DrawSprite, which flips
                // the local G2D geometry around the draw transform origin.
                //
                // Therefore for the viewer we must:
                //   1) fold direction into stored block;
                //   2) request the same sprite with mirror flag;
                //   3) negate local vertex X before rasterization;
                //   4) render with a per-frame origin, so anchor math remains:
                //        screen = unit + NF.dx/dy + localVertex
                //
                // V6 mirrored the already rasterized global bitmap. That moved the
                // frame away from the unit origin on mirrored directions.
                float minX = float.MaxValue, minY = float.MaxValue;
                float maxX = float.MinValue, maxY = float.MinValue;
                bool anyVertex = false;

                for (uint mi = 0; mi < fr.m_MeshesNumber; mi++)
                {
                    DGU2DMesh m = ReadStruct<DGU2DMesh>(_file, (int)(meshesOff + (ulong)mi * (ulong)sizeMesh));

                    ulong voff = (ulong)_hdr.m_VerticesOffset + (ulong)m.m_VerticesOffset * (ulong)vertexSize;
                    ulong vbytes = (ulong)m.m_VerticesNumber * (ulong)vertexSize;
                    if (!InRangeUlong(_file, voff, vbytes))
                        continue;

                    for (uint k = 0; k < m.m_VerticesNumber; k++)
                    {
                        int vertexOffset = (int)(voff + (ulong)k * (ulong)vertexSize);
                        float vx, vy;
                        if ((_vertexFormat & 1) != 0)
                        {
                            DGU2DVertexC v = ReadStruct<DGU2DVertexC>(_file, vertexOffset);
                            vx = v.x;
                            vy = v.y;
                        }
                        else
                        {
                            DGU2DVertex v = ReadStruct<DGU2DVertex>(_file, vertexOffset);
                            vx = v.x;
                            vy = v.y;
                        }

                        if (mirrorX) vx = -vx;

                        if (vx < minX) minX = vx;
                        if (vy < minY) minY = vy;
                        if (vx > maxX) maxX = vx;
                        if (vy > maxY) maxY = vy;
                        anyVertex = true;
                    }
                }

                if (!anyVertex || minX > maxX || minY > maxY)
                {
                    error = "No vertex data in G2D frame";
                    return false;
                }

                int frameW = Math.Max(1, (int)Math.Ceiling(maxX - minX + 2.0f));
                int frameH = Math.Max(1, (int)Math.Ceiling(maxY - minY + 2.0f));
                float tx = -minX + 1.0f;
                float ty = -minY + 1.0f;
                int originX = (int)Math.Round(tx);
                int originY = (int)Math.Round(ty);

                uint[] canvas = new uint[frameW * frameH];

                for (uint mi = 0; mi < fr.m_MeshesNumber; mi++)
                {
                    DGU2DMesh m = ReadStruct<DGU2DMesh>(_file, (int)(meshesOff + (ulong)mi * (ulong)sizeMesh));

                    if (m.m_TextureIdx >= _textures.Length || _textures[(int)m.m_TextureIdx].Length == 0)
                        continue;

                    int tW = _texW[(int)m.m_TextureIdx];
                    int tH = _texH[(int)m.m_TextureIdx];
                    uint[] tex = _textures[(int)m.m_TextureIdx];

                    ulong voff = (ulong)_hdr.m_VerticesOffset + (ulong)m.m_VerticesOffset * (ulong)vertexSize;
                    ulong vbytes = (ulong)m.m_VerticesNumber * (ulong)vertexSize;
                    ulong ioff = (ulong)_hdr.m_IndicesOffset + (ulong)m.m_TrianglesOffset * sizeof(ushort);
                    ulong ibytes = (ulong)m.m_TrianglesNumber * 3UL * sizeof(ushort);

                    if (!InRangeUlong(_file, voff, vbytes) || !InRangeUlong(_file, ioff, ibytes))
                        continue;

                    DDrawVertex[] verts = new DDrawVertex[(int)m.m_VerticesNumber];
                    if ((_vertexFormat & 1) != 0)
                    {
                        for (uint k = 0; k < m.m_VerticesNumber; k++)
                        {
                            DGU2DVertexC v = ReadStruct<DGU2DVertexC>(_file, (int)(voff + (ulong)k * (ulong)sizeVertexC));
                            float vx = v.x;
                            if (mirrorX) vx = -vx;

                            verts[(int)k].X = vx + tx;
                            verts[(int)k].Y = v.y + ty;
                            verts[(int)k].U = v.u;
                            verts[(int)k].V = v.v;
                            verts[(int)k].Color = v.color;
                        }
                    }
                    else
                    {
                        for (uint k = 0; k < m.m_VerticesNumber; k++)
                        {
                            DGU2DVertex v = ReadStruct<DGU2DVertex>(_file, (int)(voff + (ulong)k * (ulong)sizeVertex));
                            float vx = v.x;
                            if (mirrorX) vx = -vx;

                            verts[(int)k].X = vx + tx;
                            verts[(int)k].Y = v.y + ty;
                            verts[(int)k].U = v.u;
                            verts[(int)k].V = v.v;
                            verts[(int)k].Color = 0xFFFFFFFFu;
                        }
                    }

                    for (uint t = 0; t < m.m_TrianglesNumber; t++)
                    {
                        int idxOffset = (int)(ioff + (ulong)t * 3UL * sizeof(ushort));
                        ushort i0 = BitConverter.ToUInt16(_file, idxOffset + 0);
                        ushort i1 = BitConverter.ToUInt16(_file, idxOffset + 2);
                        ushort i2 = BitConverter.ToUInt16(_file, idxOffset + 4);
                        if (i0 >= verts.Length || i1 >= verts.Length || i2 >= verts.Length) continue;

                        RasterizeTriangle(
                            verts[(int)i0], verts[(int)i1], verts[(int)i2],
                            tex, tW, tH, canvas, frameW, frameH,
                            useNationColor, nationR, nationG, nationB);
                    }
                }

                byte[] rgba = new byte[frameW * frameH * 4];
                for (int i = 0; i < frameW * frameH; i++)
                {
                    uint c = canvas[i];
                    byte r = (byte)(c & 0xFF);
                    byte g = (byte)((c >> 8) & 0xFF);
                    byte b = (byte)((c >> 16) & 0xFF);
                    byte a = (byte)((c >> 24) & 0xFF);
                    if (a == 0)
                    {
                        r = 0;
                        g = 0;
                        b = 0;
                    }
                    else if (a < 255)
                    {
                        r = UnpremultiplyChannelToStraightAlpha(r, a);
                        g = UnpremultiplyChannelToStraightAlpha(g, a);
                        b = UnpremultiplyChannelToStraightAlpha(b, a);
                    }

                    rgba[i * 4 + 0] = r;
                    rgba[i * 4 + 1] = g;
                    rgba[i * 4 + 2] = b;
                    rgba[i * 4 + 3] = a;
                }

                frame = new C2RenderedFrame
                {
                    Width = frameW,
                    Height = frameH,
                    OriginX = originX,
                    OriginY = originY,
                    Rgba = rgba
                };
                return true;
            }

            private static bool ParseHeader(byte[] file, out DGU2DHeaderParsed hdr, out string error)
            {
                error = "";
                hdr = new DGU2DHeaderParsed();

                const uint kFixedDwordsBytes = 13u * 4u;
                if (file.Length < kFixedDwordsBytes + 5u)
                {
                    error = "File too small for GU2D header";
                    return false;
                }

                hdr.Core = ReadStruct<DGU2DHeaderCore>(file, 0);
                if (hdr.Core.m_Magic != 0x44325547u)
                {
                    error = "Invalid Magic (Not GU2D)";
                    return false;
                }

                uint tail = 13u * 4u;
                hdr.Core.m_Width = BitConverter.ToUInt16(file, (int)(tail + 0));
                hdr.Core.m_Height = BitConverter.ToUInt16(file, (int)(tail + 2));

                // layout 1: byte directions + optional byte vertex format
                {
                    uint dir = file[(int)(tail + 4)];
                    byte vfmt = 0;
                    uint hsz = tail + 5;
                    if (hdr.Core.m_InfoLen > 5 && file.Length > tail + 5)
                    {
                        vfmt = file[(int)(tail + 5)];
                        hsz = tail + 6;
                    }

                    byte outDir, outFmt;
                    uint outHsz;
                    if (TryLayout(file, hdr.Core, hsz, dir, vfmt, out outDir, out outFmt, out outHsz))
                    {
                        hdr.Core.m_Directions = outDir;
                        hdr.VertexFormat = outFmt;
                        hdr.HeaderSize = outHsz;
                        return true;
                    }
                }

                // layout 2: ushort directions + optional ushort vertex format
                {
                    if (file.Length >= tail + 8)
                    {
                        uint dir = BitConverter.ToUInt16(file, (int)(tail + 4));
                        ushort vfmt16 = 0;
                        uint hsz = tail + 6;
                        if (hdr.Core.m_InfoLen > 5)
                        {
                            vfmt16 = BitConverter.ToUInt16(file, (int)(tail + 6));
                            hsz = tail + 8;
                        }

                        byte outDir, outFmt;
                        uint outHsz;
                        if (TryLayout(file, hdr.Core, hsz, dir, (byte)(vfmt16 & 0xFF), out outDir, out outFmt, out outHsz))
                        {
                            hdr.Core.m_Directions = outDir;
                            hdr.VertexFormat = outFmt;
                            hdr.HeaderSize = outHsz;
                            return true;
                        }
                    }
                }

                error = "Cannot determine GU2D header tail layout";
                return false;
            }

            private static bool TryLayout(byte[] file, DGU2DHeaderCore core, uint headerSz, uint directions, byte vfmt,
                out byte outDirections, out byte outVertexFormat, out uint outHeaderSize)
            {
                outDirections = 0;
                outVertexFormat = 0;
                outHeaderSize = 0;

                if (headerSz > file.Length) return false;

                int sizeOfFrameInfo = Marshal.SizeOf(typeof(DGU2DFrameInfo));
                int sizeOfMesh = Marshal.SizeOf(typeof(DGU2DMesh));

                ulong framesBytes = (ulong)core.m_FramesNumber * (ulong)sizeOfFrameInfo;
                if ((ulong)headerSz + framesBytes > (ulong)file.Length) return false;
                if (core.m_MeshesOffset >= file.Length) return false;

                DGU2DFrameInfo f0 = ReadStruct<DGU2DFrameInfo>(file, (int)headerSz);
                if (f0.m_MeshesNumber > core.m_MeshesNumber) return false;

                ulong meshOff = (ulong)core.m_MeshesOffset + (ulong)f0.m_MeshesOffset * (ulong)sizeOfMesh;
                ulong meshBytes = (ulong)f0.m_MeshesNumber * (ulong)sizeOfMesh;
                if (meshOff + meshBytes > (ulong)file.Length) return false;
                if (directions == 0 || directions > 1024) return false;

                outDirections = (byte)Math.Min(directions, 255u);
                outVertexFormat = vfmt;
                outHeaderSize = headerSz;
                return true;
            }

            private static bool DecodeTextureARGB4444(byte[] fileData, uint texBlobOffset, uint blobSize,
                DGU2DTextureInfo ti, out uint[] outRGBA, out string outErr)
            {
                outErr = "";
                outRGBA = Array.Empty<uint>();

                uint W = ti.m_Width;
                uint H = ti.m_Height;
                if (W == 0 || H == 0)
                {
                    outErr = "texture has zero size";
                    return false;
                }

                if (ti.m_TextureOffset >= blobSize || ti.m_TextureSize == 0 ||
                    (ulong)ti.m_TextureOffset + (ulong)ti.m_TextureSize > (ulong)blobSize)
                {
                    outErr = "texture blob range is out of bounds";
                    return false;
                }

                uint inTexOffset = texBlobOffset + ti.m_TextureOffset;
                uint compressMode = (uint)(ti.m_Flags & 0xFFu);
                uint packMode = (uint)(ti.m_Flags >> 8);

                byte[] textData;
                uint textLen;
                if (compressMode != 0u)
                {
                    byte[] compressedData = new byte[(int)ti.m_TextureSize];
                    Array.Copy(fileData, (long)inTexOffset, compressedData, 0L, (long)ti.m_TextureSize);

                    if (!TryDecompressG2DBlockLikeOriginal(compressedData, ti.m_TextureSize, out textData, out string decompressErr))
                    {
                        outErr = decompressErr;
                        return false;
                    }
                    textLen = (uint)textData.Length;
                }
                else
                {
                    textData = new byte[(int)ti.m_TextureSize];
                    Array.Copy(fileData, (long)inTexOffset, textData, 0L, (long)ti.m_TextureSize);
                    textLen = ti.m_TextureSize;
                }

                uint totalPixels = W * H;
                ushort[] argb4444 = new ushort[(int)totalPixels];

                if (packMode == 0)
                {
                    uint expected = totalPixels * 2u;
                    if (textLen < expected)
                    {
                        outErr = "raw ARGB4444 texture too small";
                        return false;
                    }

                    for (uint i = 0; i < totalPixels; i++)
                        argb4444[(int)i] = BitConverter.ToUInt16(textData, (int)(i * 2));
                }
                else
                {
                    uint pos = 0;
                    if (textLen < 1) { outErr = "indexed texture missing palette size"; return false; }
                    uint colors = (uint)textData[(int)pos++] + 1u;
                    uint palBytes = colors * 3u;
                    if (textLen < pos + palBytes) { outErr = "indexed palette truncated"; return false; }

                    byte[] pal = new byte[(int)palBytes];
                    Array.Copy(textData, (long)pos, pal, 0L, (long)palBytes);
                    pos += palBytes;

                    uint colorDataOffset = pos;
                    uint remain = textLen - pos;
                    uint outPos = 0;

                    if (packMode == 1)
                    {
                        uint pixBytes = totalPixels;
                        uint alphaBytes = (totalPixels + 1u) / 2u;
                        if (remain < pixBytes + alphaBytes)
                        {
                            outErr = "indexed mode 1 truncated";
                            return false;
                        }

                        uint alphaDataOffset = colorDataOffset + pixBytes;
                        uint pairs = totalPixels / 2u;

                        for (uint i = 0; i < pairs; i++)
                        {
                            PaletteColor pc1 = GetPaletteColor(pal, textData[(int)(colorDataOffset + i * 2 + 0)]);
                            PaletteColor pc2 = GetPaletteColor(pal, textData[(int)(colorDataOffset + i * 2 + 1)]);

                            byte aa = textData[(int)(alphaDataOffset + i)];
                            byte a1 = (byte)(aa & 0xF0);
                            byte a2 = (byte)((aa << 4) & 0xF0);

                            ushort c1 = (ushort)(((a1 << 8) & 0xF000) | ((pc1.R << 4) & 0x0F00) | (pc1.G & 0x00F0) | ((pc1.B >> 4) & 0x000F));
                            ushort c2 = (ushort)(((a2 << 8) & 0xF000) | ((pc2.R << 4) & 0x0F00) | (pc2.G & 0x00F0) | ((pc2.B >> 4) & 0x000F));

                            argb4444[(int)outPos++] = c1;
                            argb4444[(int)outPos++] = c2;
                        }

                        if ((totalPixels & 1u) != 0)
                        {
                            uint last = totalPixels - 1u;
                            PaletteColor pc = GetPaletteColor(pal, textData[(int)(colorDataOffset + last)]);
                            byte aa = textData[(int)(alphaDataOffset + pairs)];
                            byte a1 = (byte)(aa & 0xF0);
                            ushort c = (ushort)(((a1 << 8) & 0xF000) | ((pc.R << 4) & 0x0F00) | (pc.G & 0x00F0) | ((pc.B >> 4) & 0x000F));
                            argb4444[(int)outPos++] = c;
                        }
                    }
                    else if (packMode == 2)
                    {
                        uint inPos = 0;
                        uint outPixels = 0;
                        uint nLen = 0;

                        while (outPixels < totalPixels)
                        {
                            if (inPos >= remain) { outErr = "indexed mode 2 truncated"; return false; }

                            byte idx = textData[(int)(colorDataOffset + inPos++)];
                            byte a = (byte)((idx << 5) & 0xFF);
                            idx >>= 3;

                            if (a == 0 && idx != 0)
                            {
                                nLen = idx;
                                continue;
                            }

                            PaletteColor pc = GetPaletteColor(pal, idx);
                            if (nLen != 0)
                            {
                                a |= 0x10;
                                nLen--;
                            }

                            ushort c = (ushort)(((a << 8) & 0xF000) | ((pc.R << 4) & 0x0F00) | (pc.G & 0x00F0) | ((pc.B >> 4) & 0x000F));
                            argb4444[(int)outPos++] = c;
                            outPixels++;
                        }
                    }
                    else
                    {
                        outErr = "unsupported texture pack mode";
                        return false;
                    }
                }

                outRGBA = new uint[(int)totalPixels];
                for (uint i = 0; i < totalPixels; i++)
                {
                    ushort c = argb4444[(int)i];
                    byte a = (byte)((c >> 12) & 0xF);
                    byte r = (byte)((c >> 8) & 0xF);
                    byte g = (byte)((c >> 4) & 0xF);
                    byte b = (byte)((c >> 0) & 0xF);

                    a = (byte)((a << 4) | a);
                    r = (byte)((r << 4) | r);
                    g = (byte)((g << 4) | g);
                    b = (byte)((b << 4) | b);

                    outRGBA[(int)i] = PackRGBA(r, g, b, a);
                }
                return true;
            }

            private static PaletteColor GetPaletteColor(byte[] pal, uint idx)
            {
                uint p = idx * 3u;
                PaletteColor result = new PaletteColor();
                if (p + 2u >= pal.Length)
                {
                    result.R = result.G = result.B = 0;
                }
                else
                {
                    result.R = pal[(int)(p + 0)];
                    result.G = pal[(int)(p + 1)];
                    result.B = pal[(int)(p + 2)];
                }
                return result;
            }

            private static float EdgeFunction(float ax, float ay, float bx, float by, float px, float py)
            {
                return (px - ax) * (by - ay) - (py - ay) * (bx - ax);
            }

            private static void RasterizeTriangle(DDrawVertex A, DDrawVertex B, DDrawVertex C,
                uint[] tex, int texW, int texH, uint[] canvas, int W, int H,
                bool useNationColor, byte nationR, byte nationG, byte nationB)
            {
                float minXf = (float)Math.Floor(Math.Min(Math.Min(A.X, B.X), C.X));
                float maxXf = (float)Math.Ceiling(Math.Max(Math.Max(A.X, B.X), C.X));
                float minYf = (float)Math.Floor(Math.Min(Math.Min(A.Y, B.Y), C.Y));
                float maxYf = (float)Math.Ceiling(Math.Max(Math.Max(A.Y, B.Y), C.Y));

                int minX = Math.Max(0, (int)minXf);
                int maxX = Math.Min(W - 1, (int)maxXf);
                int minY = Math.Max(0, (int)minYf);
                int maxY = Math.Min(H - 1, (int)maxYf);

                float area = EdgeFunction(A.X, A.Y, B.X, B.Y, C.X, C.Y);
                if (Math.Abs(area) < 1e-6f) return;
                float invArea = 1.0f / area;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float px = x + 0.5f;
                        float py = y + 0.5f;

                        float w0 = EdgeFunction(B.X, B.Y, C.X, C.Y, px, py);
                        float w1 = EdgeFunction(C.X, C.Y, A.X, A.Y, px, py);
                        float w2 = EdgeFunction(A.X, A.Y, B.X, B.Y, px, py);

                        if ((w0 >= 0 && w1 >= 0 && w2 >= 0) || (w0 <= 0 && w1 <= 0 && w2 <= 0))
                        {
                            w0 *= invArea;
                            w1 *= invArea;
                            w2 *= invArea;

                            float u = A.U * w0 + B.U * w1 + C.U * w2;
                            float v = A.V * w0 + B.V * w1 + C.V * w2;

                            int tu = (int)Math.Floor(u + 0.5f);
                            int tv = (int)Math.Floor(v + 0.5f);
                            if (tu < 0) tu = 0;
                            if (tv < 0) tv = 0;
                            if (tu >= texW) tu = texW - 1;
                            if (tv >= texH) tv = texH - 1;

                            uint src = tex[tv * texW + tu];
                            if (useNationColor)
                                ApplyNationColorToPackedArgb4444LikeOriginal(
                                    ref src, nationR, nationG, nationB);

                            uint c0 = A.Color;
                            uint c1 = B.Color;
                            uint c2 = C.Color;
                            float ca = ((c0 >> 24) & 0xFF) * w0 + ((c1 >> 24) & 0xFF) * w1 + ((c2 >> 24) & 0xFF) * w2;
                            float cr = ((c0 >> 16) & 0xFF) * w0 + ((c1 >> 16) & 0xFF) * w1 + ((c2 >> 16) & 0xFF) * w2;
                            float cg = ((c0 >> 8) & 0xFF) * w0 + ((c1 >> 8) & 0xFF) * w1 + ((c2 >> 8) & 0xFF) * w2;
                            float cb = (c0 & 0xFF) * w0 + (c1 & 0xFF) * w1 + (c2 & 0xFF) * w2;

                            uint vc = ((uint)(byte)ca << 24) |
                                      ((uint)(byte)cr << 16) |
                                      ((uint)(byte)cg << 8) |
                                      (uint)(byte)cb;

                            ApplyVertexColor(ref src, vc);
                            BlendOver(src, ref canvas[y * W + x]);
                        }
                    }
                }
            }

            private static void ApplyNationColorToPackedArgb4444LikeOriginal(
                ref uint rgba, byte nationR, byte nationG, byte nationB)
            {
                // G16PaintNationColor used by COSSACKS2/sgG2D.cpp: the low bit
                // of the ARGB4444 alpha nibble marks a nation-colour texel; the
                // remaining alpha bits are the strength added to its base RGB.
                int alphaNibble = (int)((rgba >> 28) & 15u);
                if ((alphaNibble & 1) == 0) return;
                // The MMX original clears the marker bit but keeps its weight:
                // (alpha & 0xE), then multiplies the nation high nibble by it.
                int strength = alphaNibble & 14;
                int rr = (int)(rgba & 255u) >> 4;
                int gg = (int)((rgba >> 8) & 255u) >> 4;
                int bb = (int)((rgba >> 16) & 255u) >> 4;
                rr = Math.Min(15, rr + ((strength * (nationR >> 4)) >> 4));
                gg = Math.Min(15, gg + ((strength * (nationG >> 4)) >> 4));
                bb = Math.Min(15, bb + ((strength * (nationB >> 4)) >> 4));
                rgba = PackRGBA((byte)(rr * 17), (byte)(gg * 17), (byte)(bb * 17), 255);
            }
        }

        private static MethodInfo s_g2dDecompressBlockMethod;
        private static MethodInfo s_g2dLastDecodeErrorMethod;
        private static string s_g2dReflectionError;

        private static bool TryDecompressG2DBlockLikeOriginal(byte[] compressedData, uint compressedSize, out byte[] textData, out string error)
        {
            textData = Array.Empty<byte>();
            error = "";

            if (!EnsureG2DCodecReflectionLikeOriginal(out error))
                return false;

            try
            {
                object[] args = { compressedData, compressedSize, null };
                object result = s_g2dDecompressBlockMethod.Invoke(null, args);
                bool ok = result is bool b && b;
                textData = args[2] as byte[] ?? Array.Empty<byte>();
                if (!ok)
                {
                    error = GetG2DCodecLastDecodeErrorLikeOriginal();
                    if (string.IsNullOrEmpty(error))
                        error = "G2DCodec.DecompressBlock returned false";
                }
                return ok;
            }
            catch (TargetInvocationException ex)
            {
                error = "G2DCodec.DecompressBlock invocation failed: " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                error = "G2DCodec.DecompressBlock reflection failed: " + ex.Message;
                return false;
            }
        }

        private static bool EnsureG2DCodecReflectionLikeOriginal(out string error)
        {
            error = "";
            if (s_g2dDecompressBlockMethod != null)
                return true;
            if (!string.IsNullOrEmpty(s_g2dReflectionError))
            {
                error = s_g2dReflectionError;
                return false;
            }

            try
            {
                Type codecType = Type.GetType("G2DLib.G2DCodec, Melinoja", false);
                if (codecType == null)
                {
                    s_g2dReflectionError = "G2DLib.G2DCodec type not found in Melinoja.dll";
                    error = s_g2dReflectionError;
                    return false;
                }

                s_g2dDecompressBlockMethod = codecType.GetMethod(
                    "DecompressBlock",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(byte[]), typeof(uint), typeof(byte[]).MakeByRefType() },
                    null);
                s_g2dLastDecodeErrorMethod = codecType.GetMethod("G2DGetLastDecodeError", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);

                if (s_g2dDecompressBlockMethod == null)
                {
                    s_g2dReflectionError = "G2DLib.G2DCodec.DecompressBlock(byte[], uint, out byte[]) not found";
                    error = s_g2dReflectionError;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                s_g2dReflectionError = "G2DLib.G2DCodec reflection setup failed: " + ex.Message;
                error = s_g2dReflectionError;
                return false;
            }
        }

        private static string GetG2DCodecLastDecodeErrorLikeOriginal()
        {
            if (s_g2dLastDecodeErrorMethod == null)
                return "";

            try
            {
                return s_g2dLastDecodeErrorMethod.Invoke(null, null) as string ?? "";
            }
            catch
            {
                return "";
            }
        }

        // ================================================================
        // GU16/GN16 direct renderer: decode segment -> chunks -> RGBA memory
        // ================================================================

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DGU16Header
        {
            public uint m_Magic;
            public uint m_BlockSize;
            public byte m_NFramesPerSegment;
            public ushort m_NSprites;
            public ushort m_XSize;
            public ushort m_YSize;
            public uint m_MaxWorkbuf;
            public ushort m_NPackSegments;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DGU16SegHdr
        {
            public uint data;
            public uint GetOffsetBytes() => data >> 4;
            public uint GetPackFlags() => data & 0xF;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DGU16SpriteHdr
        {
            public ushort m_NChunks;
            public ushort GetNChunks() => m_NChunks;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DGN16Header
        {
            public uint m_Magic;
            public uint m_BlockSize;
            public ushort m_NSprites;
            public uint m_MaxWorkbuf;
            public ushort m_NPackSegments;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DGN16SpriteHdr
        {
            public ushort m_NChunks;
            public ushort m_Width;
            public ushort m_Height;
            public ushort m_SegmentIdx;
            public ushort GetNChunks() => m_NChunks;
            public ushort GetW() => m_Width;
            public ushort GetH() => m_Height;
        }

        private struct DGN16PackSegHdr
        {
            public uint data0;
            public ushort nFrames;
            public uint GetOffsetBytes() => data0 >> 4;
            public uint GetPackFlags() => data0 & 0xF;
            public ushort GetNFrames() => nFrames;
        }

        private sealed class DecodedSegment
        {
            public uint FramesInSeg;
            public uint BaseFrameId;
            public byte[] OutData;
            public readonly List<uint> FrameOffs = new List<uint>();
            public readonly List<ushort> NumSquares = new List<ushort>();
        }

        private sealed class G16Impl
        {
            private bool _isGN16;
            private int _w;
            private int _h;
            private readonly List<ushort> _gnW = new List<ushort>();
            private readonly List<ushort> _gnH = new List<ushort>();
            private readonly List<ushort> _gnSquares = new List<ushort>();
            private readonly List<DecodedSegment> _segments = new List<DecodedSegment>();

            public int FrameCount { get; private set; }

            public bool Load(byte[] file, out string error)
            {
                error = "";
                _segments.Clear();
                _gnW.Clear();
                _gnH.Clear();
                _gnSquares.Clear();
                FrameCount = 0;

                int blockOffset, blockSize;
                uint blockMagic;
                if (!FindG16Block(file, out blockOffset, out blockSize, out blockMagic, out error))
                    return false;

                uint[] pal = new uint[256];
                TryLoadEmbeddedGPAL(file, pal);

                if (blockMagic == 0x36315547u) // GU16
                    return LoadGU16(file, blockOffset, blockSize, out error);

                if (blockMagic == 0x36314E47u) // GN16
                    return LoadGN16(file, blockOffset, blockSize, out error);

                error = "Invalid G16 magic";
                return false;
            }

            public bool RenderFrame(int frameIndex, out C2RenderedFrame frame, out string error)
            {
                return RenderFrame(frameIndex, false, out frame, out error);
            }

            public bool RenderFrame(int frameIndex, bool mirrorX, out C2RenderedFrame frame, out string error)
            {
                return RenderFrameInternal(frameIndex, mirrorX, false, 0, 0, 0, out frame, out error);
            }

            public bool RenderFrameNationColor(
                int frameIndex, bool mirrorX, byte nationR, byte nationG, byte nationB,
                out C2RenderedFrame frame, out string error)
            {
                return RenderFrameInternal(
                    frameIndex, mirrorX, true, nationR, nationG, nationB,
                    out frame, out error);
            }

            private bool RenderFrameInternal(
                int frameIndex, bool mirrorX, bool useNationColor,
                byte nationR, byte nationG, byte nationB,
                out C2RenderedFrame frame, out string error)
            {
                frame = null;
                error = "";

                if (frameIndex < 0 || frameIndex >= FrameCount)
                {
                    error = "G16 frame index out of range";
                    return false;
                }

                DecodedSegment seg = null;
                int local = -1;
                foreach (DecodedSegment s in _segments)
                {
                    if (frameIndex >= s.BaseFrameId && frameIndex < s.BaseFrameId + s.FramesInSeg)
                    {
                        seg = s;
                        local = frameIndex - (int)s.BaseFrameId;
                        break;
                    }
                }

                if (seg == null || local < 0 || local >= seg.FrameOffs.Count)
                {
                    error = "G16 decoded segment not found for frame";
                    return false;
                }

                int W = _w;
                int H = _h;
                ushort nsq = 0;

                if (_isGN16)
                {
                    if (frameIndex >= _gnW.Count || frameIndex >= _gnH.Count)
                    {
                        error = "GN16 sprite dimensions missing";
                        return false;
                    }
                    W = _gnW[frameIndex];
                    H = _gnH[frameIndex];
                    nsq = _gnSquares[frameIndex];
                }
                else
                {
                    if (local < seg.NumSquares.Count)
                        nsq = seg.NumSquares[local];
                }

                byte[] rgba;
                if (!BuildFrameRGBA32(
                        seg.OutData, seg.FrameOffs[local], nsq, W, H,
                        useNationColor, nationR, nationG, nationB,
                        out rgba, out error))
                    return false;

                if (mirrorX)
                    MirrorRgbaInPlace(rgba, W, H);

                frame = new C2RenderedFrame
                {
                    Width = W,
                    Height = H,
                    OriginX = 0,
                    OriginY = 0,
                    Rgba = rgba
                };
                return true;
            }

            private static void MirrorRgbaInPlace(byte[] rgba, int w, int h)
            {
                if (rgba == null || w <= 1 || h <= 0) return;

                for (int y = 0; y < h; y++)
                {
                    int row = y * w * 4;
                    for (int x = 0; x < w / 2; x++)
                    {
                        int a = row + x * 4;
                        int b = row + (w - 1 - x) * 4;

                        byte r0 = rgba[a + 0];
                        byte g0 = rgba[a + 1];
                        byte b0 = rgba[a + 2];
                        byte a0 = rgba[a + 3];

                        rgba[a + 0] = rgba[b + 0];
                        rgba[a + 1] = rgba[b + 1];
                        rgba[a + 2] = rgba[b + 2];
                        rgba[a + 3] = rgba[b + 3];

                        rgba[b + 0] = r0;
                        rgba[b + 1] = g0;
                        rgba[b + 2] = b0;
                        rgba[b + 3] = a0;
                    }
                }
            }

            private bool LoadGU16(byte[] file, int blockOffset, int blockSize, out string error)
            {
                error = "";
                DGU16Header hdr = ReadStruct<DGU16Header>(file, blockOffset);
                _isGN16 = false;
                _w = hdr.m_XSize;
                _h = hdr.m_YSize;
                FrameCount = hdr.m_NSprites;

                int segsOffset = blockOffset + Marshal.SizeOf(typeof(DGU16Header));
                int segSize = Marshal.SizeOf(typeof(DGU16SegHdr));
                int segTableBytes = Marshal.SizeOf(typeof(DGU16Header)) + hdr.m_NPackSegments * segSize;
                int spritesOffset = blockOffset + segTableBytes;
                int spriteSize = Marshal.SizeOf(typeof(DGU16SpriteHdr));

                uint workNeed = Math.Max(hdr.m_BlockSize, hdr.m_MaxWorkbuf);
                byte[] workbuf = new byte[(int)(workNeed + 8192)];
                byte[] outbuf = new byte[(int)(hdr.m_MaxWorkbuf * 4 + 8192)];
                uint[] frameOffs = new uint[(int)hdr.m_NFramesPerSegment];
                G16SegmentDecoder decoder = new G16SegmentDecoder();

                for (ushort si = 0; si < hdr.m_NPackSegments; si++)
                {
                    DGU16SegHdr seg = ReadStruct<DGU16SegHdr>(file, segsOffset + si * segSize);
                    uint startBytes = seg.GetOffsetBytes();
                    uint nextBytes = (si + 1 < hdr.m_NPackSegments)
                        ? ReadStruct<DGU16SegHdr>(file, segsOffset + (si + 1) * segSize).GetOffsetBytes()
                        : (uint)blockSize;

                    if (startBytes >= blockSize || nextBytes <= startBytes) continue;

                    uint len = nextBytes - startBytes;
                    uint flags = seg.GetPackFlags();
                    uint baseFrameId = (uint)si * hdr.m_NFramesPerSegment;
                    if (baseFrameId >= hdr.m_NSprites) break;

                    uint framesInSeg = Math.Min((uint)hdr.m_NFramesPerSegment, (uint)hdr.m_NSprites - baseFrameId);
                    Array.Clear(frameOffs, 0, frameOffs.Length);

                    byte[] segData = new byte[(int)len];
                    Array.Copy(file, (long)blockOffset + (long)startBytes, segData, 0L, (long)len);

                    Array.Clear(outbuf, 0, outbuf.Length);
                    bool ok = decoder.UnpackSegmentSafe(segData, len, outbuf, (uint)outbuf.Length, workbuf, (uint)workbuf.Length, frameOffs, framesInSeg, flags);
                    if (!ok)
                    {
                        error = decoder.GetLastUnpackError();
                        continue;
                    }

                    DecodedSegment cs = new DecodedSegment();
                    cs.FramesInSeg = framesInSeg;
                    cs.BaseFrameId = baseFrameId;
                    cs.OutData = (byte[])outbuf.Clone();

                    for (uint i = 0; i < framesInSeg; i++)
                        cs.FrameOffs.Add(frameOffs[(int)i]);

                    for (uint fi = 0; fi < framesInSeg; fi++)
                    {
                        uint globalId = baseFrameId + fi;
                        ushort chunks = 0;
                        if (globalId < hdr.m_NSprites)
                        {
                            DGU16SpriteHdr spr = ReadStruct<DGU16SpriteHdr>(file, spritesOffset + (int)globalId * spriteSize);
                            chunks = spr.GetNChunks();
                        }
                        cs.NumSquares.Add(chunks);
                    }

                    _segments.Add(cs);
                }

                if (_segments.Count == 0)
                {
                    if (string.IsNullOrEmpty(error)) error = "GU16 has no decoded segments";
                    return false;
                }

                return true;
            }

            private bool LoadGN16(byte[] file, int blockOffset, int blockSize, out string error)
            {
                error = "";
                DGN16Header hdr = ReadStruct<DGN16Header>(file, blockOffset);
                _isGN16 = true;
                FrameCount = hdr.m_NSprites;

                int segsOffset = blockOffset + Marshal.SizeOf(typeof(DGN16Header));
                int segHdrSize = 6;
                int spritesOffset = segsOffset + hdr.m_NPackSegments * segHdrSize;
                int spriteSize = Marshal.SizeOf(typeof(DGN16SpriteHdr));

                for (ushort i = 0; i < hdr.m_NSprites; i++)
                {
                    DGN16SpriteHdr spr = ReadStruct<DGN16SpriteHdr>(file, spritesOffset + i * spriteSize);
                    _gnW.Add(spr.GetW());
                    _gnH.Add(spr.GetH());
                    _gnSquares.Add(spr.GetNChunks());
                }

                byte[] workbuf = new byte[(int)(hdr.m_MaxWorkbuf + 8192)];
                byte[] outbuf = new byte[(int)(hdr.m_MaxWorkbuf * 4 + 8192)];
                G16SegmentDecoder decoder = new G16SegmentDecoder();
                uint globalFrameBase = 0;

                for (ushort si = 0; si < hdr.m_NPackSegments; si++)
                {
                    DGN16PackSegHdr seg = ReadGN16PackSegHdr(file, segsOffset + si * segHdrSize);
                    uint start = seg.GetOffsetBytes();
                    uint next = (si + 1 < hdr.m_NPackSegments)
                        ? ReadGN16PackSegHdr(file, segsOffset + (si + 1) * segHdrSize).GetOffsetBytes()
                        : hdr.m_BlockSize;

                    uint len = next > start ? next - start : 0;
                    uint flags = seg.GetPackFlags();
                    uint framesInSeg = seg.GetNFrames();

                    if (len == 0)
                    {
                        globalFrameBase += framesInSeg;
                        continue;
                    }

                    uint[] frameOffs = new uint[(int)framesInSeg];
                    byte[] segData = new byte[(int)len];
                    Array.Copy(file, (long)blockOffset + (long)start, segData, 0L, (long)len);

                    Array.Clear(outbuf, 0, outbuf.Length);
                    bool ok = decoder.UnpackSegmentSafe(segData, len, outbuf, (uint)outbuf.Length, workbuf, (uint)workbuf.Length, frameOffs, framesInSeg, flags);
                    if (!ok)
                    {
                        error = decoder.GetLastUnpackError();
                        globalFrameBase += framesInSeg;
                        continue;
                    }

                    DecodedSegment cs = new DecodedSegment();
                    cs.FramesInSeg = framesInSeg;
                    cs.BaseFrameId = globalFrameBase;
                    cs.OutData = (byte[])outbuf.Clone();
                    cs.FrameOffs.AddRange(frameOffs);

                    for (uint fi = 0; fi < framesInSeg; fi++)
                    {
                        uint globalId = globalFrameBase + fi;
                        ushort nsq = 0;
                        if (globalId < hdr.m_NSprites)
                        {
                            DGN16SpriteHdr spr = ReadStruct<DGN16SpriteHdr>(file, spritesOffset + (int)globalId * spriteSize);
                            nsq = spr.GetNChunks();
                        }
                        cs.NumSquares.Add(nsq);
                    }

                    _segments.Add(cs);
                    globalFrameBase += framesInSeg;
                }

                if (_segments.Count == 0)
                {
                    if (string.IsNullOrEmpty(error)) error = "GN16 has no decoded segments";
                    return false;
                }

                return true;
            }

            private static DGN16PackSegHdr ReadGN16PackSegHdr(byte[] data, int offset)
            {
                DGN16PackSegHdr h = new DGN16PackSegHdr();
                h.data0 = BitConverter.ToUInt32(data, offset);
                h.nFrames = BitConverter.ToUInt16(data, offset + 4);
                return h;
            }

            private static bool BuildFrameRGBA32(
                byte[] outBuf, uint frameOffset, ushort numSquares, int W, int H,
                bool useNationColor, byte nationR, byte nationG, byte nationB,
                out byte[] outRGBA, out string error)
            {
                outRGBA = new byte[W * H * 4];
                error = "";

                if (frameOffset >= outBuf.Length)
                {
                    error = "bad frame offset";
                    return false;
                }

                int remaining = outBuf.Length - (int)frameOffset;
                int srcPos = 0;
                int squaresProcessed = 0;

                if (numSquares == 0)
                {
                    error = "frame has zero chunks";
                    return false;
                }

                for (ushort s = 0; s < numSquares; s++)
                {
                    if (srcPos + 8 > remaining)
                    {
                        error = $"chunk {s}: header OOB";
                        break;
                    }

                    int pos = (int)frameOffset + srcPos;
                    uint sqHdr = BitConverter.ToUInt32(outBuf, pos);
                    uint pow = (sqHdr >> 28) & 0xF;

                    if (pow > 10)
                    {
                        error = $"chunk {s}: bad pow={pow}";
                        break;
                    }

                    int side = 1 << (int)pow;
                    int pixBytes = side * side * 2;
                    if (srcPos + 8 + pixBytes > remaining)
                    {
                        error = $"chunk {s}: pixels OOB side={side}";
                        break;
                    }

                    int x = (int)((sqHdr >> 12) & 0xFFF);
                    if ((x & 0x800) != 0) x |= unchecked((int)0xFFFFF000);

                    int y = (int)(sqHdr & 0xFFF);
                    if ((y & 0x800) != 0) y |= unchecked((int)0xFFFFF000);

                    int pixelDataOffset = pos + 8;
                    int x0 = Math.Max(0, x);
                    int y0 = Math.Max(0, y);
                    int x1 = Math.Min(W, x + side);
                    int y1 = Math.Min(H, y + side);

                    for (int yy = y0; yy < y1; yy++)
                    {
                        for (int xx = x0; xx < x1; xx++)
                        {
                            int srcIdx = ((yy - y) * side + (xx - x)) * 2;
                            ushort px = BitConverter.ToUInt16(outBuf, pixelDataOffset + srcIdx);

                            int a4 = (px >> 12) & 0xF;
                            int r4 = (px >> 8) & 0xF;
                            int g4 = (px >> 4) & 0xF;
                            int b4 = px & 0xF;

                            // COSSACKS2/sgG2D.cpp::G16PaintNationColor.  The low
                            // alpha-nibble bit marks a nation-colour texel; the
                            // remaining bits are its colour strength.  Applying
                            // this while the ARGB4444 source is still available
                            // avoids three extra decodes and GPU readbacks per
                            // animated frame.
                            byte a;
                            if (useNationColor && (a4 & 1) != 0)
                            {
                                int strength = a4 & 14;
                                r4 = Math.Min(15, r4 + ((strength * (nationR >> 4)) >> 4));
                                g4 = Math.Min(15, g4 + ((strength * (nationG >> 4)) >> 4));
                                b4 = Math.Min(15, b4 + ((strength * (nationB >> 4)) >> 4));
                                a = 255;
                            }
                            else
                            {
                                a = (byte)(a4 * 17);
                            }

                            byte r = (byte)(r4 * 17);
                            byte g = (byte)(g4 * 17);
                            byte b = (byte)(b4 * 17);

                            int di = (yy * W + xx) * 4;
                            outRGBA[di + 0] = r;
                            outRGBA[di + 1] = g;
                            outRGBA[di + 2] = b;
                            outRGBA[di + 3] = a;
                        }
                    }

                    srcPos += 8 + pixBytes;
                    squaresProcessed++;
                }

                return squaresProcessed > 0;
            }

            private static bool FindG16Block(byte[] file, out int outOffset, out int outSize, out uint outMagic, out string err)
            {
                outOffset = 0;
                outSize = 0;
                outMagic = 0;
                err = "";

                if (file.Length < 4)
                {
                    err = "file too small";
                    return false;
                }

                uint m0 = BitConverter.ToUInt32(file, 0);
                if (m0 == 0x36315547u || m0 == 0x36314E47u)
                {
                    outOffset = 0;
                    outMagic = m0;
                    uint bs = file.Length >= 8 ? BitConverter.ToUInt32(file, 4) : 0;
                    outSize = (bs >= 8 && bs <= file.Length) ? (int)bs : file.Length;
                    return true;
                }

                int pos = 0;
                while (pos + 8 <= file.Length)
                {
                    uint size = BitConverter.ToUInt32(file, pos + 4);
                    if (size == 0 || pos + 8 + size > file.Length) break;

                    uint pm = BitConverter.ToUInt32(file, pos + 8);
                    if (pm == 0x36315547u || pm == 0x36314E47u)
                    {
                        outOffset = pos + 8;
                        outMagic = pm;
                        uint bs = BitConverter.ToUInt32(file, pos + 12);
                        outSize = (bs >= 8 && bs <= size) ? (int)bs : (int)size;
                        return true;
                    }

                    pos += 8 + (int)size;
                }

                err = "Invalid G16/GN16 magic";
                return false;
            }

            private static bool TryLoadEmbeddedGPAL(byte[] file, uint[] outPal)
            {
                ResetPaletteToGray(outPal);
                if (file == null || file.Length < 16) return false;

                bool installedAny = false;
                for (int pos = 0; pos + 16 <= file.Length; pos++)
                {
                    if (file[pos] != 'G' || file[pos + 1] != 'P' || file[pos + 2] != 'A' || file[pos + 3] != 'L')
                        continue;

                    uint blockSize = BitConverter.ToUInt32(file, pos + 4);
                    uint unpackedSize = BitConverter.ToUInt32(file, pos + 8);
                    ushort flags = BitConverter.ToUInt16(file, pos + 12);

                    if (blockSize < 16 || (long)pos + (long)blockSize > file.Length) continue;
                    if (unpackedSize < 2 || unpackedSize > 1024 * 1024) continue;

                    byte[] packedData = new byte[(int)(blockSize - 16)];
                    Array.Copy(file, pos + 16, packedData, 0, packedData.Length);

                    byte[] decompressed = null;
                    byte compType = (byte)(flags & 0xFF);

                    if (compType == 0)
                    {
                        decompressed = packedData;
                    }
                    else if (compType == 1)
                    {
                        try
                        {
                            using (MemoryStream ms = new MemoryStream(packedData))
                            using (BZip2InputStream bz2 = new BZip2InputStream(ms))
                            using (MemoryStream outMs = new MemoryStream())
                            {
                                bz2.CopyTo(outMs);
                                decompressed = outMs.ToArray();
                            }
                        }
                        catch
                        {
                            decompressed = null;
                        }
                    }

                    if (decompressed == null || decompressed.Length < 2)
                        continue;

                    ushort palCount = BitConverter.ToUInt16(decompressed, 0);
                    if (palCount == 0 || palCount > 64) continue;

                    int offset = 2;
                    for (ushort palIdx = 0; palIdx < palCount && offset + 8 <= decompressed.Length; palIdx++)
                    {
                        uint palSizeOf = BitConverter.ToUInt32(decompressed, offset);
                        ushort palItems = BitConverter.ToUInt16(decompressed, offset + 4);

                        if (palSizeOf < 8 || (long)offset + (long)palSizeOf > decompressed.Length) break;

                        int palDataOffset = offset + 8;
                        int palDataSize = (int)palSizeOf - 8;

                        if (palItems > 0 && palItems <= 256 && palDataSize >= palItems * 4)
                        {
                            int zerosIn4thByte = 0;
                            int nonBlack = 0;

                            for (int j = 0; j < palItems; j++)
                            {
                                byte r = decompressed[palDataOffset + j * 4 + 0];
                                byte g = decompressed[palDataOffset + j * 4 + 1];
                                byte b = decompressed[palDataOffset + j * 4 + 2];
                                byte x = decompressed[palDataOffset + j * 4 + 3];

                                if (x == 0) zerosIn4thByte++;
                                if (r != 0 || g != 0 || b != 0) nonBlack++;
                            }

                            if (nonBlack >= 16 && zerosIn4thByte >= palItems - 16)
                            {
                                InstallPaletteFromRGBA32Bytes(decompressed, palDataOffset, palItems, outPal);
                                installedAny = true;
                            }
                        }

                        offset += (int)palSizeOf;
                    }
                }

                return installedAny;
            }

            private static void ResetPaletteToGray(uint[] outPal)
            {
                ushort[] pal444 = new ushort[256];

                for (int i = 0; i < 256; i++)
                {
                    uint v = (uint)i;
                    if (outPal != null && i < outPal.Length)
                        outPal[i] = (v << 16) | (v << 8) | v;

                    byte q = (byte)(i >> 4);
                    pal444[i] = (ushort)((q << 8) | (q << 4) | q);
                }

                G16GlobalState.G16SetPalette444(pal444);
                G16SegmentDecoder.G16SetPalette444(pal444);
            }

            private static void InstallPaletteFromRGBA32Bytes(byte[] src, int palDataOffset, int palItems, uint[] outPal)
            {
                ushort[] pal444 = new ushort[256];

                for (int j = 0; j < 256; j++)
                {
                    if (j < palItems)
                    {
                        byte r = src[palDataOffset + j * 4 + 0];
                        byte g = src[palDataOffset + j * 4 + 1];
                        byte b = src[palDataOffset + j * 4 + 2];

                        if (outPal != null && j < outPal.Length)
                            outPal[j] = ((uint)r << 16) | ((uint)g << 8) | b;

                        pal444[j] = (ushort)(((r >> 4) << 8) | ((g >> 4) << 4) | (b >> 4));
                    }
                    else
                    {
                        if (outPal != null && j < outPal.Length)
                            outPal[j] = 0;
                        pal444[j] = 0;
                    }
                }

                G16GlobalState.G16SetPalette444(pal444);
                G16SegmentDecoder.G16SetPalette444(pal444);
            }
        }

        // ================================================================
        // Shared helpers
        // ================================================================

        private static bool InRange(byte[] file, uint off, uint bytes)
        {
            if (off > file.Length) return false;
            if (bytes > file.Length) return false;
            if ((ulong)off + (ulong)bytes > (ulong)file.Length) return false;
            return true;
        }

        private static bool InRangeUlong(byte[] file, ulong off, ulong bytes)
        {
            if (off > (ulong)file.Length) return false;
            if (bytes > (ulong)file.Length) return false;
            if (off + bytes > (ulong)file.Length) return false;
            return true;
        }

        private static T ReadStruct<T>(byte[] data, int offset) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            if (offset < 0 || offset + size > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = IntPtr.Add(handle.AddrOfPinnedObject(), offset);
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        private static uint PackRGBA(byte r, byte g, byte b, byte a)
        {
            return (uint)r | ((uint)g << 8) | ((uint)b << 16) | ((uint)a << 24);
        }

        private static byte UnpremultiplyChannelToStraightAlpha(byte value, byte alpha)
        {
            if (alpha == 0)
                return 0;

            uint v = (uint)value * 255u + (uint)(alpha >> 1);
            return (byte)Math.Min(255u, v / alpha);
        }

        private static void BlendOver(uint src, ref uint dst)
        {
            byte sr = (byte)(src & 0xFF);
            byte sg = (byte)((src >> 8) & 0xFF);
            byte sb = (byte)((src >> 16) & 0xFF);
            byte sa = (byte)((src >> 24) & 0xFF);

            if (sa == 0) return;
            if (sa == 255)
            {
                dst = src;
                return;
            }

            byte dr = (byte)(dst & 0xFF);
            byte dg = (byte)((dst >> 8) & 0xFF);
            byte db = (byte)((dst >> 16) & 0xFF);
            byte da = (byte)((dst >> 24) & 0xFF);

            uint inv = 255u - sa;
            byte or_ = (byte)((sr * sa + dr * inv + 127u) / 255u);
            byte og_ = (byte)((sg * sa + dg * inv + 127u) / 255u);
            byte ob_ = (byte)((sb * sa + db * inv + 127u) / 255u);
            byte oa_ = (byte)Math.Min(255u, sa + (da * inv + 127u) / 255u);

            dst = PackRGBA(or_, og_, ob_, oa_);
        }

        private static void ApplyVertexColor(ref uint rgba, uint vcolorARGB)
        {
            byte va = (byte)((vcolorARGB >> 24) & 0xFF);
            byte vr = (byte)((vcolorARGB >> 16) & 0xFF);
            byte vg = (byte)((vcolorARGB >> 8) & 0xFF);
            byte vb = (byte)(vcolorARGB & 0xFF);

            byte r = (byte)(rgba & 0xFF);
            byte g = (byte)((rgba >> 8) & 0xFF);
            byte b = (byte)((rgba >> 16) & 0xFF);
            byte a = (byte)((rgba >> 24) & 0xFF);

            r = (byte)((r * vr) / 255u);
            g = (byte)((g * vg) / 255u);
            b = (byte)((b * vb) / 255u);
            a = (byte)((a * va) / 255u);

            rgba = PackRGBA(r, g, b, a);
        }
    }
}
