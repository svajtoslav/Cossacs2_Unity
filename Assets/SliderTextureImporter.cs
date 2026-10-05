using UnityEditor;
using UnityEngine;

/// <summary>
/// Pixel-perfect импорт ВСЕХ TGA файлов в Resources
/// </summary>
public class PixelPerfectTextureImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        // ═══════════════════════════════════════════════════════════
        // Применяем ко ВСЕМ TGA в папке Resources
        // ═══════════════════════════════════════════════════════════
        bool isTgaInResources =
            assetPath.Contains("Resources") &&
            assetPath.EndsWith(".tga", System.StringComparison.OrdinalIgnoreCase);

        // Также применяем к PNG в Resources
        bool isPngInResources =
            assetPath.Contains("Resources") &&
            assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase);

        if (!isTgaInResources && !isPngInResources)
            return;

        TextureImporter importer = (TextureImporter)assetImporter;

        // ═══════════════════════════════════════════════════════════
        // PIXEL-PERFECT НАСТРОЙКИ
        // ═══════════════════════════════════════════════════════════

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;

        // 1 пиксель текстуры = 1 пиксель на экране
        importer.spritePixelsPerUnit = 1f;

        // Point фильтрация = БЕЗ размытия
        importer.filterMode = FilterMode.Point;

        // Без сжатия
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // Без MipMaps
        importer.mipmapEnabled = false;

        // НЕ масштабировать
        importer.npotScale = TextureImporterNPOTScale.None;

        // Размер
        importer.maxTextureSize = 4096;

        // Альфа
        importer.alphaIsTransparency = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;

        // sRGB
        importer.sRGBTexture = true;

        // Без повторения
        importer.wrapMode = TextureWrapMode.Clamp;

        Debug.Log($"[PixelPerfect] {assetPath}");
    }
}