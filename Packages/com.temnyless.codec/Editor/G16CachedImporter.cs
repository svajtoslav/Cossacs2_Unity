#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace TemnyLessCodec.Editor
{
    [ScriptedImporter(2, "g16")]
    public class G16CachedImporter : ScriptedImporter
    {
        [Tooltip("Если включено — при импорте автоматически декодит и заполняет кэш.")]
        public bool decodeOnImport = true;

        [Tooltip("Прокидывается в декодер (если поддерживается).")]
        public bool doubleOverlay = false;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = ScriptableObject.CreateInstance<G16Asset>();
            asset.sourceAssetPath = ctx.assetPath;

            // Абсолютный путь к оригинальному файлу в Assets
            var srcAbs = TemnyLessCacheRuntime.ToAbsoluteFromAssetPath(ctx.assetPath);

            // SHA1 = ключ кэша
            string sha1 = "";
            try { sha1 = TemnyLessCacheRuntime.ComputeSha1OfFile(srcAbs); }
            catch (Exception ex)
            {
                Debug.LogError("[G16CachedImporter] SHA1 failed: " + ex.Message);
            }
            asset.sourceSha1 = sha1;

            // Кэш-папка
            var cacheDir = TemnyLessCacheEditor.MakeCacheDir("g16", string.IsNullOrEmpty(sha1) ? "nohash" : sha1);
            asset.cacheDirAbsolute = cacheDir;

            // Копия исходника в кэше (чтобы декодер писал рядом с ней)
            var fileName = Path.GetFileName(srcAbs);
            var cachedSrc = TemnyLessCacheEditor.CopySourceToCache(srcAbs, cacheDir, fileName);

            // Ожидаемые пути вывода декодера (рядом с cachedSrc)
            var basePath = Path.Combine(Path.GetDirectoryName(cachedSrc) ?? "", Path.GetFileNameWithoutExtension(cachedSrc));
            asset.logPathAbsolute = basePath + ".log.txt";
            asset.framesDirAbsolute = basePath + "_frames";

            if (decodeOnImport)
            {
                // Если кадры уже есть — не трогаем
                bool hasFrames = Directory.Exists(asset.framesDirAbsolute) &&
                                 Directory.GetFiles(asset.framesDirAbsolute, "*.tga").Length > 0;

                if (!hasFrames)
                {
                    // Декодим через фасад DLL: CodecFacade.DecodeG16ToLogAndFrames(...)
                    if (!TryDecodeG16(cachedSrc, out var logPath, out var err))
                    {
                        Debug.LogError("[G16CachedImporter] Decode failed: " + err);
                    }
                    else
                    {
                        // logPath будет в кэше
                        asset.logPathAbsolute = logPath;
                    }
                }

                // frameCount
                try
                {
                    if (Directory.Exists(asset.framesDirAbsolute))
                        asset.frameCount = Directory.GetFiles(asset.framesDirAbsolute, "*.tga").Length;
                }
                catch { /* ignore */ }
            }

            ctx.AddObjectToAsset("G16Asset", asset);
            ctx.SetMainObject(asset);
        }

        private bool TryDecodeG16(string absPath, out string logPath, out string err)
        {
            logPath = "";
            err = "";

            try
            {
                return CodecFacade.DecodeG16ToLogAndFrames(absPath, out logPath, out err, doubleOverlay);
            }
            catch (Exception ex)
            {
                err = ex.Message;
                return false;
            }
        }
    }
}
#endif
