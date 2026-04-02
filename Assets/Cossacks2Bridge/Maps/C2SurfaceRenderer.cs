using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2SurfaceImmediateBatch
    {
        public string NameLikeOriginal;
        public Mesh Mesh;
        public Material Material;
        public Vector4 Viewport;
        public int OrderLikeOriginal;
    }

    /// <summary>
    /// Unity-side closest lifecycle analogue of original persistent STriang for the surface pass:
    /// - one child object under parent
    /// - one MeshFilter/MeshRenderer pair
    /// - one persistent Mesh
    /// - one persistent Material
    /// Repeated Draw() calls reuse the same container and only refresh the mesh contents/state.
    /// </summary>
    internal sealed class C2SurfaceRenderer
    {
        private string _shaderNameLikeOriginal = "Surface";
        private readonly Dictionary<string, Texture> _textures = new Dictionary<string, Texture>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _floats = new Dictionary<string, float>(StringComparer.Ordinal);
        private int _renderQueue = 2000;

        public C2SurfaceRenderer SetShader(string shaderNameLikeOriginal)
        {
            if (!string.IsNullOrWhiteSpace(shaderNameLikeOriginal))
                _shaderNameLikeOriginal = shaderNameLikeOriginal;
            return this;
        }

        public C2SurfaceRenderer SetTexture0(Texture2D texture)
        {
            if (texture != null)
                _textures["_MainTex"] = texture;
            return this;
        }

        public C2SurfaceRenderer SetTexture1(Texture2D texture)
        {
            if (texture != null)
                _textures["_CrossTex"] = texture;
            return this;
        }


        public C2SurfaceRenderer SetTexture(string propertyName, Texture texture)
        {
            if (!string.IsNullOrWhiteSpace(propertyName) && texture != null)
                _textures[propertyName] = texture;
            return this;
        }

        public C2SurfaceRenderer SetFloat(string propertyName, float value)
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
                _floats[propertyName] = value;
            return this;
        }

        public C2SurfaceRenderer SetRenderQueue(int renderQueue)
        {
            _renderQueue = renderQueue;
            return this;
        }

        public Material Draw(C2SurfaceDrawCall drawCall, Transform parent, string childName, List<UnityEngine.Object> ownedObjects, C2BattleTerrainMode owner)
        {
            if (drawCall == null)
                throw new ArgumentNullException(nameof(drawCall));

            C2SurfaceMeshBuilder builder = drawCall.Builder;
            if (builder == null || builder.TriangleCount <= 0 || builder.VertexCountLikeOriginal <= 0)
                return null;

            Shader shader = Shader.Find(ResolveUnityShaderName(_shaderNameLikeOriginal));
            if (shader == null)
                return null;

            string resolvedChildName = string.IsNullOrWhiteSpace(childName) ? drawCall.NameLikeOriginal : childName;

            GameObject go = EnsureSurfaceGameObject(parent, resolvedChildName);
            MeshFilter filter = GetOrAddComponent<MeshFilter>(go);
            MeshRenderer renderer = GetOrAddComponent<MeshRenderer>(go);
            ConfigureRenderer(renderer);

            Mesh mesh = EnsurePersistentMesh(filter, drawCall.NameLikeOriginal + "_surface", ownedObjects);
            Camera activeCamera = owner != null ? owner.GetActiveBattleCameraLikeOriginal() : Camera.main;
            if (activeCamera == null)
                activeCamera = Camera.main;
            Vector2Int viewport = activeCamera != null
                ? new Vector2Int(Mathf.Max(1, activeCamera.pixelWidth), Mathf.Max(1, activeCamera.pixelHeight))
                : new Vector2Int(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));

            builder.UpdateMeshLikeOriginal(mesh, drawCall.NameLikeOriginal + "_surface", activeCamera, viewport);

            Material material = EnsurePersistentMaterial(renderer, shader, drawCall.NameLikeOriginal + "_surface_mat", ownedObjects);
            UpdateMaterial(material, activeCamera, viewport);
            renderer.sharedMaterial = material;

            return material;
        }



        public C2SurfaceImmediateBatch BuildImmediateBatch(C2SurfaceDrawCall drawCall, string batchName, List<UnityEngine.Object> ownedObjects, C2BattleTerrainMode owner, int orderLikeOriginal)
        {
            if (drawCall == null)
                throw new ArgumentNullException(nameof(drawCall));

            C2SurfaceMeshBuilder builder = drawCall.Builder;
            if (builder == null || builder.TriangleCount <= 0 || builder.VertexCountLikeOriginal <= 0)
                return null;

            Shader shader = Shader.Find(ResolveUnityShaderName(_shaderNameLikeOriginal));
            if (shader == null)
                return null;

            string resolvedBatchName = string.IsNullOrWhiteSpace(batchName) ? drawCall.NameLikeOriginal : batchName;

            Camera activeCamera = owner != null ? owner.GetActiveBattleCameraLikeOriginal() : Camera.main;
            if (activeCamera == null)
                activeCamera = Camera.main;
            Vector2Int viewport = activeCamera != null
                ? new Vector2Int(Mathf.Max(1, activeCamera.pixelWidth), Mathf.Max(1, activeCamera.pixelHeight))
                : new Vector2Int(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));

            Mesh mesh = builder.BuildMeshLikeOriginal(drawCall.NameLikeOriginal + "_surface_immediate");
            if (mesh == null)
                return null;
            ownedObjects?.Add(mesh);

            Material material = new Material(shader)
            {
                name = drawCall.NameLikeOriginal + "_surface_immediate_mat"
            };
            UpdateMaterial(material, activeCamera, viewport);
            ownedObjects?.Add(material);

            return new C2SurfaceImmediateBatch
            {
                NameLikeOriginal = resolvedBatchName,
                Mesh = mesh,
                Material = material,
                Viewport = new Vector4(Mathf.Max(1, viewport.x), Mathf.Max(1, viewport.y), 0.0f, 0.0f),
                OrderLikeOriginal = orderLikeOriginal
            };
        }
        private static GameObject EnsureSurfaceGameObject(Transform parent, string childName)
        {
            Transform existing = parent != null ? parent.Find(childName) : null;
            if (existing != null)
                return existing.gameObject;

            var go = new GameObject(childName);
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();
            return component;
        }

        private static void ConfigureRenderer(MeshRenderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Mesh EnsurePersistentMesh(MeshFilter filter, string meshName, List<UnityEngine.Object> ownedObjects)
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh != null)
                return mesh;

            mesh = new Mesh
            {
                name = meshName,
                indexFormat = IndexFormat.UInt16
            };
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;
            ownedObjects?.Add(mesh);
            return mesh;
        }

        private static Material EnsurePersistentMaterial(MeshRenderer renderer, Shader shader, string materialName, List<UnityEngine.Object> ownedObjects)
        {
            Material material = renderer.sharedMaterial;
            if (material != null && material.shader == shader)
                return material;

            material = new Material(shader)
            {
                name = materialName
            };
            renderer.sharedMaterial = material;
            ownedObjects?.Add(material);
            return material;
        }

        private void UpdateMaterial(Material material, Camera activeCamera, Vector2Int viewport)
        {
            if (material == null)
                return;

            foreach (var kv in _textures)
            {
                if (!string.IsNullOrWhiteSpace(kv.Key) && kv.Value != null)
                    material.SetTexture(kv.Key, kv.Value);
            }
            foreach (var kv in _floats)
            {
                if (!string.IsNullOrWhiteSpace(kv.Key))
                    material.SetFloat(kv.Key, kv.Value);
            }

            material.SetFloat("_C2UseStrictTnL", 1.0f);
            material.SetVector("_C2Viewport", new Vector4(Mathf.Max(1, viewport.x), Mathf.Max(1, viewport.y), 0.0f, 0.0f));
            material.renderQueue = _renderQueue;
        }

        private static string ResolveUnityShaderName(string shaderNameLikeOriginal)
        {
            if (string.Equals(shaderNameLikeOriginal, "Surface", StringComparison.OrdinalIgnoreCase))
                return "Cossacks2Bridge/Surface";
            return shaderNameLikeOriginal;
        }
    }
}
