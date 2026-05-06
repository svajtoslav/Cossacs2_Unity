// C2NeutralPeasantUnitsLikeOriginal.cs
// V19: widens V18 from UnitKri-only to saved Unit* records through original NDS->MD aliases,
// caches MD directional visuals, and keeps original-style ground selection markers.
// Reads saved 3INU/UNI3 unit records, resolves MD/USERLC/G2D frames,
// places peasants by original RealX/RealY, animates stand frames when present,
// selects by pixel alpha (CheckCoorInGP-like), and draws an MD SELTYPE/SELSHIFT marker.
// Does not touch buildings, walls, trees, water, roads or settlement houses.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const bool C2NeutralPeasantUnitsV2EnabledLikeOriginal = true;
        private const string C2NeutralPeasantUnitsV2ContractLikeOriginal =
            "V33_CONTINUOUS_XZ_NO_PARITY_JUMP_SMOOTH_Y";
        private const string C2NeutralPeasantUnitsV2RootPrefixLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V33_";
        private const string C2NeutralPeasantUnitsV1RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V1_";
        private const string C2NeutralPeasantUnitsV2RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V2_";
        private const string C2NeutralPeasantUnitsV3RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V3_";
        private const string C2NeutralPeasantUnitsV4RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V4_";
        private const string C2NeutralPeasantUnitsV5RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V5_";
        private const string C2NeutralPeasantUnitsV6RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V6_";
        private const string C2NeutralPeasantUnitsV7RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V7_";
        private const string C2NeutralPeasantUnitsV11RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V11_";
        private const string C2NeutralPeasantUnitsV13RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V13_";
        private const string C2NeutralPeasantUnitsV14RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V14_";
        private const string C2NeutralPeasantUnitsV15RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V15_";
        private const string C2NeutralPeasantUnitsV16RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V16_";
        private const string C2NeutralPeasantUnitsV17RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V17_";
        private const string C2NeutralPeasantUnitsV18RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V18_";
        private const string C2NeutralPeasantUnitsV19RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V19_";
        private const string C2NeutralPeasantUnitsV20RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V20_";
        private const string C2NeutralPeasantUnitsV21RootPrefixForCleanupLikeOriginal =
            "C2_NeutralPeasantUnits_3INU_MD_G2D_V21_";
        private const int C2NeutralPeasantUnitsV2RenderQueueLikeOriginal = 3670;
        private const float C2NeutralPeasantUnitsV2YOffsetLikeOriginal = 0.055f;
        private const float C2NeutralPeasantUnitsV2AlphaRefLikeOriginal = 4.0f / 255.0f;
        private const float C2NeutralPeasantUnitsV2PickAlphaBiasLikeOriginal = 4.0f / 255.0f;
        private const float C2NeutralPeasantUnitsV2IdleFpsLikeOriginal = 4.5f;
        private const float C2NeutralPeasantUnitsV2RestPauseMinSecondsLikeOriginal = 0.35f;
        private const float C2NeutralPeasantUnitsV2RestPauseMaxSecondsLikeOriginal = 1.25f;
        private const float C2NeutralPeasantUnitsV2MoveSpeedWorldPerSecondLikeOriginal = 7.5f;
        public const float C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal = 42.0f;
        public const float C2NeutralPeasantUnitsV2TurnSpeedDirUnitsPerSecondLikeOriginal = 96.0f;
        private const float C2NeutralPeasantUnitsV2WalkFpsLikeOriginal = 6.0f; // fallback only; normal walk is TotalPath/RInFrame in V30
        private const bool C2NeutralPeasantUnitsV2UseRestAsIdleAnimationLikeOriginal = true;
        private const int C2NeutralPeasantUnitsV2MaxIdleFramesLikeOriginal = 144;
        private const int C2NeutralPeasantUnitsV2MaxWalkFramesLikeOriginal = 64;
        private static readonly Dictionary<string, Texture2D> C2NeutralPeasantUnitsV2TextureCacheLikeOriginal = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2NeutralPeasantUnitVisualCacheEntryV19LikeOriginal> C2NeutralPeasantUnitsV19VisualCacheLikeOriginal =
            new Dictionary<string, C2NeutralPeasantUnitVisualCacheEntryV19LikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2NeutralPeasantUnitFrameV2LikeOriginal[][]> C2NeutralPeasantUnitsV19WalkBankCacheLikeOriginal =
            new Dictionary<string, C2NeutralPeasantUnitFrameV2LikeOriginal[][]>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2NeutralPeasantUnitMotionBanksV20LikeOriginal> C2NeutralPeasantUnitsV20MotionBankCacheLikeOriginal =
            new Dictionary<string, C2NeutralPeasantUnitMotionBanksV20LikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> C2NeutralPeasantUnitsV27FramesPerDirCacheLikeOriginal =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2NeutralPeasantUnitFrameV2LikeOriginal[][]> C2NeutralPeasantUnitsV23IdleBankCacheLikeOriginal =
            new Dictionary<string, C2NeutralPeasantUnitFrameV2LikeOriginal[][]>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2NeutralPeasantUnitFrameV2LikeOriginal[][]> C2NeutralPeasantUnitsV30RestBankCacheLikeOriginal =
            new Dictionary<string, C2NeutralPeasantUnitFrameV2LikeOriginal[][]>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, C2NeutralPeasantUnitsV2SelectionMdInfoLikeOriginal> C2NeutralPeasantUnitsV19SelectionCacheLikeOriginal =
            new Dictionary<string, C2NeutralPeasantUnitsV2SelectionMdInfoLikeOriginal>(StringComparer.OrdinalIgnoreCase);
        private const bool C2NeutralPeasantUnitsV2BillboardToCameraLikeOriginal = true;
        // Unity adapter safety: original sprites are projected into the engine z-buffer,
        // but our current sprite plane is a billboard. LEqual can cut peasants into terrain.
        // Keep Always and use original Y-line sort key until full DrawWorldSprite depth is ported.
        private const bool C2NeutralPeasantUnitsV2UseUnitySafeZTestAlwaysLikeOriginal = true;
        private const bool C2NeutralPeasantUnitsV2DrawDebugLabelsLikeOriginal = false;

        // Optional manual override from Inspector/debug scripts.
        public string C2NeutralPeasantUnitsV2MapPathOverride = "";

        private sealed class C2NeutralPeasantUnitVisualCacheEntryV19LikeOriginal
        {
            public C2NeutralPeasantUnitFrameV2LikeOriginal[] IdleFrames;
            public C2NeutralPeasantUnitFrameV2LikeOriginal[] WalkFrames;
            public bool WalkAnimFound;
            public string FramesAudit;
            public string WalkAudit;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void C2NeutralPeasantUnitsV2AutoInstallLikeOriginal()
        {
            if (!C2NeutralPeasantUnitsV2EnabledLikeOriginal) return;

            C2NeutralPeasantUnitsV2DestroySelectionOverlaysV18LikeOriginal();

            var existing = UnityEngine.Object.FindObjectOfType<C2NeutralPeasantUnitsV2AutoRunner>();
            GameObject go;

            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject("C2_NeutralPeasantUnits_3INU_MD_G2D_V33_AutoRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                go.AddComponent<C2NeutralPeasantUnitsV2AutoRunner>();
            }

            // V16: do not install the picker/overlay in the main menu. The picker is installed
            // only after a battle terrain mode and parsed map are ready. This prevents the
            // yellow drag rectangle from living in DontDestroyOnLoad and covering menu UI.
        }

        private static void C2NeutralPeasantUnitsV2EnsurePickerInstalledLikeOriginal(GameObject host, string source)
        {
            // V8: singleton guard. V4 could attach a new picker every auto-runner tick,
            // producing dozens of identical pick/miss logs per one click.
            var existingPicker = C2NeutralPeasantUnitPickerV2LikeOriginal.Active;
            if (existingPicker == null && host != null)
                existingPicker = host.GetComponent<C2NeutralPeasantUnitPickerV2LikeOriginal>();
            if (existingPicker == null)
                existingPicker = UnityEngine.Object.FindObjectOfType<C2NeutralPeasantUnitPickerV2LikeOriginal>();

            if (existingPicker != null)
                return;

            if (host == null)
            {
                host = new GameObject("C2_NeutralPeasantUnits_3INU_MD_G2D_V22_PickerHost");
                UnityEngine.Object.DontDestroyOnLoad(host);
                host.hideFlags = HideFlags.HideAndDontSave;
            }

            host.AddComponent<C2NeutralPeasantUnitPickerV2LikeOriginal>();
            Debug.Log("[C2:NEUTRAL PEASANT PICKER V19 AUTOINSTALL] source=" + source +
                      " status=added host='" + host.name + "'");
        }

        private sealed class C2NeutralPeasantUnitsV2AutoRunner : MonoBehaviour
        {
            private C2BattleTerrainMode _lastMode;
            private string _lastMap;
            private float _nextTick;
            private int _waitLogs;

            private void Update()
            {
                if (Time.realtimeSinceStartup < _nextTick) return;
                _nextTick = Time.realtimeSinceStartup + 0.5f;

                var mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
                if (mode == null) return;

                string map = mode.C2NeutralPeasantUnitsV2TryGetCurrentMapPathLikeOriginal();
                bool mapObjectReady = mode._map != null;

                if (string.IsNullOrWhiteSpace(map) || !mapObjectReady)
                {
                    if (_waitLogs < 12)
                    {
                        _waitLogs++;
                        Debug.Log("[C2:NEUTRAL PEASANT UNITS V23 WAIT] mapPath='" +
                                  (map ?? "<null>") + "' mapObjectReady=" + mapObjectReady +
                                  " hint=waiting for terrain parser / _mapRelativePath");
                    }
                    C2NeutralPeasantUnitsV2DestroySelectionOverlaysV18LikeOriginal();
                    return;
                }

                C2NeutralPeasantUnitsV2EnsurePickerInstalledLikeOriginal(gameObject, "battle-map-ready");

                if (_lastMode == mode && string.Equals(_lastMap, map, StringComparison.OrdinalIgnoreCase))
                    return;

                _lastMode = mode;
                _lastMap = map;
                mode.BuildNeutralPeasantUnitsFrom3InuMdV2LikeOriginal(map, "auto-runner-v2");
            }
        }

        private static void C2NeutralPeasantUnitsV2DestroySelectionOverlaysV18LikeOriginal()
        {
            string[] prefixes =
            {
                "C2_NeutralPeasantUnits_V",
                "C2_NeutralPeasant_SelectionRectPixel",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V14_PickerHost",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V15_PickerHost",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V16_PickerHost",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V17_PickerHost",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V18_PickerHost",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V33_PickerHost",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V24_PickerHost",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V20_PickerHost",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V21_PickerHost",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V22_PickerHost",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V23_PickerHost",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V25_PickerHost",
                "C2_NeutralPeasantUnits_3INU_MD_G2D_V33_PickerHost",
            };

            GameObject[] all = UnityEngine.Object.FindObjectsOfType<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                GameObject go = all[i];
                if (go == null) continue;
                string n = go.name ?? string.Empty;
                for (int p = 0; p < prefixes.Length; p++)
                {
                    if (n.StartsWith(prefixes[p], StringComparison.OrdinalIgnoreCase))
                    {
                        SafeDestroy(go);
                        break;
                    }
                }
            }
        }

        public void BuildNeutralPeasantUnitsFrom3InuMdV2LikeOriginal(string mapPath, string source = "manual")
        {
            if (!C2NeutralPeasantUnitsV2EnabledLikeOriginal) return;
            C2NeutralPeasantUnitsV2ClearOldRootsLikeOriginal();

            if (_map == null)
            {
                Debug.LogWarning("[C2:NEUTRAL PEASANT UNITS V33] parsed terrain map object is not ready; skip source=" +
                                 source + " map='" + (mapPath ?? "<null>") + "'");
                return;
            }

            string abs = C2Settlement3InuMdV2ResolveMapPathLikeOriginal(mapPath);
            if (string.IsNullOrEmpty(abs) || !File.Exists(abs))
            {
                Debug.LogWarning("[C2:NEUTRAL PEASANT UNITS V33] map not found: " + (mapPath ?? "<null>"));
                return;
            }

            List<C2Settlement3InuMdV2Record> records;
            string chunkAudit;
            if (!C2Settlement3InuMdV2TryParseRecordsLikeOriginal(abs, out records, out chunkAudit))
            {
                Debug.LogWarning("[C2:NEUTRAL PEASANT UNITS V33] no 3INU/UNI3 records map='" +
                                 mapPath + "' audit=" + chunkAudit);
                return;
            }

            var root = new GameObject(C2NeutralPeasantUnitsV2RootPrefixLikeOriginal + Path.GetFileNameWithoutExtension(abs));
            root.transform.SetParent(transform, true);

            int candidates = 0;
            int mdFound = 0;
            int mdMissing = 0;
            int visualFound = 0;
            int visualMissing = 0;
            int drawn = 0;
            int skippedNonPeasant = 0;
            int skippedNoAlias = 0;
            int idleFramesTotal = 0;
            int walkFramesTotal = 0;
            int walkAnimFound = 0;
            int walkAnimMissing = 0;
            int selectionMdFound = 0;
            int visualCacheHits = 0;
            int visualCacheMisses = 0;
            int walkBankCacheHits = 0;
            int walkBankCacheMisses = 0;
            var sample = new List<string>();
            var mdMiss = new List<string>();
            var visMiss = new List<string>();
            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < records.Count; i++)
            {
                C2Settlement3InuMdV2Record r = records[i];
                C2Settlement3InuMdV2Count(nameCounts, r.MonsterId);

                C2Settlement3InuMdV2Info md = C2Settlement3InuMdV2ResolveMdLikeOriginal(r.MonsterId);
                C2Settlement3InuMdV2Kind kind = md != null && md.Found
                    ? md.Kind
                    : C2Settlement3InuMdV2GuessKindFromNameLikeOriginal(r.MonsterId);

                if (!C2NeutralPeasantUnitsV2IsSavedUnitRecordLikeOriginal(r, md, kind))
                {
                    skippedNonPeasant++;
                    continue;
                }

                candidates++;

                string alias = C2NeutralPeasantUnitsV2ResolvedMdAliasLikeOriginal(r, md);
                if (md == null || !md.Found)
                {
                    mdMissing++;
                    if (mdMiss.Count < 24) mdMiss.Add("#" + r.Index.ToString(CultureInfo.InvariantCulture) + " " + (r.MonsterId ?? "") + " kind=" + kind + " -> " + (alias ?? "<no_md>"));
                    continue;
                }
                mdFound++;

                List<C2NeutralPeasantUnitFrameV2LikeOriginal> idleFrames;
                string framesAudit;
                List<C2NeutralPeasantUnitFrameV2LikeOriginal> walkFrames;
                string walkAudit;
                bool visualCacheHit;
                bool walkFoundForRecord;
                if (!C2NeutralPeasantUnitsV19TryGetOrBuildVisualFramesLikeOriginal(
                        md,
                        r,
                        out idleFrames,
                        out walkFrames,
                        out framesAudit,
                        out walkAudit,
                        out walkFoundForRecord,
                        out visualCacheHit) ||
                    idleFrames == null || idleFrames.Count == 0)
                {
                    visualMissing++;
                    if (visMiss.Count < 24) visMiss.Add("#" + r.Index.ToString(CultureInfo.InvariantCulture) + " " + (r.MonsterId ?? "") + " -> " + alias + " " + framesAudit + " | " + walkAudit);
                    continue;
                }

                if (visualCacheHit) visualCacheHits++; else visualCacheMisses++;

                if (walkFoundForRecord && walkFrames != null && walkFrames.Count > 0)
                {
                    walkAnimFound++;
                    walkFramesTotal += walkFrames.Count;
                }
                else walkAnimMissing++;

                C2NeutralPeasantUnitFrameV2LikeOriginal[][] walkDirectionBanks;
                string walkBankAudit;
                bool walkBankCacheHit;
                C2NeutralPeasantUnitsV19GetOrBuildWalkDirectionBanksLikeOriginal(md, r, out walkDirectionBanks, out walkBankAudit, out walkBankCacheHit);
                if (walkBankCacheHit) walkBankCacheHits++; else walkBankCacheMisses++;

                C2NeutralPeasantUnitMotionBanksV20LikeOriginal motionBanks;
                string motionBankAudit;
                bool motionBankCacheHit;
                C2NeutralPeasantUnitsV20GetOrBuildMotionBanksLikeOriginal(md, out motionBanks, out motionBankAudit, out motionBankCacheHit);

                C2NeutralPeasantUnitFrameV2LikeOriginal[][] idleDirectionBanks;
                string idleBankAudit;
                bool idleBankCacheHit;
                C2NeutralPeasantUnitsV23GetOrBuildIdleDirectionBanksLikeOriginal(md, out idleDirectionBanks, out idleBankAudit, out idleBankCacheHit);

                C2NeutralPeasantUnitFrameV2LikeOriginal[][] restDirectionBanks;
                string restBankAudit;
                bool restBankCacheHit;
                C2NeutralPeasantUnitsV30GetOrBuildRestDirectionBanksLikeOriginal(md, out restDirectionBanks, out restBankAudit, out restBankCacheHit);

                visualFound++;
                idleFramesTotal += idleFrames.Count;

                C2NeutralPeasantUnitsV2SelectionMdInfoLikeOriginal selInfo = C2NeutralPeasantUnitsV19GetSelectionInfoLikeOriginal(md);
                if (selInfo.HasSelType) selectionMdFound++;

                C2NeutralPeasantUnitsV2CreateUnitObjectLikeOriginal(root.transform, r, md, idleFrames, idleDirectionBanks, restDirectionBanks, walkFrames, walkDirectionBanks, motionBanks, selInfo, alias, framesAudit + " | " + walkAudit + " | " + walkBankAudit + " | " + motionBankAudit + " | " + idleBankAudit + " | " + restBankAudit);
                drawn++;

                if (sample.Count < 48)
                {
                    C2NeutralPeasantUnitFrameV2LikeOriginal f0 = idleFrames[0];
                    sample.Add("#" + r.Index.ToString(CultureInfo.InvariantCulture) +
                               " name='" + (r.MonsterId ?? "") + "'" +
                               " alias=" + alias +
                               " real=(" + r.RealX.ToString(CultureInfo.InvariantCulture) + "," + r.RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                               " map=(" + (r.RealX >> 4).ToString(CultureInfo.InvariantCulture) + "," + (r.RealY >> 4).ToString(CultureInfo.InvariantCulture) + ")" +
                               " dir=" + r.RealDir.ToString(CultureInfo.InvariantCulture) +
                               " idleFrames=" + idleFrames.Count.ToString(CultureInfo.InvariantCulture) +
                               " walkFrames=" + (walkFrames != null ? walkFrames.Count : 0).ToString(CultureInfo.InvariantCulture) +
                               " fileRef=" + f0.FileRef.ToString(CultureInfo.InvariantCulture) +
                               " sprite=" + f0.ExactSprite.ToString(CultureInfo.InvariantCulture) +
                               " mirror=" + f0.MirrorX +
                               " sel=" + selInfo.Audit +
                               " " + f0.DirectionAudit +
                               " motionBankAudit=[" + motionBankAudit + "]" +
                               " idleBankAudit=[" + idleBankAudit + "]");
                }
            }

            Debug.Log("[C2:NEUTRAL PEASANT UNITS V33] contract=" + C2NeutralPeasantUnitsV2ContractLikeOriginal +
                      " source=" + source +
                      " map='" + mapPath + "'" +
                      " records=" + records.Count.ToString(CultureInfo.InvariantCulture) +
                      " candidates=" + candidates.ToString(CultureInfo.InvariantCulture) +
                      " mdFound=" + mdFound.ToString(CultureInfo.InvariantCulture) +
                      " mdMissing=" + mdMissing.ToString(CultureInfo.InvariantCulture) +
                      " visualFound=" + visualFound.ToString(CultureInfo.InvariantCulture) +
                      " visualMissing=" + visualMissing.ToString(CultureInfo.InvariantCulture) +
                      " drawn=" + drawn.ToString(CultureInfo.InvariantCulture) +
                      " idleFramesTotal=" + idleFramesTotal.ToString(CultureInfo.InvariantCulture) +
                      " walkFramesTotal=" + walkFramesTotal.ToString(CultureInfo.InvariantCulture) +
                      " walkAnimFound=" + walkAnimFound.ToString(CultureInfo.InvariantCulture) +
                      " walkAnimMissing=" + walkAnimMissing.ToString(CultureInfo.InvariantCulture) +
                      " selectionMdFound=" + selectionMdFound.ToString(CultureInfo.InvariantCulture) +
                      " visualCache=" + visualCacheHits.ToString(CultureInfo.InvariantCulture) + "/" + visualCacheMisses.ToString(CultureInfo.InvariantCulture) +
                      " walkBankCache=" + walkBankCacheHits.ToString(CultureInfo.InvariantCulture) + "/" + walkBankCacheMisses.ToString(CultureInfo.InvariantCulture) +
                      " skippedNonPeasant=" + skippedNonPeasant.ToString(CultureInfo.InvariantCulture) +
                      " skippedNoAlias=" + skippedNoAlias.ToString(CultureInfo.InvariantCulture) +
                      " chunkAudit=" + chunkAudit +
                      " allNames=" + C2Settlement3InuMdV2TopNamesLikeOriginal(nameCounts, 40));

            if (sample.Count > 0) Debug.Log("[C2:NEUTRAL PEASANT UNITS V33 SAMPLE] " + string.Join(" | ", sample.ToArray()));
            if (mdMiss.Count > 0) Debug.LogWarning("[C2:NEUTRAL PEASANT UNITS V33 MD MISS] " + string.Join(" | ", mdMiss.ToArray()));
            if (visMiss.Count > 0) Debug.LogWarning("[C2:NEUTRAL PEASANT UNITS V33 VISUAL MISS] " + string.Join(" | ", visMiss.ToArray()));
        }

        private string C2NeutralPeasantUnitsV2TryGetCurrentMapPathLikeOriginal()
        {
            if (!string.IsNullOrWhiteSpace(C2NeutralPeasantUnitsV2MapPathOverride))
                return C2NeutralPeasantUnitsV2MapPathOverride.Trim();

            return TryGetCurrentMapPathForSettlement3InuMdV2LikeOriginal();
        }

        private void C2NeutralPeasantUnitsV2ClearOldRootsLikeOriginal()
        {
            var gos = UnityEngine.Object.FindObjectsOfType<GameObject>();
            for (int i = 0; i < gos.Length; i++)
            {
                var go = gos[i];
                if (go == null) continue;

                // Do not delete the V8 hidden auto-runner/picker host while rebuilding unit roots.
                if (go.GetComponent<C2NeutralPeasantUnitsV2AutoRunner>() != null ||
                    go.GetComponent<C2NeutralPeasantUnitPickerV2LikeOriginal>() != null)
                    continue;

                if (go.name.StartsWith(C2NeutralPeasantUnitsV2RootPrefixLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V23_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V22_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V21_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V20_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV17RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV18RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV19RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV20RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV21RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V32_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV16RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV15RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV14RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV13RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV11RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V29_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V28_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V27_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V26_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V25_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V24_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V23_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V10_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V9_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith("C2_NeutralPeasantUnits_3INU_MD_G2D_V8_", StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV7RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV6RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV5RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV4RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV3RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV2RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase) ||
                    go.name.StartsWith(C2NeutralPeasantUnitsV1RootPrefixForCleanupLikeOriginal, StringComparison.OrdinalIgnoreCase))
                    SafeDestroy(go);
            }
        }

        private static bool C2NeutralPeasantUnitsV2IsSavedUnitRecordLikeOriginal(
            C2Settlement3InuMdV2Record r,
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Kind kind)
        {
            string baseName = C2NeutralPeasantUnitsV2BaseNameLikeOriginal(r.MonsterId);
            if (string.IsNullOrEmpty(baseName)) return false;

            // Original saved UnitKri/UnitFuz/etc are logical nation IDs. Their real MD is resolved
            // through *.NDS [MEMBERS], the same path used by NewMonster after LoadAllNations.
            if (kind == C2Settlement3InuMdV2Kind.Unit) return true;

            // Some unit MDs are not obvious from the final parsed Kind but saved IDs still carry Unit*.
            // Keep this as a narrow saved-map guard; buildings like Bld* and houses are handled elsewhere.
            return baseName.StartsWith("Unit", StringComparison.OrdinalIgnoreCase);
        }

        private static string C2NeutralPeasantUnitsV2ResolvedMdAliasLikeOriginal(C2Settlement3InuMdV2Record r, C2Settlement3InuMdV2Info md)
        {
            if (md != null && md.Found && !string.IsNullOrEmpty(md.MdPath))
                return Path.GetFileNameWithoutExtension(md.MdPath);
            return C2Settlement3InuMdV2ResolveNdsMdAliasV50LikeOriginal(r.MonsterId);
        }

        private static string C2NeutralPeasantUnitsV2BaseNameLikeOriginal(string monsterId)
        {
            string raw = (monsterId ?? string.Empty).Trim();
            int p = raw.IndexOf('(');
            return p > 0 ? raw.Substring(0, p).Trim() : raw;
        }

        private static string C2NeutralPeasantUnitsV2SuffixLikeOriginal(string monsterId)
        {
            string raw = (monsterId ?? string.Empty).Trim();
            int p0 = raw.IndexOf('(');
            int p1 = raw.IndexOf(')');
            if (p0 >= 0 && p1 > p0) return raw.Substring(p0 + 1, p1 - p0 - 1).Trim();
            return string.Empty;
        }

        private bool C2NeutralPeasantUnitsV2TryBuildIdleFramesLikeOriginal(
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Record r,
            out List<C2NeutralPeasantUnitFrameV2LikeOriginal> frames,
            out string audit)
        {
            frames = new List<C2NeutralPeasantUnitFrameV2LikeOriginal>();
            audit = "";

            string idleAnimName;
            int restFrameCount;
            int idleRotations;
            List<C2Settlement3InuMdV2AnimFrame> sourceFrames =
                C2NeutralPeasantUnitsV2SelectIdleFramesLikeOriginal(md, out idleAnimName, out restFrameCount, out idleRotations);

            if (md == null || sourceFrames == null || sourceFrames.Count == 0)
            {
                audit = "no_md_idle_frames";
                return false;
            }

            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Settlement3InuMdV2SpriteScaleCompensator;
            int max = Math.Min(sourceFrames.Count, C2NeutralPeasantUnitsV2MaxIdleFramesLikeOriginal);
            var frameAudits = new List<string>();

            for (int i = 0; i < max; i++)
            {
                C2Settlement3InuMdV2AnimFrame baseFrame = sourceFrames[i];
                int framesPerDirection = C2NeutralPeasantUnitsV27FramesPerDirectionForFrameLikeOriginal(md, sourceFrames, baseFrame, sourceFrames.Count);

                C2Settlement3InuMdV2AnimFrame resolvedFrame;
                int exactSprite;
                bool mirrorX;
                string dirAudit;
                if (!C2NeutralPeasantUnitsV2BuildDirectionalFrameLikeOriginal(md, r, idleRotations, framesPerDirection, baseFrame, out resolvedFrame, out exactSprite, out mirrorX, out dirAudit))
                {
                    if (frameAudits.Count < 12) frameAudits.Add("DIR_MISS#" + i.ToString(CultureInfo.InvariantCulture) + ":" + dirAudit);
                    continue;
                }

                Texture2D tex;
                string visualAudit;
                bool ok = C2NeutralPeasantUnitsV2TryLoadSpecificFrameCachedLikeOriginal(
                    md,
                    resolvedFrame,
                    C2Settlement3InuMdV2Kind.Unit,
                    out tex,
                    out visualAudit);

                if (!ok || tex == null)
                {
                    if (frameAudits.Count < 12)
                    {
                        frameAudits.Add("VIS_MISS#" + i.ToString(CultureInfo.InvariantCulture) +
                                        " fileRef=" + baseFrame.FileRef.ToString(CultureInfo.InvariantCulture) +
                                        " sprite=" + exactSprite.ToString(CultureInfo.InvariantCulture) +
                                        " " + visualAudit);
                    }
                    continue;
                }

                tex = C2Settlement3InuMdV2PrepareLoadedTextureLikeOriginal(tex);
                if (tex == null) continue;

                int dx;
                int dy;
                C2Settlement3InuMdV2FramePivotLikeOriginal(md, baseFrame, out dx, out dy);

                int w = Mathf.Max(1, tex.width);
                int h = Mathf.Max(1, tex.height);

                float lx = dx * s;
                float rx = (dx + w) * s;
                float ty = -dy * s;
                float by = -(dy + h) * s;

                if (mirrorX)
                {
                    lx = -(dx + w) * s;
                    rx = -dx * s;
                }

                var f = new C2NeutralPeasantUnitFrameV2LikeOriginal();
                f.Texture = tex;
                f.FileRef = baseFrame.FileRef;
                f.BaseSprite = baseFrame.SpriteId;
                f.ExactSprite = exactSprite;
                f.MirrorX = mirrorX;
                f.PivotDx = dx;
                f.PivotDy = dy;
                f.Width = w;
                f.Height = h;
                f.Lx = lx;
                f.Rx = rx;
                f.By = by;
                f.Ty = ty;
                f.DirectionAudit = dirAudit;
                f.VisualAudit = visualAudit;
                frames.Add(f);

                if (frameAudits.Count < 12)
                    frameAudits.Add("OK#" + i.ToString(CultureInfo.InvariantCulture) +
                                    " fileRef=" + baseFrame.FileRef.ToString(CultureInfo.InvariantCulture) +
                                    " baseSprite=" + baseFrame.SpriteId.ToString(CultureInfo.InvariantCulture) +
                                    " exact=" + exactSprite.ToString(CultureInfo.InvariantCulture) +
                                    " mirror=" + mirrorX);
            }

            C2NeutralPeasantUnitsV2StabilizeFrameFootAnchorV15LikeOriginal(frames);

            audit = "idleAnim=" + idleAnimName +
                    " stableFoot=1" +
                    " sourceFrames=" + sourceFrames.Count.ToString(CultureInfo.InvariantCulture) +
                    " standFrames=" + (md.StandLoFrames != null ? md.StandLoFrames.Count : 0).ToString(CultureInfo.InvariantCulture) +
                    " restFrames=" + restFrameCount.ToString(CultureInfo.InvariantCulture) +
                    " loaded=" + frames.Count.ToString(CultureInfo.InvariantCulture) +
                    " max=" + max.ToString(CultureInfo.InvariantCulture) +
                    " animRot=" + idleRotations.ToString(CultureInfo.InvariantCulture) +
                    " mdRot=" + (md != null ? md.Rotations : 0).ToString(CultureInfo.InvariantCulture) +
                    " cache=" + C2NeutralPeasantUnitsV2TextureCacheLikeOriginal.Count.ToString(CultureInfo.InvariantCulture) +
                    " " + string.Join(" || ", frameAudits.ToArray());

            return frames.Count > 0;
        }


        private bool C2NeutralPeasantUnitsV2TryBuildWalkFramesLikeOriginal(
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Record r,
            out List<C2NeutralPeasantUnitFrameV2LikeOriginal> frames,
            out string audit)
        {
            frames = new List<C2NeutralPeasantUnitFrameV2LikeOriginal>();
            audit = "";

            C2Settlement3InuMdV2Animation motion = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, "#MOTION_L");
            string motionName = "#MOTION_L";
            if (motion == null || motion.Frames == null || motion.Frames.Count == 0)
            {
                motion = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, "#MOTION_L0");
                motionName = "#MOTION_L0";
            }

            if (md == null || motion == null || motion.Frames == null || motion.Frames.Count == 0)
            {
                audit = "walkAnim=missing #MOTION_L/#MOTION_L0";
                return false;
            }

            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Settlement3InuMdV2SpriteScaleCompensator;
            int max = Math.Min(motion.Frames.Count, C2NeutralPeasantUnitsV2MaxWalkFramesLikeOriginal);
            var frameAudits = new List<string>();

            for (int i = 0; i < max; i++)
            {
                C2Settlement3InuMdV2AnimFrame baseFrame = motion.Frames[i];
                int framesPerDirection = C2NeutralPeasantUnitsV27FramesPerDirectionForFrameLikeOriginal(md, motion.Frames, baseFrame, motion.Frames.Count);

                C2Settlement3InuMdV2AnimFrame resolvedFrame;
                int exactSprite;
                bool mirrorX;
                string dirAudit;
                if (!C2NeutralPeasantUnitsV2BuildDirectionalFrameLikeOriginal(md, r, Math.Max(1, motion.Rotations), framesPerDirection, baseFrame, out resolvedFrame, out exactSprite, out mirrorX, out dirAudit))
                {
                    if (frameAudits.Count < 12) frameAudits.Add("DIR_MISS#" + i.ToString(CultureInfo.InvariantCulture) + ":" + dirAudit);
                    continue;
                }

                Texture2D tex;
                string visualAudit;
                bool ok = C2NeutralPeasantUnitsV2TryLoadSpecificFrameCachedLikeOriginal(
                    md,
                    resolvedFrame,
                    C2Settlement3InuMdV2Kind.Unit,
                    out tex,
                    out visualAudit);

                if (!ok || tex == null)
                {
                    if (frameAudits.Count < 12)
                    {
                        frameAudits.Add("VIS_MISS#" + i.ToString(CultureInfo.InvariantCulture) +
                                        " fileRef=" + baseFrame.FileRef.ToString(CultureInfo.InvariantCulture) +
                                        " sprite=" + exactSprite.ToString(CultureInfo.InvariantCulture) +
                                        " " + visualAudit);
                    }
                    continue;
                }

                tex = C2Settlement3InuMdV2PrepareLoadedTextureLikeOriginal(tex);
                if (tex == null) continue;

                int dx;
                int dy;
                C2Settlement3InuMdV2FramePivotLikeOriginal(md, baseFrame, out dx, out dy);

                int w = Mathf.Max(1, tex.width);
                int h = Mathf.Max(1, tex.height);

                float lx = dx * s;
                float rx = (dx + w) * s;
                float ty = -dy * s;
                float by = -(dy + h) * s;

                if (mirrorX)
                {
                    lx = -(dx + w) * s;
                    rx = -dx * s;
                }

                var f = new C2NeutralPeasantUnitFrameV2LikeOriginal();
                f.Texture = tex;
                f.FileRef = baseFrame.FileRef;
                f.BaseSprite = baseFrame.SpriteId;
                f.ExactSprite = exactSprite;
                f.MirrorX = mirrorX;
                f.PivotDx = dx;
                f.PivotDy = dy;
                f.Width = w;
                f.Height = h;
                f.Lx = lx;
                f.Rx = rx;
                f.By = by;
                f.Ty = ty;
                f.DirectionAudit = dirAudit;
                f.VisualAudit = visualAudit;
                frames.Add(f);

                if (frameAudits.Count < 12)
                    frameAudits.Add("OK#" + i.ToString(CultureInfo.InvariantCulture) +
                                    " fileRef=" + baseFrame.FileRef.ToString(CultureInfo.InvariantCulture) +
                                    " baseSprite=" + baseFrame.SpriteId.ToString(CultureInfo.InvariantCulture) +
                                    " exact=" + exactSprite.ToString(CultureInfo.InvariantCulture) +
                                    " mirror=" + mirrorX);
            }

            C2NeutralPeasantUnitsV2StabilizeFrameFootAnchorV15LikeOriginal(frames);

            audit = "walkAnim=" + motionName +
                    " stableFoot=1" +
                    " sourceFrames=" + motion.Frames.Count.ToString(CultureInfo.InvariantCulture) +
                    " loaded=" + frames.Count.ToString(CultureInfo.InvariantCulture) +
                    " max=" + max.ToString(CultureInfo.InvariantCulture) +
                    " walkFps=" + C2NeutralPeasantUnitsV2WalkFpsLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                    " " + string.Join(" || ", frameAudits.ToArray());

            return frames.Count > 0;
        }


        private bool C2NeutralPeasantUnitsV2TryBuildWalkFramesForDirLikeOriginal(
            C2Settlement3InuMdV2Info md,
            byte realDir,
            out List<C2NeutralPeasantUnitFrameV2LikeOriginal> frames,
            out string audit)
        {
            frames = new List<C2NeutralPeasantUnitFrameV2LikeOriginal>();
            audit = "";

            C2Settlement3InuMdV2Animation motion = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, "#MOTION_L");
            string motionName = "#MOTION_L";
            if (motion == null || motion.Frames == null || motion.Frames.Count == 0)
            {
                motion = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, "#MOTION_L0");
                motionName = "#MOTION_L0";
            }

            if (md == null || motion == null || motion.Frames == null || motion.Frames.Count == 0)
            {
                audit = "walkAnim=missing #MOTION_L/#MOTION_L0";
                return false;
            }

            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Settlement3InuMdV2SpriteScaleCompensator;
            int max = Math.Min(motion.Frames.Count, C2NeutralPeasantUnitsV2MaxWalkFramesLikeOriginal);

            for (int i = 0; i < max; i++)
            {
                C2Settlement3InuMdV2AnimFrame baseFrame = motion.Frames[i];
                int framesPerDirection = C2NeutralPeasantUnitsV27FramesPerDirectionForFrameLikeOriginal(md, motion.Frames, baseFrame, motion.Frames.Count);

                C2Settlement3InuMdV2AnimFrame resolvedFrame;
                int exactSprite;
                bool mirrorX;
                string dirAudit;
                if (!C2NeutralPeasantUnitsV2BuildDirectionalFrameForDirLikeOriginal(md, Math.Max(1, motion.Rotations), framesPerDirection, realDir, baseFrame, out resolvedFrame, out exactSprite, out mirrorX, out dirAudit))
                    continue;

                Texture2D tex;
                string visualAudit;
                bool ok = C2NeutralPeasantUnitsV2TryLoadSpecificFrameCachedLikeOriginal(
                    md,
                    resolvedFrame,
                    C2Settlement3InuMdV2Kind.Unit,
                    out tex,
                    out visualAudit);

                if (!ok || tex == null) continue;

                tex = C2Settlement3InuMdV2PrepareLoadedTextureLikeOriginal(tex);
                if (tex == null) continue;

                int dx;
                int dy;
                C2Settlement3InuMdV2FramePivotLikeOriginal(md, baseFrame, out dx, out dy);

                int w = Mathf.Max(1, tex.width);
                int h = Mathf.Max(1, tex.height);

                float lx = dx * s;
                float rx = (dx + w) * s;
                float ty = -dy * s;
                float by = -(dy + h) * s;

                if (mirrorX)
                {
                    lx = -(dx + w) * s;
                    rx = -dx * s;
                }

                var f = new C2NeutralPeasantUnitFrameV2LikeOriginal();
                f.Texture = tex;
                f.FileRef = baseFrame.FileRef;
                f.BaseSprite = baseFrame.SpriteId;
                f.ExactSprite = exactSprite;
                f.MirrorX = mirrorX;
                f.PivotDx = dx;
                f.PivotDy = dy;
                f.Width = w;
                f.Height = h;
                f.Lx = lx;
                f.Rx = rx;
                f.By = by;
                f.Ty = ty;
                f.DirectionAudit = dirAudit;
                f.VisualAudit = visualAudit;
                frames.Add(f);
            }

            C2NeutralPeasantUnitsV2StabilizeFrameFootAnchorV15LikeOriginal(frames);

            audit = "walkAnim=" + motionName + " stableFoot=1 realDir=" + realDir.ToString(CultureInfo.InvariantCulture) +
                    " loaded=" + frames.Count.ToString(CultureInfo.InvariantCulture);
            return frames.Count > 0;
        }

        private bool C2NeutralPeasantUnitsV2BuildWalkDirectionBanksLikeOriginal(
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Record r,
            out C2NeutralPeasantUnitFrameV2LikeOriginal[][] banks,
            out string audit)
        {
            banks = new C2NeutralPeasantUnitFrameV2LikeOriginal[256][];
            audit = "walkDirBanks=none";
            if (md == null) return false;

            int built = 0;
            int framesTotal = 0;
            for (int center = 0; center < 256; center += 8)
            {
                List<C2NeutralPeasantUnitFrameV2LikeOriginal> frames;
                string a;
                if (!C2NeutralPeasantUnitsV2TryBuildWalkFramesForDirLikeOriginal(md, (byte)center, out frames, out a) || frames == null || frames.Count == 0)
                    continue;

                C2NeutralPeasantUnitFrameV2LikeOriginal[] arr = frames.ToArray();
                built++;
                framesTotal += arr.Length;

                for (int off = -4; off <= 3; off++)
                {
                    int key = (center + off) & 255;
                    banks[key] = arr;
                }
            }

            // Fill rare gaps with the nearest previous bank so runtime never falls back to a wrong initial direction.
            C2NeutralPeasantUnitFrameV2LikeOriginal[] last = null;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < 256; i++)
                {
                    if (banks[i] != null) last = banks[i];
                    else if (last != null) banks[i] = last;
                }
            }

            audit = "walkDirBanks=" + built.ToString(CultureInfo.InvariantCulture) +
                    " framesTotal=" + framesTotal.ToString(CultureInfo.InvariantCulture) +
                    " step=8 directionLock=true moonwalkFix=removed";
            return built > 0;
        }


        private void C2NeutralPeasantUnitsV2StabilizeFrameFootAnchorV15LikeOriginal(List<C2NeutralPeasantUnitFrameV2LikeOriginal> frames)
        {
            // V18: no-op. The original DrawSpriteUnit uses the USERLC dx/dy pivot from MD.
            // V15/V16 forcibly recentered every frame to feet; when switching from #STAND to
            // #MOTION_L that changed the visible quad anchor and looked like an instant teleport.
            // Keeping original pivots removes the click-time sprite jump.
        }

        public bool C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(Vector3 world, out float x, out float y)
        {
            x = 0.0f;
            y = 0.0f;
            if (_map == null) return false;

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            if (Mathf.Abs(kernel.BackingStepXWorld) < 0.000001f ||
                Mathf.Abs(kernel.BackingStepZWorld) < 0.000001f ||
                Mathf.Abs(WorldZSign) < 0.000001f)
                return false;

            float rawX = world.x + kernel.CenterX;
            float gx = rawX / kernel.BackingStepXWorld;
            int ix = Mathf.FloorToInt(gx);

            float rawZ = (world.z / WorldZSign) + kernel.CenterZ;
            float odd = ((ix & 1) == 0) ? kernel.BackingOddColumnOffsetZWorld : 0.0f;
            float gy = (rawZ - odd) / kernel.BackingStepZWorld;

            x = gx * 32.0f;
            y = gy * 32.0f;
            return true;
        }

        public Vector3 C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(float x, float y)
        {
            return WallOriginalXYToWorldV1LikeOriginal(x, y, 0.0f) + Vector3.up * C2NeutralPeasantUnitsV2YOffsetLikeOriginal;
        }

        private static List<C2Settlement3InuMdV2AnimFrame> C2NeutralPeasantUnitsV2SelectIdleFramesLikeOriginal(
            C2Settlement3InuMdV2Info md,
            out string idleAnimName,
            out int restFrameCount,
            out int idleRotations)
        {
            idleAnimName = "#STAND";
            restFrameCount = 0;
            idleRotations = md != null ? Math.Max(1, md.Rotations) : 1;

            if (md == null) return null;

            C2Settlement3InuMdV2Animation stand = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, "#STAND");
            C2Settlement3InuMdV2Animation rest = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, "#REST");
            if (rest != null && rest.Frames != null) restFrameCount = rest.Frames.Count;

            // V30: original does not use #REST as the permanent idle animation.
            // It normally keeps #STAND and only starts #REST after a rando()<128*8 decision.
            // #REST is built separately into RestFramesByDir and played as a one-shot.
            if (md.StandLoFrames != null && md.StandLoFrames.Count > 0)
            {
                idleAnimName = md.StandLoFrames.Count > 1 ? "#STAND" : "#STAND_SINGLE";
                if (stand != null) idleRotations = Math.Max(1, stand.Rotations);
                return md.StandLoFrames;
            }

            if (rest != null && rest.Frames != null && rest.Frames.Count > 0)
            {
                idleAnimName = "#REST_FALLBACK";
                idleRotations = Math.Max(1, rest.Rotations);
                return rest.Frames;
            }

            return null;
        }

        private bool C2NeutralPeasantUnitsV19TryGetOrBuildVisualFramesLikeOriginal(
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Record r,
            out List<C2NeutralPeasantUnitFrameV2LikeOriginal> idleFrames,
            out List<C2NeutralPeasantUnitFrameV2LikeOriginal> walkFrames,
            out string framesAudit,
            out string walkAudit,
            out bool walkAnimFound,
            out bool cacheHit)
        {
            idleFrames = null;
            walkFrames = null;
            framesAudit = "";
            walkAudit = "";
            walkAnimFound = false;
            cacheHit = false;

            string key = C2NeutralPeasantUnitsV19VisualCacheKeyLikeOriginal(md, r.RealDir);
            C2NeutralPeasantUnitVisualCacheEntryV19LikeOriginal cached;
            if (!string.IsNullOrEmpty(key) &&
                C2NeutralPeasantUnitsV19VisualCacheLikeOriginal.TryGetValue(key, out cached) &&
                cached != null &&
                cached.IdleFrames != null &&
                cached.IdleFrames.Length > 0)
            {
                idleFrames = new List<C2NeutralPeasantUnitFrameV2LikeOriginal>(cached.IdleFrames);
                walkFrames = cached.WalkFrames != null && cached.WalkFrames.Length > 0
                    ? new List<C2NeutralPeasantUnitFrameV2LikeOriginal>(cached.WalkFrames)
                    : idleFrames;
                framesAudit = "visual_cache_hit " + (cached.FramesAudit ?? "");
                walkAudit = "visual_cache_hit " + (cached.WalkAudit ?? "");
                walkAnimFound = cached.WalkAnimFound;
                cacheHit = true;
                return true;
            }

            if (!C2NeutralPeasantUnitsV2TryBuildIdleFramesLikeOriginal(md, r, out idleFrames, out framesAudit) ||
                idleFrames == null || idleFrames.Count == 0)
                return false;

            if (C2NeutralPeasantUnitsV2TryBuildWalkFramesLikeOriginal(md, r, out walkFrames, out walkAudit) &&
                walkFrames != null && walkFrames.Count > 0)
            {
                walkAnimFound = true;
            }
            else
            {
                walkAnimFound = false;
                walkFrames = idleFrames;
                walkAudit = "walk_fallback_to_idle " + walkAudit;
            }

            if (!string.IsNullOrEmpty(key))
            {
                var entry = new C2NeutralPeasantUnitVisualCacheEntryV19LikeOriginal();
                entry.IdleFrames = idleFrames.ToArray();
                entry.WalkFrames = walkFrames != null ? walkFrames.ToArray() : null;
                entry.WalkAnimFound = walkAnimFound;
                entry.FramesAudit = framesAudit;
                entry.WalkAudit = walkAudit;
                C2NeutralPeasantUnitsV19VisualCacheLikeOriginal[key] = entry;
            }

            return true;
        }

        private bool C2NeutralPeasantUnitsV19GetOrBuildWalkDirectionBanksLikeOriginal(
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Record r,
            out C2NeutralPeasantUnitFrameV2LikeOriginal[][] walkDirectionBanks,
            out string walkBankAudit,
            out bool cacheHit)
        {
            walkDirectionBanks = null;
            walkBankAudit = "";
            cacheHit = false;

            string key = C2NeutralPeasantUnitsV19WalkBankCacheKeyLikeOriginal(md);
            C2NeutralPeasantUnitFrameV2LikeOriginal[][] cached;
            if (!string.IsNullOrEmpty(key) &&
                C2NeutralPeasantUnitsV19WalkBankCacheLikeOriginal.TryGetValue(key, out cached) &&
                cached != null)
            {
                walkDirectionBanks = cached;
                walkBankAudit = "walkDirBanks_cache_hit";
                cacheHit = true;
                return true;
            }

            bool ok = C2NeutralPeasantUnitsV2BuildWalkDirectionBanksLikeOriginal(md, r, out walkDirectionBanks, out walkBankAudit);
            if (ok && !string.IsNullOrEmpty(key) && walkDirectionBanks != null)
                C2NeutralPeasantUnitsV19WalkBankCacheLikeOriginal[key] = walkDirectionBanks;
            return ok;
        }

        private bool C2NeutralPeasantUnitsV20GetOrBuildMotionBanksLikeOriginal(
            C2Settlement3InuMdV2Info md,
            out C2NeutralPeasantUnitMotionBanksV20LikeOriginal motionBanks,
            out string audit,
            out bool cacheHit)
        {
            motionBanks = null;
            audit = "";
            cacheHit = false;

            string key = C2NeutralPeasantUnitsV20MotionBankCacheKeyLikeOriginal(md);
            C2NeutralPeasantUnitMotionBanksV20LikeOriginal cached;
            if (!string.IsNullOrEmpty(key) &&
                C2NeutralPeasantUnitsV20MotionBankCacheLikeOriginal.TryGetValue(key, out cached) &&
                cached != null)
            {
                motionBanks = cached;
                audit = "motionBanks_cache_hit " + (cached.Audit ?? "");
                cacheHit = true;
                return true;
            }

            motionBanks = new C2NeutralPeasantUnitMotionBanksV20LikeOriginal();

            string auditL;
            string auditR;
            string auditLB;
            string auditRB;

            C2NeutralPeasantUnitFrameV2LikeOriginal[][] bankL;
            C2NeutralPeasantUnitFrameV2LikeOriginal[][] bankR;
            C2NeutralPeasantUnitFrameV2LikeOriginal[][] bankLB;
            C2NeutralPeasantUnitFrameV2LikeOriginal[][] bankRB;

            motionBanks.HasMotionL = C2NeutralPeasantUnitsV20BuildMotionDirectionBanksLikeOriginal(md, "#MOTION_L", out bankL, out auditL);
            motionBanks.HasMotionR = C2NeutralPeasantUnitsV20BuildMotionDirectionBanksLikeOriginal(md, "#MOTION_R", out bankR, out auditR);
            motionBanks.HasMotionLB = C2NeutralPeasantUnitsV20BuildMotionDirectionBanksLikeOriginal(md, "#MOTION_LB", out bankLB, out auditLB);
            motionBanks.HasMotionRB = C2NeutralPeasantUnitsV20BuildMotionDirectionBanksLikeOriginal(md, "#MOTION_RB", out bankRB, out auditRB);

            // Original TryToMove selects L/R and LB/RB. Many MDs do not provide all four names.
            // Keep strict first-choice selection, but fill missing banks from the closest existing
            // bank so runtime never falls back to a single wrong #MOTION_L-only phase.
            C2NeutralPeasantUnitFrameV2LikeOriginal[][] primary =
                bankL ?? bankR ?? bankLB ?? bankRB;

            motionBanks.MotionL = bankL ?? primary;
            motionBanks.MotionR = bankR ?? bankL ?? primary;
            motionBanks.MotionLB = bankLB ?? bankL ?? primary;
            motionBanks.MotionRB = bankRB ?? bankR ?? bankL ?? primary;
            motionBanks.FallbackWalkFramesByDir = primary;

            motionBanks.Audit =
                "motionBanksV20 " +
                "L=" + motionBanks.HasMotionL +
                " R=" + motionBanks.HasMotionR +
                " LB=" + motionBanks.HasMotionLB +
                " RB=" + motionBanks.HasMotionRB +
                " | " + auditL +
                " | " + auditR +
                " | " + auditLB +
                " | " + auditRB;

            audit = motionBanks.Audit;

            if (!string.IsNullOrEmpty(key))
                C2NeutralPeasantUnitsV20MotionBankCacheLikeOriginal[key] = motionBanks;

            return primary != null;
        }

        private bool C2NeutralPeasantUnitsV20BuildMotionDirectionBanksLikeOriginal(
            C2Settlement3InuMdV2Info md,
            string animationName,
            out C2NeutralPeasantUnitFrameV2LikeOriginal[][] banks,
            out string audit)
        {
            banks = null;
            audit = animationName + "=missing";
            if (md == null) return false;

            C2Settlement3InuMdV2Animation motion;
            string resolvedName;
            if (!C2NeutralPeasantUnitsV20TryFindMotionAnimationLikeOriginal(md, animationName, out motion, out resolvedName) ||
                motion == null || motion.Frames == null || motion.Frames.Count == 0)
            {
                audit = animationName + "=missing";
                return false;
            }

            banks = new C2NeutralPeasantUnitFrameV2LikeOriginal[256][];

            int built = 0;
            int framesTotal = 0;
            for (int center = 0; center < 256; center += 8)
            {
                List<C2NeutralPeasantUnitFrameV2LikeOriginal> frames;
                string frameAudit;
                if (!C2NeutralPeasantUnitsV20TryBuildMotionFramesForDirLikeOriginal(md, motion, (byte)center, out frames, out frameAudit) ||
                    frames == null || frames.Count == 0)
                    continue;

                C2NeutralPeasantUnitFrameV2LikeOriginal[] arr = frames.ToArray();
                built++;
                framesTotal += arr.Length;

                for (int off = -4; off <= 3; off++)
                {
                    int key = (center + off) & 255;
                    banks[key] = arr;
                }
            }

            C2NeutralPeasantUnitsV20FillDirectionBankGapsLikeOriginal(banks);

            if (built <= 0)
            {
                banks = null;
                audit = resolvedName + "=empty";
                return false;
            }

            audit = resolvedName +
                    " banks=" + built.ToString(CultureInfo.InvariantCulture) +
                    " framesTotal=" + framesTotal.ToString(CultureInfo.InvariantCulture) +
                    " step=8";
            return true;
        }

        private static bool C2NeutralPeasantUnitsV20TryFindMotionAnimationLikeOriginal(
            C2Settlement3InuMdV2Info md,
            string animationName,
            out C2Settlement3InuMdV2Animation motion,
            out string resolvedName)
        {
            motion = null;
            resolvedName = animationName;
            if (md == null || string.IsNullOrEmpty(animationName)) return false;

            motion = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, animationName);
            if (motion != null && motion.Frames != null && motion.Frames.Count > 0)
                return true;

            // Some MDs use a numbered suffix: #MOTION_L0 etc.
            resolvedName = animationName + "0";
            motion = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, resolvedName);
            return motion != null && motion.Frames != null && motion.Frames.Count > 0;
        }

        private bool C2NeutralPeasantUnitsV20TryBuildMotionFramesForDirLikeOriginal(
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Animation motion,
            byte realDir,
            out List<C2NeutralPeasantUnitFrameV2LikeOriginal> frames,
            out string audit)
        {
            frames = new List<C2NeutralPeasantUnitFrameV2LikeOriginal>();
            audit = "";

            if (md == null || motion == null || motion.Frames == null || motion.Frames.Count == 0)
            {
                audit = "motion=missing";
                return false;
            }

            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Settlement3InuMdV2SpriteScaleCompensator;
            int max = Math.Min(motion.Frames.Count, C2NeutralPeasantUnitsV2MaxWalkFramesLikeOriginal);

            for (int i = 0; i < max; i++)
            {
                C2Settlement3InuMdV2AnimFrame baseFrame = motion.Frames[i];
                int framesPerDirection = C2NeutralPeasantUnitsV27FramesPerDirectionForFrameLikeOriginal(md, motion.Frames, baseFrame, motion.Frames.Count);

                C2Settlement3InuMdV2AnimFrame resolvedFrame;
                int exactSprite;
                bool mirrorX;
                string dirAudit;
                if (!C2NeutralPeasantUnitsV2BuildDirectionalFrameForDirLikeOriginal(md, Math.Max(1, motion.Rotations), framesPerDirection, realDir, baseFrame, out resolvedFrame, out exactSprite, out mirrorX, out dirAudit))
                    continue;

                Texture2D tex;
                string visualAudit;
                bool ok = C2NeutralPeasantUnitsV2TryLoadSpecificFrameCachedLikeOriginal(
                    md,
                    resolvedFrame,
                    C2Settlement3InuMdV2Kind.Unit,
                    out tex,
                    out visualAudit);

                if (!ok || tex == null) continue;

                tex = C2Settlement3InuMdV2PrepareLoadedTextureLikeOriginal(tex);
                if (tex == null) continue;

                int dx;
                int dy;
                C2Settlement3InuMdV2FramePivotLikeOriginal(md, baseFrame, out dx, out dy);

                int w = Mathf.Max(1, tex.width);
                int h = Mathf.Max(1, tex.height);

                float lx = dx * s;
                float rx = (dx + w) * s;
                float ty = -dy * s;
                float by = -(dy + h) * s;

                if (mirrorX)
                {
                    lx = -(dx + w) * s;
                    rx = -dx * s;
                }

                var f = new C2NeutralPeasantUnitFrameV2LikeOriginal();
                f.Texture = tex;
                f.FileRef = baseFrame.FileRef;
                f.BaseSprite = baseFrame.SpriteId;
                f.ExactSprite = exactSprite;
                f.MirrorX = mirrorX;
                f.PivotDx = dx;
                f.PivotDy = dy;
                f.Width = w;
                f.Height = h;
                f.Lx = lx;
                f.Rx = rx;
                f.By = by;
                f.Ty = ty;
                f.DirectionAudit = dirAudit;
                f.VisualAudit = visualAudit;
                frames.Add(f);
            }

            C2NeutralPeasantUnitsV2StabilizeFrameFootAnchorV15LikeOriginal(frames);

            audit = "realDir=" + realDir.ToString(CultureInfo.InvariantCulture) +
                    " loaded=" + frames.Count.ToString(CultureInfo.InvariantCulture);
            return frames.Count > 0;
        }

        private static void C2NeutralPeasantUnitsV20FillDirectionBankGapsLikeOriginal(
            C2NeutralPeasantUnitFrameV2LikeOriginal[][] banks)
        {
            if (banks == null || banks.Length == 0) return;

            C2NeutralPeasantUnitFrameV2LikeOriginal[] last = null;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < banks.Length; i++)
                {
                    if (banks[i] != null) last = banks[i];
                    else if (last != null) banks[i] = last;
                }
            }
        }

        private static string C2NeutralPeasantUnitsV20MotionBankCacheKeyLikeOriginal(C2Settlement3InuMdV2Info md)
        {
            if (md == null) return "";
            string path = !string.IsNullOrEmpty(md.MdPath) ? md.MdPath : (md.MdName ?? "");
            if (string.IsNullOrEmpty(path)) return "";
            return path + "|rot=" + md.Rotations.ToString(CultureInfo.InvariantCulture) + "|motionBanksV21_animRot";
        }

        private bool C2NeutralPeasantUnitsV23GetOrBuildIdleDirectionBanksLikeOriginal(
            C2Settlement3InuMdV2Info md,
            out C2NeutralPeasantUnitFrameV2LikeOriginal[][] idleDirectionBanks,
            out string audit,
            out bool cacheHit)
        {
            idleDirectionBanks = null;
            audit = "";
            cacheHit = false;

            string key = C2NeutralPeasantUnitsV23IdleBankCacheKeyLikeOriginal(md);
            C2NeutralPeasantUnitFrameV2LikeOriginal[][] cached;
            if (!string.IsNullOrEmpty(key) &&
                C2NeutralPeasantUnitsV23IdleBankCacheLikeOriginal.TryGetValue(key, out cached) &&
                cached != null)
            {
                idleDirectionBanks = cached;
                audit = "idleDirBanks_cache_hit";
                cacheHit = true;
                return true;
            }

            string idleAnimName;
            int restFrameCount;
            C2Settlement3InuMdV2Animation idleAnim = C2NeutralPeasantUnitsV23SelectIdleAnimationObjectLikeOriginal(md, out idleAnimName, out restFrameCount);
            if (idleAnim == null || idleAnim.Frames == null || idleAnim.Frames.Count == 0)
            {
                audit = "idleDirBanks=missing";
                return false;
            }

            bool ok = C2NeutralPeasantUnitsV23BuildDirectionBanksForAnimationLikeOriginal(
                md,
                idleAnim,
                C2NeutralPeasantUnitsV2MaxIdleFramesLikeOriginal,
                16,
                out idleDirectionBanks,
                out audit);

            audit = "idleAnim=" + idleAnimName +
                    " restFrames=" + restFrameCount.ToString(CultureInfo.InvariantCulture) +
                    " " + audit;

            if (ok && !string.IsNullOrEmpty(key) && idleDirectionBanks != null)
                C2NeutralPeasantUnitsV23IdleBankCacheLikeOriginal[key] = idleDirectionBanks;

            return ok;
        }

        private static C2Settlement3InuMdV2Animation C2NeutralPeasantUnitsV23SelectIdleAnimationObjectLikeOriginal(
            C2Settlement3InuMdV2Info md,
            out string idleAnimName,
            out int restFrameCount)
        {
            idleAnimName = "#STAND";
            restFrameCount = 0;
            if (md == null) return null;

            C2Settlement3InuMdV2Animation stand = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, "#STAND");
            if (stand == null) stand = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, "#STANDLO");
            if (stand == null) stand = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, "#STAND1");

            C2Settlement3InuMdV2Animation rest = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, "#REST");
            if (rest != null && rest.Frames != null) restFrameCount = rest.Frames.Count;

            // V30: keep #STAND as idle. #REST is not permanent idle; it is one-shot random rest.

            if (stand != null && stand.Frames != null && stand.Frames.Count > 0)
            {
                idleAnimName = stand.Name ?? "#STAND";
                return stand;
            }

            if (rest != null && rest.Frames != null && rest.Frames.Count > 0)
            {
                idleAnimName = "#REST_FALLBACK";
                return rest;
            }

            return null;
        }

        private bool C2NeutralPeasantUnitsV23BuildDirectionBanksForAnimationLikeOriginal(
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Animation anim,
            int maxFrames,
            int directionStep,
            out C2NeutralPeasantUnitFrameV2LikeOriginal[][] banks,
            out string audit)
        {
            banks = null;
            audit = "dirBanks=missing";
            if (md == null || anim == null || anim.Frames == null || anim.Frames.Count == 0) return false;

            banks = new C2NeutralPeasantUnitFrameV2LikeOriginal[256][];
            int built = 0;
            int framesTotal = 0;
            int step = Mathf.Clamp(directionStep, 1, 32);

            for (int center = 0; center < 256; center += step)
            {
                List<C2NeutralPeasantUnitFrameV2LikeOriginal> frames;
                string frameAudit;
                if (!C2NeutralPeasantUnitsV23TryBuildAnimationFramesForDirLikeOriginal(md, anim, (byte)center, maxFrames, out frames, out frameAudit) ||
                    frames == null || frames.Count == 0)
                    continue;

                C2NeutralPeasantUnitFrameV2LikeOriginal[] arr = frames.ToArray();
                built++;
                framesTotal += arr.Length;

                int half = Math.Max(0, step / 2);
                for (int off = -half; off < step - half; off++)
                {
                    int key = (center + off) & 255;
                    banks[key] = arr;
                }
            }

            C2NeutralPeasantUnitsV20FillDirectionBankGapsLikeOriginal(banks);

            if (built <= 0)
            {
                banks = null;
                audit = "dirBanks=empty anim=" + (anim.Name ?? "<anim>");
                return false;
            }

            audit = "dirBanks anim=" + (anim.Name ?? "<anim>") +
                    " rot=" + Math.Max(1, anim.Rotations).ToString(CultureInfo.InvariantCulture) +
                    " built=" + built.ToString(CultureInfo.InvariantCulture) +
                    " framesTotal=" + framesTotal.ToString(CultureInfo.InvariantCulture) +
                    " step=" + step.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private bool C2NeutralPeasantUnitsV23TryBuildAnimationFramesForDirLikeOriginal(
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Animation anim,
            byte realDir,
            int maxFrames,
            out List<C2NeutralPeasantUnitFrameV2LikeOriginal> frames,
            out string audit)
        {
            frames = new List<C2NeutralPeasantUnitFrameV2LikeOriginal>();
            audit = "";
            if (md == null || anim == null || anim.Frames == null || anim.Frames.Count == 0)
            {
                audit = "anim=missing";
                return false;
            }

            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Settlement3InuMdV2SpriteScaleCompensator;
            int max = Math.Min(anim.Frames.Count, Mathf.Max(1, maxFrames));

            for (int i = 0; i < max; i++)
            {
                C2Settlement3InuMdV2AnimFrame baseFrame = anim.Frames[i];
                int framesPerDirection = C2NeutralPeasantUnitsV27FramesPerDirectionForFrameLikeOriginal(md, anim.Frames, baseFrame, anim.Frames.Count);

                C2Settlement3InuMdV2AnimFrame resolvedFrame;
                int exactSprite;
                bool mirrorX;
                string dirAudit;
                if (!C2NeutralPeasantUnitsV2BuildDirectionalFrameForDirLikeOriginal(md, Math.Max(1, anim.Rotations), framesPerDirection, realDir, baseFrame, out resolvedFrame, out exactSprite, out mirrorX, out dirAudit))
                    continue;

                Texture2D tex;
                string visualAudit;
                bool ok = C2NeutralPeasantUnitsV2TryLoadSpecificFrameCachedLikeOriginal(
                    md,
                    resolvedFrame,
                    C2Settlement3InuMdV2Kind.Unit,
                    out tex,
                    out visualAudit);

                if (!ok || tex == null) continue;

                tex = C2Settlement3InuMdV2PrepareLoadedTextureLikeOriginal(tex);
                if (tex == null) continue;

                int dx;
                int dy;
                C2Settlement3InuMdV2FramePivotLikeOriginal(md, baseFrame, out dx, out dy);

                int w = Mathf.Max(1, tex.width);
                int h = Mathf.Max(1, tex.height);

                float lx = dx * s;
                float rx = (dx + w) * s;
                float ty = -dy * s;
                float by = -(dy + h) * s;

                if (mirrorX)
                {
                    lx = -(dx + w) * s;
                    rx = -dx * s;
                }

                var f = new C2NeutralPeasantUnitFrameV2LikeOriginal();
                f.Texture = tex;
                f.FileRef = baseFrame.FileRef;
                f.BaseSprite = baseFrame.SpriteId;
                f.ExactSprite = exactSprite;
                f.MirrorX = mirrorX;
                f.PivotDx = dx;
                f.PivotDy = dy;
                f.Width = w;
                f.Height = h;
                f.Lx = lx;
                f.Rx = rx;
                f.By = by;
                f.Ty = ty;
                f.DirectionAudit = dirAudit;
                f.VisualAudit = visualAudit;
                frames.Add(f);
            }

            C2NeutralPeasantUnitsV2StabilizeFrameFootAnchorV15LikeOriginal(frames);

            audit = "realDir=" + realDir.ToString(CultureInfo.InvariantCulture) +
                    " loaded=" + frames.Count.ToString(CultureInfo.InvariantCulture);
            return frames.Count > 0;
        }


        private bool C2NeutralPeasantUnitsV30GetOrBuildRestDirectionBanksLikeOriginal(
            C2Settlement3InuMdV2Info md,
            out C2NeutralPeasantUnitFrameV2LikeOriginal[][] restDirectionBanks,
            out string audit,
            out bool cacheHit)
        {
            restDirectionBanks = null;
            audit = "";
            cacheHit = false;

            string key = C2NeutralPeasantUnitsV30RestBankCacheKeyLikeOriginal(md);
            C2NeutralPeasantUnitFrameV2LikeOriginal[][] cached;
            if (!string.IsNullOrEmpty(key) &&
                C2NeutralPeasantUnitsV30RestBankCacheLikeOriginal.TryGetValue(key, out cached) &&
                cached != null)
            {
                restDirectionBanks = cached;
                audit = "restDirBanks_cache_hit";
                cacheHit = true;
                return true;
            }

            C2Settlement3InuMdV2Animation rest = C2Settlement3InuMdV2FindAnimationLikeOriginal(md, "#REST");
            if (rest == null || rest.Frames == null || rest.Frames.Count == 0)
            {
                audit = "restDirBanks=missing";
                return false;
            }

            bool ok = C2NeutralPeasantUnitsV23BuildDirectionBanksForAnimationLikeOriginal(
                md,
                rest,
                C2NeutralPeasantUnitsV2MaxIdleFramesLikeOriginal,
                16,
                out restDirectionBanks,
                out audit);

            audit = "restAnim=#REST " + audit;

            if (ok && !string.IsNullOrEmpty(key) && restDirectionBanks != null)
                C2NeutralPeasantUnitsV30RestBankCacheLikeOriginal[key] = restDirectionBanks;

            return ok;
        }

        private static string C2NeutralPeasantUnitsV30RestBankCacheKeyLikeOriginal(C2Settlement3InuMdV2Info md)
        {
            if (md == null) return "";
            string path = !string.IsNullOrEmpty(md.MdPath) ? md.MdPath : (md.MdName ?? "");
            if (string.IsNullOrEmpty(path)) return "";
            return path + "|rot=" + md.Rotations.ToString(CultureInfo.InvariantCulture) + "|restBanksV30";
        }

        private static string C2NeutralPeasantUnitsV23IdleBankCacheKeyLikeOriginal(C2Settlement3InuMdV2Info md)
        {
            if (md == null) return "";
            string path = !string.IsNullOrEmpty(md.MdPath) ? md.MdPath : (md.MdName ?? "");
            if (string.IsNullOrEmpty(path)) return "";
            return path + "|rot=" + md.Rotations.ToString(CultureInfo.InvariantCulture) + "|idleBanksV30_standOnly";
        }

        private static C2NeutralPeasantUnitsV2SelectionMdInfoLikeOriginal C2NeutralPeasantUnitsV19GetSelectionInfoLikeOriginal(C2Settlement3InuMdV2Info md)
        {
            string key = md != null && !string.IsNullOrEmpty(md.MdPath)
                ? md.MdPath
                : (md != null ? (md.MdName ?? "") : "");
            if (string.IsNullOrEmpty(key))
                return C2NeutralPeasantUnitsV2ParseSelectionInfoLikeOriginal(md);

            C2NeutralPeasantUnitsV2SelectionMdInfoLikeOriginal info;
            if (C2NeutralPeasantUnitsV19SelectionCacheLikeOriginal.TryGetValue(key, out info))
                return info;

            info = C2NeutralPeasantUnitsV2ParseSelectionInfoLikeOriginal(md);
            C2NeutralPeasantUnitsV19SelectionCacheLikeOriginal[key] = info;
            return info;
        }

        private static string C2NeutralPeasantUnitsV19VisualCacheKeyLikeOriginal(C2Settlement3InuMdV2Info md, byte realDir)
        {
            if (md == null) return "";
            string path = !string.IsNullOrEmpty(md.MdPath) ? md.MdPath : (md.MdName ?? "");
            if (string.IsNullOrEmpty(path)) return "";
            return path + "|rot=" + md.Rotations.ToString(CultureInfo.InvariantCulture) + "|dir=" + (realDir & 255).ToString(CultureInfo.InvariantCulture) + "|idleWalkV30_standOnly";
        }

        private static string C2NeutralPeasantUnitsV19WalkBankCacheKeyLikeOriginal(C2Settlement3InuMdV2Info md)
        {
            if (md == null) return "";
            string path = !string.IsNullOrEmpty(md.MdPath) ? md.MdPath : (md.MdName ?? "");
            if (string.IsNullOrEmpty(path)) return "";
            return path + "|rot=" + md.Rotations.ToString(CultureInfo.InvariantCulture) + "|walkBanksV21_animRot";
        }

        private bool C2NeutralPeasantUnitsV2TryLoadSpecificFrameCachedLikeOriginal(
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2AnimFrame frameRef,
            C2Settlement3InuMdV2Kind kind,
            out Texture2D tex,
            out string audit)
        {
            tex = null;
            audit = string.Empty;

            string mdKey = md != null ? (md.MdPath ?? md.MdName ?? "<md>") : "<null_md>";
            string pkg = C2Settlement3InuMdV2PackageForFileRefLikeOriginal(md, frameRef.FileRef) ?? "<pkg>";
            string key = mdKey + "|" + pkg + "|" + frameRef.FileRef.ToString(CultureInfo.InvariantCulture) + "|" + frameRef.SpriteId.ToString(CultureInfo.InvariantCulture) + "|" + kind.ToString();

            Texture2D cached;
            if (C2NeutralPeasantUnitsV2TextureCacheLikeOriginal.TryGetValue(key, out cached) && cached != null)
            {
                tex = cached;
                audit = "cache_hit fileRef=" + frameRef.FileRef.ToString(CultureInfo.InvariantCulture) +
                        " sprite=" + frameRef.SpriteId.ToString(CultureInfo.InvariantCulture) +
                        " tex=" + tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            bool ok = C2Settlement3InuMdV2TryLoadSpecificFrameLikeOriginal(md, frameRef, kind, out tex, out audit);
            if (ok && tex != null)
            {
                C2NeutralPeasantUnitsV2TextureCacheLikeOriginal[key] = tex;
                audit = "cache_store " + audit;
            }
            return ok && tex != null;
        }


        private static int C2NeutralPeasantUnitsV27FramesPerDirectionForFrameLikeOriginal(
            C2Settlement3InuMdV2Info md,
            IList<C2Settlement3InuMdV2AnimFrame> animationFrames,
            C2Settlement3InuMdV2AnimFrame baseFrame,
            int fallbackFrameCount)
        {
            int fromUserLc = C2NeutralPeasantUnitsV27TryGetUserLcFramesPerDirectionLikeOriginal(md, baseFrame.FileRef);
            if (fromUserLc > 0) return fromUserLc;

            int maxSprite = -1;
            if (animationFrames != null)
            {
                for (int i = 0; i < animationFrames.Count; i++)
                {
                    C2Settlement3InuMdV2AnimFrame f = animationFrames[i];
                    if (f.FileRef == baseFrame.FileRef && f.SpriteId > maxSprite)
                        maxSprite = f.SpriteId;
                }
            }

            if (maxSprite >= 0) return Math.Max(1, maxSprite + 1);
            return Math.Max(1, fallbackFrameCount);
        }

        private static int C2NeutralPeasantUnitsV27TryGetUserLcFramesPerDirectionLikeOriginal(
            C2Settlement3InuMdV2Info md,
            int fileRef)
        {
            if (md == null || string.IsNullOrEmpty(md.MdPath) || !File.Exists(md.MdPath))
                return 0;

            string key = md.MdPath + "|" + fileRef.ToString(CultureInfo.InvariantCulture);
            int cached;
            if (C2NeutralPeasantUnitsV27FramesPerDirCacheLikeOriginal.TryGetValue(key, out cached))
                return cached;

            int result = 0;
            try
            {
                string[] lines = File.ReadAllLines(md.MdPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string raw = lines[i] ?? "";
                    string trimmed = raw.Trim();
                    if (!trimmed.StartsWith("USERLC", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string[] tokens = trimmed.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length < 2) continue;

                    int parsedFileRef;
                    if (!int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedFileRef))
                        continue;
                    if (parsedFileRef != fileRef) continue;

                    int comment = raw.IndexOf("//", StringComparison.Ordinal);
                    if (comment >= 0)
                    {
                        string tail = raw.Substring(comment + 2).Trim();
                        Match m = Regex.Match(tail, @"^-?\d+");
                        if (m.Success)
                        {
                            int maxFrame;
                            if (int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxFrame))
                            {
                                result = Math.Max(1, maxFrame + 1);
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                result = 0;
            }

            C2NeutralPeasantUnitsV27FramesPerDirCacheLikeOriginal[key] = result;
            return result;
        }

        private static bool C2NeutralPeasantUnitsV2BuildDirectionalFrameForDirLikeOriginal(
            C2Settlement3InuMdV2Info md,
            int animationRotations,
            int framesPerDirection,
            byte realDir,
            C2Settlement3InuMdV2AnimFrame baseFrame,
            out C2Settlement3InuMdV2AnimFrame resolvedFrame,
            out int exactSprite,
            out bool mirrorX,
            out string audit)
        {
            resolvedFrame = new C2Settlement3InuMdV2AnimFrame(0, 0);
            exactSprite = 0;
            mirrorX = false;
            audit = "";

            // Original NewAnimation::DrawSpriteUnit uses NANM->Rotations of the concrete
            // animation (#STAND/@MOTION_L/etc). It does not use NewMonster::Rotations/ROTATE
            // as the sprite-row multiplier. For peasants this is critical:
            // ROTATE 16, but @MOTION_L/#STAND commonly have 9 stored visual directions.
            int rot = Math.Max(1, animationRotations);
            int mdRot = md != null ? Math.Max(1, md.Rotations) : 1;
            int dir;
            int oc2;
            int ocM;
            int oc1;

            C2NeutralPeasantUnitsV2DirectionToOriginalSpriteBankLikeOriginal(realDir, rot, out dir, out oc2, out ocM, out oc1);

            bool reverse = dir < ocM;
            int dirForSprite;
            int directionBlock;
            if (reverse)
            {
                dirForSprite = dir;
                directionBlock = oc2 - dirForSprite;
                mirrorX = true;
            }
            else
            {
                dirForSprite = oc1 - dir;
                directionBlock = oc2 - dirForSprite;
                mirrorX = false;
            }

            if (directionBlock < 0) directionBlock = 0;
            if (directionBlock >= rot) directionBlock = rot - 1;

            int blockSize = Math.Max(1, framesPerDirection);
            // V27: decoded G17/G2D frame folders are laid out by direction blocks:
            //     dir0: 0..N-1, dir1: N..2N-1, ...
            // Keep the animation phase/base SpriteID and move to another direction block.
            exactSprite = directionBlock * blockSize + baseFrame.SpriteId;
            if (exactSprite < 0) exactSprite = 0;

            resolvedFrame = new C2Settlement3InuMdV2AnimFrame(baseFrame.FileRef, exactSprite);
            audit = "layout=block" +
                    " animRot=" + rot.ToString(CultureInfo.InvariantCulture) +
                    " mdRot=" + mdRot.ToString(CultureInfo.InvariantCulture) +
                    " framesPerDir=" + blockSize.ToString(CultureInfo.InvariantCulture) +
                    " directionBlock=" + directionBlock.ToString(CultureInfo.InvariantCulture) +
                    " realDir=" + realDir.ToString(CultureInfo.InvariantCulture) +
                    " dir=" + dir.ToString(CultureInfo.InvariantCulture) +
                    " oc2=" + oc2.ToString(CultureInfo.InvariantCulture) +
                    " ocM=" + ocM.ToString(CultureInfo.InvariantCulture) +
                    " oc1=" + oc1.ToString(CultureInfo.InvariantCulture) +
                    " baseFileRef=" + baseFrame.FileRef.ToString(CultureInfo.InvariantCulture) +
                    " baseSprite=" + baseFrame.SpriteId.ToString(CultureInfo.InvariantCulture) +
                    " exactSprite=" + exactSprite.ToString(CultureInfo.InvariantCulture) +
                    " reverse=" + reverse;

            return true;
        }

        private static bool C2NeutralPeasantUnitsV2BuildDirectionalFrameLikeOriginal(
            C2Settlement3InuMdV2Info md,
            C2Settlement3InuMdV2Record r,
            int animationRotations,
            int framesPerDirection,
            C2Settlement3InuMdV2AnimFrame baseFrame,
            out C2Settlement3InuMdV2AnimFrame resolvedFrame,
            out int exactSprite,
            out bool mirrorX,
            out string audit)
        {
            byte realDir = r.RealDir;
            return C2NeutralPeasantUnitsV2BuildDirectionalFrameForDirLikeOriginal(md, animationRotations, framesPerDirection, realDir, baseFrame, out resolvedFrame, out exactSprite, out mirrorX, out audit);
        }

        private static void C2NeutralPeasantUnitsV2DirectionToOriginalSpriteBankLikeOriginal(
            byte realDir,
            int rotations,
            out int dir,
            out int oc2,
            out int ocM,
            out int oc1)
        {
            int octs;
            int sesize;
            int real = realDir & 255;

            if (rotations <= 1)
            {
                oc2 = 1;
                ocM = 0;
                oc1 = 1;
                dir = 0;
                return;
            }

            if ((rotations & 1) != 0)
            {
                // Original MiniMap4X.cpp::NewAnimation::DrawSpriteUnit odd-rotation branch.
                octs = (rotations - 1) * 2;
                oc2 = rotations - 1;
                if (octs <= 0) octs = 1;
                sesize = 255 / (octs * 2);
                oc1 = octs;
                ocM = oc2;
                dir = (((real + 64 + sesize) & 255) * octs) >> 8;
            }
            else
            {
                // Original MiniMap4X.cpp::NewAnimation::DrawSpriteUnit even-rotation branch.
                octs = rotations;
                oc2 = rotations;
                ocM = 0;
                if (octs <= 0) octs = 1;
                sesize = 128 / octs;
                oc1 = octs;
                dir = (((real + 64 + sesize + 128) & 255) * octs) >> 8;
            }

            if (dir < 0) dir = 0;
            if (dir > oc1) dir = oc1;
        }

        private struct C2NeutralPeasantUnitsV2SelectionMdInfoLikeOriginal
        {
            public bool HasSelType;
            public string SelTypeName;
            public float ScaleX;
            public float ScaleY;
            public int Shift;
            public string Audit;
        }


        private static int C2NeutralPeasantUnitsV30ParseMotionDistFromMdLikeOriginal(C2Settlement3InuMdV2Info md)
        {
            // Original NewMon.cpp:
            // GEOMETRY r1 r2 motionDist -> NM->MotionDist = motionDist.
            // Single-step motion later uses RInFrame=MotionDist for distance-driven walk frames:
            // CurrentFrameLong=((abs(TotalPath+100000)<<8)/RInFrame)%(NFrames<<8).
            if (md == null || string.IsNullOrEmpty(md.MdPath) || !File.Exists(md.MdPath))
                return 40;

            try
            {
                string[] lines = File.ReadAllLines(md.MdPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = C2NeutralPeasantUnitsV2StripMdCommentLikeOriginal(lines[i]).Trim();
                    if (line.Length == 0) continue;

                    string[] t = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (t.Length >= 4 && string.Equals(t[0], "GEOMETRY", StringComparison.OrdinalIgnoreCase))
                    {
                        int motionDist;
                        if (int.TryParse(t[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out motionDist))
                            return Mathf.Clamp(motionDist, 1, 4096);
                    }
                }
            }
            catch
            {
            }

            return 40;
        }

        private static C2NeutralPeasantUnitsV2SelectionMdInfoLikeOriginal C2NeutralPeasantUnitsV2ParseSelectionInfoLikeOriginal(C2Settlement3InuMdV2Info md)
        {
            var info = new C2NeutralPeasantUnitsV2SelectionMdInfoLikeOriginal();
            info.HasSelType = false;
            info.SelTypeName = "";
            info.ScaleX = 1.0f;
            info.ScaleY = 1.0f;
            info.Shift = 0;
            info.Audit = "fallback";

            if (md == null || string.IsNullOrEmpty(md.MdPath) || !File.Exists(md.MdPath))
                return info;

            try
            {
                string[] lines = File.ReadAllLines(md.MdPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = C2NeutralPeasantUnitsV2StripMdCommentLikeOriginal(lines[i]).Trim();
                    if (line.Length == 0) continue;

                    string[] t = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (t.Length == 0) continue;

                    if (string.Equals(t[0], "SELTYPE", StringComparison.OrdinalIgnoreCase) && t.Length >= 4)
                    {
                        info.HasSelType = true;
                        info.SelTypeName = t[1];
                        info.ScaleX = C2NeutralPeasantUnitsV2ParseFloatLikeOriginal(t[2], 1.0f);
                        info.ScaleY = C2NeutralPeasantUnitsV2ParseFloatLikeOriginal(t[3], 1.0f);
                    }
                    else if (string.Equals(t[0], "SELSHIFT", StringComparison.OrdinalIgnoreCase) && t.Length >= 2)
                    {
                        int shift;
                        if (int.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out shift))
                            info.Shift = shift;
                    }
                }

                info.Audit = info.HasSelType
                    ? ("SELTYPE " + info.SelTypeName + " " + info.ScaleX.ToString(CultureInfo.InvariantCulture) + " " + info.ScaleY.ToString(CultureInfo.InvariantCulture) + " SELSHIFT " + info.Shift.ToString(CultureInfo.InvariantCulture))
                    : "no_SELTYPE_fallback_round";
            }
            catch (Exception ex)
            {
                info.Audit = "selection_parse_error:" + ex.Message;
            }

            return info;
        }

        private static string C2NeutralPeasantUnitsV2StripMdCommentLikeOriginal(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int p = s.IndexOf("//", StringComparison.Ordinal);
            if (p >= 0) s = s.Substring(0, p);
            return s;
        }

        private static float C2NeutralPeasantUnitsV2ParseFloatLikeOriginal(string s, float def)
        {
            float v;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out v)) return v;
            return def;
        }

        private void C2NeutralPeasantUnitsV2CreateUnitObjectLikeOriginal(
            Transform root,
            C2Settlement3InuMdV2Record r,
            C2Settlement3InuMdV2Info md,
            List<C2NeutralPeasantUnitFrameV2LikeOriginal> frames,
            C2NeutralPeasantUnitFrameV2LikeOriginal[][] idleDirectionBanks,
            C2NeutralPeasantUnitFrameV2LikeOriginal[][] restDirectionBanks,
            List<C2NeutralPeasantUnitFrameV2LikeOriginal> walkFrames,
            C2NeutralPeasantUnitFrameV2LikeOriginal[][] walkDirectionBanks,
            C2NeutralPeasantUnitMotionBanksV20LikeOriginal motionBanks,
            C2NeutralPeasantUnitsV2SelectionMdInfoLikeOriginal selInfo,
            string alias,
            string framesAudit)
        {
            if (frames == null || frames.Count == 0) return;

            Vector3 baseWorld = C2Settlement3InuMdV2WorldLikeOriginal(r);
            Vector3 basePos = baseWorld + Vector3.up * C2NeutralPeasantUnitsV2YOffsetLikeOriginal;
            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal() * Settlement3InuMdV2SpriteScaleCompensator;
            float mapPixelToWorld = WallOriginalXYUnitToWorldScaleV8LikeOriginal();

            var parent = new GameObject("C2_UNIT_SAVED_" +
                                        C2Settlement3InuMdV2SanitizeNameLikeOriginal(r.MonsterId) +
                                        "_alias_" + C2Settlement3InuMdV2SanitizeNameLikeOriginal(alias) +
                                        "_" + r.Index.ToString(CultureInfo.InvariantCulture));
            parent.transform.SetParent(root, true);
            parent.transform.position = basePos;

            var spriteGo = new GameObject("sprite_billboard");
            spriteGo.transform.SetParent(parent.transform, false);
            spriteGo.transform.localPosition = Vector3.zero;

            if (C2NeutralPeasantUnitsV2BillboardToCameraLikeOriginal)
            {
                var billboard = spriteGo.AddComponent<C2NeutralPeasantUnitBillboardV2LikeOriginal>();
                billboard.ApplyFullCameraRotation = true;
            }

            var mf = spriteGo.AddComponent<MeshFilter>();
            var mrend = spriteGo.AddComponent<MeshRenderer>();
            C2Settlement3InuMdV2PreparePartTextureLikeOriginal(frames[0].Texture, false);
            mrend.sharedMaterial = C2NeutralPeasantUnitsV2GetMaterialLikeOriginal(frames[0].Texture);
            mrend.shadowCastingMode = ShadowCastingMode.Off;
            mrend.receiveShadows = false;
            mrend.lightProbeUsage = LightProbeUsage.Off;
            mrend.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mrend.sortingOrder = C2NeutralPeasantUnitsV2SortOrderLikeOriginal(r);

            var mesh = new Mesh();
            mesh.name = parent.name + "_SpriteMesh";
            mf.sharedMesh = mesh;

            var animator = spriteGo.AddComponent<C2NeutralPeasantUnitSpriteAnimatorV2LikeOriginal>();
            animator.Configure(frames.ToArray(),
                               idleDirectionBanks,
                               restDirectionBanks,
                               walkFrames != null ? walkFrames.ToArray() : null,
                               walkDirectionBanks,
                               motionBanks,
                               mrend.sharedMaterial,
                               C2NeutralPeasantUnitsV2IdleFpsLikeOriginal,
                               C2NeutralPeasantUnitsV2WalkFpsLikeOriginal,
                               C2NeutralPeasantUnitsV2PickAlphaBiasLikeOriginal,
                               r.Index ^ r.RealX ^ r.RealY,
                               r.RealDir);
            animator.RestPauseMinSeconds = C2NeutralPeasantUnitsV2RestPauseMinSecondsLikeOriginal;
            animator.RestPauseMaxSeconds = C2NeutralPeasantUnitsV2RestPauseMaxSecondsLikeOriginal;

            var info = parent.AddComponent<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            info.OwnerMode = this;
            info.SpriteAnimator = animator;
            info.SourceMonsterId = r.MonsterId ?? "";
            info.ResolvedMd = alias ?? "";
            info.RecordIndex = r.Index;
            info.Nation = r.Nation;
            info.NIndex = r.NIndex;
            info.RealX = r.RealX;
            info.RealY = r.RealY;
            info.RealDir = r.RealDir;
            info.GraphDir = r.RealDir;
            info.OctantInfo = 0xFF;
            info.RealXFloat = r.RealX;
            info.RealYFloat = r.RealY;
            info.RealDirPrecise = (r.RealDir & 255) << 8;
            info.SortKey = C2NeutralPeasantUnitsV2SortOrderLikeOriginal(r);
            info.FrameCount = frames.Count;
            info.FirstFileRef = frames[0].FileRef;
            info.FirstExactSprite = frames[0].ExactSprite;
            info.FirstMirrorX = frames[0].MirrorX;
            info.DirectionAudit = frames[0].DirectionAudit ?? "";
            info.VisualAudit = frames[0].VisualAudit ?? "";
            info.FramesAudit = framesAudit ?? "";
            info.NotSelectable = md != null && md.NotSelectable;
            info.UnitRadius = md != null ? Mathf.Max(1, md.UnitRadius) : 16;
            info.MotionDist = C2NeutralPeasantUnitsV30ParseMotionDistFromMdLikeOriginal(md);
            info.SelectionTypeName = selInfo.HasSelType ? selInfo.SelTypeName : "RoundFallback";
            info.SelectionScaleX = Mathf.Max(0.05f, selInfo.HasSelType ? selInfo.ScaleX : 1.0f);
            info.SelectionScaleY = Mathf.Max(0.05f, selInfo.HasSelType ? selInfo.ScaleY : 1.0f);
            info.SelectionShift = selInfo.Shift;
            info.MapPixelToWorld = mapPixelToWorld;
            info.SelectionLocalOffset = C2NeutralPeasantUnitsV2SelectionOffsetLikeOriginal(r, selInfo, baseWorld);
            info.SelectionAudit = selInfo.Audit ?? "";
            info.MarkerYOffset = 0.085f;

            if (C2NeutralPeasantUnitsV2DrawDebugLabelsLikeOriginal)
                C2NeutralPeasantUnitsV2CreateDebugLabelLikeOriginal(parent.transform, r, alias, frames[0].ExactSprite, frames[0].Ty + 0.35f * s);
        }

        private Vector3 C2NeutralPeasantUnitsV2SelectionOffsetLikeOriginal(
            C2Settlement3InuMdV2Record r,
            C2NeutralPeasantUnitsV2SelectionMdInfoLikeOriginal selInfo,
            Vector3 baseWorld)
        {
            if (selInfo.Shift == 0) return Vector3.zero;

            float mx = r.RealX >> 4;
            float my = r.RealY >> 4;

            // Original DrawMarker:
            // xc = GetAttX()/16 + selShift*TCos[RealDir]/256
            // yc = GetAttY()/16 + selShift*TSin[RealDir]/256
            float angle = ((r.RealDir & 255) / 256.0f) * Mathf.PI * 2.0f;
            float sx = Mathf.Cos(angle) * selInfo.Shift;
            float sy = Mathf.Sin(angle) * selInfo.Shift;

            Vector3 shifted = WallOriginalXYToWorldV1LikeOriginal(mx + sx, my + sy, 0.0f);
            return shifted - baseWorld;
        }

        private static int C2NeutralPeasantUnitsV2SortOrderLikeOriginal(C2Settlement3InuMdV2Record r)
        {
            // Same line idea as original AddAnimation / visible GP registration:
            // larger map Y is later/front, X only breaks ties.
            int yLine = r.RealY >> 5;
            int xTie = (r.RealX >> 9) & 31;
            return 24000 + yLine * 32 + xTie;
        }

        private static Material C2NeutralPeasantUnitsV2GetMaterialLikeOriginal(Texture2D tex)
        {
            Shader sh = Shader.Find("Cossacks2Bridge/SettlementBuildingSpriteV23LikeOriginal");
            if (sh == null) sh = Shader.Find("Cossacks2Bridge/WallObjectSpriteV31ExactCutout");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Transparent/Cutout/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Transparent Cutout");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Standard");

            var mat = new Material(sh);
            mat.name = "C2_NeutralPeasantUnits_V2_Mat_" + (tex != null ? tex.name : "null");
            mat.mainTexture = tex != null ? tex : Texture2D.whiteTexture;
            mat.renderQueue = C2NeutralPeasantUnitsV2RenderQueueLikeOriginal;
            mat.SetOverrideTag("RenderType", "TransparentCutout");

            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex != null ? tex : Texture2D.whiteTexture);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex != null ? tex : Texture2D.whiteTexture);
            if (mat.HasProperty("_MainTex")) { mat.SetTextureScale("_MainTex", Vector2.one); mat.SetTextureOffset("_MainTex", Vector2.zero); }
            if (mat.HasProperty("_BaseMap")) { mat.SetTextureScale("_BaseMap", Vector2.one); mat.SetTextureOffset("_BaseMap", Vector2.zero); }
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_AlphaCutoff")) mat.SetFloat("_AlphaCutoff", C2NeutralPeasantUnitsV2AlphaRefLikeOriginal);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", C2NeutralPeasantUnitsV2AlphaRefLikeOriginal);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1.0f);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 1);
            if (mat.HasProperty("_ZTest"))
                mat.SetInt("_ZTest", (int)(C2NeutralPeasantUnitsV2UseUnitySafeZTestAlwaysLikeOriginal ? CompareFunction.Always : CompareFunction.LessEqual));
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);

            mat.enableInstancing = true;
            return mat;
        }

        private static void C2NeutralPeasantUnitsV2CreateDebugLabelLikeOriginal(
            Transform parent,
            C2Settlement3InuMdV2Record r,
            string alias,
            int sprite,
            float top)
        {
            var label = new GameObject("debug_label");
            label.transform.SetParent(parent, false);
            label.transform.localPosition = new Vector3(0f, top + 0.35f, 0f);
            var tm = label.AddComponent<TextMesh>();
            tm.text = (r.MonsterId ?? "") + "\n" + alias + "\nspr=" + sprite.ToString(CultureInfo.InvariantCulture);
            tm.characterSize = 0.22f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.cyan;
        }
    }

    [Serializable]
    public sealed class C2NeutralPeasantUnitFrameV2LikeOriginal
    {
        public Texture2D Texture;
        public int FileRef;
        public int BaseSprite;
        public int ExactSprite;
        public bool MirrorX;
        public int PivotDx;
        public int PivotDy;
        public int Width;
        public int Height;
        public float Lx;
        public float Rx;
        public float By;
        public float Ty;
        public string DirectionAudit;
        public string VisualAudit;
    }

    [Serializable]
    public sealed class C2NeutralPeasantUnitMotionBanksV20LikeOriginal
    {
        public C2NeutralPeasantUnitFrameV2LikeOriginal[][] MotionL;
        public C2NeutralPeasantUnitFrameV2LikeOriginal[][] MotionR;
        public C2NeutralPeasantUnitFrameV2LikeOriginal[][] MotionLB;
        public C2NeutralPeasantUnitFrameV2LikeOriginal[][] MotionRB;
        public C2NeutralPeasantUnitFrameV2LikeOriginal[][] FallbackWalkFramesByDir;

        public bool HasMotionL;
        public bool HasMotionR;
        public bool HasMotionLB;
        public bool HasMotionRB;
        public string Audit;

        public C2NeutralPeasantUnitFrameV2LikeOriginal[] SelectFramesLikeOriginal(
            byte realDir,
            bool leftLeg,
            bool backMotion)
        {
            int dir = realDir & 255;

            C2NeutralPeasantUnitFrameV2LikeOriginal[] bank = null;

            if (backMotion)
            {
                bank = GetBankLikeOriginal(leftLeg ? MotionLB : MotionRB, dir);
                if (bank != null) return bank;

                bank = GetBankLikeOriginal(leftLeg ? MotionL : MotionR, dir);
                if (bank != null) return bank;
            }
            else
            {
                bank = GetBankLikeOriginal(leftLeg ? MotionL : MotionR, dir);
                if (bank != null) return bank;

                bank = GetBankLikeOriginal(leftLeg ? MotionR : MotionL, dir);
                if (bank != null) return bank;
            }

            bank = GetBankLikeOriginal(MotionL, dir);
            if (bank != null) return bank;

            bank = GetBankLikeOriginal(MotionR, dir);
            if (bank != null) return bank;

            bank = GetBankLikeOriginal(MotionLB, dir);
            if (bank != null) return bank;

            bank = GetBankLikeOriginal(MotionRB, dir);
            if (bank != null) return bank;

            return GetBankLikeOriginal(FallbackWalkFramesByDir, dir);
        }

        private static C2NeutralPeasantUnitFrameV2LikeOriginal[] GetBankLikeOriginal(
            C2NeutralPeasantUnitFrameV2LikeOriginal[][] banks,
            int dir)
        {
            if (banks == null || banks.Length == 0) return null;
            C2NeutralPeasantUnitFrameV2LikeOriginal[] bank = banks[dir & 255];
            return bank != null && bank.Length > 0 ? bank : null;
        }
    }

    public sealed class C2NeutralPeasantUnitBillboardV2LikeOriginal : MonoBehaviour
    {
        public bool ApplyFullCameraRotation = true;

        private void LateUpdate()
        {
            Camera cam = C2NeutralPeasantUnitsV2FindIsoCameraForBillboardV15LikeOriginal();
            if (cam == null) return;

            if (ApplyFullCameraRotation)
            {
                // Original DrawSpriteUnit is screen-projected by the active battle camera.
                // Camera.main may be a free/debug camera in this Unity scene, which made sprites spin.
                transform.rotation = cam.transform.rotation;
            }
            else
            {
                Vector3 toCamera = transform.position - cam.transform.position;
                toCamera.y = 0.0f;
                if (toCamera.sqrMagnitude < 0.000001f) return;
                transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
            }
        }

        private static Camera C2NeutralPeasantUnitsV2FindIsoCameraForBillboardV15LikeOriginal()
        {
            Camera[] all = Camera.allCameras;
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    Camera c = all[i];
                    if (c == null || !c.isActiveAndEnabled) continue;
                    string n = c.name ?? string.Empty;
                    if (n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0) return c;
                }
                for (int i = 0; i < all.Length; i++)
                {
                    Camera c = all[i];
                    if (c == null || !c.isActiveAndEnabled) continue;
                    string n = c.name ?? string.Empty;
                    if (n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0 && n.IndexOf("Iso", StringComparison.OrdinalIgnoreCase) >= 0) return c;
                }
            }
            return Camera.main;
        }
    }

    public sealed class C2NeutralPeasantUnitSpriteAnimatorV2LikeOriginal : MonoBehaviour
    {
        public C2NeutralPeasantUnitFrameV2LikeOriginal[] Frames;
        public C2NeutralPeasantUnitFrameV2LikeOriginal[][] IdleFramesByDir;
        public C2NeutralPeasantUnitFrameV2LikeOriginal[][] RestFramesByDir;
        public C2NeutralPeasantUnitFrameV2LikeOriginal[] WalkFrames;
        public C2NeutralPeasantUnitFrameV2LikeOriginal[][] WalkFramesByDir;
        public C2NeutralPeasantUnitMotionBanksV20LikeOriginal MotionBanks;
        public float Fps = 3.0f;
        public float WalkFps = 5.0f;
        public float AlphaBias = 4.0f / 255.0f;
        public float RestPauseMinSeconds = 3.5f;
        public float RestPauseMaxSeconds = 8.0f;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _material;
        private Mesh _mesh;
        private int _currentIndex = -1;
        private float _phase;
        private float _restDelay;
        private bool _restPlaying;
        private bool _forceStand;
        private bool _moving;
        private bool _leftLeg = true;
        private bool _backMotion;
        private int _activeWalkDir = -1;
        private bool _pathDrivenWalk;
        private float _lastTotalPathReal;
        private float _lastRInFrameReal = 40.0f;
        private int _seed;
        private int _frameAuditCount;
        private int _lastAuditSprite = int.MinValue;
        private int _lastAuditDir = int.MinValue;
        private string _lastAuditTexture = "";

        public int CurrentFrameIndex { get { return _currentIndex; } }

        private C2NeutralPeasantUnitFrameV2LikeOriginal[] ActiveFramesLikeOriginal()
        {
            if (_moving)
            {
                int dir = _activeWalkDir >= 0 ? (_activeWalkDir & 255) : 0;

                if (MotionBanks != null)
                {
                    C2NeutralPeasantUnitFrameV2LikeOriginal[] motionBank =
                        MotionBanks.SelectFramesLikeOriginal((byte)dir, _leftLeg, _backMotion);
                    if (motionBank != null && motionBank.Length > 0) return motionBank;
                }

                if (WalkFramesByDir != null && dir >= 0 && dir < WalkFramesByDir.Length)
                {
                    C2NeutralPeasantUnitFrameV2LikeOriginal[] bank = WalkFramesByDir[dir];
                    if (bank != null && bank.Length > 0) return bank;
                }
                if (WalkFrames != null && WalkFrames.Length > 0) return WalkFrames;
            }
            int idleDir = _activeWalkDir >= 0 ? (_activeWalkDir & 255) : 0;

            if (_restPlaying && RestFramesByDir != null && idleDir >= 0 && idleDir < RestFramesByDir.Length)
            {
                C2NeutralPeasantUnitFrameV2LikeOriginal[] restBank = RestFramesByDir[idleDir];
                if (restBank != null && restBank.Length > 0) return restBank;
            }

            if (IdleFramesByDir != null && idleDir >= 0 && idleDir < IdleFramesByDir.Length)
            {
                C2NeutralPeasantUnitFrameV2LikeOriginal[] idleBank = IdleFramesByDir[idleDir];
                if (idleBank != null && idleBank.Length > 0) return idleBank;
            }

            return Frames;
        }

        public C2NeutralPeasantUnitFrameV2LikeOriginal CurrentFrame
        {
            get
            {
                C2NeutralPeasantUnitFrameV2LikeOriginal[] active = ActiveFramesLikeOriginal();
                if (active == null || active.Length == 0 || _currentIndex < 0 || _currentIndex >= active.Length) return null;
                return active[_currentIndex];
            }
        }

        public void SetSelectedVisualLikeOriginal(bool selected)
        {
            // Original selection is mainly the DrawMarker patch. A small diffuse boost helps
            // confirm selection without replacing the marker logic.
            Color c = selected ? new Color(1.18f, 1.18f, 1.18f, 1.0f) : Color.white;
            if (_material != null && _material.HasProperty("_Color")) _material.SetColor("_Color", c);
            if (_meshRenderer != null && _meshRenderer.sharedMaterial != null && _meshRenderer.sharedMaterial.HasProperty("_Color"))
                _meshRenderer.sharedMaterial.SetColor("_Color", c);
        }

        public void Configure(C2NeutralPeasantUnitFrameV2LikeOriginal[] frames, C2NeutralPeasantUnitFrameV2LikeOriginal[][] idleFramesByDir, C2NeutralPeasantUnitFrameV2LikeOriginal[][] restFramesByDir, C2NeutralPeasantUnitFrameV2LikeOriginal[] walkFrames, C2NeutralPeasantUnitFrameV2LikeOriginal[][] walkFramesByDir, C2NeutralPeasantUnitMotionBanksV20LikeOriginal motionBanks, Material material, float fps, float walkFps, float alphaBias, int phaseSeed, byte initialGraphDir)
        {
            Frames = frames ?? new C2NeutralPeasantUnitFrameV2LikeOriginal[0];
            IdleFramesByDir = idleFramesByDir;
            RestFramesByDir = restFramesByDir;
            WalkFrames = walkFrames ?? new C2NeutralPeasantUnitFrameV2LikeOriginal[0];
            WalkFramesByDir = walkFramesByDir;
            MotionBanks = motionBanks;
            _material = material;
            Fps = Mathf.Max(0.01f, fps);
            WalkFps = Mathf.Max(0.01f, walkFps);
            AlphaBias = Mathf.Clamp01(alphaBias);
            _seed = phaseSeed == int.MinValue ? 0 : Mathf.Abs(phaseSeed);
            _phase = 0.0f;
            _restPlaying = false;
            _forceStand = false;
            _moving = false;
            _leftLeg = true;
            _backMotion = false;
            _activeWalkDir = initialGraphDir & 255;
            _pathDrivenWalk = false;
            _lastTotalPathReal = 0.0f;
            _lastRInFrameReal = 40.0f;
            _frameAuditCount = 0;
            _lastAuditSprite = int.MinValue;
            _lastAuditDir = int.MinValue;
            _lastAuditTexture = "";
            _restDelay = ComputeNextRestDelayLikeOriginal();

            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshFilter == null) _meshFilter = gameObject.AddComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = gameObject.AddComponent<MeshRenderer>();

            _mesh = _meshFilter.sharedMesh;
            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.name = gameObject.name + "_Mesh";
                _meshFilter.sharedMesh = _mesh;
            }

            if (_material != null) _meshRenderer.sharedMaterial = _material;
            ApplyFrame(0, true);
        }

        public void SetForceStandLikeOriginal(bool forceStand)
        {
            if (_forceStand == forceStand) return;
            _forceStand = forceStand;
            if (_forceStand)
            {
                _moving = false;
                _restPlaying = false;
                _phase = 0.0f;
                ApplyFrame(0, true);
            }
        }

        public void SetRealDirectionLikeOriginal(byte realDir)
        {
            SetMotionStateLikeOriginal(realDir, _backMotion);
        }

        public void SetMotionStateLikeOriginal(byte realDir, bool backMotion)
        {
            int dir = realDir & 255;
            if (_activeWalkDir == dir && _backMotion == backMotion) return;

            // V27: original DrawSpriteUnit does not restart the current animation phase
            // when the object direction/graph direction changes. It keeps the base frame
            // (CurrentFrameLong >> 8) and only changes the directional sprite offset:
            //     exactSprite = directionBlock * framesPerDirection + baseSprite
            // Our prebuilt direction banks store that resolved exact block-layout sprite per direction,
            // so preserving phase means: keep the same base index and force ApplyFrame
            // from the new direction bank, even while idle/REST is playing.
            int keepIndex = _currentIndex >= 0 ? _currentIndex : Mathf.FloorToInt(_phase);
            _activeWalkDir = dir;
            _backMotion = backMotion;

            C2NeutralPeasantUnitFrameV2LikeOriginal[] nextActive = ActiveFramesLikeOriginal();
            if (nextActive != null && nextActive.Length > 0)
            {
                _currentIndex = -1;
                ApplyFrame(Mathf.Clamp(keepIndex, 0, nextActive.Length - 1), true);
            }
        }

        public void SetMovingLikeOriginal(bool moving)
        {
            if (_moving == moving) return;
            _moving = moving;
            _forceStand = false;
            _restPlaying = false;
            _phase = 0.0f;
            _leftLeg = true;
            _backMotion = false;
            _pathDrivenWalk = moving;
            ApplyFrame(0, true);
        }


        public void SetWalkPathFrameLikeOriginal(float totalPathReal, float rInFrameReal)
        {
            _pathDrivenWalk = true;
            _lastTotalPathReal = totalPathReal;
            _lastRInFrameReal = Mathf.Max(1.0f, rInFrameReal);

            if (!_moving) return;

            C2NeutralPeasantUnitFrameV2LikeOriginal[] active = ActiveFramesLikeOriginal();
            if (active == null || active.Length == 0) return;

            int nf = Mathf.Max(1, active.Length);
            int phaseFrame = Mathf.FloorToInt(Mathf.Abs(totalPathReal + 100000.0f) / _lastRInFrameReal);
            int idx = phaseFrame % nf;
            if (idx < 0) idx += nf;

            bool nextLeftLeg = ((phaseFrame / nf) & 1) == 0;
            bool changedMotionBank = false;
            if (_leftLeg != nextLeftLeg)
            {
                _leftLeg = nextLeftLeg;
                changedMotionBank = true;
                active = ActiveFramesLikeOriginal();
                if (active == null || active.Length == 0) return;
                nf = Mathf.Max(1, active.Length);
                idx = phaseFrame % nf;
                if (idx < 0) idx += nf;
            }

            _phase = idx;
            if (idx != _currentIndex || changedMotionBank)
                ApplyFrame(idx, changedMotionBank);
        }

        private void Update()
        {
            C2NeutralPeasantUnitFrameV2LikeOriginal[] active = ActiveFramesLikeOriginal();
            if (active == null || active.Length == 0) return;

            if (_moving)
            {
                if (active.Length <= 1)
                {
                    if (_currentIndex != 0) ApplyFrame(0, false);
                    return;
                }

                if (_pathDrivenWalk)
                {
                    // V30: original single-step motion does not run walk frames by timer/FPS.
                    // The frame is driven by path distance:
                    // CurrentFrameLong=((abs(TotalPath+100000)<<8)/RInFrame)%(NFrames<<8)
                    // Info.Update calls SetWalkPathFrameLikeOriginal() after each RealX/RealY step.
                    return;
                }

                // Safe fallback only for external users that set Moving without path stepping.
                _phase += Time.deltaTime * WalkFps;
                int phaseFrame = Mathf.FloorToInt(_phase);
                int activeLen = Mathf.Max(1, active.Length);
                int walkIdx = phaseFrame % activeLen;
                if (walkIdx < 0) walkIdx += activeLen;
                if (walkIdx != _currentIndex) ApplyFrame(walkIdx, false);
                return;
            }

            if (_forceStand)
            {
                if (_currentIndex != 0) ApplyFrame(0, false);
                return;
            }

            if (!_restPlaying)
            {
                _restDelay -= Time.deltaTime;
                if (_restDelay <= 0.0f)
                {
                    if (RollRestChanceLikeOriginal() && RestFramesByDir != null)
                    {
                        _restPlaying = true;
                        _phase = 0.0f;
                        ApplyFrame(0, true);
                        return;
                    }

                    _restDelay = ComputeNextRestDelayLikeOriginal();
                }

                // Normal original-style idle: stand, not permanent REST.
                if (_currentIndex != 0) ApplyFrame(0, false);
                return;
            }

            C2NeutralPeasantUnitFrameV2LikeOriginal[] restActive = ActiveFramesLikeOriginal();
            if (restActive == null || restActive.Length == 0)
            {
                _restPlaying = false;
                _phase = 0.0f;
                _restDelay = ComputeNextRestDelayLikeOriginal();
                if (_currentIndex != 0) ApplyFrame(0, true);
                return;
            }

            _phase += Time.deltaTime * Fps;
            int idx = Mathf.FloorToInt(_phase);

            if (idx >= restActive.Length)
            {
                _restPlaying = false;
                _phase = 0.0f;
                _restDelay = ComputeNextRestDelayLikeOriginal();
                ApplyFrame(0, true);
                return;
            }

            if (idx != _currentIndex) ApplyFrame(idx, false);
        }

        private float ComputeNextRestDelayLikeOriginal()
        {
            unchecked
            {
                _seed = (_seed * 1103515245 + 12345);
            }

            float t = ((_seed >> 8) & 0xFFFF) / 65535.0f;
            float min = Mathf.Max(0.25f, RestPauseMinSeconds);
            float max = Mathf.Max(min + 0.25f, RestPauseMaxSeconds);
            return Mathf.Lerp(min, max, t);
        }

        private bool RollRestChanceLikeOriginal()
        {
            unchecked
            {
                _seed = (_seed * 1103515245 + 12345);
            }

            // Original branch in TryToStand:
            // if(rest && rando() < 128*8) NewAnm = anm_Rest; else NewAnm = anm_Stand.
            // rando() is effectively a 15-bit random domain in this code path.
            int r = (_seed >> 16) & 0x7FFF;
            return r < (128 * 8);
        }

        private static void GetAnchoredFrameRectLikeOriginal(C2NeutralPeasantUnitFrameV2LikeOriginal f, out float lx, out float rx, out float by, out float ty)
        {
            // V28: decoded/cached unit textures are frame images; keep the feet stable when
            // switching idle <-> motion or direction blocks. This affects only the rendered
            // quad, not RealX/RealY path motion.
            float anchorX = (f.Lx + f.Rx) * 0.5f;
            float anchorY = f.By;
            lx = f.Lx - anchorX;
            rx = f.Rx - anchorX;
            by = f.By - anchorY;
            ty = f.Ty - anchorY;
        }

        private void ApplyFrame(int idx, bool force)
        {
            C2NeutralPeasantUnitFrameV2LikeOriginal[] active = ActiveFramesLikeOriginal();
            if (active == null || active.Length == 0) return;
            idx = Mathf.Clamp(idx, 0, active.Length - 1);
            if (!force && idx == _currentIndex) return;

            C2NeutralPeasantUnitFrameV2LikeOriginal f = active[idx];
            if (f == null || f.Texture == null) return;

            _currentIndex = idx;

            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.name = gameObject.name + "_Mesh";
                if (_meshFilter != null) _meshFilter.sharedMesh = _mesh;
            }

            _mesh.Clear(false);
            float lx, rx, by, ty;
            GetAnchoredFrameRectLikeOriginal(f, out lx, out rx, out by, out ty);

            _mesh.vertices = new[]
            {
                new Vector3(lx, by, 0f),
                new Vector3(rx, by, 0f),
                new Vector3(rx, ty, 0f),
                new Vector3(lx, ty, 0f)
            };

            _mesh.uv = f.MirrorX
                ? new[]
                {
                    new Vector2(1f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                }
                : new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f)
                };

            _mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _mesh.RecalculateBounds();

            if (_material != null)
            {
                _material.mainTexture = f.Texture;
                if (_material.HasProperty("_MainTex")) _material.SetTexture("_MainTex", f.Texture);
                if (_material.HasProperty("_BaseMap")) _material.SetTexture("_BaseMap", f.Texture);
                if (_material.HasProperty("_MainTex")) { _material.SetTextureScale("_MainTex", Vector2.one); _material.SetTextureOffset("_MainTex", Vector2.zero); }
                if (_material.HasProperty("_BaseMap")) { _material.SetTextureScale("_BaseMap", Vector2.one); _material.SetTextureOffset("_BaseMap", Vector2.zero); }
            }

            C2NeutralPeasantUnitsV27LogAppliedFrameLikeOriginal(f, idx, force);
        }

        private void C2NeutralPeasantUnitsV27LogAppliedFrameLikeOriginal(C2NeutralPeasantUnitFrameV2LikeOriginal f, int idx, bool force)
        {
            if (f == null || f.Texture == null) return;
            if (_frameAuditCount >= 160) return;

            string texName = f.Texture.name ?? "<unnamed>";
            bool changed = force || f.ExactSprite != _lastAuditSprite || _activeWalkDir != _lastAuditDir || !string.Equals(texName, _lastAuditTexture, StringComparison.Ordinal);
            if (!changed) return;

            _frameAuditCount++;
            _lastAuditSprite = f.ExactSprite;
            _lastAuditDir = _activeWalkDir;
            _lastAuditTexture = texName;

            Debug.Log("[C2:NEUTRAL PEASANT UNIT FRAME V33] go='" + gameObject.name + "'" +
                      " moving=" + _moving +
                      " dir=" + (_activeWalkDir & 255).ToString(CultureInfo.InvariantCulture) +
                      " leftLeg=" + _leftLeg +
                      " back=" + _backMotion +
                      " idx=" + idx.ToString(CultureInfo.InvariantCulture) +
                      " fileRef=" + f.FileRef.ToString(CultureInfo.InvariantCulture) +
                      " baseSprite=" + f.BaseSprite.ToString(CultureInfo.InvariantCulture) +
                      " exactSprite=" + f.ExactSprite.ToString(CultureInfo.InvariantCulture) +
                      " mirror=" + f.MirrorX +
                      " tex='" + texName + "'" +
                      " size=" + f.Width.ToString(CultureInfo.InvariantCulture) + "x" + f.Height.ToString(CultureInfo.InvariantCulture) +
                      " dirAudit='" + (f.DirectionAudit ?? "") + "'" +
                      " visual='" + (f.VisualAudit ?? "") + "'");
        }

        public bool TryPixelHit(Camera cam, Vector3 screenPosition, out float alpha, out Vector2 uv)
        {
            alpha = 0.0f;
            uv = Vector2.zero;

            C2NeutralPeasantUnitFrameV2LikeOriginal f = CurrentFrame;
            if (cam == null || f == null || f.Texture == null) return false;

            // V3: closer to original RegisterVisibleGP/CheckCoorInGP behaviour.
            // Do not depend on Physics colliders or Unity raycast. Project the actually rendered
            // sprite quad to screen, solve mouse -> sprite UV in screen space, then sample texture alpha.
            if (TryPixelHitScreenQuadLikeOriginal(cam, screenPosition, f, out alpha, out uv))
                return true;

            // Fallback for rare camera/near-plane cases: old plane hit against the billboard plane.
            return TryPixelHitPlaneLikeOriginal(cam, screenPosition, f, out alpha, out uv);
        }

        private bool TryPixelHitScreenQuadLikeOriginal(
            Camera cam,
            Vector3 screenPosition,
            C2NeutralPeasantUnitFrameV2LikeOriginal f,
            out float alpha,
            out Vector2 uv)
        {
            alpha = 0.0f;
            uv = Vector2.zero;

            float lx, rx, by, ty;
            GetAnchoredFrameRectLikeOriginal(f, out lx, out rx, out by, out ty);
            Vector3 p0 = cam.WorldToScreenPoint(transform.TransformPoint(new Vector3(lx, by, 0f)));
            Vector3 p1 = cam.WorldToScreenPoint(transform.TransformPoint(new Vector3(rx, by, 0f)));
            Vector3 p2 = cam.WorldToScreenPoint(transform.TransformPoint(new Vector3(rx, ty, 0f)));
            Vector3 p3 = cam.WorldToScreenPoint(transform.TransformPoint(new Vector3(lx, ty, 0f)));

            if (p0.z <= 0f && p1.z <= 0f && p2.z <= 0f && p3.z <= 0f)
                return false;

            Vector2 m = new Vector2(screenPosition.x, screenPosition.y);

            Vector2 uv0 = f.MirrorX ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
            Vector2 uv1 = f.MirrorX ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            Vector2 uv2 = f.MirrorX ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            Vector2 uv3 = f.MirrorX ? new Vector2(1f, 1f) : new Vector2(0f, 1f);

            if (TryPointInTriangleScreenLikeOriginal(m, p0, p2, p1, uv0, uv2, uv1, out uv) ||
                TryPointInTriangleScreenLikeOriginal(m, p0, p3, p2, uv0, uv3, uv2, out uv))
            {
                uv.x = Mathf.Clamp01(uv.x);
                uv.y = Mathf.Clamp01(uv.y);
                return TrySampleAlphaLikeOriginal(f.Texture, uv, out alpha) && alpha > AlphaBias;
            }

            return false;
        }

        public bool TryGetScreenQuadDistanceLikeOriginal(
            Camera cam,
            Vector3 screenPosition,
            out float distancePx,
            out Vector2 anchor,
            out Vector4 rect)
        {
            distancePx = float.PositiveInfinity;
            anchor = Vector2.zero;
            rect = Vector4.zero;

            C2NeutralPeasantUnitFrameV2LikeOriginal f = CurrentFrame;
            if (cam == null || f == null) return false;

            float lx, rx, by, ty;
            GetAnchoredFrameRectLikeOriginal(f, out lx, out rx, out by, out ty);
            Vector3 p0 = cam.WorldToScreenPoint(transform.TransformPoint(new Vector3(lx, by, 0f)));
            Vector3 p1 = cam.WorldToScreenPoint(transform.TransformPoint(new Vector3(rx, by, 0f)));
            Vector3 p2 = cam.WorldToScreenPoint(transform.TransformPoint(new Vector3(rx, ty, 0f)));
            Vector3 p3 = cam.WorldToScreenPoint(transform.TransformPoint(new Vector3(lx, ty, 0f)));

            if (p0.z <= 0f && p1.z <= 0f && p2.z <= 0f && p3.z <= 0f)
                return false;

            float minX = Mathf.Min(Mathf.Min(p0.x, p1.x), Mathf.Min(p2.x, p3.x));
            float maxX = Mathf.Max(Mathf.Max(p0.x, p1.x), Mathf.Max(p2.x, p3.x));
            float minY = Mathf.Min(Mathf.Min(p0.y, p1.y), Mathf.Min(p2.y, p3.y));
            float maxY = Mathf.Max(Mathf.Max(p0.y, p1.y), Mathf.Max(p2.y, p3.y));

            if (float.IsNaN(minX) || float.IsNaN(maxX) || float.IsNaN(minY) || float.IsNaN(maxY))
                return false;

            // Reject obviously broken projections, but keep large enough tolerance for camera zoom.
            if ((maxX - minX) < 1.0f || (maxY - minY) < 1.0f)
                return false;

            rect = new Vector4(minX, minY, maxX, maxY);
            anchor = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

            float mx = screenPosition.x;
            float my = screenPosition.y;
            float clampedX = Mathf.Clamp(mx, minX, maxX);
            float clampedY = Mathf.Clamp(my, minY, maxY);
            float dx = mx - clampedX;
            float dy = my - clampedY;
            distancePx = Mathf.Sqrt(dx * dx + dy * dy);
            return true;
        }

        private bool TryPixelHitPlaneLikeOriginal(
            Camera cam,
            Vector3 screenPosition,
            C2NeutralPeasantUnitFrameV2LikeOriginal f,
            out float alpha,
            out Vector2 uv)
        {
            alpha = 0.0f;
            uv = Vector2.zero;

            Ray ray = cam.ScreenPointToRay(screenPosition);
            Vector3 n = transform.TransformDirection(Vector3.forward);
            float enter;
            Plane plane = new Plane(n, transform.position);
            if (!plane.Raycast(ray, out enter))
            {
                plane = new Plane(-n, transform.position);
                if (!plane.Raycast(ray, out enter)) return false;
            }

            if (enter < 0.0f) return false;

            Vector3 world = ray.GetPoint(enter);
            Vector3 local = transform.InverseTransformPoint(world);

            float lx, rx, by, ty;
            GetAnchoredFrameRectLikeOriginal(f, out lx, out rx, out by, out ty);
            float minX = Mathf.Min(lx, rx);
            float maxX = Mathf.Max(lx, rx);
            float minY = Mathf.Min(by, ty);
            float maxY = Mathf.Max(by, ty);

            if (local.x < minX || local.x > maxX || local.y < minY || local.y > maxY)
                return false;

            float dx = rx - lx;
            float dy = ty - by;
            if (Mathf.Abs(dx) < 0.000001f || Mathf.Abs(dy) < 0.000001f) return false;

            float u = (local.x - lx) / dx;
            float v = (local.y - by) / dy;
            if (f.MirrorX) u = 1.0f - u;

            uv = new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
            return TrySampleAlphaLikeOriginal(f.Texture, uv, out alpha) && alpha > AlphaBias;
        }

        private static bool TrySampleAlphaLikeOriginal(Texture2D tex, Vector2 uv, out float alpha)
        {
            alpha = 0.0f;
            if (tex == null) return false;

            try
            {
                alpha = tex.GetPixelBilinear(uv.x, uv.y).a;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryPointInTriangleScreenLikeOriginal(
            Vector2 p,
            Vector3 a3,
            Vector3 b3,
            Vector3 c3,
            Vector2 auv,
            Vector2 buv,
            Vector2 cuv,
            out Vector2 uv)
        {
            uv = Vector2.zero;

            Vector2 a = new Vector2(a3.x, a3.y);
            Vector2 b = new Vector2(b3.x, b3.y);
            Vector2 c = new Vector2(c3.x, c3.y);

            float v0x = b.x - a.x;
            float v0y = b.y - a.y;
            float v1x = c.x - a.x;
            float v1y = c.y - a.y;
            float v2x = p.x - a.x;
            float v2y = p.y - a.y;

            float den = v0x * v1y - v1x * v0y;
            if (Mathf.Abs(den) < 0.00001f) return false;

            float inv = 1.0f / den;
            float u = (v2x * v1y - v1x * v2y) * inv;
            float v = (v0x * v2y - v2x * v0y) * inv;

            const float eps = -0.0005f;
            if (u < eps || v < eps || (u + v) > 1.0005f) return false;

            float w = 1.0f - u - v;
            uv = auv * w + buv * u + cuv * v;
            return true;
        }
    }

    public sealed class C2NeutralPeasantUnitInfoV2LikeOriginal : MonoBehaviour
    {
        public C2BattleTerrainMode OwnerMode;
        public C2NeutralPeasantUnitSpriteAnimatorV2LikeOriginal SpriteAnimator;

        public string SourceMonsterId;
        public string ResolvedMd;
        public int RecordIndex;
        public byte Nation;
        public ushort NIndex;
        public int RealX;
        public int RealY;
        public byte RealDir;
        public byte GraphDir;
        public byte OctantInfo = 0xFF;
        public float RealXFloat;
        public float RealYFloat;
        public int RealDirPrecise;
        public int SortKey;
        public int FrameCount;
        public int FirstFileRef;
        public int FirstExactSprite;
        public bool FirstMirrorX;
        public string DirectionAudit;
        public string VisualAudit;
        public string FramesAudit;
        public bool NotSelectable;
        public int UnitRadius = 16;
        public int MotionDist = 40;

        public string SelectionTypeName;
        public float SelectionScaleX = 1.0f;
        public float SelectionScaleY = 1.0f;
        public int SelectionShift;
        public float MapPixelToWorld = 0.1f;
        public Vector3 SelectionLocalOffset;
        public float MarkerYOffset = 0.045f;
        public string SelectionAudit;

        private GameObject _selectionMarker;
        private bool _selected;

        public bool IsSelected { get { return _selected; } }

        public bool TryPixelHit(Camera cam, Vector3 screenPosition, out float alpha, out Vector2 uv)
        {
            alpha = 0.0f;
            uv = Vector2.zero;
            return SpriteAnimator != null && SpriteAnimator.TryPixelHit(cam, screenPosition, out alpha, out uv);
        }

        public bool TryGetScreenQuadDistance(Camera cam, Vector3 screenPosition, out float distancePx, out Vector2 anchor, out Vector4 rect)
        {
            distancePx = float.PositiveInfinity;
            anchor = Vector2.zero;
            rect = Vector4.zero;
            return SpriteAnimator != null && SpriteAnimator.TryGetScreenQuadDistanceLikeOriginal(cam, screenPosition, out distancePx, out anchor, out rect);
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;

            if (SpriteAnimator != null)
                SpriteAnimator.SetSelectedVisualLikeOriginal(selected);

            if (_selectionMarker == null)
                _selectionMarker = CreateSelectionMarkerLikeOriginal();

            if (_selectionMarker != null)
                _selectionMarker.SetActive(selected);
        }

        private GameObject CreateSelectionMarkerLikeOriginal()
        {
            var go = new GameObject("selection_marker_" + (string.IsNullOrEmpty(SelectionTypeName) ? "Round" : SelectionTypeName));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = SelectionLocalOffset + Vector3.up * MarkerYOffset;
            go.transform.localRotation = Quaternion.identity;

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = BuildSelectionRingMeshLikeOriginal();

            Shader sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Standard");
            var mat = new Material(sh);
            mat.name = "C2_NeutralPeasantUnits_V33_Selection_" + SelectionTypeName;

            // Original path:
            // mapa.cpp::DrawMarker -> DrawSelPatchDir -> SelectionRect.cpp::DrawSelPatchDir.
            // The real marker is a small terrain patch from Dialogs\\SelType.xml
            // (Round -> textures\\selection\\round3.tga, 32x32, centered 16/16).
            // Until the textured terrain patch path is fully ported, draw a thin yellow
            // ground-space frame/patch that is visually close and very clear in Unity.
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(1.0f, 0.92f, 0.05f, 0.95f));
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.Always);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            mat.renderQueue = 6000;

            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

            go.SetActive(false);
            return go;
        }

        private Mesh BuildSelectionRingMeshLikeOriginal()
        {
            // V28: visible round/oval SELTYPE marker under unit feet.
            // Original path: DrawMarker -> DrawSelPatchDir -> SelType Round texture patch.
            // We draw a flat oval ring in world space; IMGUI also duplicates it for visibility.
            float rx = Mathf.Max(0.16f, MapPixelToWorld * 18.0f * Mathf.Max(0.05f, SelectionScaleX));
            float rz = Mathf.Max(0.10f, MapPixelToWorld * 11.5f * Mathf.Max(0.05f, SelectionScaleY));
            float t = Mathf.Clamp(MapPixelToWorld * 2.25f, 0.035f, Mathf.Min(rx, rz) * 0.55f);
            float irx = Mathf.Max(0.02f, rx - t);
            float irz = Mathf.Max(0.02f, rz - t);

            const int seg = 48;
            Vector3[] verts = new Vector3[seg * 2];
            int[] tris = new int[seg * 6];

            float a0 = ((RealDir & 255) / 256.0f) * Mathf.PI * 2.0f;
            float ca = Mathf.Cos(a0);
            float sa = Mathf.Sin(a0);

            for (int i = 0; i < seg; i++)
            {
                float a = (i / (float)seg) * Mathf.PI * 2.0f;
                Vector3 o = new Vector3(Mathf.Cos(a) * rx, 0.0f, Mathf.Sin(a) * rz);
                Vector3 inn = new Vector3(Mathf.Cos(a) * irx, 0.0f, Mathf.Sin(a) * irz);
                verts[i] = RotateSelectionPointLikeOriginal(o, ca, sa);
                verts[i + seg] = RotateSelectionPointLikeOriginal(inn, ca, sa);
            }

            int ti = 0;
            for (int i = 0; i < seg; i++)
            {
                int j = (i + 1) % seg;
                tris[ti++] = i;
                tris[ti++] = j;
                tris[ti++] = seg + j;
                tris[ti++] = i;
                tris[ti++] = seg + j;
                tris[ti++] = seg + i;
            }

            var mesh = new Mesh();
            mesh.name = "C2_NeutralPeasant_SelectionOvalRing_V33";
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }


        private Mesh BuildSelectionBillboardFrameMeshV15LikeOriginal()
        {
            float halfX = Mathf.Max(0.38f, MapPixelToWorld * 20.0f * Mathf.Max(0.05f, SelectionScaleX));
            float halfY = Mathf.Max(0.25f, MapPixelToWorld * 12.0f * Mathf.Max(0.05f, SelectionScaleY));
            float t = Mathf.Clamp(MapPixelToWorld * 2.50f, 0.060f, 0.26f);

            Vector3[] outer =
            {
                new Vector3(-halfX, -halfY, 0.0f),
                new Vector3( halfX, -halfY, 0.0f),
                new Vector3( halfX,  halfY, 0.0f),
                new Vector3(-halfX,  halfY, 0.0f)
            };
            Vector3[] inner =
            {
                new Vector3(-halfX + t, -halfY + t, 0.0f),
                new Vector3( halfX - t, -halfY + t, 0.0f),
                new Vector3( halfX - t,  halfY - t, 0.0f),
                new Vector3(-halfX + t,  halfY - t, 0.0f)
            };

            var verts = new Vector3[8];
            for (int i = 0; i < 4; i++)
            {
                verts[i] = outer[i];
                verts[i + 4] = inner[i];
            }

            var tris = new int[]
            {
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7
            };

            var mesh = new Mesh();
            mesh.name = "C2_NeutralPeasant_SelectionBillboardFrame_V15";
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 RotateSelectionPointLikeOriginal(Vector3 p, float ca, float sa)
        {
            return new Vector3(p.x * ca - p.z * sa, 0.0f, p.x * sa + p.z * ca);
        }

        private bool _hasMoveTarget;
        private float _destRealX;
        private float _destRealY;
        private float _totalPathReal;
        private float _moveSpeedOriginalPixelsPerSecond = 42.0f;

        // V33: V31 foot-lock was wrong and V32 only smoothed Y. The V32 log proved the
        // remaining side/diagonal jump is a 16-world-unit XZ parity discontinuity from
        // OriginalPixelToWorld/WallOriginalXYToWorld when RealX crosses a staggered terrain column.
        // Movement now keeps RealX/RealY and TotalPath/RInFrame original-style, but projects X/Z
        // continuously from the previous world position using deltaReal/16. Only terrain height Y
        // is sampled from the old absolute projection and smoothed.
        private const float C2NeutralPeasantUnitsV33MaxVisualHeightUnitsPerSecondLikeOriginal = 5.0f;
        private bool _v33VisualHeightReady;
        private float _v33VisualHeightY;

        private void C2NeutralPeasantUnitsV33ResetVisualHeightLikeOriginal()
        {
            _v33VisualHeightY = transform.position.y;
            _v33VisualHeightReady = true;
        }

        private Vector3 C2NeutralPeasantUnitsV33ApplySmoothedWorldYLikeOriginal(Vector3 targetWorld, bool moving)
        {
            if (!_v33VisualHeightReady)
            {
                _v33VisualHeightY = transform.position.y;
                _v33VisualHeightReady = true;
            }

            if (!moving)
            {
                _v33VisualHeightY = targetWorld.y;
                targetWorld.y = _v33VisualHeightY;
                return targetWorld;
            }

            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            float maxStep = C2NeutralPeasantUnitsV33MaxVisualHeightUnitsPerSecondLikeOriginal * dt;
            _v33VisualHeightY = Mathf.MoveTowards(_v33VisualHeightY, targetWorld.y, maxStep);
            targetWorld.y = _v33VisualHeightY;
            return targetWorld;
        }

        private Vector3 C2NeutralPeasantUnitsV33ProjectRealStepContinuousXZLikeOriginal(
            Vector3 beforeWorld,
            float beforeRealX,
            float beforeRealY,
            float afterRealX,
            float afterRealY,
            bool moving)
        {
            float s = Mathf.Abs(MapPixelToWorld) > 0.000001f ? Mathf.Abs(MapPixelToWorld) : 1.0f;
            float dxWorld = ((afterRealX - beforeRealX) / 16.0f) * s;
            float dzWorld = -((afterRealY - beforeRealY) / 16.0f) * s;

            Vector3 absoluteTerrainSample = OwnerMode != null
                ? OwnerMode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(afterRealX / 16.0f, afterRealY / 16.0f)
                : beforeWorld;

            Vector3 continuous = new Vector3(beforeWorld.x + dxWorld, absoluteTerrainSample.y, beforeWorld.z + dzWorld);
            return C2NeutralPeasantUnitsV33ApplySmoothedWorldYLikeOriginal(continuous, moving);
        }

        private Vector3 C2NeutralPeasantUnitsV33ProjectRealDestinationContinuousXZLikeOriginal(
            Vector3 beforeWorld,
            float beforeRealX,
            float beforeRealY,
            float destRealX,
            float destRealY)
        {
            float s = Mathf.Abs(MapPixelToWorld) > 0.000001f ? Mathf.Abs(MapPixelToWorld) : 1.0f;
            Vector3 terrainSample = OwnerMode != null
                ? OwnerMode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(destRealX / 16.0f, destRealY / 16.0f)
                : beforeWorld;
            return new Vector3(
                beforeWorld.x + ((destRealX - beforeRealX) / 16.0f) * s,
                terrainSample.y,
                beforeWorld.z - ((destRealY - beforeRealY) / 16.0f) * s);
        }

        public void SetMoveSpeedLikeOriginal(float speedOriginalPixelsPerSecond)
        {
            // Original stores RealX/RealY in 1/16 map-pixel units. This value is map pixels/sec.
            _moveSpeedOriginalPixelsPerSecond = Mathf.Clamp(speedOriginalPixelsPerSecond, 2.0f, 140.0f);
        }

        // V18: keep the old signature only as a safe fallback for external calls.
        // It converts the requested world point back to original map pixels and then uses RealX/RealY motion.
        public void SetMoveDestinationLikeOriginal(Vector3 target)
        {
            if (OwnerMode == null)
                return;

            float ox;
            float oy;
            if (!OwnerMode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(target, out ox, out oy))
                return;

            SetMoveDestinationRealLikeOriginal(ox * 16.0f, oy * 16.0f, _moveSpeedOriginalPixelsPerSecond);
        }

        public void SetMoveDestinationRealLikeOriginal(float destRealX, float destRealY, float speedOriginalPixelsPerSecond)
        {
            SetMoveSpeedLikeOriginal(speedOriginalPixelsPerSecond);

            Vector3 beforeWorld = transform.position;
            if (!_v33VisualHeightReady) C2NeutralPeasantUnitsV33ResetVisualHeightLikeOriginal();
            float beforeRealX = RealXFloat;
            float beforeRealY = RealYFloat;
            byte beforeRealDir = RealDir;
            byte beforeGraphDir = GraphDir;
            C2NeutralPeasantUnitFrameV2LikeOriginal beforeFrame = SpriteAnimator != null ? SpriteAnimator.CurrentFrame : null;

            _destRealX = destRealX;
            _destRealY = destRealY;
            _hasMoveTarget = true;
            _v29MoveCommandSeq++;
            _v29MoveTickLogCount = 0;

            // Do not move transform here. V10-V17 visually jumped at RMB because the target/world
            // projection and frame pivot changed immediately. Original SetDstPoint only writes DstX/DstY.
            // But do switch the animation bank to the first movement segment immediately, so RMB
            // does not play one frame of the old standing direction.
            if (RealDirPrecise == 0) RealDirPrecise = (RealDir & 255) << 8;
            float firstDx = _destRealX - RealXFloat;
            float firstDy = _destRealY - RealYFloat;
            if (Mathf.Abs(firstDx) > 0.0001f || Mathf.Abs(firstDy) > 0.0001f)
            {
                RealDir = C2NeutralPeasantUnitsV2GetDirFromRealDeltaV18LikeOriginal(firstDx, firstDy);
                RealDirPrecise = (RealDir & 255) << 8;
            }
            GraphDir = C2NeutralPeasantUnitsV2UpdateOctantGraphDirLikeOriginal(RealDir, ref OctantInfo);
            if (SpriteAnimator != null)
            {
                SpriteAnimator.SetMovingLikeOriginal(true);
                SpriteAnimator.SetMotionStateLikeOriginal(GraphDir, false);
                SpriteAnimator.SetWalkPathFrameLikeOriginal(_totalPathReal, Mathf.Max(1.0f, MotionDist));
            }

            Vector3 afterWorld = transform.position;
            Vector3 destWorld = OwnerMode != null
                ? C2NeutralPeasantUnitsV33ProjectRealDestinationContinuousXZLikeOriginal(beforeWorld, beforeRealX, beforeRealY, _destRealX, _destRealY)
                : Vector3.zero;
            float immediateWorldJump = Vector3.Distance(beforeWorld, afterWorld);
            float targetWorldDistance = OwnerMode != null ? Vector3.Distance(beforeWorld, destWorld) : -1.0f;
            C2NeutralPeasantUnitFrameV2LikeOriginal afterFrame = SpriteAnimator != null ? SpriteAnimator.CurrentFrame : null;

            Debug.Log("[C2:NEUTRAL PEASANT UNIT MOVE V33 COMMAND] seq=" + _v29MoveCommandSeq.ToString(CultureInfo.InvariantCulture) +
                      " idx=" + RecordIndex.ToString(CultureInfo.InvariantCulture) +
                      " name='" + SourceMonsterId + "'" +
                      " beforeReal=(" + beforeRealX.ToString("0.0", CultureInfo.InvariantCulture) + "," + beforeRealY.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                      " destReal=(" + _destRealX.ToString("0.0", CultureInfo.InvariantCulture) + "," + _destRealY.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                      " deltaReal=(" + firstDx.ToString("0.0", CultureInfo.InvariantCulture) + "," + firstDy.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                      " beforeWorld=" + C2NeutralPeasantUnitsV29Vec3LikeOriginal(beforeWorld) +
                      " afterWorld=" + C2NeutralPeasantUnitsV29Vec3LikeOriginal(afterWorld) +
                      " destWorld=" + C2NeutralPeasantUnitsV29Vec3LikeOriginal(destWorld) +
                      " immediateWorldJump=" + immediateWorldJump.ToString("0.000", CultureInfo.InvariantCulture) +
                      " targetWorldDistance=" + targetWorldDistance.ToString("0.000", CultureInfo.InvariantCulture) +
                      " projection=continuousXZ_fromRealDelta_absYOnly" +
                      " beforeDir=" + beforeRealDir.ToString(CultureInfo.InvariantCulture) +
                      " beforeGraph=" + beforeGraphDir.ToString(CultureInfo.InvariantCulture) +
                      " newDir=" + RealDir.ToString(CultureInfo.InvariantCulture) +
                      " newGraph=" + GraphDir.ToString(CultureInfo.InvariantCulture) +
                      " motionDist=" + MotionDist.ToString(CultureInfo.InvariantCulture) +
                      " totalPath=" + _totalPathReal.ToString("0.0", CultureInfo.InvariantCulture) +
                      " beforeSprite=" + (beforeFrame != null ? beforeFrame.ExactSprite.ToString(CultureInfo.InvariantCulture) : "<null>") +
                      " afterSprite=" + (afterFrame != null ? afterFrame.ExactSprite.ToString(CultureInfo.InvariantCulture) : "<null>") +
                      " warningImmediateTeleport=" + (immediateWorldJump > 0.05f));
        }

        private void Update()
        {
            if (!_hasMoveTarget) return;
            if (OwnerMode == null)
            {
                _hasMoveTarget = false;
                if (SpriteAnimator != null) SpriteAnimator.SetMovingLikeOriginal(false);
                return;
            }

            Vector3 beforeWorld = transform.position;
            float beforeRealX = RealXFloat;
            float beforeRealY = RealYFloat;

            float dx = _destRealX - RealXFloat;
            float dy = _destRealY - RealYFloat;
            float dis = Mathf.Sqrt(dx * dx + dy * dy);

            // Original CalculateMotion2 stops when DIS <= 64 in Real units.
            if (dis <= 64.0f)
            {
                RealXFloat = _destRealX;
                RealYFloat = _destRealY;
                RealX = Mathf.RoundToInt(RealXFloat);
                RealY = Mathf.RoundToInt(RealYFloat);
                transform.position = C2NeutralPeasantUnitsV33ProjectRealStepContinuousXZLikeOriginal(
                    beforeWorld,
                    beforeRealX,
                    beforeRealY,
                    RealXFloat,
                    RealYFloat,
                    true);
                Vector3 afterWorldSnap = transform.position;
                _hasMoveTarget = false;
                if (SpriteAnimator != null) SpriteAnimator.SetMovingLikeOriginal(false);
                C2NeutralPeasantUnitsV29LogMoveTickLikeOriginal(0, GraphDir, dx, dy, dis, 0.0f, beforeRealX, beforeRealY, RealXFloat, RealYFloat, beforeWorld, afterWorldSnap, true);
                return;
            }

            byte targetDir = C2NeutralPeasantUnitsV2GetDirFromRealDeltaV18LikeOriginal(dx, dy);

            // V27: original TryToMove path sets the movement animation direction from the
            // current path segment directly:
            //     OB->RealDir = NewDir;
            //     OB->GraphDir = NewDir;
            // The previous Unity adapter smoothed RealDirPrecise toward targetDir and blocked
            // translation until the delta was small. That made the peasant visibly spin through
            // intermediate sprite banks and looked like there was no stable angle-based walk.
            RealDir = targetDir;
            RealDirPrecise = (RealDir & 255) << 8;
            GraphDir = C2NeutralPeasantUnitsV2UpdateOctantGraphDirLikeOriginal(RealDir, ref OctantInfo);

            if (SpriteAnimator != null)
                SpriteAnimator.SetMotionStateLikeOriginal(GraphDir, false);

            // Move in original RealX/RealY units. Direction is the segment direction; no Unity
            // transform/camera rotation participates.
            float stepReal = _moveSpeedOriginalPixelsPerSecond * 16.0f * Time.deltaTime;
            if (stepReal > dis) stepReal = dis;

            float inv = 1.0f / Mathf.Max(dis, 0.0001f);
            RealXFloat += dx * inv * stepReal;
            RealYFloat += dy * inv * stepReal;
            RealX = Mathf.RoundToInt(RealXFloat);
            RealY = Mathf.RoundToInt(RealYFloat);
            _totalPathReal += stepReal;
            transform.position = C2NeutralPeasantUnitsV33ProjectRealStepContinuousXZLikeOriginal(
                beforeWorld,
                beforeRealX,
                beforeRealY,
                RealXFloat,
                RealYFloat,
                true);

            if (SpriteAnimator != null)
                SpriteAnimator.SetWalkPathFrameLikeOriginal(_totalPathReal, Mathf.Max(1.0f, MotionDist));

            Vector3 afterWorld = transform.position;
            C2NeutralPeasantUnitsV29LogMoveTickLikeOriginal(targetDir, GraphDir, dx, dy, dis, stepReal, beforeRealX, beforeRealY, RealXFloat, RealYFloat, beforeWorld, afterWorld, false);
        }

        private static byte C2NeutralPeasantUnitsV2UpdateOctantGraphDirLikeOriginal(byte realDir, ref byte octantInfo)
        {
            // Original MiniMap4X.cpp::NewAnimation::DrawSpriteUnit keeps OB->OctantInfo
            // for 16/9-rotation unit sprites. It prevents the visible sprite bank from
            // flickering around sector borders while RealDirPrecise is turning smoothly.
            int rd = realDir & 255;

            if (octantInfo == 0xFF)
            {
                octantInfo = (byte)(((rd + 8) >> 4) & 0xFF);
                return realDir;
            }

            int oct = octantInfo & 15;
            int cd = oct << 4;
            int dd = cd - rd;
            while (dd > 127) dd -= 256;
            while (dd < -128) dd += 256;
            int ad = Mathf.Abs(dd);

            if (ad <= 8)
            {
                octantInfo = (byte)(octantInfo & 0x0F);
                return (byte)((octantInfo & 15) << 4);
            }

            if (ad < 16)
            {
                int ot = octantInfo >> 4;
                if (ot < 12)
                {
                    byte visual = (byte)((octantInfo & 15) << 4);
                    ot += ad >> 3;
                    octantInfo = (byte)((octantInfo & 15) + (ot << 4));
                    return visual;
                }

                octantInfo = (byte)(((rd + 8) >> 4) & 0xFF);
                return (byte)((octantInfo & 15) << 4);
            }

            octantInfo = (byte)(((rd + 8) >> 4) & 0xFF);
            return (byte)((octantInfo & 15) << 4);
        }

        private int _v29MoveCommandSeq;
        private int _v29MoveTickLogCount;

        private static string C2NeutralPeasantUnitsV29Vec3LikeOriginal(Vector3 v)
        {
            return "(" + v.x.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                   v.y.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                   v.z.ToString("0.000", CultureInfo.InvariantCulture) + ")";
        }

        private void C2NeutralPeasantUnitsV29LogMoveTickLikeOriginal(
            byte targetDir,
            byte graphDir,
            float dx,
            float dy,
            float dis,
            float stepReal,
            float beforeRealX,
            float beforeRealY,
            float afterRealX,
            float afterRealY,
            Vector3 beforeWorld,
            Vector3 afterWorld,
            bool finalSnap)
        {
            if (_v29MoveTickLogCount >= 120) return;
            _v29MoveTickLogCount++;

            float deltaWorld = Vector3.Distance(beforeWorld, afterWorld);
            float deltaReal = Mathf.Sqrt((afterRealX - beforeRealX) * (afterRealX - beforeRealX) +
                                         (afterRealY - beforeRealY) * (afterRealY - beforeRealY));
            float expectedWorld = Mathf.Abs(MapPixelToWorld) > 0.000001f ? (stepReal / 16.0f) * Mathf.Abs(MapPixelToWorld) : -1.0f;
            bool teleportWarn = deltaWorld > Mathf.Max(0.35f, expectedWorld * 4.0f + 0.10f);

            C2NeutralPeasantUnitFrameV2LikeOriginal cf = SpriteAnimator != null ? SpriteAnimator.CurrentFrame : null;
            Debug.Log("[C2:NEUTRAL PEASANT UNIT MOVE V33 TICK] seq=" + _v29MoveCommandSeq.ToString(CultureInfo.InvariantCulture) +
                      " tick=" + _v29MoveTickLogCount.ToString(CultureInfo.InvariantCulture) +
                      " idx=" + RecordIndex.ToString(CultureInfo.InvariantCulture) +
                      " name='" + SourceMonsterId + "'" +
                      " targetDir=" + targetDir.ToString(CultureInfo.InvariantCulture) +
                      " graphDir=" + graphDir.ToString(CultureInfo.InvariantCulture) +
                      " realDir=" + RealDir.ToString(CultureInfo.InvariantCulture) +
                      " octant=0x" + OctantInfo.ToString("X2", CultureInfo.InvariantCulture) +
                      " beforeReal=(" + beforeRealX.ToString("0.0", CultureInfo.InvariantCulture) + "," + beforeRealY.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                      " afterReal=(" + afterRealX.ToString("0.0", CultureInfo.InvariantCulture) + "," + afterRealY.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                      " destReal=(" + _destRealX.ToString("0.0", CultureInfo.InvariantCulture) + "," + _destRealY.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                      " deltaReal=" + deltaReal.ToString("0.0", CultureInfo.InvariantCulture) +
                      " dx=" + dx.ToString("0.0", CultureInfo.InvariantCulture) +
                      " dy=" + dy.ToString("0.0", CultureInfo.InvariantCulture) +
                      " disBefore=" + dis.ToString("0.0", CultureInfo.InvariantCulture) +
                      " stepReal=" + stepReal.ToString("0.0", CultureInfo.InvariantCulture) +
                      " totalPath=" + _totalPathReal.ToString("0.0", CultureInfo.InvariantCulture) +
                      " motionDist=" + MotionDist.ToString(CultureInfo.InvariantCulture) +
                      " beforeWorld=" + C2NeutralPeasantUnitsV29Vec3LikeOriginal(beforeWorld) +
                      " afterWorld=" + C2NeutralPeasantUnitsV29Vec3LikeOriginal(afterWorld) +
                      " deltaWorld=" + deltaWorld.ToString("0.000", CultureInfo.InvariantCulture) +
                      " expectedWorld=" + expectedWorld.ToString("0.000", CultureInfo.InvariantCulture) +
                      " finalSnap=" + finalSnap +
                      " projection=continuousXZ_fromRealDelta_absYOnly" +
                      " teleportWarning=" + teleportWarn +
                      " currentFrame=" + (SpriteAnimator != null ? SpriteAnimator.CurrentFrameIndex.ToString(CultureInfo.InvariantCulture) : "-1") +
                      " currentSprite=" + (cf != null ? cf.ExactSprite.ToString(CultureInfo.InvariantCulture) : "<null>") +
                      " currentTex='" + (cf != null && cf.Texture != null ? (cf.Texture.name ?? "<unnamed>") : "<null>") + "'");
        }

        private static byte C2NeutralPeasantUnitsV2GetDirFromRealDeltaV18LikeOriginal(float dx, float dy)
        {
            if (Mathf.Abs(dx) < 0.0001f && Mathf.Abs(dy) < 0.0001f) return 0;
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            int raw = Mathf.RoundToInt(Mathf.Repeat(angle / 360.0f * 256.0f, 256.0f));
            return (byte)(raw & 255);
        }

        private static int C2NeutralPeasantUnitsV2SignedDirDeltaV15LikeOriginal(byte target, byte current)
        {
            int d = (target & 255) - (current & 255);
            while (d > 127) d -= 256;
            while (d < -128) d += 256;
            return d;
        }

        public string DebugPickLine(float alpha, Vector2 uv)
        {
            C2NeutralPeasantUnitFrameV2LikeOriginal cf = SpriteAnimator != null ? SpriteAnimator.CurrentFrame : null;
            return "[C2:NEUTRAL PEASANT UNIT PICK V33] idx=" + RecordIndex +
                   " name='" + SourceMonsterId + "'" +
                   " md='" + ResolvedMd + "'" +
                   " real=(" + RealX + "," + RealY + ")" +
                   " dir=" + RealDir +
                   " graphDir=" + GraphDir +
                   " octant=0x" + OctantInfo.ToString("X2", CultureInfo.InvariantCulture) +
                   " frames=" + FrameCount +
                   " moving=" + _hasMoveTarget +
                   " currentFrame=" + (SpriteAnimator != null ? SpriteAnimator.CurrentFrameIndex.ToString(CultureInfo.InvariantCulture) : "-1") +
                   " fileRef=" + (cf != null ? cf.FileRef.ToString(CultureInfo.InvariantCulture) : FirstFileRef.ToString(CultureInfo.InvariantCulture)) +
                   " sprite=" + (cf != null ? cf.ExactSprite.ToString(CultureInfo.InvariantCulture) : FirstExactSprite.ToString(CultureInfo.InvariantCulture)) +
                   " mirror=" + (cf != null ? cf.MirrorX : FirstMirrorX) +
                   " alpha=" + alpha.ToString("0.000", CultureInfo.InvariantCulture) +
                   " uv=(" + uv.x.ToString("0.000", CultureInfo.InvariantCulture) + "," + uv.y.ToString("0.000", CultureInfo.InvariantCulture) + ")" +
                   " sel=" + SelectionAudit;
        }
    }

    public sealed class C2NeutralPeasantUnitPickerV2LikeOriginal : MonoBehaviour
    {
        public static C2NeutralPeasantUnitPickerV2LikeOriginal Active { get; private set; }

        private const float V8FallbackPickRadiusPixelsLikeOriginal = 48.0f;
        private const float V8SelectionDragThresholdPixelsLikeOriginal = 12.0f;

        private readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> _selectedUnits =
            new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(64);
        private readonly List<C2SettlementBuildingSelectableV1LikeOriginal> _selectedBuildings =
            new List<C2SettlementBuildingSelectableV1LikeOriginal>(64);

        private bool _prevLeftPressed;
        private bool _prevRightPressed;
        private bool _loggedReady;
        private bool _duplicateDead;

        private bool _dragActive;
        private bool _dragExceeded;
        private Vector3 _dragStart;
        private Vector3 _dragCurrent;

        private static Texture2D _guiPixel;

        private RectTransform _screenOverlayRoot;
        private Image _dragFillImage;
        private GameObject _worldDragFrameRoot;
        private LineRenderer[] _worldDragLines;
        private Material _worldDragLineMaterial;
        private RectTransform _dragTopLine;
        private RectTransform _dragBottomLine;
        private RectTransform _dragLeftLine;
        private RectTransform _dragRightLine;
        private readonly Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, RectTransform> _screenSelectionMarkers =
            new Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, RectTransform>();
        private readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> _markerRemoveScratch =
            new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(64);
        private bool _overlayLogged;
        private Vector3 _lastMoveScreenPoint;
        private float _lastMoveFeedbackUntil;
        private string _lastMoveFeedbackText = "";

        private struct MouseStateLikeOriginal
        {
            public Vector3 Position;
            public bool LeftDown;
            public bool LeftHeld;
            public bool LeftUp;
            public bool RightDown;
        }

        private struct PickMissInfoLikeOriginal
        {
            public float NearestDist;
            public int NearestIdx;
            public Vector2 NearestAnchor;
            public Vector4 NearestRect;
            public string NearestCam;
        }


        private static Camera C2NeutralPeasantUnitsV2BestIsoCameraLikeOriginal()
        {
            Camera[] all = Camera.allCameras;
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    Camera c = all[i];
                    if (c == null || !c.isActiveAndEnabled) continue;
                    string n = c.name ?? string.Empty;
                    if (n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) >= 0)
                        return c;
                }

                for (int i = 0; i < all.Length; i++)
                {
                    Camera c = all[i];
                    if (c == null || !c.isActiveAndEnabled) continue;
                    string n = c.name ?? string.Empty;
                    if (n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        n.IndexOf("Iso", StringComparison.OrdinalIgnoreCase) >= 0)
                        return c;
                }

                for (int i = 0; i < all.Length; i++)
                {
                    Camera c = all[i];
                    if (c == null || !c.isActiveAndEnabled) continue;
                    string n = c.name ?? string.Empty;
                    if (n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        n.IndexOf("Free", StringComparison.OrdinalIgnoreCase) < 0)
                        return c;
                }
            }

            return Camera.main;
        }

        private bool HasBattleUnitsForOverlayV16LikeOriginal()
        {
            var units = C2NeutralPeasantUnitsV2FindUnitsLikeOriginal();
            if (units != null && units.Length > 0 && C2NeutralPeasantUnitsV2BestIsoCameraLikeOriginal() != null)
                return true;

            var buildings = C2NeutralPeasantUnitsV2FindBuildingsLikeOriginal();
            return buildings != null && buildings.Length > 0 && C2NeutralPeasantUnitsV2BestIsoCameraLikeOriginal() != null;
        }


        private void Awake()
        {
            if (Active != null && Active != this)
            {
                _duplicateDead = true;
                enabled = false;
                UnityEngine.Object.Destroy(this);
                return;
            }

            Active = this;
            // V16: the picker is hosted by the map auto-runner. Do not create the UI overlay here:
            // in the main menu this produced the huge yellow rectangle over the menu. Overlay is
            // created lazily only when battle units exist.
            _dragActive = false;
            _dragExceeded = false;
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        private void Update()
        {
            if (_duplicateDead) return;

            if (!_loggedReady)
            {
                _loggedReady = true;
                Debug.Log("[C2:NEUTRAL PEASANT PICKER V18] installed input=" + C2NeutralPeasantUnitsV2InputBackendLikeOriginal());
            }

            if (!HasBattleUnitsForOverlayV16LikeOriginal())
            {
                _dragActive = false;
                _dragExceeded = false;
                if (_selectedUnits.Count > 0 || _selectedBuildings.Count > 0)
                    SetSelectionLikeOriginal(new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(0), new List<C2SettlementBuildingSelectableV1LikeOriginal>(0), false);
                return;
            }

            // V18: do not create a persistent ScreenSpaceOverlay canvas at all.
            // The selection rectangle/markers are drawn by OnGUI only, so they cannot leak into the main menu.

            MouseStateLikeOriginal mouse;
            if (!C2NeutralPeasantUnitsV2ReadMouseStateLikeOriginal(out mouse))
                return;

            CleanupSelectionListLikeOriginal();

            if (mouse.LeftDown)
            {
                _dragActive = true;
                _dragExceeded = false;
                _dragStart = mouse.Position;
                _dragCurrent = mouse.Position;
            }

            if (_dragActive && mouse.LeftHeld)
            {
                _dragCurrent = mouse.Position;
                if ((_dragCurrent - _dragStart).sqrMagnitude >= V8SelectionDragThresholdPixelsLikeOriginal * V8SelectionDragThresholdPixelsLikeOriginal)
                    _dragExceeded = true;
            }

            if (_dragActive && mouse.LeftUp)
            {
                _dragCurrent = mouse.Position;
                bool additive = C2NeutralPeasantUnitsV2ShiftAddLikeOriginal();

                Camera[] cameras = C2NeutralPeasantUnitsV2GetPickCamerasLikeOriginal(mouse.Position);
                if (cameras == null || cameras.Length == 0)
                {
                    Debug.Log("[C2:NEUTRAL PEASANT UNIT PICK V33] miss camera=null");
                    _dragActive = false;
                    _dragExceeded = false;
                    return;
                }

                if (_dragExceeded)
                    SelectDragRectLikeOriginal(C2NeutralPeasantUnitsV2ScreenRectFromPointsLikeOriginal(_dragStart, _dragCurrent), cameras, additive);
                else
                    SelectSingleAtPointLikeOriginal(mouse.Position, cameras, additive);

                _dragActive = false;
                _dragExceeded = false;
                return;
            }

            if (mouse.RightDown)
            {
                if (_selectedUnits.Count == 0)
                {
                    Debug.Log("[C2:NEUTRAL PEASANT UNIT MOVE V33] miss no_movable_selected_units selectedBuildings=" +
                              _selectedBuildings.Count.ToString(CultureInfo.InvariantCulture));
                    return;
                }

                Camera[] cameras = C2NeutralPeasantUnitsV2GetPickCamerasLikeOriginal(mouse.Position);
                if (cameras == null || cameras.Length == 0)
                {
                    Debug.Log("[C2:NEUTRAL PEASANT UNIT MOVE V33] miss camera=null selected=" +
                              _selectedUnits.Count.ToString(CultureInfo.InvariantCulture));
                    return;
                }

                MoveSelectedGroupLikeOriginal(mouse.Position, cameras);
            }
        }

        private void LateUpdate()
        {
            if (_duplicateDead) return;
            if (!HasBattleUnitsForOverlayV16LikeOriginal())
            {
                if (_screenOverlayRoot != null) _screenOverlayRoot.gameObject.SetActive(false);
                return;
            }
            // V18: Canvas overlay disabled; screen feedback is IMGUI only.
            // V15: original selection rectangle is a screen-space UI rectangle, not a terrain/world rectangle.
            // The previous LineRenderer frame was misleading and is intentionally disabled.
            // UpdateWorldDragFrameV15LikeOriginal();
        }

        private void OnGUI()
        {
            if (_duplicateDead) return;
            if (!HasBattleUnitsForOverlayV16LikeOriginal()) return;
            int oldDepth = GUI.depth;
            GUI.depth = -100000;

            if (_dragActive && _dragExceeded)
            {
                // Strong IMGUI fallback: visible even if the ScreenSpaceOverlay canvas is hidden
                // behind another project UI layer in Game View.
                Rect screenRect = C2NeutralPeasantUnitsV2ScreenRectFromPointsLikeOriginal(_dragStart, _dragCurrent);
                DrawSelectionRectGuiLikeOriginal(screenRect);
            }

            // V28: selected unit marker must be visible even when the old ScreenSpaceOverlay
            // canvas path is disabled. This draws the original-like SELTYPE oval under feet.
            DrawSelectedMarkersGuiV10LikeOriginal();
            DrawSelectedCounterGuiV10LikeOriginal();
            DrawMoveFeedbackGuiV10LikeOriginal();
            GUI.depth = oldDepth;
        }

        private void SelectSingleAtPointLikeOriginal(Vector3 mousePosition, Camera[] cameras, bool additive)
        {
            C2NeutralPeasantUnitInfoV2LikeOriginal hit;
            float hitAlpha;
            Vector2 hitUv;
            string hitMode;
            PickMissInfoLikeOriginal missInfo;

            if (TryPickUnitAtScreenPointLikeOriginal(mousePosition, cameras, out hit, out hitAlpha, out hitUv, out hitMode, out missInfo))
            {
                var list = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(1);
                list.Add(hit);
                SetSelectionLikeOriginal(list, additive);
                Debug.Log(hit.DebugPickLine(hitAlpha, hitUv) + " mode=" + hitMode +
                          " selectedCount=" + _selectedUnits.Count.ToString(CultureInfo.InvariantCulture));
                return;
            }

            C2SettlementBuildingSelectableV1LikeOriginal buildingHit;
            float buildingDist;
            string buildingMode;
            if (TryPickBuildingAtScreenPointLikeOriginal(mousePosition, cameras, out buildingHit, out buildingDist, out buildingMode))
            {
                var buildings = new List<C2SettlementBuildingSelectableV1LikeOriginal>(1);
                buildings.Add(buildingHit);
                SetSelectionLikeOriginal(new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(0), buildings, additive);
                Debug.Log(buildingHit.DebugPickLineLikeOriginal(buildingDist) + " mode=" + buildingMode +
                          " selectedUnits=" + _selectedUnits.Count.ToString(CultureInfo.InvariantCulture) +
                          " selectedBuildings=" + _selectedBuildings.Count.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (!additive)
                SetSelectionLikeOriginal(new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(0), new List<C2SettlementBuildingSelectableV1LikeOriginal>(0), false);

            Debug.Log("[C2:NEUTRAL PEASANT UNIT PICK V33] miss pixelAlpha=no_visible_unit mouse=(" +
                      mousePosition.x.ToString("0", CultureInfo.InvariantCulture) + "," +
                      mousePosition.y.ToString("0", CultureInfo.InvariantCulture) + ") units=" +
                      C2NeutralPeasantUnitsV2FindUnitsLikeOriginal().Length.ToString(CultureInfo.InvariantCulture) +
                      " cameras=" + (cameras != null ? cameras.Length.ToString(CultureInfo.InvariantCulture) : "0") +
                      " nearestIdx=" + missInfo.NearestIdx.ToString(CultureInfo.InvariantCulture) +
                      " nearestDistPx=" + (float.IsInfinity(missInfo.NearestDist) ? "inf" : missInfo.NearestDist.ToString("0.0", CultureInfo.InvariantCulture)) +
                      " nearestAnchor=(" + missInfo.NearestAnchor.x.ToString("0", CultureInfo.InvariantCulture) + "," +
                      missInfo.NearestAnchor.y.ToString("0", CultureInfo.InvariantCulture) + ")" +
                      " nearestRect=(" + missInfo.NearestRect.x.ToString("0", CultureInfo.InvariantCulture) + "," +
                      missInfo.NearestRect.y.ToString("0", CultureInfo.InvariantCulture) + "," +
                      missInfo.NearestRect.z.ToString("0", CultureInfo.InvariantCulture) + "," +
                      missInfo.NearestRect.w.ToString("0", CultureInfo.InvariantCulture) + ")" +
                      " nearestCam='" + missInfo.NearestCam + "'");
        }

        private void SelectDragRectLikeOriginal(Rect selectionRectScreen, Camera[] cameras, bool additive)
        {
            var result = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(64);
            var seen = new HashSet<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            var buildingResult = new List<C2SettlementBuildingSelectableV1LikeOriginal>(64);
            var seenBuildings = new HashSet<C2SettlementBuildingSelectableV1LikeOriginal>();

            var units = C2NeutralPeasantUnitsV2FindUnitsLikeOriginal();
            var buildings = C2NeutralPeasantUnitsV2FindBuildingsLikeOriginal();
            if ((units == null || units.Length == 0) && (buildings == null || buildings.Length == 0))
            {
                if (!additive) SetSelectionLikeOriginal(result, buildingResult, false);
                Debug.Log("[C2:NEUTRAL PEASANT GROUP SELECT V19] miss no_units_or_buildings rect=" +
                          C2NeutralPeasantUnitsV2RectToLogLikeOriginal(selectionRectScreen));
                return;
            }

            if (units != null) Array.Sort(units, C2NeutralPeasantUnitPickerV2SortLikeOriginal);
            if (buildings != null) Array.Sort(buildings, C2NeutralPeasantUnitPickerV2BuildingSortLikeOriginal);

            for (int c = 0; c < cameras.Length; c++)
            {
                Camera cam = cameras[c];
                if (cam == null) continue;

                if (units != null)
                {
                    for (int i = 0; i < units.Length; i++)
                    {
                        C2NeutralPeasantUnitInfoV2LikeOriginal u = units[i];
                        if (u == null || !u.isActiveAndEnabled || u.NotSelectable || seen.Contains(u)) continue;

                        float dist;
                        Vector2 anchor;
                        Vector4 rect4;
                        if (!u.TryGetScreenQuadDistance(cam, Vector3.zero, out dist, out anchor, out rect4))
                            continue;

                        Rect unitRect = Rect.MinMaxRect(rect4.x, rect4.y, rect4.z, rect4.w);
                        if (!selectionRectScreen.Overlaps(unitRect, true))
                            continue;

                        seen.Add(u);
                        result.Add(u);
                    }
                }

                if (buildings != null)
                {
                    for (int i = 0; i < buildings.Length; i++)
                    {
                        C2SettlementBuildingSelectableV1LikeOriginal b = buildings[i];
                        if (b == null || !b.isActiveAndEnabled || b.NotSelectable || seenBuildings.Contains(b)) continue;

                        Rect buildingRect;
                        if (!b.TryGetScreenRectLikeOriginal(cam, out buildingRect))
                            continue;

                        if (!selectionRectScreen.Overlaps(buildingRect, true))
                            continue;

                        seenBuildings.Add(b);
                        buildingResult.Add(b);
                    }
                }
            }

            SetSelectionLikeOriginal(result, buildingResult, additive);

            Debug.Log("[C2:NEUTRAL PEASANT GROUP SELECT V19] units=" +
                      result.Count.ToString(CultureInfo.InvariantCulture) +
                      " buildings=" + buildingResult.Count.ToString(CultureInfo.InvariantCulture) +
                      " selectedUnits=" + _selectedUnits.Count.ToString(CultureInfo.InvariantCulture) +
                      " selectedBuildings=" + _selectedBuildings.Count.ToString(CultureInfo.InvariantCulture) +
                      " additive=" + additive +
                      " rect=" + C2NeutralPeasantUnitsV2RectToLogLikeOriginal(selectionRectScreen) +
                      " note=original_like_CmdCreateGoodSelection_screen_rect_overlap");
        }

        private bool TryPickUnitAtScreenPointLikeOriginal(
            Vector3 mousePosition,
            Camera[] cameras,
            out C2NeutralPeasantUnitInfoV2LikeOriginal hit,
            out float hitAlpha,
            out Vector2 hitUv,
            out string hitMode,
            out PickMissInfoLikeOriginal missInfo)
        {
            hit = null;
            hitAlpha = 0.0f;
            hitUv = Vector2.zero;
            hitMode = "pixelAlpha";
            missInfo = new PickMissInfoLikeOriginal();
            missInfo.NearestDist = float.PositiveInfinity;
            missInfo.NearestIdx = -1;
            missInfo.NearestCam = "<none>";

            var units = C2NeutralPeasantUnitsV2FindUnitsLikeOriginal();
            if (units == null || units.Length == 0)
            {
                Debug.Log("[C2:NEUTRAL PEASANT UNIT PICK V33] miss no_units");
                return false;
            }

            Array.Sort(units, C2NeutralPeasantUnitPickerV2SortLikeOriginal);

            for (int c = 0; c < cameras.Length && hit == null; c++)
            {
                Camera cam = cameras[c];
                if (cam == null) continue;

                for (int i = 0; i < units.Length; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = units[i];
                    if (u == null || !u.isActiveAndEnabled || u.NotSelectable) continue;

                    float alpha;
                    Vector2 uv;
                    if (u.TryPixelHit(cam, mousePosition, out alpha, out uv))
                    {
                        hit = u;
                        hitAlpha = alpha;
                        hitUv = uv;
                        hitMode = "pixelAlpha camera='" + cam.name + "'";
                        return true;
                    }
                }
            }

            // Fallback: original CheckCoorInGP operates in registered screen sprite space.
            // Use a small radius around the projected sprite rect when decoded alpha is unreadable
            // or the current frame is very thin.
            for (int c = 0; c < cameras.Length; c++)
            {
                Camera cam = cameras[c];
                if (cam == null) continue;

                for (int i = 0; i < units.Length; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = units[i];
                    if (u == null || !u.isActiveAndEnabled || u.NotSelectable) continue;

                    float dist;
                    Vector2 anchor;
                    Vector4 rect;
                    if (!u.TryGetScreenQuadDistance(cam, mousePosition, out dist, out anchor, out rect))
                        continue;

                    if (dist < missInfo.NearestDist)
                    {
                        missInfo.NearestDist = dist;
                        missInfo.NearestIdx = u.RecordIndex;
                        missInfo.NearestAnchor = anchor;
                        missInfo.NearestRect = rect;
                        missInfo.NearestCam = cam.name;
                    }

                    if (dist <= V8FallbackPickRadiusPixelsLikeOriginal)
                    {
                        hit = u;
                        hitAlpha = 1.0f;
                        hitUv = new Vector2(0.5f, 0.5f);
                        hitMode = "screenQuadFallback camera='" + cam.name + "' distPx=" +
                                  dist.ToString("0.0", CultureInfo.InvariantCulture);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryPickBuildingAtScreenPointLikeOriginal(
            Vector3 mousePosition,
            Camera[] cameras,
            out C2SettlementBuildingSelectableV1LikeOriginal hit,
            out float hitDist,
            out string hitMode)
        {
            hit = null;
            hitDist = float.PositiveInfinity;
            hitMode = "screenRect";

            var buildings = C2NeutralPeasantUnitsV2FindBuildingsLikeOriginal();
            if (buildings == null || buildings.Length == 0)
                return false;

            Array.Sort(buildings, C2NeutralPeasantUnitPickerV2BuildingSortLikeOriginal);

            for (int c = 0; c < cameras.Length; c++)
            {
                Camera cam = cameras[c];
                if (cam == null) continue;

                for (int i = 0; i < buildings.Length; i++)
                {
                    C2SettlementBuildingSelectableV1LikeOriginal b = buildings[i];
                    if (b == null || !b.isActiveAndEnabled || b.NotSelectable) continue;

                    Rect rect;
                    float dist;
                    if (!b.TryPickScreenPointLikeOriginal(cam, mousePosition, out rect, out dist))
                        continue;

                    hit = b;
                    hitDist = dist;
                    hitMode = "screenRect camera='" + cam.name + "' rect=" +
                              C2NeutralPeasantUnitsV2RectToLogLikeOriginal(rect);
                    return true;
                }
            }

            return false;
        }

        private void SetSelectionLikeOriginal(List<C2NeutralPeasantUnitInfoV2LikeOriginal> units, bool additive)
        {
            SetSelectionLikeOriginal(units, new List<C2SettlementBuildingSelectableV1LikeOriginal>(0), additive);
        }

        private void SetSelectionLikeOriginal(
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            List<C2SettlementBuildingSelectableV1LikeOriginal> buildings,
            bool additive)
        {
            if (units == null) units = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(0);
            if (buildings == null) buildings = new List<C2SettlementBuildingSelectableV1LikeOriginal>(0);

            var next = new HashSet<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            var nextBuildings = new HashSet<C2SettlementBuildingSelectableV1LikeOriginal>();
            if (additive)
            {
                for (int i = 0; i < _selectedUnits.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = _selectedUnits[i];
                    if (u != null && u.isActiveAndEnabled) next.Add(u);
                }

                for (int i = 0; i < _selectedBuildings.Count; i++)
                {
                    C2SettlementBuildingSelectableV1LikeOriginal b = _selectedBuildings[i];
                    if (b != null && b.isActiveAndEnabled) nextBuildings.Add(b);
                }
            }

            for (int i = 0; i < units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = units[i];
                if (u != null && u.isActiveAndEnabled) next.Add(u);
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = buildings[i];
                if (b != null && b.isActiveAndEnabled) nextBuildings.Add(b);
            }

            for (int i = 0; i < _selectedUnits.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selectedUnits[i];
                if (u != null && !next.Contains(u)) u.SetSelected(false);
            }

            for (int i = 0; i < _selectedBuildings.Count; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = _selectedBuildings[i];
                if (b != null && !nextBuildings.Contains(b)) b.SetSelected(false);
            }

            _selectedUnits.Clear();
            foreach (var u in next)
            {
                if (u == null || !u.isActiveAndEnabled) continue;
                u.SetSelected(true);
                _selectedUnits.Add(u);
            }

            _selectedUnits.Sort(C2NeutralPeasantUnitPickerV2SortLikeOriginal);

            _selectedBuildings.Clear();
            foreach (var b in nextBuildings)
            {
                if (b == null || !b.isActiveAndEnabled) continue;
                b.SetSelected(true);
                _selectedBuildings.Add(b);
            }

            _selectedBuildings.Sort(C2NeutralPeasantUnitPickerV2BuildingSortLikeOriginal);
        }

        private void CleanupSelectionListLikeOriginal()
        {
            for (int i = _selectedUnits.Count - 1; i >= 0; i--)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selectedUnits[i];
                if (u == null || !u.isActiveAndEnabled)
                    _selectedUnits.RemoveAt(i);
            }

            for (int i = _selectedBuildings.Count - 1; i >= 0; i--)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = _selectedBuildings[i];
                if (b == null || !b.isActiveAndEnabled)
                    _selectedBuildings.RemoveAt(i);
            }
        }

        private void MoveSelectedGroupLikeOriginal(Vector3 mousePosition, Camera[] cameras)
        {
            CleanupSelectionListLikeOriginal();
            if (_selectedUnits.Count == 0)
            {
                Debug.Log("[C2:NEUTRAL PEASANT UNIT MOVE V33] miss no_selected_units");
                return;
            }

            float centroidRealX = 0.0f;
            float centroidRealY = 0.0f;
            int count = 0;
            float planeY = 0.0f;
            for (int i = 0; i < _selectedUnits.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selectedUnits[i];
                if (u == null) continue;
                centroidRealX += u.RealXFloat;
                centroidRealY += u.RealYFloat;
                planeY += u.transform.position.y;
                count++;
            }

            if (count == 0)
            {
                Debug.Log("[C2:NEUTRAL PEASANT UNIT MOVE V33] miss selected_destroyed");
                return;
            }

            centroidRealX /= count;
            centroidRealY /= count;
            planeY /= count;

            Vector3 movePoint;
            Camera moveCam;
            if (!C2NeutralPeasantUnitsV2TryScreenToMovePointLikeOriginal(mousePosition, cameras, planeY, out movePoint, out moveCam))
            {
                Debug.Log("[C2:NEUTRAL PEASANT UNIT MOVE V33] miss screen_to_world_failed selected=" +
                          count.ToString(CultureInfo.InvariantCulture));
                return;
            }

            C2BattleTerrainMode mode = null;
            for (int i = 0; i < _selectedUnits.Count && mode == null; i++)
            {
                if (_selectedUnits[i] != null) mode = _selectedUnits[i].OwnerMode;
            }
            if (mode == null)
            {
                Debug.Log("[C2:NEUTRAL PEASANT UNIT MOVE V33] miss owner_mode_null selected=" +
                          count.ToString(CultureInfo.InvariantCulture));
                return;
            }

            float destPxX;
            float destPxY;
            if (!mode.C2NeutralPeasantUnitsV2WorldToOriginalPixelV15LikeOriginal(movePoint, out destPxX, out destPxY))
            {
                Debug.Log("[C2:NEUTRAL PEASANT UNIT MOVE V33] miss world_to_original_failed selected=" +
                          count.ToString(CultureInfo.InvariantCulture));
                return;
            }

            float destRealCenterX = destPxX * 16.0f;
            float destRealCenterY = destPxY * 16.0f;

            _lastMoveScreenPoint = mousePosition;
            _lastMoveFeedbackUntil = Time.realtimeSinceStartup + 1.35f;
            _lastMoveFeedbackText = "MOVE " + count.ToString(CultureInfo.InvariantCulture);

            for (int i = 0; i < _selectedUnits.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selectedUnits[i];
                if (u == null) continue;

                float offX = u.RealXFloat - centroidRealX;
                float offY = u.RealYFloat - centroidRealY;
                u.SetMoveDestinationRealLikeOriginal(destRealCenterX + offX,
                                                     destRealCenterY + offY,
                                                     C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal);
            }

            Debug.Log("[C2:NEUTRAL PEASANT UNIT MOVE V33] selected=" +
                      count.ToString(CultureInfo.InvariantCulture) +
                      " targetOriginalPx=(" + destPxX.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                      destPxY.ToString("0.0", CultureInfo.InvariantCulture) + ")" +
                      " targetReal=(" + destRealCenterX.ToString("0", CultureInfo.InvariantCulture) + "," +
                      destRealCenterY.ToString("0", CultureInfo.InvariantCulture) + ")" +
                      " camera='" + (moveCam != null ? moveCam.name : "<none>") + "'" +
                      " mode=RealXRealY_DestXDestY_noRootTeleport_exactSnap speedOriginalPx=" +
                      C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal.ToString("0.0", CultureInfo.InvariantCulture) +
                      " anim=#MOTION_L direction=targetSegment_noTurnSmoothing cameraDoesNotControlDirection=true screenGuiSelectionOnly=true");
        }

        private static bool C2NeutralPeasantUnitsV2TryScreenToMovePointLikeOriginal(
            Vector3 mousePosition,
            Camera[] cameras,
            float planeY,
            out Vector3 world,
            out Camera usedCamera)
        {
            world = Vector3.zero;
            usedCamera = null;
            if (cameras == null) return false;

            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                if (cam == null) continue;

                Ray ray = cam.ScreenPointToRay(mousePosition);

                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 20000.0f, ~0, QueryTriggerInteraction.Ignore))
                {
                    string hn = hit.collider != null ? hit.collider.name ?? string.Empty : string.Empty;
                    if (hn.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        hn.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        hn.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        world = hit.point;
                        usedCamera = cam;
                        return true;
                    }
                }

                Plane plane = new Plane(Vector3.up, new Vector3(0.0f, planeY, 0.0f));
                float enter;
                if (!plane.Raycast(ray, out enter) || enter < 0.0f) continue;

                world = ray.GetPoint(enter);
                world.y = planeY;
                usedCamera = cam;
                return true;
            }

            return false;
        }

        private static C2NeutralPeasantUnitInfoV2LikeOriginal[] C2NeutralPeasantUnitsV2FindUnitsLikeOriginal()
        {
            return UnityEngine.Object.FindObjectsOfType<C2NeutralPeasantUnitInfoV2LikeOriginal>();
        }

        private static C2SettlementBuildingSelectableV1LikeOriginal[] C2NeutralPeasantUnitsV2FindBuildingsLikeOriginal()
        {
            return UnityEngine.Object.FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
        }

        private static Rect C2NeutralPeasantUnitsV2ScreenRectFromPointsLikeOriginal(Vector3 a, Vector3 b)
        {
            float x0 = Mathf.Min(a.x, b.x);
            float x1 = Mathf.Max(a.x, b.x);
            float y0 = Mathf.Min(a.y, b.y);
            float y1 = Mathf.Max(a.y, b.y);
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        private static string C2NeutralPeasantUnitsV2RectToLogLikeOriginal(Rect r)
        {
            return "(" + r.xMin.ToString("0", CultureInfo.InvariantCulture) + "," +
                   r.yMin.ToString("0", CultureInfo.InvariantCulture) + "," +
                   r.xMax.ToString("0", CultureInfo.InvariantCulture) + "," +
                   r.yMax.ToString("0", CultureInfo.InvariantCulture) + ")";
        }

        private static void DrawSelectionRectGuiLikeOriginal(Rect screenRectBottomLeft)
        {
            if (_guiPixel == null)
            {
                _guiPixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _guiPixel.name = "C2_NeutralPeasant_SelectionRectPixel_V10";
                _guiPixel.SetPixel(0, 0, Color.white);
                _guiPixel.Apply(false, true);
            }

            Rect r = new Rect(
                screenRectBottomLeft.xMin,
                Screen.height - screenRectBottomLeft.yMax,
                Mathf.Max(1.0f, screenRectBottomLeft.width),
                Mathf.Max(1.0f, screenRectBottomLeft.height));

            Color old = GUI.color;

            // Original mapa.cpp draws translucent fill while the LMB selection bar is active:
            // GPS.DrawFillRect(VS.x,VS.y,x-VS.x,y-VS.y,0x60000000).
            GUI.color = new Color(0.0f, 0.0f, 0.0f, 0.38f);
            GUI.DrawTexture(r, _guiPixel);

            GUI.color = new Color(1.0f, 1.0f, 0.0f, 1.0f);
            float t = 2.0f;
            GUI.DrawTexture(new Rect(r.xMin, r.yMin, r.width, t), _guiPixel);
            GUI.DrawTexture(new Rect(r.xMin, r.yMax - t, r.width, t), _guiPixel);
            GUI.DrawTexture(new Rect(r.xMin, r.yMin, t, r.height), _guiPixel);
            GUI.DrawTexture(new Rect(r.xMax - t, r.yMin, t, r.height), _guiPixel);

            GUI.color = old;
        }



        private void DrawSelectedMarkersGuiV10LikeOriginal()
        {
            CleanupSelectionListLikeOriginal();
            if (_selectedUnits.Count == 0) return;

            for (int i = 0; i < _selectedUnits.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selectedUnits[i];
                if (u == null || !u.isActiveAndEnabled) continue;

                Vector2 anchor;
                Vector2 size;
                if (!TryGetUnitSelectionMarkerScreenRectV10LikeOriginal(u, out anchor, out size))
                    continue;

                // V28: original marker is a terrain patch under the feet. Draw a visible oval,
                // not a body rectangle, so a selected peasant clearly has a circle under legs.
                size.x = Mathf.Clamp(size.x, 34.0f, 72.0f);
                size.y = Mathf.Clamp(size.y, 16.0f, 34.0f);

                Rect bottomLeftRect = new Rect(anchor.x - size.x * 0.5f, anchor.y - size.y * 0.5f, size.x, size.y);
                DrawGuiOvalRingLikeOriginal(
                    bottomLeftRect,
                    new Color(1.0f, 0.92f, 0.05f, 0.30f),
                    new Color(1.0f, 1.0f, 0.0f, 1.0f),
                    3.0f);
            }
        }

        private void DrawMoveFeedbackGuiV10LikeOriginal()
        {
            if (Time.realtimeSinceStartup > _lastMoveFeedbackUntil) return;

            float x = _lastMoveScreenPoint.x;
            float y = _lastMoveScreenPoint.y;
            float s = 18.0f;

            DrawGuiFilledFrameRectV10LikeOriginal(
                new Rect(x - s, y - s, s * 2.0f, s * 2.0f),
                new Color(0.1f, 1.0f, 0.1f, 0.20f),
                new Color(0.2f, 1.0f, 0.1f, 1.0f),
                3.0f);

            DrawGuiLineRectV10LikeOriginal(new Rect(x - 2.0f, y - s * 1.45f, 4.0f, s * 2.9f), new Color(0.2f, 1.0f, 0.1f, 1.0f));
            DrawGuiLineRectV10LikeOriginal(new Rect(x - s * 1.45f, y - 2.0f, s * 2.9f, 4.0f), new Color(0.2f, 1.0f, 0.1f, 1.0f));
        }

        private void DrawSelectedCounterGuiV10LikeOriginal()
        {
            if (_selectedUnits.Count <= 0) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.yellow;

            string text = _selectedUnits.Count == 1
                ? "SELECTED: 1 PEASANT"
                : "SELECTED: " + _selectedUnits.Count.ToString(CultureInfo.InvariantCulture) + " PEASANTS";

            GUI.color = Color.black;
            GUI.Label(new Rect(17, 17, 360, 28), text, style);
            GUI.color = Color.yellow;
            GUI.Label(new Rect(15, 15, 360, 28), text, style);
            GUI.color = Color.white;
        }

        private static void DrawGuiOvalRingLikeOriginal(Rect bottomLeftRect, Color fill, Color line, float thickness)
        {
            EnsureGuiPixelV10LikeOriginal();

            Rect r = new Rect(
                bottomLeftRect.xMin,
                Screen.height - bottomLeftRect.yMax,
                Mathf.Max(1.0f, bottomLeftRect.width),
                Mathf.Max(1.0f, bottomLeftRect.height));

            Color old = GUI.color;
            int rows = Mathf.Clamp(Mathf.CeilToInt(r.height), 12, 64);
            float cx = r.xMin + r.width * 0.5f;
            float cy = r.yMin + r.height * 0.5f;
            float rx = Mathf.Max(1.0f, r.width * 0.5f);
            float ry = Mathf.Max(1.0f, r.height * 0.5f);
            float t = Mathf.Max(1.0f, thickness);

            for (int i = 0; i < rows; i++)
            {
                float y0 = r.yMin + (i / (float)rows) * r.height;
                float y1 = r.yMin + ((i + 1) / (float)rows) * r.height;
                float yy = ((y0 + y1) * 0.5f - cy) / ry;
                float k = 1.0f - yy * yy;
                if (k <= 0.0f) continue;

                float halfOuter = Mathf.Sqrt(k) * rx;
                float innerRx = Mathf.Max(0.0f, rx - t);
                float innerRy = Mathf.Max(0.0f, ry - t);
                float halfInner = 0.0f;
                if (innerRx > 0.0f && innerRy > 0.0f)
                {
                    float iyy = ((y0 + y1) * 0.5f - cy) / innerRy;
                    float ik = 1.0f - iyy * iyy;
                    if (ik > 0.0f) halfInner = Mathf.Sqrt(ik) * innerRx;
                }

                GUI.color = fill;
                GUI.DrawTexture(new Rect(cx - halfOuter, y0, halfOuter * 2.0f, Mathf.Max(1.0f, y1 - y0)), _guiPixel);

                GUI.color = line;
                if (halfInner <= 0.0f || halfOuter - halfInner >= 1.0f)
                {
                    GUI.DrawTexture(new Rect(cx - halfOuter, y0, Mathf.Max(1.0f, halfOuter - halfInner), Mathf.Max(1.0f, y1 - y0)), _guiPixel);
                    GUI.DrawTexture(new Rect(cx + halfInner, y0, Mathf.Max(1.0f, halfOuter - halfInner), Mathf.Max(1.0f, y1 - y0)), _guiPixel);
                }
            }

            GUI.color = old;
        }

        private static void DrawGuiFilledFrameRectV10LikeOriginal(Rect bottomLeftRect, Color fill, Color line, float thickness)
        {
            EnsureGuiPixelV10LikeOriginal();

            Rect r = new Rect(
                bottomLeftRect.xMin,
                Screen.height - bottomLeftRect.yMax,
                Mathf.Max(1.0f, bottomLeftRect.width),
                Mathf.Max(1.0f, bottomLeftRect.height));

            Color old = GUI.color;

            GUI.color = fill;
            GUI.DrawTexture(r, _guiPixel);

            GUI.color = line;
            float t = Mathf.Max(1.0f, thickness);
            GUI.DrawTexture(new Rect(r.xMin, r.yMin, r.width, t), _guiPixel);
            GUI.DrawTexture(new Rect(r.xMin, r.yMax - t, r.width, t), _guiPixel);
            GUI.DrawTexture(new Rect(r.xMin, r.yMin, t, r.height), _guiPixel);
            GUI.DrawTexture(new Rect(r.xMax - t, r.yMin, t, r.height), _guiPixel);

            GUI.color = old;
        }

        private static void DrawGuiLineRectV10LikeOriginal(Rect bottomLeftRect, Color color)
        {
            EnsureGuiPixelV10LikeOriginal();

            Rect r = new Rect(
                bottomLeftRect.xMin,
                Screen.height - bottomLeftRect.yMax,
                Mathf.Max(1.0f, bottomLeftRect.width),
                Mathf.Max(1.0f, bottomLeftRect.height));

            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, _guiPixel);
            GUI.color = old;
        }

        private static void EnsureGuiPixelV10LikeOriginal()
        {
            if (_guiPixel != null) return;

            _guiPixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _guiPixel.name = "C2_NeutralPeasant_SelectionRectPixel_V10";
            _guiPixel.SetPixel(0, 0, Color.white);
            _guiPixel.Apply(false, true);
        }



        private void UpdateWorldDragFrameV15LikeOriginal()
        {
            EnsureWorldDragFrameV15LikeOriginal();
            if (_worldDragFrameRoot == null || _worldDragLines == null) return;

            bool show = _dragActive && _dragExceeded;
            _worldDragFrameRoot.SetActive(show);
            if (!show) return;

            Camera[] cameras = C2NeutralPeasantUnitsV2GetPickCamerasLikeOriginal(_dragCurrent);
            if (cameras == null || cameras.Length == 0) return;

            Rect r = C2NeutralPeasantUnitsV2ScreenRectFromPointsLikeOriginal(_dragStart, _dragCurrent);
            float planeY = 50.0f;
            if (_selectedUnits.Count > 0 && _selectedUnits[0] != null) planeY = _selectedUnits[0].transform.position.y;
            else
            {
                var any = UnityEngine.Object.FindObjectOfType<C2NeutralPeasantUnitInfoV2LikeOriginal>();
                if (any != null) planeY = any.transform.position.y;
            }

            Vector3 p0, p1, p2, p3;
            Camera c;
            if (!C2NeutralPeasantUnitsV2TryScreenToMovePointLikeOriginal(new Vector3(r.xMin, r.yMin, 0.0f), cameras, planeY, out p0, out c)) return;
            if (!C2NeutralPeasantUnitsV2TryScreenToMovePointLikeOriginal(new Vector3(r.xMax, r.yMin, 0.0f), cameras, planeY, out p1, out c)) return;
            if (!C2NeutralPeasantUnitsV2TryScreenToMovePointLikeOriginal(new Vector3(r.xMax, r.yMax, 0.0f), cameras, planeY, out p2, out c)) return;
            if (!C2NeutralPeasantUnitsV2TryScreenToMovePointLikeOriginal(new Vector3(r.xMin, r.yMax, 0.0f), cameras, planeY, out p3, out c)) return;

            const float raise = 1.15f;
            p0.y += raise; p1.y += raise; p2.y += raise; p3.y += raise;
            SetWorldDragLineV15LikeOriginal(0, p0, p1);
            SetWorldDragLineV15LikeOriginal(1, p1, p2);
            SetWorldDragLineV15LikeOriginal(2, p2, p3);
            SetWorldDragLineV15LikeOriginal(3, p3, p0);
        }

        private void EnsureWorldDragFrameV15LikeOriginal()
        {
            if (_worldDragFrameRoot != null) return;

            _worldDragFrameRoot = new GameObject("C2_NeutralPeasantUnits_V15_WorldDragSelectionFrame");
            _worldDragFrameRoot.hideFlags = HideFlags.HideAndDontSave;

            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Standard");
            _worldDragLineMaterial = new Material(sh);
            _worldDragLineMaterial.name = "C2_NeutralPeasantUnits_V15_WorldDragLine_Yellow";
            if (_worldDragLineMaterial.HasProperty("_Color")) _worldDragLineMaterial.SetColor("_Color", new Color(1.0f, 1.0f, 0.0f, 1.0f));
            _worldDragLineMaterial.renderQueue = 5000;

            _worldDragLines = new LineRenderer[4];
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("world_drag_line_" + i.ToString(CultureInfo.InvariantCulture));
                go.transform.SetParent(_worldDragFrameRoot.transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.startWidth = 2.80f;
                lr.endWidth = 2.80f;
                lr.numCapVertices = 2;
                lr.numCornerVertices = 2;
                lr.shadowCastingMode = ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.material = _worldDragLineMaterial;
                lr.startColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);
                lr.endColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);
                _worldDragLines[i] = lr;
            }
            _worldDragFrameRoot.SetActive(false);
        }

        private void SetWorldDragLineV15LikeOriginal(int index, Vector3 a, Vector3 b)
        {
            if (_worldDragLines == null || index < 0 || index >= _worldDragLines.Length || _worldDragLines[index] == null) return;
            _worldDragLines[index].SetPosition(0, a);
            _worldDragLines[index].SetPosition(1, b);
        }

        private void UpdateScreenOverlayV10LikeOriginal()
        {
            EnsureScreenOverlayV10LikeOriginal();
            if (_screenOverlayRoot == null) return;

            UpdateDragSelectionOverlayV10LikeOriginal();
            UpdateSelectedMarkersOverlayV10LikeOriginal();
        }

        private void EnsureScreenOverlayV10LikeOriginal()
        {
            if (_screenOverlayRoot != null) return;

            const string canvasName = "C2_NeutralPeasantUnits_V18_ScreenOverlay";
            GameObject canvasGo = GameObject.Find(canvasName);
            if (canvasGo == null)
            {
                canvasGo = new GameObject(canvasName);
            }

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1.0f;

            Transform oldRoot = canvasGo.transform.Find("Root");
            GameObject rootGo;
            if (oldRoot != null)
                rootGo = oldRoot.gameObject;
            else
            {
                rootGo = new GameObject("Root");
                rootGo.transform.SetParent(canvasGo.transform, false);
            }

            _screenOverlayRoot = rootGo.GetComponent<RectTransform>();
            if (_screenOverlayRoot == null) _screenOverlayRoot = rootGo.AddComponent<RectTransform>();
            _screenOverlayRoot.anchorMin = Vector2.zero;
            _screenOverlayRoot.anchorMax = Vector2.one;
            _screenOverlayRoot.offsetMin = Vector2.zero;
            _screenOverlayRoot.offsetMax = Vector2.zero;
            _screenOverlayRoot.pivot = new Vector2(0.5f, 0.5f);

            _dragFillImage = CreateOverlayImageV10LikeOriginal("drag_fill_black", _screenOverlayRoot, new Color(0.0f, 0.0f, 0.0f, 0.42f));
            _dragTopLine = CreateOverlayImageV10LikeOriginal("drag_line_top_yellow", _screenOverlayRoot, new Color(1.0f, 1.0f, 0.0f, 1.0f)).rectTransform;
            _dragBottomLine = CreateOverlayImageV10LikeOriginal("drag_line_bottom_yellow", _screenOverlayRoot, new Color(1.0f, 1.0f, 0.0f, 1.0f)).rectTransform;
            _dragLeftLine = CreateOverlayImageV10LikeOriginal("drag_line_left_yellow", _screenOverlayRoot, new Color(1.0f, 1.0f, 0.0f, 1.0f)).rectTransform;
            _dragRightLine = CreateOverlayImageV10LikeOriginal("drag_line_right_yellow", _screenOverlayRoot, new Color(1.0f, 1.0f, 0.0f, 1.0f)).rectTransform;

            SetDragOverlayVisibleV10LikeOriginal(false);

            if (!_overlayLogged)
            {
                _overlayLogged = true;
                Debug.Log("[C2:NEUTRAL PEASANT SCREEN OVERLAY V18] installed canvas='" + canvasName +
                          "' mode=ScreenSpaceOverlay selectionRect=ui selectedMarkers=ui");
            }
        }

        private static Image CreateOverlayImageV10LikeOriginal(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private void UpdateDragSelectionOverlayV10LikeOriginal()
        {
            bool show = _dragActive && _dragExceeded;
            SetDragOverlayVisibleV10LikeOriginal(show);
            if (!show) return;

            Rect r = C2NeutralPeasantUnitsV2ScreenRectFromPointsLikeOriginal(_dragStart, _dragCurrent);
            r.width = Mathf.Max(1.0f, r.width);
            r.height = Mathf.Max(1.0f, r.height);

            SetOverlayRectV10LikeOriginal(_dragFillImage.rectTransform, r);

            const float t = 9.0f;
            SetOverlayRectV10LikeOriginal(_dragTopLine, new Rect(r.xMin, r.yMax - t, r.width, t));
            SetOverlayRectV10LikeOriginal(_dragBottomLine, new Rect(r.xMin, r.yMin, r.width, t));
            SetOverlayRectV10LikeOriginal(_dragLeftLine, new Rect(r.xMin, r.yMin, t, r.height));
            SetOverlayRectV10LikeOriginal(_dragRightLine, new Rect(r.xMax - t, r.yMin, t, r.height));
        }

        private void SetDragOverlayVisibleV10LikeOriginal(bool visible)
        {
            if (_dragFillImage != null) _dragFillImage.gameObject.SetActive(visible);
            if (_dragTopLine != null) _dragTopLine.gameObject.SetActive(visible);
            if (_dragBottomLine != null) _dragBottomLine.gameObject.SetActive(visible);
            if (_dragLeftLine != null) _dragLeftLine.gameObject.SetActive(visible);
            if (_dragRightLine != null) _dragRightLine.gameObject.SetActive(visible);
        }

        private void UpdateSelectedMarkersOverlayV10LikeOriginal()
        {
            CleanupSelectionListLikeOriginal();

            _markerRemoveScratch.Clear();
            foreach (var kv in _screenSelectionMarkers)
            {
                if (kv.Key == null || !_selectedUnits.Contains(kv.Key))
                    _markerRemoveScratch.Add(kv.Key);
            }

            for (int i = 0; i < _markerRemoveScratch.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal key = _markerRemoveScratch[i];
                RectTransform rt;
                if (_screenSelectionMarkers.TryGetValue(key, out rt))
                {
                    if (rt != null) UnityEngine.Object.Destroy(rt.gameObject);
                    _screenSelectionMarkers.Remove(key);
                }
            }

            for (int i = 0; i < _selectedUnits.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _selectedUnits[i];
                if (u == null || !u.isActiveAndEnabled) continue;

                RectTransform marker;
                if (!_screenSelectionMarkers.TryGetValue(u, out marker) || marker == null)
                {
                    marker = CreateSelectionMarkerUiFrameV10LikeOriginal("selected_unit_marker_" + u.RecordIndex.ToString(CultureInfo.InvariantCulture));
                    _screenSelectionMarkers[u] = marker;
                }

                Vector2 anchor;
                Vector2 size;
                if (TryGetUnitSelectionMarkerScreenRectV10LikeOriginal(u, out anchor, out size))
                {
                    Rect r = new Rect(anchor.x - size.x * 0.5f, anchor.y - size.y * 0.5f, size.x, size.y);
                    SetOverlayRectV10LikeOriginal(marker, r);
                    marker.gameObject.SetActive(true);
                }
                else
                {
                    marker.gameObject.SetActive(false);
                }
            }
        }

        private RectTransform CreateSelectionMarkerUiFrameV10LikeOriginal(string name)
        {
            var rootGo = new GameObject(name);
            rootGo.transform.SetParent(_screenOverlayRoot, false);

            var root = rootGo.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.zero;
            root.pivot = Vector2.zero;
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(24.0f, 12.0f);

            Image fill = CreateOverlayImageV10LikeOriginal("fill_soft_yellow", root, new Color(1.0f, 0.92f, 0.05f, 0.42f));
            StretchChildRectV10LikeOriginal(fill.rectTransform);

            CreateFrameLineV10LikeOriginal(root, "top", true, true);
            CreateFrameLineV10LikeOriginal(root, "bottom", true, false);
            CreateFrameLineV10LikeOriginal(root, "left", false, false);
            CreateFrameLineV10LikeOriginal(root, "right", false, true);

            return root;
        }

        private void CreateFrameLineV10LikeOriginal(RectTransform parent, string name, bool horizontal, bool highSide)
        {
            Image img = CreateOverlayImageV10LikeOriginal("line_" + name, parent, new Color(1.0f, 1.0f, 0.0f, 1.0f));
            RectTransform rt = img.rectTransform;
            const float t = 9.0f;

            if (horizontal)
            {
                rt.anchorMin = new Vector2(0.0f, highSide ? 1.0f : 0.0f);
                rt.anchorMax = new Vector2(1.0f, highSide ? 1.0f : 0.0f);
                rt.pivot = new Vector2(0.5f, highSide ? 1.0f : 0.0f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0.0f, t);
            }
            else
            {
                rt.anchorMin = new Vector2(highSide ? 1.0f : 0.0f, 0.0f);
                rt.anchorMax = new Vector2(highSide ? 1.0f : 0.0f, 1.0f);
                rt.pivot = new Vector2(highSide ? 1.0f : 0.0f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(t, 0.0f);
            }
        }

        private static void StretchChildRectV10LikeOriginal(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetOverlayRectV10LikeOriginal(RectTransform rt, Rect r)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(r.xMin, r.yMin);
            rt.sizeDelta = new Vector2(Mathf.Max(1.0f, r.width), Mathf.Max(1.0f, r.height));
        }

        private static bool TryGetUnitSelectionMarkerScreenRectV10LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            out Vector2 anchor,
            out Vector2 size)
        {
            anchor = Vector2.zero;
            size = Vector2.zero;
            if (unit == null) return false;

            Camera[] all = Camera.allCameras;
            if (all == null || all.Length == 0) return false;

            var cameras = new List<Camera>(all);
            cameras.Sort(C2NeutralPeasantUnitsV2CameraPriorityLikeOriginal);

            for (int i = 0; i < cameras.Count; i++)
            {
                Camera cam = cameras[i];
                if (cam == null || !cam.isActiveAndEnabled) continue;

                float dist;
                Vector2 quadCenter;
                Vector4 rect4;
                if (!unit.TryGetScreenQuadDistance(cam, Vector3.zero, out dist, out quadCenter, out rect4))
                    continue;

                float w = Mathf.Max(1.0f, rect4.z - rect4.x);
                float h = Mathf.Max(1.0f, rect4.w - rect4.y);
                if (w < 1.0f || h < 1.0f) continue;

                // The original DrawMarker marker is under the sprite feet, not around the body.
                // In current billboard quads the lower part of the screen rect is the most stable
                // approximation, so V10 projects a visible UI frame there.
                anchor = new Vector2((rect4.x + rect4.z) * 0.5f, rect4.y + Mathf.Clamp(h * 0.08f, 3.0f, 10.0f));
                size = new Vector2(Mathf.Clamp(w * 0.86f, 34.0f, 72.0f), Mathf.Clamp(h * 0.24f, 16.0f, 34.0f));
                return true;
            }

            return false;
        }

        private bool C2NeutralPeasantUnitsV2ReadMouseStateLikeOriginal(out MouseStateLikeOriginal state)
        {
            state = new MouseStateLikeOriginal();

#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                Vector2 p = mouse.position.ReadValue();
                bool left = mouse.leftButton.isPressed;
                bool right = mouse.rightButton.isPressed;

                state.Position = new Vector3(p.x, p.y, 0.0f);
                state.LeftHeld = left;
                state.LeftDown = (left && !_prevLeftPressed) || mouse.leftButton.wasPressedThisFrame;
                state.LeftUp = (!left && _prevLeftPressed) || mouse.leftButton.wasReleasedThisFrame;
                state.RightDown = (right && !_prevRightPressed) || mouse.rightButton.wasPressedThisFrame;

                _prevLeftPressed = left;
                _prevRightPressed = right;
                return true;
            }

            var pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer != null)
            {
                Vector2 p = pointer.position.ReadValue();
                bool left = pointer.press.isPressed;

                state.Position = new Vector3(p.x, p.y, 0.0f);
                state.LeftHeld = left;
                state.LeftDown = (left && !_prevLeftPressed) || pointer.press.wasPressedThisFrame;
                state.LeftUp = (!left && _prevLeftPressed) || pointer.press.wasReleasedThisFrame;
                state.RightDown = false;

                _prevLeftPressed = left;
                _prevRightPressed = false;
                return true;
            }

            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            bool left = Input.GetMouseButton(0);
            bool right = Input.GetMouseButton(1);

            state.Position = Input.mousePosition;
            state.LeftHeld = left;
            state.LeftDown = Input.GetMouseButtonDown(0) || (left && !_prevLeftPressed);
            state.LeftUp = Input.GetMouseButtonUp(0) || (!left && _prevLeftPressed);
            state.RightDown = Input.GetMouseButtonDown(1) || (right && !_prevRightPressed);

            _prevLeftPressed = left;
            _prevRightPressed = right;
            return true;
