using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2SurfaceMeshBuilder
    {
        public readonly List<VertexTnL2LikeOriginal> VertexData = new List<VertexTnL2LikeOriginal>(8192);
        public readonly List<int> Indices = new List<int>(8192);

        private readonly Dictionary<Mesh, MeshUploadState> _meshStates = new Dictionary<Mesh, MeshUploadState>();

        private struct SurfaceVertexUpload
        {
            public Vector3 Position;
            public Color32 Color;
            public Vector2 Uv0;
            public Vector2 Uv1;
            public Vector4 Uv2;
        }

        private const int MaxFVertLikeOriginal = 65500;

        private sealed class MeshUploadState
        {
            public bool LayoutInitialized;
            public int CapacityVertexCount;
            public int CapacityIndexCount;
            public SurfaceVertexUpload[] VertexBuffer;
            public ushort[] IndexBuffer;
            public Bounds ActiveBounds;
        }

        public int TriangleCount => Indices.Count / 3;
        public int VertexCountLikeOriginal => VertexData.Count;

        public void AddTriangleLikeOriginal(
            VertexTnL2LikeOriginal v0,
            VertexTnL2LikeOriginal v1,
            VertexTnL2LikeOriginal v2)
        {
            int baseIndex = VertexData.Count;
            VertexData.Add(v0);
            VertexData.Add(v1);
            VertexData.Add(v2);
            Indices.Add(baseIndex + 0);
            Indices.Add(baseIndex + 1);
            Indices.Add(baseIndex + 2);
        }

        public Mesh BuildMeshLikeOriginal(string name)
        {
            var mesh = new Mesh
            {
                name = name,
                indexFormat = IndexFormat.UInt16
            };
            mesh.MarkDynamic();
            UpdateMeshLikeOriginal(mesh, name);
            return mesh;
        }

        public void UpdateMeshLikeOriginal(Mesh mesh, string name = null, Camera activeCamera = null, Vector2Int viewport = default)
        {
            if (mesh == null)
                return;

            if (!string.IsNullOrWhiteSpace(name))
                mesh.name = name;

            mesh.indexFormat = IndexFormat.UInt16;

            MeshUploadState state = GetOrCreateState(mesh);
            int activeCountLikeOriginal = Mathf.Min(VertexData.Count, Indices.Count);
            activeCountLikeOriginal = Mathf.Min(activeCountLikeOriginal, MaxFVertLikeOriginal);
            activeCountLikeOriginal -= activeCountLikeOriginal % 3;
            if (activeCountLikeOriginal < 0)
                activeCountLikeOriginal = 0;

            EnsurePersistentCapacityLikeOriginal(mesh, state);
            FillActiveVertexRangeLikeOriginal(state, activeCountLikeOriginal, activeCamera, viewport);
            UploadActiveVertexRangeLikeOriginal(mesh, state, activeCountLikeOriginal);
            UpdateActiveCountsLikeOriginal(mesh, state, activeCountLikeOriginal);
        }

        private MeshUploadState GetOrCreateState(Mesh mesh)
        {
            if (!_meshStates.TryGetValue(mesh, out MeshUploadState state) || state == null)
            {
                state = new MeshUploadState();
                _meshStates[mesh] = state;
            }
            return state;
        }

        private static void EnsurePersistentCapacityLikeOriginal(Mesh mesh, MeshUploadState state)
        {
            if (state.LayoutInitialized)
                return;

            state.CapacityVertexCount = MaxFVertLikeOriginal;
            state.CapacityIndexCount = MaxFVertLikeOriginal;
            state.VertexBuffer = new SurfaceVertexUpload[state.CapacityVertexCount];
            state.IndexBuffer = new ushort[state.CapacityIndexCount];

            for (int i = 0; i < state.CapacityVertexCount; i++)
            {
                state.VertexBuffer[i] = new SurfaceVertexUpload
                {
                    Position = new Vector3(0.0f, 0.0f, 0.0f),
                    Color = new Color32(0, 0, 0, 0),
                    Uv0 = Vector2.zero,
                    Uv1 = Vector2.zero,
                    Uv2 = new Vector4(1.0f, 0.0f, 0.0f, 0.0f)
                };
            }

            for (int i = 0; i < state.CapacityIndexCount; i++)
                state.IndexBuffer[i] = (ushort)i;

            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 0),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 0),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2, 0),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4, 0)
            };

            mesh.Clear(false);
            mesh.MarkDynamic();
            mesh.SetVertexBufferParams(state.CapacityVertexCount, layout);
            mesh.SetIndexBufferParams(state.CapacityIndexCount, IndexFormat.UInt16);
            mesh.subMeshCount = 1;

            const MeshUpdateFlags flags = MeshUpdateFlags.DontRecalculateBounds |
                                          MeshUpdateFlags.DontValidateIndices |
                                          MeshUpdateFlags.DontNotifyMeshUsers |
                                          MeshUpdateFlags.DontResetBoneBounds;
            mesh.SetVertexBufferData(state.VertexBuffer, 0, 0, state.CapacityVertexCount, 0, flags);
            mesh.SetIndexBufferData(state.IndexBuffer, 0, 0, state.CapacityIndexCount, flags);
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, 0, MeshTopology.Triangles)
            {
                baseVertex = 0,
                firstVertex = 0,
                vertexCount = 0,
                bounds = new Bounds(Vector3.zero, Vector3.zero)
            }, flags);

            state.LayoutInitialized = true;
        }

        private void FillActiveVertexRangeLikeOriginal(MeshUploadState state, int activeCountLikeOriginal, Camera activeCamera, Vector2Int viewport)
        {
            for (int i = 0; i < activeCountLikeOriginal; i++)
            {
                VertexTnL2LikeOriginal src = VertexData[i];
                state.VertexBuffer[i] = new SurfaceVertexUpload
                {
                    Position = ProjectToScreenSpaceLikeOriginal(src, activeCamera, viewport),
                    Color = VertexTnL2LikeOriginal.UnpackDiffuseLikeOriginal(src.Diffuse),
                    Uv0 = src.GetUvLikeOriginal(),
                    Uv1 = src.GetUv2LikeOriginal(),
                    Uv2 = new Vector4(src.W, 0.0f, 0.0f, 0.0f)
                };
            }

            state.ActiveBounds = CalculateProjectedActiveBoundsLikeOriginal(state, activeCountLikeOriginal, viewport);
        }

        private static void UploadActiveVertexRangeLikeOriginal(Mesh mesh, MeshUploadState state, int activeCountLikeOriginal)
        {
            const MeshUpdateFlags flags = MeshUpdateFlags.DontRecalculateBounds |
                                          MeshUpdateFlags.DontValidateIndices |
                                          MeshUpdateFlags.DontNotifyMeshUsers |
                                          MeshUpdateFlags.DontResetBoneBounds;

            if (activeCountLikeOriginal > 0)
                mesh.SetVertexBufferData(state.VertexBuffer, 0, 0, activeCountLikeOriginal, 0, flags);
        }

        private static void UpdateActiveCountsLikeOriginal(Mesh mesh, MeshUploadState state, int activeCountLikeOriginal)
        {
            int safeCount = Mathf.Max(0, activeCountLikeOriginal - (activeCountLikeOriginal % 3));

            var descriptor = new SubMeshDescriptor(0, safeCount, MeshTopology.Triangles)
            {
                baseVertex = 0,
                firstVertex = 0,
                vertexCount = safeCount,
                bounds = state.ActiveBounds
            };

            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, descriptor, MeshUpdateFlags.DontRecalculateBounds |
                                           MeshUpdateFlags.DontValidateIndices |
                                           MeshUpdateFlags.DontNotifyMeshUsers |
                                           MeshUpdateFlags.DontResetBoneBounds);
            mesh.bounds = state.ActiveBounds;
        }

        
        private static Vector3 ProjectToScreenSpaceLikeOriginal(VertexTnL2LikeOriginal src, Camera activeCamera, Vector2Int viewport)
        {
            // STriang is already emitted as pretransformed XYZRHW-style raster vertices.
            // Do not run it through Unity camera projection a second time.
            return new Vector3(src.X, src.Y, src.Z);
        }

        private static Bounds CalculateProjectedActiveBoundsLikeOriginal(MeshUploadState state, int activeCountLikeOriginal, Vector2Int viewport)
        {
            if (activeCountLikeOriginal <= 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            // Strict XYZRHW-like path stores screen-space x/y in the mesh, so world-space mesh culling would be wrong.
            // Keep the bounds generously large to mirror original always-draw BaseMesh behavior for STriang.
            float extent = Mathf.Max(100000.0f, Mathf.Max(viewport.x, viewport.y) * 1024.0f);
            return new Bounds(Vector3.zero, new Vector3(extent, extent, extent));
        }
    }
}
