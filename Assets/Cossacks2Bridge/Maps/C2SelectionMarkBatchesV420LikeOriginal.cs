using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2UnitOriginalRuntimeAndRendererV1
    {
        // SelectionRect.cpp submits terrain patches, not independent scene objects.
        // Preserve the port's round3/round4 geometry/material/depth settings while
        // collecting selected quads into two shared meshes at the DrawUnits boundary.
        internal bool UseBatchedSelectionMarksV420 = true;
        private readonly OriginalGpsUnitBatchLikeOriginal[] _selectionBatchesV420 = new OriginalGpsUnitBatchLikeOriginal[2];

        private OriginalGpsUnitBatchLikeOriginal SelectionMarkBatchV420(int kind, int layer)
        {
            var batch = _selectionBatchesV420[kind];
            if (batch != null && batch.Root != null) return batch;
            batch = new OriginalGpsUnitBatchLikeOriginal();
            batch.Root = new GameObject(kind == 0 ? "C2_SelectionMarks_round3_batch" : "C2_SelectionMarks_round4_batch");
            batch.Root.transform.SetParent(_runtimeRoot != null ? _runtimeRoot.transform : transform, false);
            batch.Root.layer = layer;
            batch.Mesh = new Mesh { name = batch.Root.name, indexFormat = IndexFormat.UInt32 };
            batch.Mesh.MarkDynamic();
            batch.Root.AddComponent<MeshFilter>().sharedMesh = batch.Mesh;
            batch.BodyRenderer = batch.Root.AddComponent<MeshRenderer>();
            batch.BodyRenderer.shadowCastingMode = ShadowCastingMode.Off;
            batch.BodyRenderer.receiveShadows = false;
            batch.BodyRenderer.lightProbeUsage = LightProbeUsage.Off;
            batch.BodyRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            batch.BodyRenderer.sharedMaterial = kind == 0 ? GetSelectionMaterialLikeOriginal() : GetSelectionBrigadeMaterialLikeOriginal();
            _selectionBatchesV420[kind] = batch;
            return batch;
        }

        private void RebuildSelectionMarkBatchesV420(int renderCount)
        {
            for (int k = 0; k < 2; k++) _selectionBatchesV420[k]?.BeginFrameLikeOriginal();
            if (UseOriginalGpsUnitBatchRendererLikeOriginal && UseBatchedSelectionMarksV420)
            for (int i = 0; i < renderCount; i++)
            {
                var unit = UseOriginalDrawUnitsCellVisibilityLikeOriginal ? _drawUnitsCurrentLikeOriginal[i] : _units[i];
                if (unit == null || !unit.Selected || !unit.ActiveLikeOriginal || unit.HiddenInsideBuildingLikeOriginal) continue;
                if(unit.OriginalComplexObjectV430LikeOriginal!=null)continue; // Native SELTYPE patch is owned by its complex visual.
                int kind = unit.Info != null && C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(unit.Info) ? 1 : 0;
                var batch = SelectionMarkBatchV420(kind, unit.VisibleLayerLikeOriginal);
                if (!batch.Touched) batch.WorldToLocalThisFrameV378LikeOriginal = batch.Root.transform.worldToLocalMatrix;
                float pixel = Mathf.Max(0.0001f, GetFixedPixelWorldScaleForUnitLikeOriginal(unit));
                float width = Mathf.Max(pixel, SelectionRingWidth * Mathf.Max(0.01f, SelectionRoundScale) * pixel);
                float height = Mathf.Max(pixel, SelectionRingHeight * Mathf.Max(0.01f, SelectionRoundScale) * pixel);
                Vector3 position = CalculateSelectionRingWorldPositionLikeOriginal(unit);
                Quaternion rotation = SelectionRingUseGroundPlane ? Quaternion.Euler(90, 0, 0) : unit.WorldRotationLikeOriginal;
                Matrix4x4 matrix = batch.WorldToLocalThisFrameV378LikeOriginal * Matrix4x4.TRS(position, rotation, Vector3.one);
                int first = batch.Vertices.Count;
                batch.Vertices.Add(matrix.MultiplyPoint3x4(new Vector3(-width / 2, -height / 2, 0)));
                batch.Vertices.Add(matrix.MultiplyPoint3x4(new Vector3(width / 2, -height / 2, 0)));
                batch.Vertices.Add(matrix.MultiplyPoint3x4(new Vector3(width / 2, height / 2, 0)));
                batch.Vertices.Add(matrix.MultiplyPoint3x4(new Vector3(-width / 2, height / 2, 0)));
                batch.Uv.Add(new Vector2(0, 0)); batch.Uv.Add(new Vector2(1, 0));
                batch.Uv.Add(new Vector2(1, 1)); batch.Uv.Add(new Vector2(0, 1));
                batch.Triangles.Add(first); batch.Triangles.Add(first + 2); batch.Triangles.Add(first + 1);
                batch.Triangles.Add(first); batch.Triangles.Add(first + 3); batch.Triangles.Add(first + 2);
                batch.Touched = true;
            }
            for (int k = 0; k < 2; k++)
            {
                var batch = _selectionBatchesV420[k];
                if (batch == null || batch.BodyRenderer == null) continue;
                batch.BodyRenderer.enabled = batch.Touched;
                if (!batch.Touched) continue;
                batch.BodyRenderer.sortingOrder = SelectionRingBehindUnit ? short.MinValue : SortingOrderBase + 1;
                Material mat = batch.BodyRenderer.sharedMaterial;
                mat.renderQueue = SelectionRingBehindUnit ? UnitBodyRenderQueueLikeOriginal - 1 : UnitBodyRenderQueueLikeOriginal + 1;
                if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
                if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", SelectionRingThroughTerrain ? 8 : 4);
                batch.Mesh.Clear(false);
                batch.Mesh.SetVertices(batch.Vertices);
                batch.Mesh.SetUVs(0, batch.Uv);
                batch.Mesh.SetTriangles(batch.Triangles, 0, false);
                batch.Mesh.RecalculateBounds();
            }
        }

        private void ClearSelectionMarkBatchesV420()
        {
            for (int k = 0; k < 2; k++)
            {
                var batch = _selectionBatchesV420[k];
                if (batch == null) continue;
                if (batch.Mesh != null) Destroy(batch.Mesh);
                if (batch.Root != null) Destroy(batch.Root);
                _selectionBatchesV420[k] = null;
            }
        }
    }
}
