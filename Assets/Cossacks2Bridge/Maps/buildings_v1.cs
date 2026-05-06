// buildings_v1.cs
// V23 shim: old OC/COMPLEX buildings adapter is intentionally disabled.
// Real settlements/buildings are loaded by C2SettlementBuildings3INUParserLikeOriginal.cs:
// 3INU -> MonsterID -> .md -> USERLC/USERLCEXT -> G16 buildings / G2D units.

using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed class C2MapComplexBuildingsFromOriginalRecordsV1 : MonoBehaviour
    {
        public const string Contract = "V23_DISABLED_OC_COMPLEX_REDIRECT_TO_3INU_MD";

        public bool Enabled = false;
        public bool BuildOnceWhenMapIsAvailable = false;
        public float SearchIntervalSeconds = 1.0f;
        public int MaxSearchAttempts = 180;
        public string MapPathOverride = "";

        private bool _started;

        private void OnEnable()
        {
            if (_started) return;
            _started = true;
            Debug.Log("[C2:BUILDINGS OC V1 DISABLED] OC/COMPLEX is not buildings. Redirect target: 3INU -> MD -> G16/G2D. component=" + name);
            StartCoroutine(TryKickSettlement3InuV23LikeOriginal());
        }

        private IEnumerator TryKickSettlement3InuV23LikeOriginal()
        {
            for (int attempt = 1; attempt <= Mathf.Max(1, MaxSearchAttempts); attempt++)
            {
                var mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
                if (mode != null)
                {
                    Type t = mode.GetType();
                    BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                    if (!string.IsNullOrWhiteSpace(MapPathOverride))
                    {
                        FieldInfo overrideField = t.GetField("Settlement3InuMdV2MapPathOverride", f);
                        if (overrideField != null && overrideField.FieldType == typeof(string))
                            overrideField.SetValue(mode, MapPathOverride.Trim());
                    }

                    bool mapReady = false;
                    FieldInfo mapField = t.GetField("_map", f);
                    if (mapField != null)
                    {
                        try { mapReady = mapField.GetValue(mode) != null; } catch { mapReady = false; }
                    }

                    string mapPath = !string.IsNullOrWhiteSpace(MapPathOverride) ? MapPathOverride.Trim() : null;
                    if (string.IsNullOrWhiteSpace(mapPath))
                    {
                        MethodInfo getPath = t.GetMethod("TryGetCurrentMapPathForSettlement3InuMdV2LikeOriginal", f);
                        if (getPath != null)
                        {
                            try { mapPath = getPath.Invoke(mode, null) as string; } catch { mapPath = null; }
                        }
                    }

                    MethodInfo build = t.GetMethod("BuildSettlementBuildingsFrom3InuMdV2LikeOriginal", f);
                    if (build != null && mapReady && !string.IsNullOrWhiteSpace(mapPath))
                    {
                        build.Invoke(mode, new object[] { mapPath, "old-buildings-v1-shim-v23" });
                        Debug.Log("[C2:BUILDINGS OC V1 DISABLED] kicked 3INU V23 parser map='" + mapPath + "'");
                        enabled = false;
                        yield break;
                    }

                    if (attempt == 1 || attempt == 10 || attempt == 30 || attempt == 60 || attempt == 120)
                    {
                        Debug.Log("[C2:BUILDINGS OC V1 DISABLED WAIT] attempt=" + attempt +
                                  " mapReady=" + mapReady +
                                  " mapPath='" + (mapPath ?? "<null>") +
                                  "' buildMethodFound=" + (build != null));
                    }
                }

                yield return new WaitForSeconds(Mathf.Max(0.1f, SearchIntervalSeconds));
            }

            Debug.LogWarning("[C2:BUILDINGS OC V1 DISABLED] timeout. If no [C2:SETTLEMENT 3INU V23] appears, C2SettlementBuildings3INUParserLikeOriginal.cs is not installed/compiled.");
            enabled = false;
        }
    }
}