#else
            return false;
#endif
        }

        private static bool C2NeutralPeasantUnitsV2ShiftAddLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
            return false;
#endif
        }

        private static Camera[] C2NeutralPeasantUnitsV2GetPickCamerasLikeOriginal(Vector3 mousePosition)
        {
            Camera[] all = Camera.allCameras;
            if (all == null || all.Length == 0)
            {
                Camera main = Camera.main;
                return main != null ? new[] { main } : new Camera[0];
            }

            var list = new List<Camera>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                Camera c = all[i];
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                string n = c.name ?? string.Empty;
                // V18: gameplay commands must use the battle isometric camera. Main/Menu/Free cameras
                // previously produced wrong world targets and visible jumps.
                if (n.IndexOf("C2_BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) < 0 &&
                    n.IndexOf("BattleTerrainCamera_Iso", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                list.Add(c);
            }

            if (list.Count == 0)
            {
                // fallback for renamed battle cameras, but never prefer Main Camera over map camera.
                for (int i = 0; i < all.Length; i++)
                {
                    Camera c = all[i];
                    if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                    string n = c.name ?? string.Empty;
                    if (n.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Main Camera", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    list.Add(c);
                }
            }

            list.Sort(C2NeutralPeasantUnitsV2CameraPriorityLikeOriginal);
            return list.ToArray();
        }

        private static int C2NeutralPeasantUnitsV2CameraPriorityLikeOriginal(Camera a, Camera b)
        {
            int pa = C2NeutralPeasantUnitsV2CameraPickPriorityLikeOriginal(a);
            int pb = C2NeutralPeasantUnitsV2CameraPickPriorityLikeOriginal(b);
            int c = pa.CompareTo(pb);
            if (c != 0) return c;
            return string.Compare(a != null ? a.name : "", b != null ? b.name : "", StringComparison.OrdinalIgnoreCase);
        }

        private static int C2NeutralPeasantUnitsV2CameraPickPriorityLikeOriginal(Camera c)
        {
            if (c == null) return 1000;
            string n = c.name ?? "";
            if (n.IndexOf("BattleTerrain", StringComparison.OrdinalIgnoreCase) >= 0) return 0;
            if (n.IndexOf("Iso", StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            if (c == Camera.main) return 10;
            return 20;
        }

        private static bool C2NeutralPeasantUnitsV2CameraCanPickLikeOriginal(Camera cam, Vector3 mousePosition)
        {
            if (cam == null || !cam.isActiveAndEnabled) return false;

            Rect r = cam.pixelRect;
            if (r.width <= 1.0f || r.height <= 1.0f) return false;

            return mousePosition.x >= r.xMin - 8.0f &&
                   mousePosition.x <= r.xMax + 8.0f &&
                   mousePosition.y >= r.yMin - 8.0f &&
                   mousePosition.y <= r.yMax + 8.0f;
        }

        private static string C2NeutralPeasantUnitsV2InputBackendLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            return "InputSystem";
#elif ENABLE_LEGACY_INPUT_MANAGER
            return "LegacyInput";
#else
            return "None";
#endif
        }

        private static int C2NeutralPeasantUnitPickerV2SortLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal a, C2NeutralPeasantUnitInfoV2LikeOriginal b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int c = b.SortKey.CompareTo(a.SortKey);
            if (c != 0) return c;

            return b.RecordIndex.CompareTo(a.RecordIndex);
        }

        private static int C2NeutralPeasantUnitPickerV2BuildingSortLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal a, C2SettlementBuildingSelectableV1LikeOriginal b)
        {
            if (ReferenceEquals(a, b)) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int c = b.SortKey.CompareTo(a.SortKey);
            if (c != 0) return c;

            return b.RecordIndex.CompareTo(a.RecordIndex);
        }
    }
}
