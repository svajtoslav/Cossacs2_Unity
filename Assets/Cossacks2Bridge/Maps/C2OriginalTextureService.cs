using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public enum C2OriginalTexturePolicy
    {
        Default = 0,
        TerrainAtlasLikeOriginal = 1,
        CrossMaskLikeOriginal = 2,
        WorldTextureLikeOriginal = 3,
        UiPictureLikeOriginal = 4,
        UnfilteredPictureLikeOriginal = 5,
    }

    public static class C2OriginalTextureService
    {
        private static readonly Dictionary<string, Texture2D> s_textureCache =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        public static void ClearCache(bool destroyTextures)
        {
            if (destroyTextures)
            {
                foreach (KeyValuePair<string, Texture2D> pair in s_textureCache)
                {
                    if (pair.Value != null)
                        UnityEngine.Object.DestroyImmediate(pair.Value);
                }
            }

            s_textureCache.Clear();
        }

        public static Texture2D TryLoadTexture(
            Cossacks2Bridge.Core.CoreFileSystem fs,
            string requestPath,
            string debugName,
            C2OriginalTexturePolicy policy,
            out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (fs == null || string.IsNullOrWhiteSpace(requestPath))
                return null;

            string canonicalPath;
            if (!C2OriginalImageIO.TryResolveImagePath(fs, requestPath, out canonicalPath) || string.IsNullOrWhiteSpace(canonicalPath))
                return null;

            string cacheKey = BuildCacheKey(fs, canonicalPath, policy);
            Texture2D cached;
            if (s_textureCache.TryGetValue(cacheKey, out cached) && cached != null)
            {
                resolvedPath = canonicalPath;
                return cached;
            }

            string absolutePath = SafeResolvePath(fs, canonicalPath);
            try
            {
                C2OriginalImageData image;
                if (!C2OriginalImageIO.TryReadImage(fs, canonicalPath, out image, out resolvedPath) || image == null)
                    return null;

                Texture2D tex = CreateTexture(image, debugName, policy);
                if (tex == null)
                {
                    Debug.LogWarning($"[C2:TEX] Texture creation returned null rel='{resolvedPath}' abs='{absolutePath}' policy='{policy}'");
                    return null;
                }

                s_textureCache[cacheKey] = tex;
                return tex;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[C2:TEX] Texture load failed rel='{canonicalPath}' abs='{absolutePath}' policy='{policy}': {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        public static Texture2D TryLoadTextureByCandidates(
            Cossacks2Bridge.Core.CoreFileSystem fs,
            string[] candidates,
            string debugName,
            C2OriginalTexturePolicy policy,
            out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (fs == null || candidates == null)
                return null;

            for (int i = 0; i < candidates.Length; i++)
            {
                string requestPath = candidates[i];
                if (string.IsNullOrWhiteSpace(requestPath))
                    continue;

                Texture2D tex = TryLoadTexture(fs, requestPath, debugName, policy, out resolvedPath);
                if (tex != null)
                    return tex;
            }

            return null;
        }

        public static Texture2D CreateTexture(
            C2OriginalImageData image,
            string debugName,
            C2OriginalTexturePolicy policy)
        {
            if (image == null || image.Width <= 0 || image.Height <= 0 || image.Pixels == null || image.Pixels.Length == 0)
                return null;

            bool useMipMaps = ShouldUseMipMaps(policy);
            Texture2D tex = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, useMipMaps, false);
            tex.name = string.IsNullOrWhiteSpace(debugName) ? GetDefaultDebugName(image.SourcePath) : debugName;
            tex.SetPixels32(image.Pixels);
            ApplyPolicy(tex, policy);
            tex.Apply(useMipMaps, false);
            return tex;
        }

        private static string BuildCacheKey(
            Cossacks2Bridge.Core.CoreFileSystem fs,
            string relativePath,
            C2OriginalTexturePolicy policy)
        {
            string root = string.Empty;
            try
            {
                root = fs != null && !string.IsNullOrWhiteSpace(fs.DataRoot) ? fs.DataRoot : string.Empty;
            }
            catch
            {
                root = string.Empty;
            }

            string normalized = (relativePath ?? string.Empty).Replace('/', '\\').Trim();
            return root + "|" + normalized + "|" + ((int)policy).ToString();
        }

        private static string GetDefaultDebugName(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                return "OriginalTexture";
            int slash = Math.Max(sourcePath.LastIndexOf('/'), sourcePath.LastIndexOf('\\'));
            return slash >= 0 && slash + 1 < sourcePath.Length
                ? sourcePath.Substring(slash + 1)
                : sourcePath;
        }

        private static string SafeResolvePath(Cossacks2Bridge.Core.CoreFileSystem fs, string relativePath)
        {
            try
            {
                return fs != null ? fs.ResolvePath(relativePath) : (relativePath ?? string.Empty);
            }
            catch
            {
                return relativePath ?? string.Empty;
            }
        }

        private static bool ShouldUseMipMaps(C2OriginalTexturePolicy policy)
        {
            switch (policy)
            {
                case C2OriginalTexturePolicy.TerrainAtlasLikeOriginal:
                case C2OriginalTexturePolicy.WorldTextureLikeOriginal:
                    return true;
                default:
                    return false;
            }
        }

        private static void ApplyPolicy(Texture2D tex, C2OriginalTexturePolicy policy)
        {
            if (tex == null)
                return;

            tex.anisoLevel = 1;
            tex.mipMapBias = 0.0f;

            switch (policy)
            {
                case C2OriginalTexturePolicy.CrossMaskLikeOriginal:
                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.filterMode = FilterMode.Trilinear;
                    tex.anisoLevel = 8;
                    tex.mipMapBias = -0.25f;
                    break;

                case C2OriginalTexturePolicy.TerrainAtlasLikeOriginal:
                case C2OriginalTexturePolicy.WorldTextureLikeOriginal:
                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.filterMode = FilterMode.Trilinear;
                    tex.anisoLevel = 8;
                    tex.mipMapBias = -0.25f;
                    break;

                case C2OriginalTexturePolicy.UiPictureLikeOriginal:
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    break;

                case C2OriginalTexturePolicy.UnfilteredPictureLikeOriginal:
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Point;
                    break;

                default:
                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.filterMode = FilterMode.Bilinear;
                    break;
            }
        }
    }
}
