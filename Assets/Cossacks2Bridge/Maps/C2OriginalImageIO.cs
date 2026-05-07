using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed class C2OriginalImageData
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color32[] Pixels;
        public readonly string SourcePath;

        public C2OriginalImageData(int width, int height, Color32[] pixels, string sourcePath)
        {
            Width = width;
            Height = height;
            Pixels = pixels ?? Array.Empty<Color32>();
            SourcePath = sourcePath ?? string.Empty;
        }
    }

    public static class C2OriginalImageIO
    {
        private static readonly Dictionary<string, C2OriginalImageData> s_imageCache =
            new Dictionary<string, C2OriginalImageData>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> s_resolvedPathCache =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> s_recursiveLookupCache =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void ClearCache()
        {
            s_imageCache.Clear();
            s_resolvedPathCache.Clear();
            s_recursiveLookupCache.Clear();
        }

        public static bool TryResolveImagePath(
            Cossacks2Bridge.Core.CoreFileSystem fs,
            string requestPath,
            out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (fs == null || string.IsNullOrWhiteSpace(requestPath))
                return false;

            string requestKey = BuildPathCacheKey(fs, requestPath);
            if (s_resolvedPathCache.TryGetValue(requestKey, out resolvedPath) && !string.IsNullOrWhiteSpace(resolvedPath))
                return true;

            string normalizedRequest = NormalizeRelativePath(requestPath);
            string fileName = SafeGetFileName(normalizedRequest);
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            if (TryResolveDirect(fs, normalizedRequest, out resolvedPath) ||
                TryResolveDirect(fs, fileName, out resolvedPath) ||
                TryResolveDirect(fs, @"Textures\" + fileName, out resolvedPath) ||
                TryResolveDirect(fs, @"textures\" + fileName, out resolvedPath))
            {
                s_resolvedPathCache[requestKey] = resolvedPath;
                return true;
            }

            if (TryLocateFileLikeEngine(fs, fileName, @"Textures", out resolvedPath) ||
                TryLocateFileLikeEngine(fs, fileName, string.Empty, out resolvedPath))
            {
                s_resolvedPathCache[requestKey] = resolvedPath;
                return true;
            }

            return false;
        }

        public static bool TryReadImage(
            Cossacks2Bridge.Core.CoreFileSystem fs,
            string requestPath,
            out C2OriginalImageData image,
            out string resolvedPath)
        {
            image = null;
            resolvedPath = string.Empty;

            if (!TryResolveImagePath(fs, requestPath, out resolvedPath))
                return false;

            string cacheKey = BuildImageCacheKey(fs, resolvedPath);
            if (s_imageCache.TryGetValue(cacheKey, out image) && image != null)
                return true;

            string absolutePath = SafeResolvePath(fs, resolvedPath);
            byte[] bytes;
            try
            {
                bytes = fs.ReadAllBytes(resolvedPath);
            }
            catch (Exception ex)
            {
                return false;
            }

            image = CreateImageFromBytes(bytes, resolvedPath);
            if (image == null)
            {
                return false;
            }

            s_imageCache[cacheKey] = image;
            return true;
        }

        public static bool TryReadImage(
            Cossacks2Bridge.Core.CoreFileSystem fs,
            string requestPath,
            out C2OriginalImageData image)
        {
            string resolvedPath;
            return TryReadImage(fs, requestPath, out image, out resolvedPath);
        }

        public static bool TryReadImageByCandidates(
            Cossacks2Bridge.Core.CoreFileSystem fs,
            string[] candidates,
            out C2OriginalImageData image,
            out string resolvedPath)
        {
            image = null;
            resolvedPath = string.Empty;

            if (fs == null || candidates == null)
                return false;

            for (int i = 0; i < candidates.Length; i++)
            {
                string requestPath = candidates[i];
                if (string.IsNullOrWhiteSpace(requestPath))
                    continue;

                if (TryReadImage(fs, requestPath, out image, out resolvedPath))
                    return true;
            }

            return false;
        }

        public static C2OriginalImageData CreateImageFromBytes(byte[] bytes, string path)
        {
            if (bytes == null || bytes.Length < 4)
                return null;

            string ext = Path.GetExtension(path) ?? string.Empty;
            if (ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
                return TryLoadBmpLikeOriginal(bytes, path);
            if (ext.Equals(".tga", StringComparison.OrdinalIgnoreCase))
                return TryLoadTgaLikeOriginal(bytes, path);

            return null;
        }

        private static bool TryResolveDirect(
            Cossacks2Bridge.Core.CoreFileSystem fs,
            string relativePath,
            out string resolvedPath)
        {
            resolvedPath = string.Empty;
            string normalized = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            try
            {
                if (fs.Exists(normalized))
                {
                    resolvedPath = normalized;
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool TryLocateFileLikeEngine(
            Cossacks2Bridge.Core.CoreFileSystem fs,
            string fileName,
            string searchRootRelative,
            out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (fs == null || string.IsNullOrWhiteSpace(fileName))
                return false;

            string normalizedRoot = NormalizeRelativePath(searchRootRelative);
            string lookupKey = BuildRecursiveLookupCacheKey(fs, normalizedRoot, fileName);
            if (s_recursiveLookupCache.TryGetValue(lookupKey, out resolvedPath) && !string.IsNullOrWhiteSpace(resolvedPath))
                return true;

            string absoluteRoot = string.IsNullOrWhiteSpace(normalizedRoot)
                ? fs.DataRoot
                : SafeResolvePath(fs, normalizedRoot);

            if (string.IsNullOrWhiteSpace(absoluteRoot) || !Directory.Exists(absoluteRoot))
                return false;

            try
            {
                foreach (string absolutePath in Directory.EnumerateFiles(absoluteRoot, "*", SearchOption.AllDirectories))
                {
                    if (!string.Equals(Path.GetFileName(absolutePath), fileName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string relativePath = MakeGameRelativePath(fs, absolutePath);
                    if (string.IsNullOrWhiteSpace(relativePath))
                        continue;

                    s_recursiveLookupCache[lookupKey] = relativePath;
                    resolvedPath = relativePath;
                    return true;
                }
            }
            catch (Exception ex)
            {
            }

            return false;
        }

        private static string MakeGameRelativePath(Cossacks2Bridge.Core.CoreFileSystem fs, string absolutePath)
        {
            if (fs == null || string.IsNullOrWhiteSpace(absolutePath))
                return string.Empty;

            try
            {
                string dataRoot = fs.DataRoot ?? string.Empty;
                if (string.IsNullOrWhiteSpace(dataRoot))
                    return string.Empty;

                string fullRoot = Path.GetFullPath(dataRoot);
                string fullPath = Path.GetFullPath(absolutePath);

                if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                    return string.Empty;

                string relative = fullPath.Substring(fullRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return NormalizeRelativePath(relative);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizeRelativePath(string path)
        {
            return (path ?? string.Empty).Trim().Replace('/', '\\');
        }

        private static string SafeGetFileName(string path)
        {
            try
            {
                return Path.GetFileName((path ?? string.Empty).Replace('\\', Path.DirectorySeparatorChar));
            }
            catch
            {
                return string.Empty;
            }
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

        private static string BuildImageCacheKey(Cossacks2Bridge.Core.CoreFileSystem fs, string relativePath)
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

            return root + "|" + NormalizeRelativePath(relativePath);
        }

        private static string BuildPathCacheKey(Cossacks2Bridge.Core.CoreFileSystem fs, string requestPath)
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

            return root + "|" + NormalizeRelativePath(requestPath);
        }

        private static string BuildRecursiveLookupCacheKey(
            Cossacks2Bridge.Core.CoreFileSystem fs,
            string searchRootRelative,
            string fileName)
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

            return root + "|" + NormalizeRelativePath(searchRootRelative) + "|" + (fileName ?? string.Empty).Trim();
        }

        private static C2OriginalImageData TryLoadBmpLikeOriginal(byte[] bytes, string path)
        {
            if (bytes.Length < 54 || bytes[0] != (byte)'B' || bytes[1] != (byte)'M')
                return null;

            int pixelOffset = BitConverter.ToInt32(bytes, 10);
            int dibSize = BitConverter.ToInt32(bytes, 14);
            int width = BitConverter.ToInt32(bytes, 18);
            int height = BitConverter.ToInt32(bytes, 22);
            short planes = BitConverter.ToInt16(bytes, 26);
            short bpp = BitConverter.ToInt16(bytes, 28);
            int compression = BitConverter.ToInt32(bytes, 30);
            int colorsUsed = dibSize >= 40 ? BitConverter.ToInt32(bytes, 46) : 0;
            if (dibSize < 40 || planes != 1 || width <= 0 || height == 0 || compression != 0)
                return null;

            int absHeight = Math.Abs(height);
            bool topDown = height < 0;

            if (bpp == 8)
            {
                int paletteEntries = colorsUsed > 0 ? colorsUsed : 256;
                int paletteOffset = 14 + dibSize;
                int paletteSize = paletteEntries * 4;
                if (paletteOffset + paletteSize > bytes.Length || pixelOffset <= 0 || pixelOffset >= bytes.Length)
                    return null;

                Color32[] palette = new Color32[paletteEntries];
                for (int i = 0; i < paletteEntries; i++)
                {
                    int p = paletteOffset + i * 4;
                    palette[i] = new Color32(bytes[p + 2], bytes[p + 1], bytes[p + 0], 255);
                }

                int rowStride = (width + 3) & ~3;
                if (pixelOffset + rowStride * absHeight > bytes.Length)
                    return null;

                Color32[] pixels = new Color32[width * absHeight];
                for (int y = 0; y < absHeight; y++)
                {
                    int srcY = topDown ? y : (absHeight - 1 - y);
                    int rowOff = pixelOffset + srcY * rowStride;
                    for (int x = 0; x < width; x++)
                    {
                        byte idx = bytes[rowOff + x];
                        pixels[y * width + x] = idx < palette.Length ? palette[idx] : new Color32(255, 0, 255, 255);
                    }
                }

                return new C2OriginalImageData(width, absHeight, pixels, path);
            }

            if (bpp != 24 && bpp != 32)
                return null;

            int bytesPerPixel = bpp / 8;
            int rowStride24 = ((width * bytesPerPixel) + 3) & ~3;
            if (pixelOffset <= 0 || pixelOffset + rowStride24 * absHeight > bytes.Length)
                return null;

            Color32[] pixels24 = new Color32[width * absHeight];
            for (int y = 0; y < absHeight; y++)
            {
                int srcY = topDown ? y : (absHeight - 1 - y);
                int rowOff = pixelOffset + srcY * rowStride24;
                for (int x = 0; x < width; x++)
                {
                    int i = rowOff + x * bytesPerPixel;
                    byte b = bytes[i + 0];
                    byte g = bytes[i + 1];
                    byte r = bytes[i + 2];
                    byte a = bytesPerPixel >= 4 ? bytes[i + 3] : (byte)255;
                    pixels24[y * width + x] = new Color32(r, g, b, a);
                }
            }

            return new C2OriginalImageData(width, absHeight, pixels24, path);
        }

        private static C2OriginalImageData TryLoadTgaLikeOriginal(byte[] bytes, string path)
        {
            if (bytes.Length < 18)
                return null;

            int idLen = bytes[0];
            int colorMapType = bytes[1];
            int imageType = bytes[2];
            int width = bytes[12] | (bytes[13] << 8);
            int height = bytes[14] | (bytes[15] << 8);
            int bpp = bytes[16];
            int desc = bytes[17];
            if (colorMapType != 0 || imageType != 2 || width <= 0 || height <= 0)
                return null;
            if (bpp != 24 && bpp != 32)
                return null;

            int bytesPerPixel = bpp / 8;
            int header = 18 + idLen;
            int expected = width * height * bytesPerPixel;
            if (header + expected > bytes.Length)
                return null;

            bool originTop = (desc & 0x20) != 0;
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                int srcY = originTop ? y : (height - 1 - y);
                int dstY = height - 1 - y;
                for (int x = 0; x < width; x++)
                {
                    int srcIndex = header + (srcY * width + x) * bytesPerPixel;
                    byte b = bytes[srcIndex + 0];
                    byte g = bytes[srcIndex + 1];
                    byte r = bytes[srcIndex + 2];
                    byte a = bytesPerPixel >= 4 ? bytes[srcIndex + 3] : (byte)255;
                    pixels[dstY * width + x] = new Color32(r, g, b, a);
                }
            }

            return new C2OriginalImageData(width, height, pixels, path);
        }
    }
}
