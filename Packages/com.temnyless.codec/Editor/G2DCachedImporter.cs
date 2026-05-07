#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace TemnyLessCodec.Editor
{
    [ScriptedImporter(2, "g2d")]
    public class G2DCachedImporter : ScriptedImporter
    {
        public bool decodeOnImport = true;
        public bool doubleOverlay = false;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = ScriptableObject.CreateInstance<G2DAsset>();
            asset.sourceAssetPath = ctx.assetPath;

            var srcAbs = TemnyLessCacheRuntime.ToAbsoluteFromAssetPath(ctx.assetPath);

            string sha1 = "";
            try { sha1 = TemnyLessCacheRuntime.ComputeSha1OfFile(srcAbs); }
            catch (Exception ex)
            {
                Debug.LogError("[G2DCachedImporter] SHA1 failed: " + ex.Message);
            }
            asset.sourceSha1 = sha1;

            var cacheDir = TemnyLessCacheEditor.MakeCacheDir("g2d", string.IsNullOrEmpty(sha1) ? "nohash" : sha1);
            asset.cacheDirAbsolute = cacheDir;

            var fileName = Path.GetFileName(srcAbs);
            var cachedSrc = TemnyLessCacheEditor.CopySourceToCache(srcAbs, cacheDir, fileName);

            var basePath = Path.Combine(Path.GetDirectoryName(cachedSrc) ?? "", Path.GetFileNameWithoutExtension(cachedSrc));
            asset.logPathAbsolute = basePath + ".log.txt";
            asset.framesDirAbsolute = basePath + "_frames";

            if (decodeOnImport)
            {
                bool hasFrames = Directory.Exists(asset.framesDirAbsolute) &&
                                 Directory.GetFiles(asset.framesDirAbsolute, "*.tga").Length > 0;

                if (!hasFrames)
                {
                    if (!TryDecodeG2D(cachedSrc, out var logPath, out var err))
                        Debug.LogError("[G2DCachedImporter] Decode failed: " + err);
                    else
                        asset.logPathAbsolute = logPath;
                }

                try
                {
                    if (Directory.Exists(asset.framesDirAbsolute))
                        asset.frameCount = Directory.GetFiles(asset.framesDirAbsolute, "*.tga").Length;
                }
                catch { }
            }

            ctx.AddObjectToAsset("G2DAsset", asset);
            ctx.SetMainObject(asset);
        }

        private bool TryDecodeG2D(string absPath, out string logPath, out string err)
        {
            logPath = "";
            err = "";

            try
            {
                return MelinojaCodecBridge.DecodeG2DToLogAndFrames(absPath, out logPath, out err, doubleOverlay);
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
