using UnityEditor;
using UnityEngine;

public class SliderTextureImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        // Применяем к текстурам слайдера
        if (assetPath.Contains("interf3_elements_slider_frames") ||
            assetPath.Contains("interf3_elements_checkbox_frames") ||
            assetPath.Contains("Buttons"))
        {
            TextureImporter importer = (TextureImporter)assetImporter;
            
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1f;  // ← КЛЮЧЕВОЕ!
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None; // ← НЕ масштабировать!
            importer.maxTextureSize = 2048;
            
            Debug.Log($"[SliderTextureImporter] Configured: {assetPath}");
        }
    }
}