using System;
using System.IO;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // COSSACKS2/UnSyncro.cpp + MapDiscr.h release build.
    // There is ONE process-wide randoma[8192]/rpos stream. Every subsystem that
    // calls rando() must consume from this same sequence; addrand(v) is a NO-OP
    // in the shipped/release macro configuration.
    internal static class C2RetailRandomV407LikeOriginal
    {
        private const int RandomCountLikeOriginal = 8192;
        private static readonly object SyncLikeOriginal = new object();
        private static short[] _randomaLikeOriginal;
        private static int _rposLikeOriginal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ResetLikeOriginal()
        {
            lock (SyncLikeOriginal)
            {
                _randomaLikeOriginal = null;
                _rposLikeOriginal = 0;
            }
        }

        internal static int Rando(C2NeutralPeasantUnitInfoV2LikeOriginal contextUnit = null)
        {
            lock (SyncLikeOriginal)
            {
                EnsureLoadedLikeOriginal(contextUnit);
                int result = _randomaLikeOriginal[_rposLikeOriginal];
                _rposLikeOriginal = (_rposLikeOriginal + 1) & (RandomCountLikeOriginal - 1);
                return result;
            }
        }

        // MapDiscr.h release: #define addrand(v)
        internal static void AddRand(int value)
        {
        }

        internal static int RPosLikeOriginal
        {
            get { lock (SyncLikeOriginal) { return _rposLikeOriginal; } }
        }

        private static void EnsureLoadedLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal contextUnit)
        {
            if (_randomaLikeOriginal != null && _randomaLikeOriginal.Length == RandomCountLikeOriginal)
                return;

            byte[] bytes = null;
            try
            {
                TextAsset resource = Resources.Load<TextAsset>("random");
                if (resource != null && resource.bytes != null && resource.bytes.Length >= RandomCountLikeOriginal * 2)
                    bytes = resource.bytes;
            }
            catch { }

            if (bytes == null)
            {
                string[] candidates = BuildCandidatesLikeOriginal(contextUnit);
                for (int i = 0; i < candidates.Length; i++)
                {
                    string path = candidates[i];
                    if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                    try
                    {
                        byte[] raw = File.ReadAllBytes(path);
                        if (raw.Length >= RandomCountLikeOriginal * 2)
                        {
                            bytes = raw;
                            break;
                        }
                    }
                    catch { }
                }
            }

            if (bytes == null || bytes.Length < RandomCountLikeOriginal * 2)
                throw new InvalidOperationException(
                    "COSSACKS2 random.lst is required for source-faithful rando() semantics (8192 shorts).");

            short[] table = new short[RandomCountLikeOriginal];
            for (int i = 0; i < RandomCountLikeOriginal; i++)
                table[i] = unchecked((short)(bytes[i * 2] | (bytes[i * 2 + 1] << 8)));
            _randomaLikeOriginal = table;
        }

        private static string[] BuildCandidatesLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal contextUnit)
        {
            string mdPath = string.Empty;
            try
            {
                if (contextUnit != null)
                    mdPath = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(contextUnit).Path ?? string.Empty;
            }
            catch { }

            string mdDir = string.IsNullOrEmpty(mdPath) ? string.Empty : Path.GetDirectoryName(mdPath);
            string dataRoot = string.IsNullOrEmpty(mdDir) ? string.Empty : Directory.GetParent(mdDir) != null
                ? Directory.GetParent(mdDir).FullName : mdDir;

            return new[]
            {
                Path.Combine(Application.dataPath, "Resources", "random.lst"),
                Path.Combine(Application.streamingAssetsPath ?? string.Empty, "random.lst"),
                Path.Combine(dataRoot ?? string.Empty, "random.lst"),
                Path.Combine(dataRoot ?? string.Empty, "Data", "random.lst"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "random.lst"),
                Path.Combine(Directory.GetCurrentDirectory(), "Data", "random.lst")
            };
        }
    }
}
