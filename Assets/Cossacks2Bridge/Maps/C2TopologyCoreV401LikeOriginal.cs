// C2TopologyCoreV401LikeOriginal.cs
// V401B_TOPOLOGY_CORE
// Source port: COSSACKS2/TopoGraf.cpp + TopoGraf.h + HashTop.cpp + HashTop.h
// and COSSACKS2/Brigade.cpp::GetTopology.
//
// Port scope: CII 1.1 land topology (TopType=0) without road-network/gate-specific
// dependencies (PreCreateTopLinks/GetNRoads*/CreateNationDependentLinksForGates).
// Those branches are not faked. The only Unity adaptation in the land core is the
// MotionField CheckPt/CheckBar/BSetPt backend. Initial CreateAreas follows the original
// SetClearBuildigsLock(0)->CreateAreas()->SetClearBuildigsLock(1) contract.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        internal bool C2TopologyTryGetMapGeometryV401LikeOriginal(
            out int addsh,
            out int mapSx,
            out int mapSy,
            out string sourcePath)
        {
            addsh = 1;
            mapSx = 0;
            mapSy = 0;
            sourcePath = string.Empty;
            if (_map == null)
                return false;

            addsh = Mathf.Clamp(_map.Addsh, 1, 3);
            mapSx = _map.MAPSX;
            mapSy = _map.MAPSY;
            sourcePath = _map.SourcePath ?? string.Empty;
            return mapSx > 0 && mapSy > 0;
        }
    }

    internal sealed class C2TopologyBootstrapV401LikeOriginal : MonoBehaviour
    {
        private int _lastModeInstanceId;
        private string _lastSourcePath = string.Empty;
        private float _retryAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallV401LikeOriginal()
        {
            const string rootName = "C2_TOPOLOGY_CORE_V401";
            GameObject existing = GameObject.Find(rootName);
            if (existing != null && existing.GetComponent<C2TopologyBootstrapV401LikeOriginal>() != null)
                return;

            GameObject go = new GameObject(rootName);
            go.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(go);
            go.AddComponent<C2TopologyBootstrapV401LikeOriginal>();
        }

        private void Update()
        {
            if (Time.unscaledTime < _retryAt)
                return;

            C2BattleTerrainMode mode = UnityEngine.Object.FindFirstObjectByType<C2BattleTerrainMode>(FindObjectsInactive.Exclude);
            if (mode == null)
                return;

            int addsh;
            int mapSx;
            int mapSy;
            string sourcePath;
            if (!mode.C2TopologyTryGetMapGeometryV401LikeOriginal(out addsh, out mapSx, out mapSy, out sourcePath))
                return;

            int modeId = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(mode);
            if (C2TopologyCoreV401LikeOriginal.ReadyLikeOriginal &&
                modeId == _lastModeInstanceId &&
                string.Equals(sourcePath, _lastSourcePath, StringComparison.OrdinalIgnoreCase))
                return;

            _retryAt = Time.unscaledTime + 1.0f;
            if (C2TopologyCoreV401LikeOriginal.RebuildFromCurrentMapV401LikeOriginal(mode))
            {
                _lastModeInstanceId = modeId;
                _lastSourcePath = sourcePath;
            }
        }
    }

    public static class C2TopologyCoreV401LikeOriginal
    {
        private const ushort TopFreeUnassignedV401 = 0xFFFE;
        private const ushort TopBlockedV401 = 0xFFFF;
        private const int RRadV401 = 127;
        private const int NKey0V401 = 1024;
        private const int NKey1V401 = 40;
        private const int NKeyV401 = NKey0V401 * NKey1V401;
        private const int MXZV401 = 16384;
        private const int SupportedLandTopTypeV401 = 0;
        private const float MotionCellRealSizeV401 = 256.0f;
        private const float MotionCellRealHalfV401 = 128.0f;

        private sealed class RadioV401
        {
            public sbyte[] Xi = Array.Empty<sbyte>();
            public sbyte[] Yi = Array.Empty<sbyte>();
            public int N;
        }

        public struct OneLinkInfoV401LikeOriginal
        {
            public ushort NextAreaID;
            public ushort NextAreaDist;
            public uint LinkNatMask;
        }

        public struct StrategyInfoV401LikeOriginal
        {
            public ushort BuildInfo;
            public byte NPeasants;
            public byte NShortRange;
            public byte NLongRange;
            public byte NMortir;
            public byte NTowers;
            public byte NPushek;
        }

        // TopoGraf.h::Area. C++ pointer-owned buffers become managed containers only.
        public sealed class AreaV401LikeOriginal
        {
            public short x;
            public short y;
            public byte Importance;
            public byte NTrees;
            public byte NStones;
            public ushort NMines;
            public ushort[] MinesIdx = Array.Empty<ushort>();
            public ushort MaxLink = 6;
            public readonly StrategyInfoV401LikeOriginal[] SINF = new StrategyInfoV401LikeOriginal[8];
            public readonly List<OneLinkInfoV401LikeOriginal> Link = new List<OneLinkInfoV401LikeOriginal>(6);
            public int NLinks { get { return Link.Count; } }
        }

        private sealed class HashTopV401
        {
            // CII bitfield widths are preserved when values are assigned.
            public ushort K1 = 0xFFFF;
            public ushort ML;
            public ushort LD;
            public byte Prio;
            public byte NI;

            public void ClearV401LikeOriginal()
            {
                K1 = 0xFFFF;
            }
        }

        private sealed class HashTopTableV401
        {
            public byte TopType;
            public ushort[] TopRef = Array.Empty<ushort>();
            public readonly List<AreaV401LikeOriginal> TopMap = new List<AreaV401LikeOriginal>(256);
            public readonly HashTopV401[] Key1 = new HashTopV401[NKeyV401];

            public int NAreas { get { return TopMap.Count; } }

            public HashTopTableV401()
            {
                for (int i = 0; i < Key1.Length; i++)
                    Key1[i] = new HashTopV401();
            }

            public void SetUpV401LikeOriginal(byte lockType, int topCellCount)
            {
                TopType = lockType;
                TopRef = new ushort[Mathf.Max(0, topCellCount)];
                ClearV401LikeOriginal();
            }

            public void EraseAreasV401LikeOriginal()
            {
                TopMap.Clear();
            }

            public void ClearV401LikeOriginal()
            {
                for (int i = 0; i < Key1.Length; i++)
                    Key1[i].ClearV401LikeOriginal();
            }

            public bool AddAreaV401LikeOriginal(short x, short y, byte sliv)
            {
                int minNorm = 1; // CII AddArea switch(ADDSH): 1 for 1/2/3.
                if (sliv != 2)
                {
                    for (int i = 0; i < TopMap.Count; i++)
                    {
                        AreaV401LikeOriginal ar = TopMap[i];
                        if (NormaV401LikeOriginal(ar.x - x, ar.y - y) <= minNorm)
                        {
                            if (sliv != 0)
                            {
                                ar.x = (short)((ar.x + x) >> 1);
                                ar.y = (short)((ar.y + y) >> 1);
                            }
                            return false;
                        }
                    }
                }

                AreaV401LikeOriginal area = new AreaV401LikeOriginal();
                area.x = x;
                area.y = y;
                TopMap.Add(area);
                return true;
            }

            public void AddLinkV401LikeOriginal(int n1, int n2, uint mask)
            {
                // Original CII has one extra early-out only for road zones. Roads are not
                // part of V401_TOPOLOGY_CORE, so NR=0 and that branch is unreachable here.
                if (n1 < 0 || n2 < 0 || n1 >= NAreas || n2 >= NAreas)
                    return;

                AreaV401LikeOriginal ar = TopMap[n1];
                for (int i = 0; i < ar.Link.Count; i++)
                {
                    OneLinkInfoV401LikeOriginal link = ar.Link[i];
                    if (link.NextAreaID == n2)
                    {
                        link.LinkNatMask |= mask;
                        ar.Link[i] = link;
                        return;
                    }
                }

                AreaV401LikeOriginal a2 = TopMap[n2];
                int sc = 16; // GetLinkScale() when neither endpoint is a road zone.
                int d = (NormaV401LikeOriginal(ar.x - a2.x, ar.y - a2.y) * sc) >> 4;
                if (d <= 0) d = 1;

                OneLinkInfoV401LikeOriginal nl = new OneLinkInfoV401LikeOriginal();
                nl.NextAreaID = unchecked((ushort)n2);
                nl.NextAreaDist = unchecked((ushort)d);
                nl.LinkNatMask = mask;
                ar.Link.Add(nl);
            }

            public void CreateAreasV401LikeOriginal()
            {
                EraseAreasV401LikeOriginal();
                if (TopRef == null || TopRef.Length != s_topLxV401 * s_topLyV401)
                    TopRef = new ushort[s_topLxV401 * s_topLyV401];

                for (int x = 0; x < s_topLxV401; x++)
                {
                    for (int y = 0; y < s_topLyV401; y++)
                    {
                        int ofs = x + (y << s_topSHV401);
                        TopRef[ofs] = GetTCStatusV401LikeOriginal(x, y, TopType)
                            ? TopFreeUnassignedV401
                            : TopBlockedV401;
                    }
                }

                // CII _USE3D calls PreCreateTopLinks here. That function seeds road knots.
                // Roads are explicitly deferred in this core, therefore NRAreas/NR == 0.

                int hexSize = 64 * 5;
                int max = 16384 << (s_addshV401 - 1);
                int nmax = max / hexSize;
                for (int i = 0; i < nmax; i++)
                {
                    if ((i & 1) != 0)
                    {
                        for (int j = 0; j < nmax - 1; j++)
                            TryAddHexAreaV401LikeOriginal(hexSize + j * hexSize, hexSize / 2 + i * hexSize);
                    }
                    else
                    {
                        for (int j = 0; j < nmax; j++)
                            TryAddHexAreaV401LikeOriginal(hexSize / 2 + j * hexSize, hexSize / 2 + i * hexSize);
                    }
                }

                ReCreateAreasV401LikeOriginal(0, 0, 10000, 10000);
            }

            private void TryAddHexAreaV401LikeOriginal(int xx, int yy)
            {
                int x = xx >> 6;
                int y = yy >> 6;
                if (!(x > 1 && y > 1 && x < s_topLxV401 - 4 && y < s_topLxV401 - 4))
                    return;

                bool empty = true;
                for (int dx = -1; dx <= 1 && empty; dx++)
                {
                    for (int dy = -1; dy <= 1 && empty; dy++)
                    {
                        if (TopRef[(x + dx) + (y + dy) * s_topLxV401] != TopFreeUnassignedV401)
                            empty = false;
                    }
                }

                if (empty && !CheckBarV401LikeOriginal((x << 2) - 4, (y << 2) - 4, 12, 12))
                    AddAreaV401LikeOriginal((short)x, (short)y, 2);
            }

            public void ReCreateAreasV401LikeOriginal(int x0, int y0, int x1, int y1)
            {
                if (x0 < 0) x0 = 0;
                if (y0 < 0) y0 = 0;
                if (x1 >= s_topLxV401) x1 = s_topLxV401 - 1;
                if (y1 >= s_topLyV401) y1 = s_topLyV401 - 1;

                int[] linksList = new int[4096];
                int nfLinks = 0;

                for (int ix = x0; ix <= x1; ix++)
                {
                    for (int iy = y0; iy <= y1; iy++)
                    {
                        int ofs = ix + (iy << s_topSHV401);
                        TopRef[ofs] = GetTCStatusV401LikeOriginal(ix, iy, TopType)
                            ? TopFreeUnassignedV401
                            : TopBlockedV401;
                    }
                }

                x0 -= 10;
                y0 -= 10;
                x1 += 10;
                y1 += 10;
                if (x0 < 0) x0 = 0;
                if (y0 < 0) y0 = 0;
                if (x1 >= s_topLxV401) x1 = s_topLxV401 - 1;
                if (y1 >= s_topLyV401) y1 = s_topLyV401 - 1;

                for (int i = 0; i < NAreas; i++)
                {
                    AreaV401LikeOriginal ar = TopMap[i];
                    if (ar.x >= x0 && ar.y >= y0 && ar.x <= x1 && ar.y <= y1)
                    {
                        ar.Link.Clear();
                        if (nfLinks < linksList.Length)
                            linksList[nfLinks++] = i;
                    }
                }

                int mmx = s_topLxV401 - 1;
                int mmy = s_topLxV401 - 1; // CII uses TopLx here as well.
                int nr = 0; // GetNRoads(); roads deferred by contract.

                for (int i = 0; i < NAreas; i++)
                {
                    AreaV401LikeOriginal ar = TopMap[i];
                    if (GetTCStatusV401LikeOriginal(ar.x, ar.y, TopType) || i < nr)
                        TopRef[ar.x + ar.y * s_topLxV401] = unchecked((ushort)i);
                }

                // CII road-knot AddLink block is intentionally skipped because NR==0.

                for (int ix = x0; ix <= x1; ix++)
                    for (int iy = y0; iy < y1; iy++)
                        MakeCellSingledV401LikeOriginal(ix, iy, TopType);

                byte[] temp = new byte[s_topLxV401 * s_topLyV401];
                for (int ix = x0; ix <= x1; ix++)
                {
                    for (int iy = y0; iy < y1; iy++)
                    {
                        int ofs = ix + (iy << s_topSHV401);
                        temp[ofs] = GetCellLinksV401LikeOriginal(ix, iy, TopType);
                    }
                }

                bool change = true;
                for (int i = 1; i < 8 && change; i++)
                {
                    change = false;
                    RadioV401 radio = s_rarrV401[i];
                    int n = radio.N;
                    for (int k = 0; k < n; k++)
                    {
                        for (int q = 0; q < nfLinks; q++)
                        {
                            int j = linksList[q];
                            if (i < 3 || j >= nr || TopType != 0)
                            {
                                AreaV401LikeOriginal ar = TopMap[j];
                                int x = ar.x + radio.Xi[k];
                                int y = ar.y + radio.Yi[k];
                                if (x > 0 && y > 0 && x < mmx && y < mmy)
                                {
                                    int ofs = x + (y << s_topSHV401);
                                    byte t = temp[ofs];
                                    int tc = TopRef[ofs];
                                    int tl = TopRef[ofs - 1];
                                    int tr = TopRef[ofs + 1];
                                    int tu = TopRef[ofs - s_topLxV401];
                                    int td = TopRef[ofs + s_topLxV401];

                                    if (t != 0 && tc >= TopFreeUnassignedV401)
                                    {
                                        if ((t & 1) != 0 && tl == j) { TopRef[ofs] = unchecked((ushort)tl); tc = tl; change = true; }
                                        else if ((t & 2) != 0 && tr == j) { TopRef[ofs] = unchecked((ushort)tr); tc = tr; change = true; }
                                        else if ((t & 4) != 0 && tu == j) { TopRef[ofs] = unchecked((ushort)tu); tc = tu; change = true; }
                                        else if ((t & 8) != 0 && td == j) { TopRef[ofs] = unchecked((ushort)td); tc = td; change = true; }
                                    }

                                    if (tc < TopFreeUnassignedV401)
                                    {
                                        if ((t & 1) != 0 && tc != tl && tl < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, tl, uint.MaxValue); AddLinkV401LikeOriginal(tl, tc, uint.MaxValue); }
                                        if ((t & 2) != 0 && tc != tr && tr < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, tr, uint.MaxValue); AddLinkV401LikeOriginal(tr, tc, uint.MaxValue); }
                                        if ((t & 4) != 0 && tc != tu && tu < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, tu, uint.MaxValue); AddLinkV401LikeOriginal(tu, tc, uint.MaxValue); }
                                        if ((t & 8) != 0 && tc != td && td < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, td, uint.MaxValue); AddLinkV401LikeOriginal(td, tc, uint.MaxValue); }
                                    }
                                }
                            }
                        }
                    }
                }

                change = true;
                for (int i = 0; i < 8 && change; i++)
                {
                    change = false;
                    for (int ix = x0; ix <= x1; ix++)
                    {
                        for (int iy = y0; iy <= y1; iy++)
                        {
                            if (ix > 0 && iy > 0 && ix < mmx && iy < mmy)
                            {
                                int ofs = ix + (iy << s_topSHV401);
                                byte t = temp[ofs];
                                int tc = TopRef[ofs];
                                int tl = TopRef[ofs - 1];
                                int tr = TopRef[ofs + 1];
                                int tu = TopRef[ofs - s_topLxV401];
                                int td = TopRef[ofs + s_topLxV401];

                                if (t != 0 && tc >= TopFreeUnassignedV401)
                                {
                                    if ((t & 1) != 0 && tl < TopFreeUnassignedV401) { TopRef[ofs] = unchecked((ushort)tl); tc = tl; change = true; }
                                    else if ((t & 2) != 0 && tr < TopFreeUnassignedV401) { TopRef[ofs] = unchecked((ushort)tr); tc = tr; change = true; }
                                    else if ((t & 4) != 0 && tu < TopFreeUnassignedV401) { TopRef[ofs] = unchecked((ushort)tu); tc = tu; change = true; }
                                    else if ((t & 8) != 0 && td < TopFreeUnassignedV401) { TopRef[ofs] = unchecked((ushort)td); tc = td; change = true; }

                                    if (tc < TopFreeUnassignedV401)
                                    {
                                        if ((t & 1) != 0 && tc != tl && tl < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, tl, uint.MaxValue); AddLinkV401LikeOriginal(tl, tc, uint.MaxValue); }
                                        if ((t & 2) != 0 && tc != tr && tr < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, tr, uint.MaxValue); AddLinkV401LikeOriginal(tr, tc, uint.MaxValue); }
                                        if ((t & 4) != 0 && tc != tu && tu < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, tu, uint.MaxValue); AddLinkV401LikeOriginal(tu, tc, uint.MaxValue); }
                                        if ((t & 8) != 0 && tc != td && td < TopFreeUnassignedV401) { AddLinkV401LikeOriginal(tc, td, uint.MaxValue); AddLinkV401LikeOriginal(td, tc, uint.MaxValue); }
                                    }
                                }
                            }
                        }
                    }
                }

                // Original next calls CreateNationDependentLinksForGates(); omitted in V401.
                ClearV401LikeOriginal();
            }

            public bool CalculateWayV401LikeOriginal(int ofs, HashTopV401 ht, uint mask)
            {
                s_nwptsV401 = 0;
                int nAreas = NAreas;
                if (nAreas <= 0 || nAreas > MXZV401)
                    return false;

                int start = ofs / nAreas;
                int fin = ofs % nAreas;
                if (start < 0 || fin < 0 || start >= nAreas || fin >= nAreas)
                    return false;

                ushort[] precisePointWeight = new ushort[MXZV401];
                ushort[] precisePointPrevIndex = new ushort[MXZV401];
                ushort[] candidatPointWeight = new ushort[MXZV401];
                ushort[] candidatPointPrevIndex = new ushort[MXZV401];
                ushort[] candidatList = new ushort[MXZV401];
                for (int i = 0; i < nAreas; i++)
                {
                    precisePointWeight[i] = 0xFFFF;
                    precisePointPrevIndex[i] = 0xFFFF;
                    candidatPointWeight[i] = 0xFFFF;
                    candidatPointPrevIndex[i] = 0xFFFF;
                }

                candidatList[0] = unchecked((ushort)fin);
                candidatPointWeight[fin] = 0;
                candidatPointPrevIndex[fin] = 0xFFFF;
                int nCandidates = 1;

                do
                {
                    nCandidates--;
                    int tz = candidatList[nCandidates];
                    precisePointWeight[tz] = candidatPointWeight[tz];
                    candidatPointWeight[tz] = 0xFFFF;
                    precisePointPrevIndex[tz] = candidatPointPrevIndex[tz];

                    if (tz == start)
                    {
                        int t0 = tz;
                        s_fullWayV401[0] = unchecked((ushort)tz);
                        s_nwptsV401 = 1;
                        for (int q = 0; t0 != 0xFFFF; q++)
                        {
                            t0 = candidatPointPrevIndex[t0];
                            if (t0 != 0xFFFF && s_nwptsV401 < s_fullWayV401.Length)
                                s_fullWayV401[s_nwptsV401++] = unchecked((ushort)t0);
                        }

                        ht.LD = unchecked((ushort)(precisePointWeight[tz] & 8191));
                        ht.ML = unchecked((ushort)(precisePointPrevIndex[tz] & 8191));
                        ht.Prio = 0;
                        return true;
                    }

                    AreaV401LikeOriginal tar = TopMap[tz];
                    int w0 = precisePointWeight[tz];
                    for (int i = 0; i < tar.Link.Count; i++)
                    {
                        OneLinkInfoV401LikeOriginal link = tar.Link[i];
                        if ((link.LinkNatMask & mask) == 0)
                            continue;

                        int pi = link.NextAreaID;
                        if (pi < 0 || pi >= nAreas)
                            continue;

                        if (precisePointWeight[pi] == 0xFFFF)
                        {
                            int wiInt = link.NextAreaDist + w0;
                            ushort wi = unchecked((ushort)wiInt);
                            ushort wc = candidatPointWeight[pi];
                            bool add = true;

                            if (wc != 0xFFFF)
                            {
                                if (wc > wi)
                                {
                                    for (int j = 0; j < nCandidates; j++)
                                    {
                                        if (candidatList[j] == pi)
                                        {
                                            for (int m = j; m < nCandidates - 1; m++)
                                                candidatList[m] = candidatList[m + 1];
                                            nCandidates--;
                                            break;
                                        }
                                    }
                                    candidatPointWeight[pi] = wi;
                                }
                                else
                                {
                                    add = false;
                                }
                            }

                            if (add)
                            {
                                if (nCandidates >= MXZV401)
                                    return false;

                                if (nCandidates == 0)
                                {
                                    candidatList[0] = unchecked((ushort)pi);
                                    candidatPointWeight[pi] = wi;
                                    candidatPointPrevIndex[pi] = unchecked((ushort)tz);
                                    nCandidates++;
                                }
                                else
                                {
                                    int idxMax = 0;
                                    int idxMin = nCandidates - 1;
                                    ushort wcMax = candidatPointWeight[candidatList[idxMax]];
                                    ushort wcMin = candidatPointWeight[candidatList[idxMin]];

                                    if (wi <= wcMin)
                                    {
                                        candidatList[nCandidates] = unchecked((ushort)pi);
                                        candidatPointWeight[pi] = wi;
                                        candidatPointPrevIndex[pi] = unchecked((ushort)tz);
                                        nCandidates++;
                                    }
                                    else if (wi > wcMax)
                                    {
                                        for (int m = nCandidates; m > 0; m--)
                                            candidatList[m] = candidatList[m - 1];
                                        candidatList[0] = unchecked((ushort)pi);
                                        candidatPointWeight[pi] = wi;
                                        candidatPointPrevIndex[pi] = unchecked((ushort)tz);
                                        nCandidates++;
                                    }
                                    else
                                    {
                                        while (idxMax != idxMin - 1)
                                        {
                                            int idxMid = (idxMin + idxMax) >> 1;
                                            ushort wm = candidatPointWeight[candidatList[idxMid]];
                                            if (wm > wi)
                                            {
                                                wcMax = wm;
                                                idxMax = idxMid;
                                            }
                                            else
                                            {
                                                wcMin = wm;
                                                idxMin = idxMid;
                                            }
                                        }

                                        for (int m = nCandidates; m > idxMin; m--)
                                            candidatList[m] = candidatList[m - 1];
                                        candidatList[idxMin] = unchecked((ushort)pi);
                                        candidatPointWeight[pi] = wi;
                                        candidatPointPrevIndex[pi] = unchecked((ushort)tz);
                                        nCandidates++;
                                    }
                                }
                            }
                        }
                    }
                }
                while (nCandidates > 0);

                return false;
            }

            public HashTopV401 GetHashTopV401LikeOriginal(int ofs, byte ni)
            {
                if (ofs < 0 || NAreas <= 0)
                    return null;

                int key0 = ofs & 1023;
                int key1 = ofs >> 10;
                int startIndex = key0 * NKey1V401;
                int finalIndex = startIndex + NKey1V401;
                int curIndex = startIndex;

                for (int scan = startIndex; scan < finalIndex; scan++)
                {
                    HashTopV401 record = Key1[scan];
                    if (record.NI == (ni & 7))
                    {
                        if (record.K1 == unchecked((ushort)key1))
                        {
                            if (record.Prio < 7) record.Prio++;
                            return record;
                        }
                        if (record.K1 == 0xFFFF)
                        {
                            curIndex = scan;
                            Key1[curIndex].Prio = 0;
                        }
                        else if (Key1[curIndex].Prio > record.Prio)
                        {
                            curIndex = scan;
                        }
                    }
                }

                HashTopV401 cur = Key1[curIndex];
                if (CalculateWayV401LikeOriginal(ofs, cur, 1u << (ni & 31)))
                {
                    cur.K1 = unchecked((ushort)key1);
                    cur.Prio = 0;
                    cur.NI = (byte)(ni & 7);
                    return cur;
                }
                return null;
            }
        }

        private static readonly RadioV401[] s_rarrV401 = CreateEmptyRadioArrayV401LikeOriginal();
        private static readonly HashTopTableV401[] s_hashTablesV401 =
        {
            new HashTopTableV401(), new HashTopTableV401(), new HashTopTableV401(), new HashTopTableV401()
        };

        private static bool[] s_motionBlockedV401 = Array.Empty<bool>();
        private static readonly ushort[] s_fullWayV401 = new ushort[128];
        private static int s_nwptsV401;
        private static bool s_buildingLocksEnabledForTopologyV401 = true;
        private static int s_motionSxV401;
        private static int s_motionSyV401;
        private static int s_addshV401 = 1;
        private static int s_topLxV401;
        private static int s_topLyV401;
        private static int s_topSHV401;
        private static bool s_radioReadyV401;
        private static bool s_readyV401;
        private static string s_sourcePathV401 = string.Empty;
        private static long s_lastBuildMsV401;

        public static bool ReadyLikeOriginal { get { return s_readyV401; } }
        public static int TopLxLikeOriginal { get { return s_topLxV401; } }
        public static int TopLyLikeOriginal { get { return s_topLyV401; } }
        public static int TopSHLikeOriginal { get { return s_topSHV401; } }
        public static string SourcePathLikeOriginal { get { return s_sourcePathV401; } }
        public static long LastBuildMillisecondsLikeOriginal { get { return s_lastBuildMsV401; } }
        public static int NWPTSLikeOriginal { get { return s_nwptsV401; } }
        public static ushort GetFULLWAYPointLikeOriginal(int index)
        {
            return index >= 0 && index < s_nwptsV401 ? s_fullWayV401[index] : TopBlockedV401;
        }

        private static RadioV401[] CreateEmptyRadioArrayV401LikeOriginal()
        {
            RadioV401[] result = new RadioV401[RRadV401];
            for (int i = 0; i < result.Length; i++)
                result[i] = new RadioV401();
            return result;
        }

        public static bool RebuildFromCurrentMapV401LikeOriginal(C2BattleTerrainMode mode)
        {
            if (mode == null)
                return false;

            int addsh;
            int mapSx;
            int mapSy;
            string sourcePath;
            if (!mode.C2TopologyTryGetMapGeometryV401LikeOriginal(out addsh, out mapSx, out mapSy, out sourcePath))
                return false;

            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                s_readyV401 = false;
                s_addshV401 = Mathf.Clamp(addsh, 1, 3);
                s_motionSxV401 = mapSx;
                s_motionSyV401 = mapSy;
                s_topLxV401 = mapSx >> 2;
                s_topLyV401 = mapSy >> 2;
                s_topSHV401 = 7 + s_addshV401;
                s_sourcePathV401 = sourcePath ?? string.Empty;

                if (s_topLxV401 <= 0 || s_topLyV401 <= 0 || s_topLxV401 != (1 << s_topSHV401))
                {
                    UnityEngine.Debug.LogError("[C2:TOPOLOGY V401B] build_failed invalid_geometry addsh=" + s_addshV401 +
                                               " map=" + mapSx + "x" + mapSy +
                                               " top=" + s_topLxV401 + "x" + s_topLyV401 +
                                               " topSH=" + s_topSHV401);
                    return false;
                }

                CreateRadioV401LikeOriginal();

                // TopoGraf.cpp::CreateAreas(): SetClearBuildigsLock(0), build topology,
                // SetClearBuildigsLock(1). We mirror this only inside the topology snapshot;
                // the live Unity movement obstruction map is never globally modified.
                SetClearBuildigsLockV401LikeOriginal(false);
                BuildMotionFieldAdapterV401LikeOriginal();

                // CII 1.1 has NMFIELDS=4 under HASH_TOP. V401B intentionally builds only
                // the land table (type 0); other types remain unavailable, not duplicated/faked.
                for (int i = 0; i < s_hashTablesV401.Length; i++)
                {
                    s_hashTablesV401[i].EraseAreasV401LikeOriginal();
                    s_hashTablesV401[i].SetUpV401LikeOriginal((byte)i, s_topLxV401 * s_topLyV401);
                }
                s_hashTablesV401[SupportedLandTopTypeV401].CreateAreasV401LikeOriginal();
                SetClearBuildigsLockV401LikeOriginal(true);

                s_readyV401 = true;
                sw.Stop();
                s_lastBuildMsV401 = sw.ElapsedMilliseconds;
                LogAuditV401LikeOriginal();
                return true;
            }
            catch (Exception ex)
            {
                sw.Stop();
                s_lastBuildMsV401 = sw.ElapsedMilliseconds;
                s_readyV401 = false;
                SetClearBuildigsLockV401LikeOriginal(true);
                UnityEngine.Debug.LogError("[C2:TOPOLOGY V401B] build_failed source='" + (sourcePath ?? string.Empty) + "' ms=" +
                                           s_lastBuildMsV401 + "\n" + ex);
                return false;
            }
        }

        private static void SetClearBuildigsLockV401LikeOriginal(bool set)
        {
            // CII spelling preserved. set=false means building locks are cleared.
            s_buildingLocksEnabledForTopologyV401 = set;
        }

        private static void BuildMotionFieldAdapterV401LikeOriginal()
        {
            int total = checked(s_motionSxV401 * s_motionSyV401);
            s_motionBlockedV401 = new bool[total];
            for (int x = 0; x < s_motionSxV401; x++)
            {
                float realX = x * MotionCellRealSizeV401 + MotionCellRealHalfV401;
                int baseOfs = x * s_motionSyV401;
                for (int y = 0; y < s_motionSyV401; y++)
                {
                    float realY = y * MotionCellRealSizeV401 + MotionCellRealHalfV401;
                    bool blocked = C2BattleTerrainMode.C2IsHardWaterRealLikeOriginal(realX, realY);
                    if (s_buildingLocksEnabledForTopologyV401)
                        blocked |= C2BuildingRuntimeInfoV247LikeOriginal.IsBlockedRealV247LikeOriginal(realX, realY);
                    s_motionBlockedV401[baseOfs + y] = blocked;
                }
            }
        }

        private static bool CheckPtV401LikeOriginal(int x, int y)
        {
            if (x < 0 || y < 0 || x >= s_motionSxV401 || y >= s_motionSyV401)
                return true; // MotionField::CheckPt returns 1 outside MAPSX/MAPSY.
            return s_motionBlockedV401[x * s_motionSyV401 + y];
        }

        private static void BSetPtV401LikeOriginal(int x, int y)
        {
            if (x < 0 || y < 0 || x >= s_motionSxV401 || y >= s_motionSyV401)
                return;
            s_motionBlockedV401[x * s_motionSyV401 + y] = true;
        }

        private static bool CheckBarV401LikeOriginal(int x, int y, int lx, int ly)
        {
            for (int ix = 0; ix < lx; ix++)
            {
                for (int iy = 0; iy < ly; iy++)
                {
                    if (CheckPtV401LikeOriginal(x + ix, y + iy))
                        return true;
                }
            }
            return false;
        }

        private static bool GetTCStatusV401LikeOriginal(int x, int y, byte topType)
        {
            if (topType != SupportedLandTopTypeV401)
                return false;
            int xxx = x << 2;
            int yyy = y << 2;
            if (!CheckBarV401LikeOriginal(xxx, yyy, 4, 4))
                return true;
            return false;
        }

        private static void MakeCellSingledV401LikeOriginal(int tx, int ty, int lockType)
        {
            if (lockType != SupportedLandTopTypeV401)
                return;

            byte[] tt = new byte[16];
            bool[] l = new bool[16];
            for (int i = 0; i < 16; i++) tt[i] = 0xFF;
            int x0 = tx << 2;
            int y0 = ty << 2;
            int nBlocked = 0;
            for (int ix = 0; ix < 4; ix++)
            {
                for (int iy = 0; iy < 4; iy++)
                {
                    int ofs = ix + (iy << 2);
                    l[ofs] = CheckPtV401LikeOriginal(x0 + ix, y0 + iy);
                    if (l[ofs]) nBlocked++;
                }
            }
            if (nBlocked == 0 || nBlocked == 16)
                return;

            int nTop = 0;
            bool change;
            do
            {
                change = false;
                for (int ix = 0; ix < 4; ix++)
                {
                    for (int iy = 0; iy < 4; iy++)
                    {
                        int ofs = ix + (iy << 2);
                        if (!l[ofs])
                        {
                            int tL = ix > 0 ? tt[ofs - 1] : 0xFF;
                            int tR = ix < 3 ? tt[ofs + 1] : 0xFF;
                            int tU = iy > 0 ? tt[ofs - 4] : 0xFF;
                            int tD = iy < 3 ? tt[ofs + 4] : 0xFF;
                            int tC = tt[ofs];
                            int tm = Math.Min(tC, Math.Min(Math.Min(tL, tR), Math.Min(tU, tD)));
                            if (tm == 255)
                            {
                                tt[ofs] = unchecked((byte)nTop);
                                nTop++;
                                change = true;
                            }
                            else if (tt[ofs] != tm)
                            {
                                tt[ofs] = unchecked((byte)tm);
                                change = true;
                            }
                        }
                    }
                }
            }
            while (change);

            byte[] nZon = new byte[Mathf.Max(1, nTop)];
            for (int i = 0; i < 16; i++)
            {
                int t = tt[i];
                if (t != 255 && t < nZon.Length)
                    nZon[t]++;
            }

            int maxIdx = -1;
            int cm = 0;
            for (int i = 0; i < nTop; i++)
            {
                int t = nZon[i];
                if (t > cm)
                {
                    cm = t;
                    maxIdx = i;
                }
            }
            if (maxIdx == -1)
                return;

            for (int i = 0; i < 16; i++)
            {
                if (tt[i] != maxIdx && tt[i] != 255)
                    BSetPtV401LikeOriginal(x0 + (i & 3), y0 + (i >> 2));
            }
        }

        private static byte GetCellLinksV401LikeOriginal(int tx, int ty, int lockType)
        {
            if (lockType != SupportedLandTopTypeV401)
                return 0;

            bool linkL = false;
            bool linkR = false;
            bool linkU = false;
            bool linkD = false;
            int x0 = tx << 2;
            int y0 = ty << 2;
            for (int i = 0; i < 4; i++)
            {
                linkL |= !(CheckPtV401LikeOriginal(x0 - 1, y0 + i) || CheckPtV401LikeOriginal(x0, y0 + i));
                linkR |= !(CheckPtV401LikeOriginal(x0 + 3, y0 + i) || CheckPtV401LikeOriginal(x0 + 4, y0 + i));
                linkU |= !(CheckPtV401LikeOriginal(x0 + i, y0) || CheckPtV401LikeOriginal(x0 + i, y0 - 1));
                linkD |= !(CheckPtV401LikeOriginal(x0 + i, y0 + 3) || CheckPtV401LikeOriginal(x0 + i, y0 + 4));
            }

            return unchecked((byte)((linkL ? 1 : 0) +
                                    (linkR ? 2 : 0) +
                                    (linkU ? 4 : 0) +
                                    (linkD ? 8 : 0)));
        }

        private static void CreateRadioV401LikeOriginal()
        {
            if (s_radioReadyV401)
                return;

            int[] counts = new int[RRadV401];
            for (int ix = -RRadV401; ix <= RRadV401; ix++)
            {
                for (int iy = -RRadV401; iy <= RRadV401; iy++)
                {
                    int r = (int)Math.Sqrt(ix * ix + iy * iy);
                    if (r < RRadV401) counts[r]++;
                }
            }

            for (int i = 0; i < RRadV401; i++)
            {
                s_rarrV401[i].Xi = counts[i] > 0 ? new sbyte[counts[i]] : Array.Empty<sbyte>();
                s_rarrV401[i].Yi = counts[i] > 0 ? new sbyte[counts[i]] : Array.Empty<sbyte>();
                s_rarrV401[i].N = 0;
            }

            for (int ix = -RRadV401; ix <= RRadV401; ix++)
            {
                for (int iy = -RRadV401; iy <= RRadV401; iy++)
                {
                    int r = (int)Math.Sqrt(ix * ix + iy * iy);
                    if (r < RRadV401)
                    {
                        int n = s_rarrV401[r].N;
                        s_rarrV401[r].Xi[n] = unchecked((sbyte)ix);
                        s_rarrV401[r].Yi[n] = unchecked((sbyte)iy);
                        s_rarrV401[r].N++;
                    }
                }
            }

            if (s_rarrV401[1].Xi.Length >= 8)
            {
                s_rarrV401[1].Xi[0] = -1; s_rarrV401[1].Yi[0] = 0;
                s_rarrV401[1].Xi[1] = 1;  s_rarrV401[1].Yi[1] = 0;
                s_rarrV401[1].Xi[2] = 0;  s_rarrV401[1].Yi[2] = -1;
                s_rarrV401[1].Xi[3] = 0;  s_rarrV401[1].Yi[3] = 1;
                s_rarrV401[1].Xi[4] = -1; s_rarrV401[1].Yi[4] = 1;
                s_rarrV401[1].Xi[5] = -1; s_rarrV401[1].Yi[5] = -1;
                s_rarrV401[1].Xi[6] = 1;  s_rarrV401[1].Yi[6] = -1;
                s_rarrV401[1].Xi[7] = 1;  s_rarrV401[1].Yi[7] = 1;
            }

            s_radioReadyV401 = true;
        }

        // Read-only adapter for the exact TopoGraf.cpp::Rarr table.  Morale.cpp
        // consumes this table directly; exposing it avoids reconstructing/hardcoding
        // panic radii in a second subsystem.
        public static int GetRadioCountV407LikeOriginal(int radius)
        {
            CreateRadioV401LikeOriginal();
            if (radius < 0 || radius >= s_rarrV401.Length) return 0;
            return s_rarrV401[radius] != null ? s_rarrV401[radius].N : 0;
        }

        public static bool TryGetRadioOffsetV407LikeOriginal(
            int radius, int index, out int x, out int y)
        {
            x = 0;
            y = 0;
            CreateRadioV401LikeOriginal();
            if (radius < 0 || radius >= s_rarrV401.Length) return false;
            RadioV401 radio = s_rarrV401[radius];
            if (radio == null || index < 0 || index >= radio.N ||
                index >= radio.Xi.Length || index >= radio.Yi.Length) return false;
            x = radio.Xi[index];
            y = radio.Yi[index];
            return true;
        }

        private static int NormaV401LikeOriginal(int x, int y)
        {
            int ax = Math.Abs(x);
            int ay = Math.Abs(y);
            int mx = Math.Max(ax, ay);
            return (mx + ax + ay) >> 1;
        }

        public static ushort GetTopRefV401LikeOriginal(int ofs, byte topType = 0)
        {
            if (!s_readyV401 || topType >= s_hashTablesV401.Length || topType != SupportedLandTopTypeV401)
                return TopBlockedV401;
            HashTopTableV401 table = s_hashTablesV401[topType];
            if (ofs < 0 || ofs >= table.TopRef.Length)
                return TopBlockedV401;
            return table.TopRef[ofs];
        }

        public static AreaV401LikeOriginal GetTopMapV401LikeOriginal(int ofs, byte topType = 0)
        {
            if (!s_readyV401 || topType >= s_hashTablesV401.Length || topType != SupportedLandTopTypeV401)
                return null;
            HashTopTableV401 table = s_hashTablesV401[topType];
            if (ofs < 0 || ofs >= table.NAreas)
                return null;
            return table.TopMap[ofs];
        }

        public static int GetNAreasV401LikeOriginal(byte topType = 0)
        {
            if (!s_readyV401 || topType >= s_hashTablesV401.Length || topType != SupportedLandTopTypeV401)
                return 0;
            return s_hashTablesV401[topType].NAreas;
        }

        public static ushort GetLinksDistV401LikeOriginal(int ofs, byte topType = 0, byte ni = 0)
        {
            if (!s_readyV401 || topType >= s_hashTablesV401.Length || topType != SupportedLandTopTypeV401)
                return TopBlockedV401;
            HashTopV401 ht = s_hashTablesV401[topType].GetHashTopV401LikeOriginal(ofs, ni);
            return ht != null ? ht.LD : TopBlockedV401;
        }

        public static ushort GetMotionLinksV401LikeOriginal(int ofs, byte topType = 0, byte ni = 0)
        {
            if (!s_readyV401 || topType >= s_hashTablesV401.Length || topType != SupportedLandTopTypeV401)
                return TopBlockedV401;
            HashTopV401 ht = s_hashTablesV401[topType].GetHashTopV401LikeOriginal(ofs, ni);
            if (ht == null)
                return TopBlockedV401;
            int ml = ht.ML;
            if (ml == 8191) ml = TopBlockedV401;
            return unchecked((ushort)ml);
        }

        public static ushort GetTopFastV401LikeOriginal(int x, int y, byte topType = 0)
        {
            if (!s_readyV401 || x < 0 || y < 0 || x >= s_topLxV401 || y >= s_topLyV401)
                return TopBlockedV401;
            return GetTopRefV401LikeOriginal(x + (y << s_topSHV401), topType);
        }

        public static int GetTopologyV401LikeOriginal(int x, int y, byte lockType = 0)
        {
            if (!s_readyV401 || lockType != SupportedLandTopTypeV401)
                return TopBlockedV401;

            int xc = x >> 6;
            int yc = y >> 6;
            ushort tr;
            if (xc < 0 || yc < 0 || xc >= s_topLxV401 || yc >= s_topLyV401)
                tr = TopBlockedV401;
            else
                tr = GetTopRefV401LikeOriginal(xc + (yc << s_topSHV401), lockType);
            if (tr < TopFreeUnassignedV401)
                return tr;

            for (int i = 0; i < 20; i++)
            {
                RadioV401 radio = s_rarrV401[i];
                for (int j = 0; j < radio.N; j++)
                {
                    int xx = xc + radio.Xi[j];
                    int yy = yc + radio.Yi[j];
                    if (xx >= 0 && yy >= 0 && xx < s_topLxV401 && yy < s_topLyV401)
                    {
                        tr = GetTopRefV401LikeOriginal(xx + (yy << s_topSHV401), lockType);
                        if (tr < TopFreeUnassignedV401)
                            return tr;
                    }
                }
            }
            return TopBlockedV401;
        }

        public static int GetTopologyV401LikeOriginal(ref int x, ref int y, byte lockType = 0)
        {
            if (!s_readyV401 || lockType != SupportedLandTopTypeV401)
                return TopBlockedV401;

            int xc = x >> 6;
            int yc = y >> 6;
            ushort tr;
            if (xc < 0 || yc < 0 || xc >= s_topLxV401 || yc >= s_topLyV401)
                tr = TopBlockedV401;
            else
                tr = GetTopRefV401LikeOriginal(xc + (yc << s_topSHV401), lockType);
            if (tr < TopFreeUnassignedV401)
                return tr;

            for (int i = 0; i < 20; i++)
            {
                RadioV401 radio = s_rarrV401[i];
                for (int j = 0; j < radio.N; j++)
                {
                    int xx = xc + radio.Xi[j];
                    int yy = yc + radio.Yi[j];
                    if (xx >= 0 && yy >= 0 && xx < s_topLxV401 && yy < s_topLyV401)
                    {
                        tr = GetTopRefV401LikeOriginal(xx + (yy << s_topSHV401), lockType);
                        if (tr < TopFreeUnassignedV401)
                        {
                            x = (xx << 6) + 32;
                            y = (yy << 6) + 32;
                            return tr;
                        }
                    }
                }
            }
            return TopBlockedV401;
        }

        private static void LogAuditV401LikeOriginal()
        {
            HashTopTableV401 table = s_hashTablesV401[0];
            int assigned = 0;
            int freeUnassigned = 0;
            int blocked = 0;
            for (int i = 0; i < table.TopRef.Length; i++)
            {
                ushort tr = table.TopRef[i];
                if (tr < TopFreeUnassignedV401) assigned++;
                else if (tr == TopFreeUnassignedV401) freeUnassigned++;
                else blocked++;
            }

            int directedLinks = 0;
            int isolatedAreas = 0;
            for (int i = 0; i < table.NAreas; i++)
            {
                int nl = table.TopMap[i].Link.Count;
                directedLinks += nl;
                if (nl == 0) isolatedAreas++;
            }

            string sample = "none";
            if (table.NAreas >= 2)
            {
                int first = 0;
                int last = table.NAreas - 1;
                ushort d1 = GetLinksDistV401LikeOriginal(first + last * table.NAreas, 0, 0);
                ushort d2 = GetLinksDistV401LikeOriginal(first + last * table.NAreas, 0, 0);
                sample = first + "->" + last + " dist=" + d1 + " cached=" + d2;
            }

            UnityEngine.Debug.Log(
                "[C2:TOPOLOGY V401B] READY source='" + s_sourcePathV401 + "'" +
                " addsh=" + s_addshV401 +
                " motion=" + s_motionSxV401 + "x" + s_motionSyV401 +
                " top=" + s_topLxV401 + "x" + s_topLyV401 +
                " topSH=" + s_topSHV401 +
                " areas=" + table.NAreas +
                " directedLinks=" + directedLinks +
                " assignedCells=" + assigned +
                " freeUnassigned=" + freeUnassigned +
                " blockedCells=" + blocked +
                " isolatedAreas=" + isolatedAreas +
                " hashSample='" + sample + "'" +
                " buildMs=" + s_lastBuildMsV401 +
                " adapter=MFIELDS_CheckPt_CheckBar_BSetPt_to_Unity_motion_snapshot" +
                " initialBuildingLocks=CLEARED_LIKE_CII roads=DEFERRED gates=DEFERRED lockTypes=0_ONLY");
        }
    }
}
