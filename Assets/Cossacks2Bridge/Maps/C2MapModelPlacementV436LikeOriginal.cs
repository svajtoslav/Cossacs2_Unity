using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        // SaveNewMap.cpp::LoadSprites2 + MiniMap4X.cpp::DrawSpriteTrees.
        // Saved Matrix4D is a row-vector matrix in the original renderer's
        // skewed space. Unity's terrain is unskewed; convert exactly once.
        private Vector3 SavedModelPointToWorldV436LikeOriginal(Matrix4x4 matrix, Vector3 local)
        {
            Vector3 projected = TransformOriginalMatrix4DPointV19LikeOriginal(matrix, local);
            float z = projected.z / 0.8660254037844386f;
            return OriginalWallXYZToWorldV6LikeOriginal(projected.x, projected.y + z * 0.5f, z);
        }

        private Matrix4x4 SavedModelMatrixV436LikeOriginal(WallSavedMapSpriteV6LikeOriginal sprite, WallSpriteDescV1LikeOriginal desc)
        {
            if (UseSavedMatrixAfterLoadSprites2V172LikeOriginal(sprite, desc)) return sprite.Matrix;
            // ObjCharacter::GetMatrix4D. Editable objects without a saved matrix
            // use the editor defaults (Scale=1, RotFI/RotX/RotY/ModelDZ=0).
            Matrix4x4 matrix = desc.EditableModelV436 ? BuildSkewMatrix4DLikeOriginalV159() : Matrix4x4.identity;
            matrix.m30 = sprite.X;
            matrix.m31 = sprite.Y;
            float height = desc.HasFixHeightV436 ? desc.FixHeight : SampleWallHeightOriginalXYV1LikeOriginal(sprite.X, sprite.Y);
            if (height > 1024) height -= 2048;
            matrix.m32 = desc.EditableModelV436 ? 0 : height;
            return matrix;
        }

        private int BuildSavedMapModelsV436LikeOriginal(List<WallSavedMapSpriteV6LikeOriginal> sprites,
            WallSpriteCatalogV1LikeOriginal catalog, Transform parent, out HashSet<WallSavedMapSpriteV6LikeOriginal> handled)
        {
            handled = new HashSet<WallSavedMapSpriteV6LikeOriginal>();
            var materials = new Dictionary<string, Material>();
            int built = 0, missing = 0, saved = 0;
            for (int i = 0; i < sprites.Count; i++)
            {
                var sprite = sprites[i];
                WallSpriteDescV1LikeOriginal desc;
                if (sprite == null || !catalog.ByIndex.TryGetValue(sprite.SpriteIndex, out desc) || string.IsNullOrEmpty(desc.ModelPath)) continue;
                handled.Add(sprite);
                string audit;
                var model = TryLoadWallC2MVisualMeshV23LikeOriginal(desc.ModelPath, out audit);
                if (model == null || model.Vertices.Length == 0 || model.Triangles.Length == 0)
                {
                    Debug.LogWarning("[C2 WALS V436] missing model=" + desc.ModelPath + " " + audit);
                    missing++; continue;
                }
                Matrix4x4 matrix = SavedModelMatrixV436LikeOriginal(sprite, desc);
                if (UseSavedMatrixAfterLoadSprites2V172LikeOriginal(sprite, desc)) saved++;
                var vertices = new Vector3[model.Vertices.Length];
                for (int v = 0; v < vertices.Length; v++) vertices[v] = SavedModelPointToWorldV436LikeOriginal(matrix, model.Vertices[v]);
                Mesh mesh = TryBuildWallC2MGPObjDrawWChunkBakedMeshV50LikeOriginal(model, vertices, desc, out audit);
                if (mesh == null)
                {
                    mesh = new Mesh { name = "C2_SavedModel_V436_" + desc.Name };
                    if (vertices.Length > 65000) mesh.indexFormat = IndexFormat.UInt32;
                    mesh.vertices = vertices;
                    mesh.uv = model.UV;
                    mesh.colors32 = model.Colors;
                    mesh.triangles = model.Triangles;
                    mesh.RecalculateBounds(); mesh.RecalculateNormals();
                }
                Material material;
                if (!materials.TryGetValue(desc.ModelPath, out material))
                {
                    Texture2D texture = null;
                    List<WallG16SquareV47LikeOriginal> squares;
                    if (model.GPObj != null) texture = TryLoadWallC2MGPObjFrameTextureV42LikeOriginal(model, out audit, out squares);
                    if (texture == null) texture = TryLoadWallC2MTXRETextureV48LikeOriginal(model, out audit);
                    if (texture == null)
                    {
                        SafeDestroy(mesh); missing++;
                        Debug.LogWarning("[C2 WALS V436] missing texture=" + desc.ModelPath + " " + audit);
                        continue;
                    }
                    material = CreateWallC2MModelMaterialV26LikeOriginal(texture, desc);
                    // Remove old diagnostic overrides; original geometry owns its depth.
                    if (material.HasProperty("_ZTest")) material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                    if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                    materials.Add(desc.ModelPath, material);
                }
                var go = new GameObject("C2_SavedModel_V436_" + i + "_" + desc.Name);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                ApplyWallRendererShadowContractV44LikeOriginal(renderer);
                renderer.sortingOrder = Mathf.Clamp(sprite.Y, -32768, 32767);
                built++;
            }
            Debug.Log("[C2 WALS V436] sourceModels=" + handled.Count + " built=" + built + " savedMatrices=" + saved + " missing=" + missing + " syntheticRows=0");
            return built;
        }
    }
}
