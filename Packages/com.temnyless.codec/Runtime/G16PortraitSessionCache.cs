using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TemnyLessCodec;

public static class G16PortraitSessionCache
{
    private static readonly Dictionary<string, Sprite> _spriteCache =
        new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

    private static string[] _roots;

    public static void InitRoots()
    {
        // 1) Assets\Resources\Cash (в редакторе и в билде путь другой — поэтому через Application.dataPath)
        var a = Path.Combine(Application.dataPath, "Resources", "Cash");
        // 2) Game Cash
        var b = @"C:\GSC Game World\Cossacks II\Data\Cash";

        _roots = new[] { a, b };
    }

    public static void ClearSession()
    {
        _spriteCache.Clear();
        MelinojaCodecBridge.ClearG16Memory();
    }

    // nationCode: "EGs", "FRs", "RSs" и т.п.
    // frameIndex: id героя/портрета (обычно 0..N)
    public static bool TryGetPortrait(string nationCode, int frameIndex, out Sprite sprite, bool doubleOverlay = false)
    {
        sprite = null;
        if (_roots == null) InitRoots();

        string g16Name = $"Interf3_TotalWarGraph_lva_{nationCode}.g16";
        string g16Path = FindG16(g16Name);
        if (string.IsNullOrEmpty(g16Path))
        {
            Debug.LogWarning($"[PortraitCache] G16 not found: {g16Name}");
            return false;
        }

        string key = $"{g16Path}|{frameIndex}|{(doubleOverlay ? 1 : 0)}";
        if (_spriteCache.TryGetValue(key, out sprite))
            return true;

        if (!MelinojaCodecBridge.LoadG16ToMemory(g16Path, out var err, doubleOverlay))
        {
            Debug.LogError($"[PortraitCache] LoadG16ToMemory failed: {err}");
            return false;
        }

        if (!MelinojaCodecBridge.TryGetG16FrameRGBA(g16Path, frameIndex, out int w, out int h, out byte[] rgba, out var err2))
        {
            Debug.LogError($"[PortraitCache] GetFrame failed: {err2}");
            return false;
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
        tex.LoadRawTextureData(rgba);
        tex.Apply(false, true); // make texture non-readable to save RAM

        sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        _spriteCache[key] = sprite;
        return true;
    }

    private static string FindG16(string fileName)
    {
        foreach (var r in _roots)
        {
            try
            {
                if (!Directory.Exists(r)) continue;
                var p = Path.Combine(r, fileName);
                if (File.Exists(p)) return p;

                // fallback: case-insensitive scan (на всякий)
                var hit = Directory.GetFiles(r, fileName, SearchOption.TopDirectoryOnly);
                if (hit != null && hit.Length > 0) return hit[0];
            }
            catch { }
        }
        return null;
    }
}
