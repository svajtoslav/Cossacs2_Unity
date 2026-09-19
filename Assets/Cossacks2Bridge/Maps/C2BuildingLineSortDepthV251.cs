using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // V267: LINESORT data is geometry metadata here. Original DrawSpriteBuilding uses LineInfo to
    // choose GROUND/TOP/LINE transforms; it does not scan nearby units and rewrite building part
    // order. Keep this component disabled so building parts never react to unit movement.
    public sealed partial class C2BattleTerrainMode
    {
        private void C2BuildingLineSortDepthV251AttachLikeOriginal(
            GameObject parent,
            C2Building3InuRecordLikeOriginal record,
            C2BuildingMdInfoLikeOriginal md,
            List<C2BuildingLoadedPartLikeOriginal> parts,
            List<MeshRenderer> renderers,
            float scale)
        {
            if (parent == null || md == null || parts == null || renderers == null)
                return;

            int count = Math.Min(parts.Count, renderers.Count);
            if (count <= 0)
                return;

            C2BuildingLineSortDepthRuntimeV251 runtime = parent.GetComponent<C2BuildingLineSortDepthRuntimeV251>();
            if (runtime == null) runtime = parent.AddComponent<C2BuildingLineSortDepthRuntimeV251>();

            runtime.RecordIndex = record.Index;
            runtime.SourceMonsterId = record.MonsterId ?? string.Empty;
            runtime.MdName = md.MdName ?? string.Empty;
            runtime.RealX = record.RealX;
            runtime.RealY = record.RealY;
            runtime.MapPixelToWorldScale = scale;
            runtime.Parts = new C2BuildingLineSortDepthRuntimeV251.Part[count];

            Vector3 buildingWorld = WallOriginalXYToWorldV1LikeOriginal(record.RealX >> 4, record.RealY >> 4, 0.0f);

            int lineCount = 0;
            int groundCount = 0;
            int topCount = 0;
            int noneCount = 0;

            for (int i = 0; i < count; i++)
            {
                C2BuildingLoadedPartLikeOriginal part = parts[i];
                MeshRenderer mr = renderers[i];

                C2BuildingLineSortDepthRuntimeV251.Part dst = new C2BuildingLineSortDepthRuntimeV251.Part();
                dst.Renderer = mr;
                dst.PartIndex = i;
                dst.BaseSortingOrder = mr != null ? mr.sortingOrder : 0;
                dst.CurrentSortingOrder = dst.BaseSortingOrder;
                dst.PendingSortingOrder = dst.BaseSortingOrder;
                dst.PendingFrames = 0;
                dst.HasLine = false;
                dst.LineType = C2BuildingLineSortDepthRuntimeV251.LineTypeNone;
                dst.LocalX1 = 0;
                dst.LocalY1 = 0;
                dst.LocalX2 = 0;
                dst.LocalY2 = 0;

                if (part != null && part.HasLineSort)
                {
                    C2BuildingLineSortLikeOriginal li = part.LineSort;
                    dst.LocalX1 = li.X1;
                    dst.LocalY1 = li.Y1;
                    dst.LocalX2 = li.X2;
                    dst.LocalY2 = li.Y2;

                    if (li.IsGround)
                    {
                        dst.LineType = C2BuildingLineSortDepthRuntimeV251.LineTypeGround;
                        groundCount++;
                    }
                    else if (li.IsTop)
                    {
                        dst.LineType = C2BuildingLineSortDepthRuntimeV251.LineTypeTop;
                        topCount++;
                    }
                    else
                    {
                        dst.LineType = C2BuildingLineSortDepthRuntimeV251.LineTypeLine;
                        dst.HasLine = true;
                        lineCount++;

                        FramePivotLikeOriginal(md, part.Frame, out int dx, out int dy);
                        Vector3 pivot = SkewPtLikeOriginal(-dx, -dy, 0.0f);
                        C2BuildingMatrix4LikeOriginal tm = GetAlignLineTransformLikeOriginal(pivot, li.X1, li.Y1, li.X2, li.Y2);

                        Vector3 p0 = tm.TransformPoint(new Vector3(li.X1, li.Y1, 0.0f));
                        Vector3 p1 = tm.TransformPoint(new Vector3(li.X2, li.Y2, 0.0f));

                        dst.LineWorldA = buildingWorld + OriginalDrawSpaceToUnityLocalLikeOriginal(p0, scale);
                        dst.LineWorldB = buildingWorld + OriginalDrawSpaceToUnityLocalLikeOriginal(p1, scale);

                        // Original MiniMap4X.cpp::DrawSpriteBuilding CINFMOD line draw:
                        //   Vector3D pivot1( -NF->dx, -NF->dy*2, 0 );
                        //   Vector3D lp = wPos; lp -= pivot1; WorldToScreenSpace(lp);
                        //   screenLine = (LineInfo.x/y + lp.x/y)
                        // Unity screen Y grows upward, therefore LocalY is subtracted later in
                        // TryBuildOriginalDebugLineScreenV260K().
                        Vector3 debugOriginOriginal = new Vector3(dx, dy * 2.0f, 0.0f);
                        dst.LineDebugOriginWorldV260K = buildingWorld + OriginalDrawSpaceToUnityLocalLikeOriginal(debugOriginOriginal, scale);
                    }
                }
                else
                {
                    noneCount++;
                }

                runtime.Parts[i] = dst;
            }

            runtime.enabled = false;

            if (!C2BuildingLineSortDepthRuntimeV251.InstallLogged)
            {
                C2BuildingLineSortDepthRuntimeV251.InstallLogged = true;
                Debug.Log("[C2:BUILD LINESORT DEPTH V267] installed mode=metadata_only_no_unit_reaction source=MDLINESORT+Scape3D.GetAlignLineTransform geometry_done_in_DrawSpriteBuilding dynamic_building_part_resort=0 unit_scan=0 anchor=PicDxPicDy_original_for_BUILDING");
            }

            if (C2BuildingLineSortDepthRuntimeV251.DebugInitLog)
            {
                Debug.Log("[C2:BUILD LINESORT DEPTH V267 INIT] record=" + record.Index.ToString(CultureInfo.InvariantCulture) +
                          " name='" + (record.MonsterId ?? string.Empty) + "'" +
                          " md='" + (md.MdName ?? string.Empty) + "'" +
                          " parts=" + count.ToString(CultureInfo.InvariantCulture) +
                          " line=" + lineCount.ToString(CultureInfo.InvariantCulture) +
                          " ground=" + groundCount.ToString(CultureInfo.InvariantCulture) +
                          " top=" + topCount.ToString(CultureInfo.InvariantCulture) +
                          " none=" + noneCount.ToString(CultureInfo.InvariantCulture) +
                          " unitScan=0");
            }
        }
    }

    internal sealed class C2BuildingLineSortDepthRuntimeV251 : MonoBehaviour
    {
        internal const int LineTypeNone = 0;
        internal const int LineTypeGround = 1;
        internal const int LineTypeTop = 2;
        internal const int LineTypeLine = 3;

        internal static bool InstallLogged;
        internal const bool DebugInitLog = false;

        internal struct Part
        {
            internal MeshRenderer Renderer;
            internal int PartIndex;
            internal int BaseSortingOrder;
            internal int CurrentSortingOrder;
            internal int PendingSortingOrder;
            internal int PendingFrames;
            internal int LineType;
            internal bool HasLine;
            internal int LocalX1;
            internal int LocalY1;
            internal int LocalX2;
            internal int LocalY2;
            internal Vector3 LineWorldA;
            internal Vector3 LineWorldB;
            internal Vector3 LineDebugOriginWorldV260K;
        }

        internal int RecordIndex;
        internal string SourceMonsterId = string.Empty;
        internal string MdName = string.Empty;
        internal int RealX;
        internal int RealY;
        internal float MapPixelToWorldScale = 1.0f;
        internal Part[] Parts;

        private void LateUpdate()
        {
            if (Parts == null || Parts.Length == 0)
                return;

            // Metadata-only component: do not scan units or rewrite part ordering.
            RestoreBaseOrdersV251();
            enabled = false;
        }

        private void RestoreBaseOrdersV251()
        {
            for (int i = 0; i < Parts.Length; i++)
            {
                Part p = Parts[i];
                if (p.Renderer != null && p.Renderer.sortingOrder != p.BaseSortingOrder)
                {
                    p.Renderer.sortingOrder = p.BaseSortingOrder;
                    p.CurrentSortingOrder = p.BaseSortingOrder;
                    p.PendingSortingOrder = p.BaseSortingOrder;
                    p.PendingFrames = 0;
                    Parts[i] = p;
                }
            }
        }

    }
}
