using UnityEditor;
using UnityEngine;
using System.IO;

public class ReimportAllUITextures : EditorWindow
{
    [MenuItem("Tools/Cossacks2/Reimport ALL TGA in Resources")]
    public static void ReimportAllTGA()
    {
        string resourcesPath = "Assets/Resources";
        
        if (!Directory.Exists(resourcesPath))
        {
            Debug.LogError($"[Reimport] Resources folder not found: {resourcesPath}");
            return;
        }

        // Находим ВСЕ TGA файлы
        var tgaFiles = Directory.GetFiles(resourcesPath, "*.tga", SearchOption.AllDirectories);
        var pngFiles = Directory.GetFiles(resourcesPath, "*.png", SearchOption.AllDirectories);

        int configured = 0;

        foreach (var file in tgaFiles)
        {
            if (ConfigureTexture(file.Replace("\\", "/")))
                configured++;
        }

        foreach (var file in pngFiles)
        {
            if (ConfigureTexture(file.Replace("\\", "/")))
                configured++;
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Reimport Complete",
            $"Configured {configured} textures for pixel-perfect.\n" +
            $"({tgaFiles.Length} TGA + {pngFiles.Length} PNG found)",
            "OK"
        );

        Debug.Log($"[Reimport] Done! {configured} textures configured.");
    }

    private static bool ConfigureTexture(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return false;

        bool needsUpdate = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            needsUpdate = true;
        }

        if (importer.spritePixelsPerUnit != 1f)
        {
            importer.spritePixelsPerUnit = 1f;
            needsUpdate = true;
        }

        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            needsUpdate = true;
        }

        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            needsUpdate = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            needsUpdate = true;
        }

        if (importer.npotScale != TextureImporterNPOTScale.None)
        {
            importer.npotScale = TextureImporterNPOTScale.None;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            importer.SaveAndReimport();
            return true;
        }

        return false;
    }
}