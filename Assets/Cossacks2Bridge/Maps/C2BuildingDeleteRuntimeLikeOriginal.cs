// C2BuildingDeleteRuntimeLikeOriginal.cs
// V266: mechanical port of the old working delete/death/fire/smoke pipeline into the current V259/V256 building renderer.
// Goal: do NOT touch LINESORT. DeathLie is built through the same CreateBuildingCompositeLikeOriginal path as normal buildings.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        internal const string C2BuildingDeleteRuntimeContractLikeOriginal = "V273_ORIGINAL_DESTRUCT_OBL_AND_PODRIV_SCALE";
        private const float C2BuildingDeleteRuntimeSlowDeathSecondsLikeOriginal = 20.0f;
        private const int C2BuildingDeleteRuntimeMinDeathLikeOriginal = 2000;
        private const int C2BuildingDeleteRuntimeDeathDecLikeOriginal = 5;

        private static float C2BuildingDeleteRuntimeProgressiveFireDelayLikeOriginal(int index, int count)
        {
            if (count <= 1) return 0.0f;
            if (index <= 0) return 0.00f;
            if (index == 1) return 0.45f;
            if (index == 2) return 0.90f;

            float usable = C2BuildingDeleteRuntimeSlowDeathSecondsLikeOriginal * 0.78f;
            float t = (index - 2) / Mathf.Max(1.0f, (float)(count - 3));
            float jitter = (((index * 37) % 11) - 5) * 0.035f;
            return Mathf.Clamp(1.35f + t * usable + jitter, 0.0f, C2BuildingDeleteRuntimeSlowDeathSecondsLikeOriginal - 2.6f);
        }

        private static float C2BuildingDeleteRuntimeProgressiveSmokeDelayLikeOriginal(int index, int count)
        {
            if (count <= 1) return 0.8f;
            float t = index / Mathf.Max(1.0f, (float)(count - 1));
            float jitter = (((index * 23) % 9) - 4) * 0.05f;
            return Mathf.Clamp(1.10f + t * (C2BuildingDeleteRuntimeSlowDeathSecondsLikeOriginal * 0.70f) + jitter,
                0.25f,
                C2BuildingDeleteRuntimeSlowDeathSecondsLikeOriginal - 2.0f);
        }

        internal bool C2BuildingDeleteRuntimeTryGetMdFlagsLikeOriginal(
            string mdName,
            out bool immortal,
            out bool slowDeath,
            out int fireCount,
            out int smokeCount,
            out bool hasDestruct,
            out string audit)
        {
            immortal = false;
            slowDeath = false;
            fireCount = 0;
            smokeCount = 0;
            hasDestruct = false;
            audit = "no_md";

            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found)
            {
                audit = "md_missing md='" + (mdName ?? string.Empty) + "'";
                return false;
            }

            immortal = md.Immortal;
            slowDeath = md.SlowDeath;
            fireCount = md.FirePoints != null ? md.FirePoints.Count : 0;
            smokeCount = md.SmokePoints != null ? md.SmokePoints.Count : 0;
            hasDestruct = md.DestructWeapons != null && md.DestructWeapons.Count > 0;
            audit =
                "md='" + (md.MdName ?? mdName ?? string.Empty) + "'" +
                " immortal=" + immortal +
                " slowDeath=" + slowDeath +
                " fires=" + fireCount.ToString(CultureInfo.InvariantCulture) +
                " smoke=" + smokeCount.ToString(CultureInfo.InvariantCulture) +
                " destruct=" + hasDestruct;
            return true;
        }

        internal int C2BuildingDeleteRuntimeLifeMaxLikeOriginal(string mdName)
        {
            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found)
                return C2BuildingDeleteRuntimeMinDeathLikeOriginal;

            if (md.Life > 0) return Mathf.Max(1, md.Life);

            return C2BuildingDeleteRuntimeMinDeathLikeOriginal;
        }

        internal static int C2BuildingDeleteRuntimeDesiredFxCountLikeOriginal(int total, int life, int lifeMax)
        {
            if (total <= 0) return 0;
            int maxLife = Mathf.Max(1, lifeMax);
            int clampedLife = Mathf.Clamp(life, 0, maxLife);
            int damagePercent = 100 - (clampedLife * 100 / maxLife);
            if (damagePercent <= 0) return 0;
            return Mathf.Clamp((total * damagePercent) / 100, 0, total);
        }

        internal bool C2BuildingDeleteRuntimeHasDeathLieLikeOriginal(string mdName, int deathLieIndex)
        {
            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found) return false;
            string animName = deathLieIndex == 2 ? "#DEATHLIE2" : "#DEATHLIE1";
            C2BuildingAnimationLikeOriginal anim = FindBuildingAnimationLikeOriginal(md, animName);
            return anim != null && anim.Frames != null && anim.Frames.Count > 0;
        }

        internal GameObject C2BuildingDeleteRuntimeCreateDeathLieVisualLikeOriginal(
            Transform root,
            C2SettlementBuildingSelectableV1LikeOriginal source,
            C2RuntimeConstructionSiteProxyLikeOriginal proxy,
            int deathLieIndex,
            out string audit)
        {
            audit = "not_created";
            if (root == null)
            {
                audit = "no_root";
                return null;
            }

            string mdName = C2BuildingDeleteRuntimeResolveMdNameLikeOriginal(source, proxy);
            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found)
            {
                audit = "md_missing md='" + (mdName ?? string.Empty) + "'";
                return null;
            }

            string animName = deathLieIndex == 2 ? "#DEATHLIE2" : "#DEATHLIE1";
            C2BuildingAnimationLikeOriginal anim = FindBuildingAnimationLikeOriginal(md, animName);
            if (anim == null || anim.Frames == null || anim.Frames.Count == 0)
            {
                audit = "deathlie_missing md='" + (mdName ?? string.Empty) + "' anim='" + animName + "'";
                return null;
            }

            int nation = proxy != null ? proxy.Nation : (source != null ? source.Nation : 0);
            int realX = proxy != null ? proxy.RealX : (source != null ? source.RealX : 0);
            int realY = proxy != null ? proxy.RealY : (source != null ? source.RealY : 0);
            byte realDir = source != null ? (byte)Mathf.Clamp(source.RealDir, 0, 255) : (byte)0;
            int recordIndex = source != null ? source.RecordIndex : -1;

            var r = new C2Building3InuRecordLikeOriginal();
            r.Index = recordIndex;
            r.Nation = (byte)Mathf.Clamp(nation, 0, 255);
            r.RealX = realX;
            r.RealY = realY;
            r.RealDir = realDir;
            r.Life = 0;
            r.Stage = 0;
            r.MonsterId = !string.IsNullOrEmpty(mdName) ? mdName : (source != null ? source.SourceMonsterId : string.Empty);

            var loadedFrames = new List<C2BuildingLoadedPartLikeOriginal>(anim.Frames.Count);
            string visualAudit = "";
            for (int i = 0; i < anim.Frames.Count; i++)
            {
                C2BuildingAnimFrameLikeOriginal frameRef = anim.Frames[i];
                string pkg = PackageForFileRefLikeOriginal(md, frameRef.FileRef);
                Texture2D tex = TryLoadBuildingFrameTextureLikeOriginal(pkg, frameRef.SpriteId, out string partAudit);
                if (tex != null)
                {
                    RegisterBuildingTextureUseV226LikeOriginal(tex, pkg, frameRef.SpriteId, "deathlie");
                    var loaded = new C2BuildingLoadedPartLikeOriginal();
                    loaded.Texture = tex;
                    loaded.Frame = frameRef;
                    loaded.AnimationName = anim.Name;
                    if (anim.LineSort != null && i < anim.LineSort.Count)
                    {
                        loaded.HasLineSort = true;
                        loaded.LineSort = anim.LineSort[i];
                    }
                    loadedFrames.Add(loaded);
                    if (visualAudit.Length < 512)
                    {
                        if (visualAudit.Length > 0) visualAudit += ";";
                        visualAudit += "ok " + frameRef.FileRef.ToString(CultureInfo.InvariantCulture) + "/" + frameRef.SpriteId.ToString(CultureInfo.InvariantCulture);
                    }
                }
                else if (visualAudit.Length < 512)
                {
                    if (visualAudit.Length > 0) visualAudit += ";";
                    visualAudit += "miss " + frameRef.FileRef.ToString(CultureInfo.InvariantCulture) + "/" + frameRef.SpriteId.ToString(CultureInfo.InvariantCulture) + ":" + partAudit;
                }
            }

            if (loadedFrames.Count == 0)
            {
                audit = "deathlie_frames_missing md='" + (mdName ?? string.Empty) + "' anim='" + animName + "' " + visualAudit;
                return null;
            }

            int before = root.childCount;
            CreateBuildingCompositeLikeOriginal(root, r, md, loadedFrames);
            GameObject created = null;
            if (root.childCount > before)
                created = root.GetChild(root.childCount - 1).gameObject;

            if (created != null)
            {
                created.name = "C2_Building_DEATHLIE_" + SanitizeNameLikeOriginal(mdName) + "_" + recordIndex.ToString(CultureInfo.InvariantCulture);
                C2SettlementBuildingSelectableV1LikeOriginal[] selectables = created.GetComponentsInChildren<C2SettlementBuildingSelectableV1LikeOriginal>(true);
                for (int i = 0; i < selectables.Length; i++)
                {
                    if (selectables[i] == null) continue;
                    selectables[i].SetSelected(false);
                    selectables[i].NotSelectable = true;
                    selectables[i].enabled = false;
                }
            }

            audit =
                "created_with_current_composite_builder md='" + (mdName ?? string.Empty) + "'" +
                " anim='" + animName + "'" +
                " frames=" + loadedFrames.Count.ToString(CultureInfo.InvariantCulture);
            return created;
        }

        internal int C2BuildingDeleteRuntimeRemovePassabilityLikeOriginal(
            int recordIndex,
            string mdName,
            int realX,
            int realY,
            string reason)
        {
            // V266: current project already tracks blocked cells through C2BuildingRuntimeInfoV247LikeOriginal.
            // Do not mutate V247/LINESORT/runtime lists here. Keeping this no-op prevents the old branch from deleting
            // current building data too early.
            return 0;
        }

        internal int C2BuildingDeleteRuntimeAttachFireSmokeLikeOriginal(
            Transform visualRoot,
            string mdName,
            int sortingOrder,
            bool includeFires,
            bool includeSmoke,
            string reason)
        {
            if (visualRoot == null) return 0;

            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found) return 0;

            int created = 0;
            if (includeFires && md.FirePoints != null)
            {
                for (int i = 0; i < md.FirePoints.Count; i++)
                {
                    C2BuildingDeleteRuntimeCreateEffectQuadLikeOriginal(
                        visualRoot,
                        md,
                        md.FirePoints[i],
                        true,
                        sortingOrder + 80 + i,
                        "FIRE_" + i.ToString(CultureInfo.InvariantCulture),
                        i,
                        false,
                        -1.0f,
                        C2BuildingDeleteRuntimeProgressiveFireDelayLikeOriginal(i, md.FirePoints.Count),
                        false,
                        false);
                    created++;
                }
            }

            if (includeSmoke && md.SmokePoints != null)
            {
                for (int i = 0; i < md.SmokePoints.Count; i++)
                {
                    created += C2BuildingDeleteRuntimeCreateC2MStructuredSmokeStackLikeOriginal(
                        visualRoot,
                        md,
                        md.SmokePoints[i],
                        sortingOrder + 100 + i * 4,
                        "SMOKE_" + i.ToString(CultureInfo.InvariantCulture),
                        i,
                        C2BuildingDeleteRuntimeProgressiveSmokeDelayLikeOriginal(i, md.SmokePoints.Count));
                }
            }

            Debug.Log("[C2:BUILD DELETE V266E FX_ATTACH] md='" + (mdName ?? string.Empty) + "' fire=" +
                      (md.FirePoints != null ? md.FirePoints.Count : 0).ToString(CultureInfo.InvariantCulture) +
                      " smoke=" + (md.SmokePoints != null ? md.SmokePoints.Count : 0).ToString(CultureInfo.InvariantCulture) +
                      " created=" + created.ToString(CultureInfo.InvariantCulture) +
                      " reason='" + (reason ?? string.Empty) + "'");
            return created;
        }

        internal int C2BuildingDeleteRuntimeAttachFireSmokeForLifeLikeOriginal(
            Transform visualRoot,
            string mdName,
            int sortingOrder,
            int life,
            int lifeMax,
            ref int createdFirePoints,
            ref int createdSmokePoints,
            string reason)
        {
            if (visualRoot == null) return 0;

            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found) return 0;

            int fireTotal = md.FirePoints != null ? md.FirePoints.Count : 0;
            int smokeTotal = md.SmokePoints != null ? md.SmokePoints.Count : 0;
            int desiredFire = C2BuildingDeleteRuntimeDesiredFxCountLikeOriginal(fireTotal, life, lifeMax);
            int desiredSmoke = C2BuildingDeleteRuntimeDesiredFxCountLikeOriginal(smokeTotal, life, lifeMax);

            int created = 0;
            int startFire = Mathf.Clamp(createdFirePoints, 0, fireTotal);
            for (int i = startFire; i < desiredFire; i++)
            {
                C2BuildingDeleteRuntimeCreateEffectQuadLikeOriginal(
                    visualRoot,
                    md,
                    md.FirePoints[i],
                    true,
                    sortingOrder + 80 + i,
                    "FIRE_" + i.ToString(CultureInfo.InvariantCulture),
                    i,
                    false,
                    -1.0f,
                    0.0f,
                    false,
                    false);
                created++;
            }
            createdFirePoints = Mathf.Clamp(Mathf.Max(createdFirePoints, desiredFire), 0, fireTotal);

            int startSmoke = Mathf.Clamp(createdSmokePoints, 0, smokeTotal);
            for (int i = startSmoke; i < desiredSmoke; i++)
            {
                created += C2BuildingDeleteRuntimeCreateC2MStructuredSmokeStackLikeOriginal(
                    visualRoot,
                    md,
                    md.SmokePoints[i],
                    sortingOrder + 100 + i * 4,
                    "SMOKE_" + i.ToString(CultureInfo.InvariantCulture),
                    i,
                    0.0f);
            }
            createdSmokePoints = Mathf.Clamp(Mathf.Max(createdSmokePoints, desiredSmoke), 0, smokeTotal);

            if (created > 0)
            {
                Debug.Log("[C2:BUILD DELETE V267 FX_LIFE] md='" + (mdName ?? string.Empty) +
                          "' hp=" + Mathf.Clamp(life, 0, Mathf.Max(1, lifeMax)).ToString(CultureInfo.InvariantCulture) +
                          "/" + Mathf.Max(1, lifeMax).ToString(CultureInfo.InvariantCulture) +
                          " desiredFire=" + desiredFire.ToString(CultureInfo.InvariantCulture) +
                          " firePoints=" + createdFirePoints.ToString(CultureInfo.InvariantCulture) +
                          " desiredSmoke=" + desiredSmoke.ToString(CultureInfo.InvariantCulture) +
                          " smokePoints=" + createdSmokePoints.ToString(CultureInfo.InvariantCulture) +
                          " createdObjects=" + created.ToString(CultureInfo.InvariantCulture) +
                          " reason='" + (reason ?? string.Empty) + "'");
            }

            return created;
        }

        private int C2BuildingDeleteRuntimeCreateC2MStructuredSmokeStackLikeOriginal(
            Transform visualRoot,
            C2BuildingMdInfoLikeOriginal md,
            Vector2 point,
            int sortingOrder,
            string name,
            int smokeIndex,
            float initialDelay)
        {
            int smockVariant = Mathf.Abs(smokeIndex) & 3;
            int layers = smockVariant == 3 ? 3 : 2;
            int created = 0;

            for (int layer = 0; layer < layers; layer++)
            {
                int encodedVariant = Mathf.Abs(smokeIndex) * 10 + layer + smockVariant * 100;
                C2BuildingDeleteRuntimeCreateEffectQuadLikeOriginal(
                    visualRoot,
                    md,
                    point,
                    false,
                    sortingOrder + layer,
                    (name ?? "SMOKE") + "_SMOCK" + (smockVariant + 1).ToString(CultureInfo.InvariantCulture) + "_L" + layer.ToString(CultureInfo.InvariantCulture),
                    encodedVariant,
                    false,
                    -1.0f,
                    Mathf.Max(0.0f, initialDelay + layer * 0.38f + smockVariant * 0.08f),
                    true,
                    true);
                created++;
            }

            return created;
        }

        internal int C2BuildingDeleteRuntimeSpawnDestructLikeOriginal(
            Transform visualRoot,
            string mdName,
            int sortingOrder,
            string reason)
        {
            if (visualRoot == null) return 0;

            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found) return 0;
            if (md.CheckPoints == null || md.CheckPoints.Count == 0) return 0;
            int weaponCount = md.DestructWeapons != null ? md.DestructWeapons.Count : 0;
            if (weaponCount <= 0) return 0;

            int created = 0;
            int visualCreated = 0;
            int probability = Mathf.Clamp(md.DestructProbability, 0, 32768);
            string firstPointAudit = string.Empty;
            if (probability <= 0) return 0;
            for (int i = 0; i < md.CheckPoints.Count; i++)
            {
                int roll = C2BuildingDeleteRuntimeStableRandom15LikeOriginal(mdName, i, 17);
                if (probability < 32768 && roll >= probability) continue;

                int weaponIndex = C2BuildingDeleteRuntimeStableRandom15LikeOriginal(mdName, i, 71) % weaponCount;
                string weaponName = md.DestructWeapons[weaponIndex] ?? string.Empty;
                Vector2 point = md.CheckPoints[i];
                string pointAudit;
                Vector3 localBase = C2BuildingDeleteRuntimeCheckpointToUnityLocalLikeOriginal(visualRoot, md, point, out pointAudit);
                if (string.IsNullOrEmpty(firstPointAudit))
                    firstPointAudit = pointAudit ?? string.Empty;
                if (C2BuildingDeleteRuntimeIsPodrivZdaniyaWeaponLikeOriginal(weaponName))
                {
                    visualCreated += C2BuildingDeleteRuntimeCreatePodrivZdaniyaEffectLikeOriginal(
                        visualRoot,
                        md,
                        point * 16.0f,
                        sortingOrder + 140 + created * 16,
                        "DESTRUCT_" + weaponName + "_" + created.ToString(CultureInfo.InvariantCulture),
                        C2BuildingDeleteRuntimeStableRandom15LikeOriginal(weaponName, i, 131),
                        0.0f,
                        localBase,
                        pointAudit);
                }
                else
                {
                    visualCreated += C2BuildingDeleteRuntimeCreateOblFragmentLikeOriginal(
                        visualRoot,
                        weaponName,
                        md,
                        point * 16.0f,
                        sortingOrder + 140 + created * 16,
                        "DESTRUCT_" + weaponName + "_" + created.ToString(CultureInfo.InvariantCulture),
                        C2BuildingDeleteRuntimeStableRandom15LikeOriginal(weaponName, i, 131),
                        0.0f,
                        localBase);
                }
                created++;
            }

            Debug.Log("[C2:BUILD DELETE V273 DESTRUCT] md='" + (mdName ?? string.Empty) +
                      "' weapons=" + weaponCount.ToString(CultureInfo.InvariantCulture) +
                      " checkPoints=" + md.CheckPoints.Count.ToString(CultureInfo.InvariantCulture) +
                      " created=" + created.ToString(CultureInfo.InvariantCulture) +
                      " visuals=" + visualCreated.ToString(CultureInfo.InvariantCulture) +
                      " firstPoint='" + firstPointAudit + "'" +
                      " reason='" + (reason ?? string.Empty) + "'");
            return created;
        }

        private static bool C2BuildingDeleteRuntimeIsPodrivZdaniyaWeaponLikeOriginal(string weaponName)
        {
            if (string.IsNullOrEmpty(weaponName)) return false;
            return weaponName.IndexOf("ZDANIA", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   weaponName.IndexOf("PODRIV", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int C2BuildingDeleteRuntimeStableRandom15LikeOriginal(string seed, int index, int salt)
        {
            unchecked
            {
                int h = 216613626;
                string s = seed ?? string.Empty;
                for (int i = 0; i < s.Length; i++)
                    h = (h ^ s[i]) * 16777619;
                h = (h ^ index) * 16777619;
                h = (h ^ salt) * 16777619;
                return (h & 0x7FFFFFFF) & 0x7FFF;
            }
        }

        private static string C2BuildingDeleteRuntimeResolveMdNameLikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal source,
            C2RuntimeConstructionSiteProxyLikeOriginal proxy)
        {
            if (proxy != null && !string.IsNullOrEmpty(proxy.MdName)) return proxy.MdName;
            if (source != null && !string.IsNullOrEmpty(source.SourceMonsterId)) return source.SourceMonsterId;
            if (proxy != null && !string.IsNullOrEmpty(proxy.UnitId)) return proxy.UnitId;
            return string.Empty;
        }

        private void C2BuildingDeleteRuntimeCreateEffectQuadLikeOriginal(
            Transform visualRoot,
            C2BuildingMdInfoLikeOriginal md,
            Vector2 point,
            bool fire,
            int sortingOrder,
            string name,
            int variant,
            bool oneShot,
            float autoDestroyAfter,
            float initialDelay,
            bool restartMotionEachLoop,
            bool fadeOutPerLoop)
        {
            if (visualRoot == null || md == null) return;

            float s = BuildingSpritePixelToWorldScaleV276LikeOriginal(WallOriginalXYUnitToWorldScaleV8LikeOriginal());

            int smokeLayer = fire ? 0 : Mathf.Abs(variant) % 10;
            int smokeStyle = fire ? 0 : (Mathf.Abs(variant) / 100) & 3;

            float drawX = md.PicDx + point.x;
            float drawY = md.PicDy + point.y;
            float modelZ = fire ? 42.0f : (64.0f + smokeLayer * 26.0f);
            Transform emitterAnchor = CreateBuildingEffectAnchorLikeOriginal(visualRoot, md, drawX, drawY);
            Vector3 localAnchor = emitterAnchor.localPosition;
            GameObject go = new GameObject("C2_DELETE_" + (fire ? "FIRE_" : "SMOKE_") + (name ?? ""));
            go.transform.SetParent(emitterAnchor, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            float smokeSizeMul = C2BuildingDeleteRuntimeSmokeSizeMultiplierLikeOriginal(smokeLayer, smokeStyle);
            float smokeHeightMul = C2BuildingDeleteRuntimeSmokeHeightMultiplierLikeOriginal(smokeLayer, smokeStyle);
            float size = fire ? (oneShot ? 54.0f * s : 48.0f * s) : 74.0f * s * smokeSizeMul;
            if (fire)
            {
                // V12: FIRES coordinates are the base of the building-fire billboard.
                // Fire grows upward from the anchor; the quad must not be centered on the point.
                mf.sharedMesh = C2BuildingDeleteRuntimeCreateBottomPivotQuadMeshLikeOriginal(size, size * 1.48f);
            }
            else
            {
                // V15: C2M SmokeList behavior is emitter-like: smoke starts from the MD point,
                // grows upward, drifts, fades, then restarts. Bottom pivot keeps the source point stable.
                mf.sharedMesh = C2BuildingDeleteRuntimeCreateBottomPivotQuadMeshLikeOriginal(size, size * smokeHeightMul);
            }

            Texture2D[] frames = C2BuildingDeleteRuntimeGetEffectFramesLikeOriginal(fire, variant);
            Shader shader = C2BuildingDeleteRuntimeFindTransparentShaderLikeOriginal();
            if (shader == null)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }
            Material mat = new Material(shader);
            mat.name = fire ? "C2_DELETE_BuildingFire_FrameAnim_Mat" : "C2_DELETE_Smoke_Mat";
            mat.mainTexture = frames != null && frames.Length > 0 ? frames[0] : Texture2D.whiteTexture;
            mat.renderQueue = 4990;
            mat.SetOverrideTag("RenderType", "Transparent");
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1.0f);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.003f);
            if (mat.HasProperty("_AlphaCutoff")) mat.SetFloat("_AlphaCutoff", 0.003f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0.0f);
            if (mat.HasProperty("_ZTest")) mat.SetFloat("_ZTest", (float)CompareFunction.Always);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            int safeSortingOrder = Mathf.Clamp(sortingOrder, -30000, 30000);
            mr.sortingOrder = safeSortingOrder;

            // V12: do not let delayed fire/smoke render even for the first frame.
            // In V11 all FIRES GameObjects were created immediately and MeshRenderer was enabled by default,
            // so Unity could draw every fire once before the animator hid delayed points.
            // Original RealFires appears progressively; delayed emitters must start invisible.
            mr.enabled = false;

            C2BuildingDeleteEffectAnimatorLikeOriginal anim = go.AddComponent<C2BuildingDeleteEffectAnimatorLikeOriginal>();
            anim.Renderer = mr;
            anim.OwnedMesh = mf.sharedMesh;
            anim.Frames = frames;
            anim.Fps = fire ? 13.0f + (Mathf.Abs(variant) % 3) : C2BuildingDeleteRuntimeSmokeFpsLikeOriginal(variant);
            anim.FloatPerSecond = fire ? Vector3.zero : C2BuildingDeleteRuntimeSmokeVelocityLikeOriginal(variant, s);
            anim.Loop = !oneShot;
            anim.InitialDelay = Mathf.Max(0.0f, initialDelay);
            anim.AutoDestroyAfter = autoDestroyAfter;
            anim.RestartMotionEachLoop = fire ? false : restartMotionEachLoop;
            anim.FadeOutPerLoop = fire ? false : fadeOutPerLoop;
            anim.BaseColor = fire ? new Color(1.0f, 1.0f, 1.0f, 0.96f) : C2BuildingDeleteRuntimeSmokeColorLikeOriginal(variant);
            anim.ScalePulse = fire ? 0.0f : 0.09f;
            anim.AlphaPulse = fire ? 0.0f : 0.16f;
            anim.PulseSpeed = fire ? 0.0f : (1.35f + (Mathf.Abs(variant) % 4) * 0.21f);
            anim.PulsePhase = variant * 1.731f + initialDelay * 0.37f;
            anim.FrameOffset = fire ? Mathf.Abs(variant * 3) : Mathf.Abs(variant * 5);
            anim.FadeInSeconds = fire ? 0.85f : C2BuildingDeleteRuntimeSmokeFadeInLikeOriginal(variant);
            anim.GrowInStartScale = fire ? 0.68f : C2BuildingDeleteRuntimeSmokeGrowStartLikeOriginal(variant);
            anim.FaceMainCamera = true;
            anim.FacingCamera = _strictIsoCamera;

            Debug.Log("[C2:BUILD DELETE V266E FX_CREATE] kind='" + (fire ? "fire" : "smoke") +
                      "' name='" + (name ?? string.Empty) +
                      "' draw=(" + drawX.ToString("0.###", CultureInfo.InvariantCulture) +
                      "," + drawY.ToString("0.###", CultureInfo.InvariantCulture) +
                      "," + modelZ.ToString("0.###", CultureInfo.InvariantCulture) +
                      ") localUnity=(" + localAnchor.x.ToString("0.###", CultureInfo.InvariantCulture) +
                      "," + localAnchor.y.ToString("0.###", CultureInfo.InvariantCulture) +
                      "," + localAnchor.z.ToString("0.###", CultureInfo.InvariantCulture) +
                      ") size=" + size.ToString("0.###", CultureInfo.InvariantCulture) +
                      " queue=4990 ztest=Always sort=" + safeSortingOrder.ToString(CultureInfo.InvariantCulture) +
                      " faceCamera=1 delay=" + Mathf.Max(0.0f, initialDelay).ToString("0.###", CultureInfo.InvariantCulture));
        }

        private int C2BuildingDeleteRuntimeCreateOblFragmentLikeOriginal(
            Transform visualRoot,
            string weaponName,
            C2BuildingMdInfoLikeOriginal md,
            Vector2 point,
            int sortingOrder,
            string name,
            int variant,
            float initialDelay,
            Vector3? localAnchorOverride = null)
        {
            if (visualRoot == null || md == null) return 0;

            Texture2D[] frames = C2BuildingDeleteRuntimeGetOblWeaponFramesLikeOriginal(weaponName);
            if (frames == null || frames.Length == 0) return 0;

            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal();
            float drawX = md.PicDx + point.x;
            float drawY = md.PicDy + point.y;
            const float startZ = 16.0f; // Nation.cpp uses GetHeight(xp,yp)+16 for DESTRUCTING.

            Texture2D first = frames[0];
            float width = Mathf.Max(4.0f * s, (first != null ? first.width : 36.0f) * s);
            float height = Mathf.Max(4.0f * s, (first != null ? first.height : 36.0f) * s);

            float angle = (C2BuildingDeleteRuntimeStableRandom15LikeOriginal(name, variant, 811) / 32767.0f) * Mathf.PI * 2.0f;
            float speed = (34.0f + (C2BuildingDeleteRuntimeStableRandom15LikeOriginal(name, variant, 887) / 32767.0f) * 22.0f) * s;
            float lift = (58.0f + (C2BuildingDeleteRuntimeStableRandom15LikeOriginal(name, variant, 919) / 32767.0f) * 28.0f) * s;
            Vector3 velocity = new Vector3(Mathf.Cos(angle) * speed, lift, Mathf.Sin(angle) * speed);
            Vector3 gravity = new Vector3(0.0f, -150.0f * s, 0.0f);

            return C2BuildingDeleteRuntimeCreatePodrivBillboardLikeOriginal(
                visualRoot,
                "C2_DELETE_OBL_" + (weaponName ?? string.Empty) + "_" + (name ?? string.Empty),
                frames,
                drawX,
                drawY,
                startZ,
                width,
                height,
                false,
                sortingOrder,
                15.0f,
                false,
                1.15f,
                initialDelay,
                velocity,
                Color.white,
                true,
                0.0f,
                0.0f,
                0.0f,
                1.0f,
                Mathf.Abs(variant) % frames.Length,
                localAnchorOverride,
                gravity);
        }

        private int C2BuildingDeleteRuntimeCreatePodrivZdaniyaEffectLikeOriginal(
            Transform visualRoot,
            C2BuildingMdInfoLikeOriginal md,
            Vector2 point,
            int sortingOrder,
            string name,
            int variant,
            float initialDelay,
            Vector3? localAnchorOverride = null,
            string anchorAudit = null)
        {
            if (visualRoot == null || md == null) return 0;

            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal();
            const float adsScale = 2.0f; // WEAPON.ADS: !PODRIVZDANIA 200.
            const float startZ = 16.0f;  // Nation.cpp: Create3DAnmObject(..., GetHeight(xp,yp)+16, ...).
            float scaled = s * adsScale;
            float drawX = md.PicDx + point.x;
            float drawY = md.PicDy + point.y;
            int created = 0;

            Texture2D[] flashFrames = C2BuildingDeleteRuntimeGetPodrivFlashFramesLikeOriginal();
            Texture2D[] smokeFrames = C2BuildingDeleteRuntimeGetPodrivSmokeFramesLikeOriginal();
            Texture2D[] starFrames = C2BuildingDeleteRuntimeGetPodrivStarFramesLikeOriginal();
            Texture2D[] stoneFrames = C2BuildingDeleteRuntimeGetPodrivStoneDebrisFramesLikeOriginal();
            Texture2D[] woodFrames = C2BuildingDeleteRuntimeGetPodrivWoodDebrisFramesLikeOriginal();

            // Original weapon.nds: PU*ZDANIA -> #PODRIVZDANIA.
            // Podriv_Zdaniya.c2m is Modular: Vspih(pfx_sparks1) + Dim_Vv/Dim_Koltso(smoke_01)
            // + Ugli(star2) + B_Kuski(pfx_stonedebris1) + Palki_2/Musor(woodchips).
            created += C2BuildingDeleteRuntimeCreatePodrivBillboardLikeOriginal(
                visualRoot,
                "C2_DELETE_PODRIV_VSPIH_" + (name ?? string.Empty),
                flashFrames,
                drawX,
                drawY,
                startZ,
                116.0f * scaled,
                90.0f * scaled,
                false,
                sortingOrder,
                18.0f,
                false,
                0.58f,
                initialDelay,
                new Vector3(0.0f, 10.0f * s, 0.0f),
                new Color(1.0f, 0.92f, 0.72f, 0.98f),
                true,
                0.22f,
                0.10f,
                15.0f,
                0.28f,
                Mathf.Abs(variant) % 7,
                localAnchorOverride);

            created += C2BuildingDeleteRuntimeCreatePodrivBillboardLikeOriginal(
                visualRoot,
                "C2_DELETE_PODRIV_DIM_KOLTSO_" + (name ?? string.Empty),
                smokeFrames,
                drawX,
                drawY,
                startZ + 2.0f,
                150.0f * scaled,
                54.0f * scaled,
                false,
                sortingOrder + 1,
                6.0f,
                false,
                1.05f,
                initialDelay + 0.03f,
                new Vector3(0.0f, 14.0f * s, 0.0f),
                new Color(0.56f, 0.53f, 0.47f, 0.42f),
                true,
                0.08f,
                0.14f,
                3.0f,
                0.25f,
                Mathf.Abs(variant + 11),
                localAnchorOverride);

            for (int i = 0; i < 3; i++)
            {
                Vector3 drift = new Vector3(
                    C2BuildingDeleteRuntimeStableSignedOffsetLikeOriginal(name, variant + i, 401, 10.0f) * scaled,
                    (24.0f + i * 10.0f) * scaled,
                    C2BuildingDeleteRuntimeStableSignedOffsetLikeOriginal(name, variant + i, 509, 10.0f) * scaled);

                created += C2BuildingDeleteRuntimeCreatePodrivBillboardLikeOriginal(
                    visualRoot,
                    "C2_DELETE_PODRIV_DIM_" + i.ToString(CultureInfo.InvariantCulture) + "_" + (name ?? string.Empty),
                    smokeFrames,
                    drawX,
                    drawY,
                    startZ + 4.0f + i * 7.0f,
                    (82.0f + i * 26.0f) * scaled,
                    (96.0f + i * 34.0f) * scaled,
                    true,
                    sortingOrder + 2 + i,
                    7.0f,
                    false,
                    1.35f + i * 0.22f,
                    initialDelay + 0.04f + i * 0.07f,
                    drift,
                    new Color(0.58f, 0.54f, 0.48f, 0.64f),
                    true,
                    0.10f,
                    0.18f,
                    4.0f,
                    0.35f,
                    Mathf.Abs(variant + i * 3),
                    localAnchorOverride);
            }

            for (int i = 0; i < 4; i++)
            {
                float ox = C2BuildingDeleteRuntimeStableSignedOffsetLikeOriginal(name, variant + i, 613, 36.0f);
                float oy = C2BuildingDeleteRuntimeStableSignedOffsetLikeOriginal(name, variant + i, 719, 28.0f);
                Vector3 drift = new Vector3(
                    ox * 0.28f * scaled,
                    (18.0f + i * 4.0f) * scaled,
                    -oy * 0.22f * scaled);

                created += C2BuildingDeleteRuntimeCreatePodrivBillboardLikeOriginal(
                    visualRoot,
                    "C2_DELETE_PODRIV_UGLI_" + i.ToString(CultureInfo.InvariantCulture) + "_" + (name ?? string.Empty),
                    starFrames,
                    drawX,
                    drawY,
                    startZ + 8.0f + i * 2.0f,
                    (26.0f + i * 3.0f) * scaled,
                    (26.0f + i * 3.0f) * scaled,
                    false,
                    sortingOrder + 5 + i,
                    10.0f,
                    false,
                    0.72f,
                    initialDelay + 0.02f + i * 0.025f,
                    drift,
                    new Color(1.0f, 0.76f, 0.46f, 0.92f),
                    true,
                    0.18f,
                    0.25f,
                    18.0f,
                    0.40f,
                    0,
                    localAnchorOverride);
            }

            float debrisAngle = (C2BuildingDeleteRuntimeStableRandom15LikeOriginal(name, variant, 811) / 32767.0f) * Mathf.PI * 2.0f;
            Vector3 debrisGravity = new Vector3(0.0f, -125.0f * scaled, 0.0f);
            Vector3 stoneVelocity = new Vector3(Mathf.Cos(debrisAngle) * 36.0f * scaled, 58.0f * scaled, Mathf.Sin(debrisAngle) * 36.0f * scaled);
            Vector3 woodVelocity = new Vector3(Mathf.Cos(debrisAngle + 1.85f) * 42.0f * scaled, 68.0f * scaled, Mathf.Sin(debrisAngle + 1.85f) * 42.0f * scaled);
            Vector3 smallVelocity = new Vector3(Mathf.Cos(debrisAngle - 1.20f) * 28.0f * scaled, 46.0f * scaled, Mathf.Sin(debrisAngle - 1.20f) * 28.0f * scaled);

            created += C2BuildingDeleteRuntimeCreatePodrivBillboardLikeOriginal(
                visualRoot,
                "C2_DELETE_PODRIV_B_KUSKI_" + (name ?? string.Empty),
                stoneFrames,
                drawX,
                drawY,
                startZ + 10.0f,
                30.0f * scaled,
                30.0f * scaled,
                false,
                sortingOrder + 9,
                14.0f,
                false,
                1.05f,
                initialDelay + 0.03f,
                stoneVelocity,
                new Color(0.84f, 0.80f, 0.72f, 0.92f),
                true,
                0.05f,
                0.08f,
                9.0f,
                0.70f,
                Mathf.Abs(variant + 19),
                localAnchorOverride,
                debrisGravity);

            created += C2BuildingDeleteRuntimeCreatePodrivBillboardLikeOriginal(
                visualRoot,
                "C2_DELETE_PODRIV_PALKI_2_" + (name ?? string.Empty),
                woodFrames,
                drawX,
                drawY,
                startZ + 12.0f,
                36.0f * scaled,
                36.0f * scaled,
                false,
                sortingOrder + 10,
                13.0f,
                false,
                1.15f,
                initialDelay + 0.05f,
                woodVelocity,
                new Color(0.80f, 0.68f, 0.52f, 0.90f),
                true,
                0.07f,
                0.08f,
                10.0f,
                0.72f,
                Mathf.Abs(variant + 23),
                localAnchorOverride,
                debrisGravity);

            created += C2BuildingDeleteRuntimeCreatePodrivBillboardLikeOriginal(
                visualRoot,
                "C2_DELETE_PODRIV_MUSOR_" + (name ?? string.Empty),
                woodFrames,
                drawX,
                drawY,
                startZ + 7.0f,
                22.0f * scaled,
                22.0f * scaled,
                false,
                sortingOrder + 11,
                12.0f,
                false,
                0.95f,
                initialDelay + 0.06f,
                smallVelocity,
                new Color(0.70f, 0.62f, 0.50f, 0.82f),
                true,
                0.06f,
                0.10f,
                10.0f,
                0.68f,
                Mathf.Abs(variant + 29),
                localAnchorOverride,
                debrisGravity);

            Debug.Log("[C2:BUILD DELETE V273 PODRIV_ZDANIA] name='" + (name ?? string.Empty) +
                      "' draw=(" + drawX.ToString("0.###", CultureInfo.InvariantCulture) +
                      "," + drawY.ToString("0.###", CultureInfo.InvariantCulture) +
                      ") visuals=" + created.ToString(CultureInfo.InvariantCulture) +
                      " anchor='" + (anchorAudit ?? string.Empty) + "'" +
                      " source=weapon.nds:PU*ZDANIA->#PODRIVZDANIA weapon_ads_scale=200 startZ=GetHeight+16 textures=pfx_sparks1/smoke_01/star2/pfx_stonedebris1/woodchips");
            return created;
        }

        private int C2BuildingDeleteRuntimeCreatePodrivBillboardLikeOriginal(
            Transform visualRoot,
            string name,
            Texture2D[] frames,
            float drawX,
            float drawY,
            float drawZ,
            float width,
            float height,
            bool bottomPivot,
            int sortingOrder,
            float fps,
            bool loop,
            float autoDestroyAfter,
            float initialDelay,
            Vector3 floatPerSecond,
            Color baseColor,
            bool fadeOutPerLoop,
            float scalePulse,
            float alphaPulse,
            float pulseSpeed,
            float growInStartScale,
            int frameOffset,
            Vector3? localAnchorOverride = null,
            Vector3? accelerationPerSecondSquared = null)
        {
            if (visualRoot == null || frames == null || frames.Length == 0) return 0;

            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal();
            Vector3 localAnchor = localAnchorOverride.HasValue
                ? localAnchorOverride.Value
                : C2BuildingDeleteRuntimeEffectScreenPointToUnityLocalLikeOriginal(drawX, drawY, s);
            localAnchor.y += drawZ * WallOriginalZUnitToWorldScaleV8LikeOriginal();
            localAnchor.y += 0.025f;

            GameObject go = new GameObject(name ?? "C2_DELETE_PODRIV");
            go.transform.SetParent(visualRoot, false);
            go.transform.localPosition = localAnchor;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = bottomPivot
                ? C2BuildingDeleteRuntimeCreateBottomPivotQuadMeshLikeOriginal(width, height)
                : C2BuildingDeleteRuntimeCreateQuadMeshLikeOriginal(width, height);

            Shader shader = C2BuildingDeleteRuntimeFindTransparentShaderLikeOriginal();
            if (shader == null)
            {
                UnityEngine.Object.Destroy(go);
                return 0;
            }

            Material mat = new Material(shader);
            mat.name = "C2_DELETE_PodrivZdaniya_Mat";
            mat.mainTexture = frames[0];
            mat.renderQueue = 4992;
            mat.SetOverrideTag("RenderType", "Transparent");
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1.0f);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.003f);
            if (mat.HasProperty("_AlphaCutoff")) mat.SetFloat("_AlphaCutoff", 0.003f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0.0f);
            if (mat.HasProperty("_ZTest")) mat.SetFloat("_ZTest", (float)CompareFunction.Always);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = Mathf.Clamp(sortingOrder, -30000, 30000);
            mr.enabled = false;

            C2BuildingDeleteEffectAnimatorLikeOriginal anim = go.AddComponent<C2BuildingDeleteEffectAnimatorLikeOriginal>();
            anim.Renderer = mr;
            anim.OwnedMesh = mf.sharedMesh;
            anim.Frames = frames;
            anim.Fps = fps;
            anim.Loop = loop;
            anim.InitialDelay = Mathf.Max(0.0f, initialDelay);
            anim.AutoDestroyAfter = autoDestroyAfter;
            anim.RestartMotionEachLoop = false;
            anim.FadeOutPerLoop = fadeOutPerLoop;
            anim.BaseColor = baseColor;
            anim.ScalePulse = scalePulse;
            anim.AlphaPulse = alphaPulse;
            anim.PulseSpeed = pulseSpeed;
            anim.PulsePhase = frameOffset * 0.61f + initialDelay * 2.0f;
            anim.FrameOffset = frameOffset;
            anim.FloatPerSecond = floatPerSecond;
            anim.AccelerationPerSecondSquared = accelerationPerSecondSquared.HasValue ? accelerationPerSecondSquared.Value : Vector3.zero;
            anim.FadeInSeconds = 0.0f;
            anim.GrowInStartScale = growInStartScale;
            anim.FaceMainCamera = true;
            anim.FacingCamera = _strictIsoCamera;
            return 1;
        }

        private static float C2BuildingDeleteRuntimeStableSignedOffsetLikeOriginal(string seed, int index, int salt, float amplitude)
        {
            int v = C2BuildingDeleteRuntimeStableRandom15LikeOriginal(seed, index, salt);
            return ((v / 32767.0f) * 2.0f - 1.0f) * amplitude;
        }

        private static Shader C2BuildingDeleteRuntimeFindTransparentShaderLikeOriginal()
        {
            Shader shader = Shader.Find("Cossacks2Bridge/SettlementBuildingSpriteV205BlendLikeOriginal");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Standard");
            return shader;
        }

        internal int C2BuildingDeleteRuntimeResolveFxSortingOrderLikeOriginal(Transform visualRoot, int fallback)
        {
            int best = int.MinValue;
            if (visualRoot != null)
            {
                MeshRenderer[] renderers = visualRoot.GetComponentsInChildren<MeshRenderer>(true);
                if (renderers != null)
                {
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        MeshRenderer mr = renderers[i];
                        if (mr == null)
                            continue;

                        string n = mr.gameObject != null ? (mr.gameObject.name ?? string.Empty) : string.Empty;
                        if (n.StartsWith("C2_DELETE_", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (mr.sortingOrder > best)
                            best = mr.sortingOrder;
                    }
                }
            }

            if (best == int.MinValue)
                best = Mathf.Clamp(fallback, -28000, 28000);

            return Mathf.Clamp(best + 64, -28000, 28000);
        }

        internal void C2BuildingDeleteRuntimeCleanupBadFxArtifactsLikeOriginal(Transform root)
        {
            if (root == null)
                return;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            if (all == null || all.Length == 0)
                return;

            int removed = 0;
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t == root)
                    continue;

                string n = t.name ?? string.Empty;
                bool bad =
                    n.IndexOf("C2_DELETE_PODRIV_BOOM", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("C2_DELETE_PODRIV_DEBRIS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("BigBoomLayer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("StoneDebris", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("WoodChips", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!bad)
                    continue;

                UnityEngine.Object.Destroy(t.gameObject);
                removed++;
            }

            if (removed > 0)
                Debug.Log("[C2:BUILD DELETE V272 CLEANUP_BAD_FX] removed=" + removed.ToString(CultureInfo.InvariantCulture));
        }

        private Vector3 C2BuildingDeleteRuntimeCheckpointToUnityLocalLikeOriginal(
            Transform visualRoot,
            C2BuildingMdInfoLikeOriginal md,
            Vector2 checkpointCell,
            out string audit)
        {
            audit = "checkpoint_unresolved";
            if (visualRoot == null)
                return Vector3.zero;

            C2BuildingRuntimeInfoV247LikeOriginal info = visualRoot.GetComponent<C2BuildingRuntimeInfoV247LikeOriginal>();
            if (info == null)
                info = visualRoot.GetComponentInChildren<C2BuildingRuntimeInfoV247LikeOriginal>(true);
            if (info == null)
                info = visualRoot.GetComponentInParent<C2BuildingRuntimeInfoV247LikeOriginal>();

            if (info != null)
            {
                int localCellX = Mathf.RoundToInt(checkpointCell.x);
                int localCellY = Mathf.RoundToInt(checkpointCell.y);
                int cellX = info.CornerCellX + localCellX;
                int cellY = info.CornerCellY + localCellY;
                float originalX = C2BuildingRuntimeV247ScaleOriginalXLikeOriginal(info, cellX * 16.0f, info.VisualToMapScaleV277);
                float originalY = C2BuildingRuntimeV247ScaleOriginalYLikeOriginal(info, cellY * 16.0f, info.VisualToMapScaleV277);
                Vector3 world = WallOriginalXYToWorldV1LikeOriginal(originalX, originalY, 0.0f);
                Vector3 local = visualRoot.InverseTransformPoint(world);
                audit =
                    "runtime corner=(" + info.CornerCellX.ToString(CultureInfo.InvariantCulture) +
                    "," + info.CornerCellY.ToString(CultureInfo.InvariantCulture) +
                    ") check=(" + localCellX.ToString(CultureInfo.InvariantCulture) +
                    "," + localCellY.ToString(CultureInfo.InvariantCulture) +
                    ") original=(" + originalX.ToString("0.###", CultureInfo.InvariantCulture) +
                    "," + originalY.ToString("0.###", CultureInfo.InvariantCulture) +
                    ") local=(" + local.x.ToString("0.###", CultureInfo.InvariantCulture) +
                    "," + local.y.ToString("0.###", CultureInfo.InvariantCulture) +
                    "," + local.z.ToString("0.###", CultureInfo.InvariantCulture) + ")";
                return local;
            }

            float s = WallOriginalXYUnitToWorldScaleV8LikeOriginal();
            float drawX = (md != null ? md.PicDx : 0) + checkpointCell.x * 16.0f;
            float drawY = (md != null ? md.PicDy : 0) + checkpointCell.y * 16.0f;
            Vector3 fallback = C2BuildingDeleteRuntimeEffectScreenPointToUnityLocalLikeOriginal(drawX, drawY, s);
            audit =
                "fallback_drawspace check=(" + checkpointCell.x.ToString("0.###", CultureInfo.InvariantCulture) +
                "," + checkpointCell.y.ToString("0.###", CultureInfo.InvariantCulture) +
                ") draw=(" + drawX.ToString("0.###", CultureInfo.InvariantCulture) +
                "," + drawY.ToString("0.###", CultureInfo.InvariantCulture) +
                ") local=(" + fallback.x.ToString("0.###", CultureInfo.InvariantCulture) +
                "," + fallback.y.ToString("0.###", CultureInfo.InvariantCulture) +
                "," + fallback.z.ToString("0.###", CultureInfo.InvariantCulture) + ")";
            return fallback;
        }

        private static Vector3 C2BuildingDeleteRuntimeEffectScreenPointToUnityLocalLikeOriginal(
            float screenX,
            float screenY,
            float scale)
        {
            // Original MiniMap4X.cpp::ShowFiresNearBuilding:
            //   Vector4D pos(PicDx + FireX, 0, -PicDy - FireY, 1);
            //   pos *= ScreenToWorldSpace() * DrawSpriteBuildingM4;
            // Our building sprites are already baked in DrawSpriteBuilding draw-space, where original.z is Unity vertical.
            // Therefore the original screen Y term (-PicDy-FireY) must move along Unity Y, not along map Z.
            return new Vector3(screenX * scale, -screenY * scale, 0.0f);
        }

        private static float C2BuildingDeleteRuntimeSmokeSizeMultiplierLikeOriginal(int layer, int style)
        {
            if (layer <= 0) return style == 3 ? 1.05f : 0.95f;
            if (layer == 1) return style == 3 ? 1.55f : 1.35f;
            return 1.95f;
        }

        private static float C2BuildingDeleteRuntimeSmokeHeightMultiplierLikeOriginal(int layer, int style)
        {
            if (layer <= 0) return 1.15f;
            if (layer == 1) return style == 3 ? 1.55f : 1.42f;
            return 1.85f;
        }

        private static Vector3 C2BuildingDeleteRuntimeSmokeVelocityLikeOriginal(int variant, float s)
        {
            int layer = Mathf.Abs(variant) % 10;
            int smokeIndex = Mathf.Abs(variant) / 10;
            int style = (Mathf.Abs(variant) / 100) & 3;

            float sideSeed = (((smokeIndex * 19 + style * 7 + layer * 11) % 13) - 6) / 6.0f;
            float x = sideSeed * (layer <= 0 ? 0.24f : (layer == 1 ? 0.46f : 0.70f)) * s;
            float y = (layer <= 0 ? 18.0f : (layer == 1 ? 29.0f : 40.0f)) * s;
            if (style == 3) y *= 1.10f; // Smock4/Dop4 is taller
            return new Vector3(x, y, 0.0f);
        }

        private static Color C2BuildingDeleteRuntimeSmokeColorLikeOriginal(int variant)
        {
            int layer = Mathf.Abs(variant) % 10;
            int style = (Mathf.Abs(variant) / 100) & 3;

            if (layer <= 0)
                return new Color(0.54f, 0.55f, 0.54f, style == 3 ? 0.34f : 0.30f);
            if (layer == 1)
                return new Color(0.86f, 0.87f, 0.84f, style == 3 ? 0.48f : 0.42f);
            return new Color(0.92f, 0.93f, 0.90f, 0.24f);
        }

        private static float C2BuildingDeleteRuntimeSmokeFpsLikeOriginal(int variant)
        {
            int layer = Mathf.Abs(variant) % 10;
            int style = (Mathf.Abs(variant) / 100) & 3;
            return 4.4f + layer * 0.55f + style * 0.18f;
        }

        private static float C2BuildingDeleteRuntimeSmokeFadeInLikeOriginal(int variant)
        {
            int layer = Mathf.Abs(variant) % 10;
            return layer <= 0 ? 0.75f : (layer == 1 ? 1.15f : 1.55f);
        }

        private static float C2BuildingDeleteRuntimeSmokeGrowStartLikeOriginal(int variant)
        {
            int layer = Mathf.Abs(variant) % 10;
            return layer <= 0 ? 0.38f : (layer == 1 ? 0.25f : 0.18f);
        }

        private static void C2BuildingDeleteRuntimeCreateC2MStyleFireParticleEmitterLikeOriginal(
            GameObject go,
            int sortingOrder,
            int variant,
            bool oneShot,
            float autoDestroyAfter,
            float initialDelay,
            float worldScale)
        {
            if (go == null) return;

            Texture2D atlas = C2BuildingDeleteRuntimeGetFireAtlasTextureLikeOriginal();
            Shader shader = Shader.Find("Particles/Additive");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

            if (shader == null || atlas == null)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }

            Material mat = new Material(shader);
            mat.name = "C2_DELETE_BuildingFire_C2MParticle_Mat";
            mat.mainTexture = atlas;
            mat.renderQueue = 4990;
            mat.SetOverrideTag("RenderType", "Transparent");
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1.0f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2.0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.One);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0.0f);
            if (mat.HasProperty("_ZTest")) mat.SetFloat("_ZTest", (float)CompareFunction.Always);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", new Color(1.0f, 0.78f, 0.34f, 0.92f));
            if (mat.HasProperty("_TintColor"))
                mat.SetColor("_TintColor", new Color(1.0f, 0.62f, 0.20f, 0.72f));

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = !oneShot;
            main.duration = oneShot ? Mathf.Max(0.30f, autoDestroyAfter > 0.0f ? autoDestroyAfter : 0.75f) : 1.25f;
            main.startDelay = Mathf.Max(0.0f, initialDelay);
            main.startLifetime = oneShot
                ? new ParticleSystem.MinMaxCurve(0.28f, 0.55f)
                : new ParticleSystem.MinMaxCurve(0.55f, 0.95f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.0f, 0.018f * Mathf.Max(1.0f, worldScale));
            main.startSize = oneShot
                ? new ParticleSystem.MinMaxCurve(38.0f * worldScale, 62.0f * worldScale)
                : new ParticleSystem.MinMaxCurve(34.0f * worldScale, 58.0f * worldScale);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1.0f, 0.56f, 0.18f, 0.78f),
                new Color(1.0f, 0.88f, 0.38f, 0.95f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = oneShot ? 18 : 28;
            main.playOnAwake = false;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = oneShot
                ? new ParticleSystem.MinMaxCurve(0.0f)
                : new ParticleSystem.MinMaxCurve(8.0f + (variant % 4) * 1.5f, 13.0f + (variant % 5) * 1.4f);
            if (oneShot)
            {
                ParticleSystem.Burst burst = new ParticleSystem.Burst(0.0f, (short)(4 + (variant % 5)));
                emission.SetBursts(new ParticleSystem.Burst[] { burst });
            }

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8.0f;
            shape.radius = Mathf.Max(0.002f, 7.0f * worldScale);
            shape.length = Mathf.Max(0.002f, 3.0f * worldScale);

            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-4.0f * worldScale, 4.0f * worldScale);
            vel.y = new ParticleSystem.MinMaxCurve(12.0f * worldScale, 26.0f * worldScale);
            vel.z = new ParticleSystem.MinMaxCurve(0.0f);

            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0.0f, 0.45f),
                new Keyframe(0.18f, 1.0f),
                new Keyframe(0.70f, 0.82f),
                new Keyframe(1.0f, 0.20f));
            sol.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1.0f, 0.86f, 0.26f), 0.00f),
                    new GradientColorKey(new Color(1.0f, 0.35f, 0.05f), 0.42f),
                    new GradientColorKey(new Color(0.22f, 0.08f, 0.02f), 1.00f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.00f, 0.00f),
                    new GradientAlphaKey(0.95f, 0.12f),
                    new GradientAlphaKey(0.76f, 0.55f),
                    new GradientAlphaKey(0.00f, 1.00f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);

            ParticleSystem.TextureSheetAnimationModule tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Grid;
            tsa.numTilesX = 8;
            tsa.numTilesY = 8;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.useRandomRow = false;
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(
                1.0f,
                new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f)));
            tsa.startFrame = new ParticleSystem.MinMaxCurve(((variant * 11) & 63) / 64.0f);
            tsa.cycleCount = 1;

            ParticleSystemRenderer pr = ps.GetComponent<ParticleSystemRenderer>();
            if (pr != null)
            {
                pr.renderMode = ParticleSystemRenderMode.Billboard;
                pr.material = mat;
                pr.sortingOrder = Mathf.Clamp(sortingOrder, -30000, 30000);
                pr.shadowCastingMode = ShadowCastingMode.Off;
                pr.receiveShadows = false;
                pr.minParticleSize = 0.0f;
                pr.maxParticleSize = 0.18f;
            }

            if (autoDestroyAfter > 0.0f)
            {
                C2BuildingDeleteAutoDestroyLikeOriginal killer = go.AddComponent<C2BuildingDeleteAutoDestroyLikeOriginal>();
                killer.Delay = autoDestroyAfter + Mathf.Max(0.0f, initialDelay) + 0.45f;
            }

            ps.Play(true);
        }

        private static Mesh C2BuildingDeleteRuntimeCreateQuadMeshLikeOriginal(float width, float height)
        {
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            Mesh mesh = new Mesh();
            mesh.name = "C2_Delete_Effect_Quad";
            mesh.vertices = new Vector3[]
            {
                new Vector3(-hw, -hh, 0.0f),
                new Vector3( hw, -hh, 0.0f),
                new Vector3( hw,  hh, 0.0f),
                new Vector3(-hw,  hh, 0.0f)
            };
            mesh.uv = new Vector2[]
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(0.0f, 1.0f)
            };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh C2BuildingDeleteRuntimeCreateBottomPivotQuadMeshLikeOriginal(float width, float height)
        {
            float hw = width * 0.5f;
            Mesh mesh = new Mesh();
            mesh.name = "C2_Delete_Fire_BottomPivot_Quad";
            mesh.vertices = new Vector3[]
            {
                new Vector3(-hw, 0.0f, 0.0f),
                new Vector3( hw, 0.0f, 0.0f),
                new Vector3( hw, height, 0.0f),
                new Vector3(-hw, height, 0.0f)
            };
            mesh.uv = new Vector2[]
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(0.0f, 1.0f)
            };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Texture2D[][] s_C2DeleteFireClipFrames;
        private static Texture2D s_C2DeleteFireAtlasTexture;
        private static Texture2D[] s_C2DeleteSmokeFrames;
        private static Texture2D[] s_C2DeletePodrivFlashFrames;
        private static Texture2D[] s_C2DeletePodrivSmokeFrames;
        private static Texture2D[] s_C2DeletePodrivStarFrames;
        private static Texture2D[] s_C2DeletePodrivStoneDebrisFrames;
        private static Texture2D[] s_C2DeletePodrivWoodDebrisFrames;
        private static bool s_C2DeleteSmokeTried;
        private static bool s_C2DeleteFireAtlasTried;
        private static bool s_C2DeletePodrivFlashTried;
        private static bool s_C2DeletePodrivSmokeTried;
        private static bool s_C2DeletePodrivStarTried;
        private static bool s_C2DeletePodrivStoneDebrisTried;
        private static bool s_C2DeletePodrivWoodDebrisTried;
        private static readonly Dictionary<string, Texture2D[]> s_C2DeleteOblWeaponFrames =
            new Dictionary<string, Texture2D[]>(StringComparer.OrdinalIgnoreCase);
        private static global::TemnyLessViewer.C2GpSystem s_C2DeleteWeaponGps;

        private static Texture2D C2BuildingDeleteRuntimeGetFireAtlasTextureLikeOriginal()
        {
            if (s_C2DeleteFireAtlasTexture != null) return s_C2DeleteFireAtlasTexture;

            string[] paths = C2BuildingDeleteRuntimeFireAtlasCandidatesLikeOriginal();
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

                int width;
                int height;
                Color32[] pixels;
                string audit;
                if (!C2BuildingDeleteRuntimeTryReadTgaRgba32LikeOriginal(path, out width, out height, out pixels, out audit))
                    continue;

                C2BuildingDeleteRuntimeFireAtlasApplyBlackAlphaCutoutLikeOriginal(pixels, width, height);

                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.name = "C2_Delete_BuildingFire_full_fire_anim1_alpha_cutout";
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                tex.SetPixels32(pixels);
                tex.Apply(false, false);
                s_C2DeleteFireAtlasTexture = tex;

                Debug.Log("[C2:BUILD DELETE V15 FIRE ATLAS] loaded='" + path + "' size=" +
                          width.ToString(CultureInfo.InvariantCulture) + "x" +
                          height.ToString(CultureInfo.InvariantCulture) +
                          " rule=particle_system_texture_sheet_alpha_cutout_black_pixels_removed original=EngineSettings.FiresList/Gorenie_Zdaniya_Ogon*.c2m");

                return s_C2DeleteFireAtlasTexture;
            }

            return null;
        }

        private static void C2BuildingDeleteRuntimeFireAtlasApplyBlackAlphaCutoutLikeOriginal(
            Color32[] pixels,
            int width,
            int height)
        {
            if (pixels == null || width <= 0 || height <= 0) return;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = pixels[i];

                int max = Math.Max(c.r, Math.Max(c.g, c.b));
                int brightness = (c.r * 54 + c.g * 183 + c.b * 19) >> 8;

                // fire_anim1.tga is an engine particle texture; in Unity its dark/red background
                // must become transparent before frame animation. Never preserve original alpha here:
                // some source frames carry non-zero alpha in almost-black pixels, which created squares.
                if (max <= 36 || brightness <= 24 || (c.g < 18 && brightness < 74))
                {
                    c.r = 0;
                    c.g = 0;
                    c.b = 0;
                    c.a = 0;
                    pixels[i] = c;
                    continue;
                }

                int alphaFromLight = Mathf.Clamp((brightness - 22) * 5, 0, 255);
                if (c.g < 42 && brightness < 96)
                    alphaFromLight = Mathf.Min(alphaFromLight, 90);
                c.a = (byte)Mathf.Clamp(alphaFromLight, 0, 255);
                pixels[i] = c;
            }
        }

        private static Texture2D[] C2BuildingDeleteRuntimeGetEffectFramesLikeOriginal(bool fire, int variant)
        {
            if (fire)
            {
                if (s_C2DeleteFireClipFrames == null)
                {
                    s_C2DeleteFireClipFrames = C2BuildingDeleteRuntimeTryLoadFireAtlasTgaClipsLikeOriginal();
                    if (s_C2DeleteFireClipFrames == null || s_C2DeleteFireClipFrames.Length == 0)
                    {
                        s_C2DeleteFireClipFrames = new Texture2D[][]
                        {
                            C2BuildingDeleteRuntimeCreateProceduralFramesLikeOriginal(true, 8)
                        };
                    }
                }

                int idx = 0;
                if (s_C2DeleteFireClipFrames.Length > 1)
                    idx = Mathf.Abs(variant) % s_C2DeleteFireClipFrames.Length;
                return s_C2DeleteFireClipFrames[idx];
            }
            // V15: use C2M-structured procedural smoke frames.
            // The uploaded Gorenie_Zdaniya_Smock*.c2m blocks show Billboard + SizeRamp + VelRamp + AlphaRamp + ColorRamp + Wind.
            // Raw smoke_01/smoke_02 quads looked like paper; this keeps soft puffs but drives them with C2M-like ramps/layers.
            if (s_C2DeleteSmokeFrames == null)
                s_C2DeleteSmokeFrames = C2BuildingDeleteRuntimeCreateProceduralFramesLikeOriginal(false, 24);
            return s_C2DeleteSmokeFrames;
        }

        private static Texture2D[] C2BuildingDeleteRuntimeGetPodrivFlashFramesLikeOriginal()
        {
            if (s_C2DeletePodrivFlashFrames != null && s_C2DeletePodrivFlashFrames.Length > 0)
                return s_C2DeletePodrivFlashFrames;
            if (!s_C2DeletePodrivFlashTried)
            {
                s_C2DeletePodrivFlashTried = true;
                s_C2DeletePodrivFlashFrames = C2BuildingDeleteRuntimeTryLoadParticleAtlasFramesLikeOriginal(
                    "pfx_sparks1.tga",
                    4,
                    2,
                    "C2_Delete_Podriv_PfxSparks1",
                    true);
            }

            if (s_C2DeletePodrivFlashFrames == null || s_C2DeletePodrivFlashFrames.Length == 0)
                s_C2DeletePodrivFlashFrames = C2BuildingDeleteRuntimeCreateProceduralFramesLikeOriginal(true, 8);
            return s_C2DeletePodrivFlashFrames;
        }

        private static Texture2D[] C2BuildingDeleteRuntimeGetPodrivSmokeFramesLikeOriginal()
        {
            if (s_C2DeletePodrivSmokeFrames != null && s_C2DeletePodrivSmokeFrames.Length > 0)
                return s_C2DeletePodrivSmokeFrames;
            if (!s_C2DeletePodrivSmokeTried)
            {
                s_C2DeletePodrivSmokeTried = true;
                s_C2DeletePodrivSmokeFrames = C2BuildingDeleteRuntimeTryLoadParticleSmoke01FramesLikeOriginal();
            }

            if (s_C2DeletePodrivSmokeFrames == null || s_C2DeletePodrivSmokeFrames.Length == 0)
                s_C2DeletePodrivSmokeFrames = C2BuildingDeleteRuntimeCreateProceduralFramesLikeOriginal(false, 12);
            return s_C2DeletePodrivSmokeFrames;
        }

        private static Texture2D[] C2BuildingDeleteRuntimeGetPodrivStarFramesLikeOriginal()
        {
            if (s_C2DeletePodrivStarFrames != null && s_C2DeletePodrivStarFrames.Length > 0)
                return s_C2DeletePodrivStarFrames;
            if (!s_C2DeletePodrivStarTried)
            {
                s_C2DeletePodrivStarTried = true;
                Texture2D star = C2BuildingDeleteRuntimeTryLoadParticleSingleTextureLikeOriginal(
                    "star2.tga",
                    "C2_Delete_Podriv_Star2",
                    false);
                if (star == null)
                    star = C2BuildingDeleteRuntimeTryLoadParticleSingleTextureLikeOriginal(
                        "Vzriv.tga",
                        "C2_Delete_Podriv_Vzriv",
                        false);
                if (star != null)
                    s_C2DeletePodrivStarFrames = new Texture2D[] { star };
            }

            if (s_C2DeletePodrivStarFrames == null || s_C2DeletePodrivStarFrames.Length == 0)
                s_C2DeletePodrivStarFrames = C2BuildingDeleteRuntimeCreateProceduralFramesLikeOriginal(true, 1);
            return s_C2DeletePodrivStarFrames;
        }

        private static Texture2D[] C2BuildingDeleteRuntimeGetPodrivStoneDebrisFramesLikeOriginal()
        {
            if (s_C2DeletePodrivStoneDebrisFrames != null && s_C2DeletePodrivStoneDebrisFrames.Length > 0)
                return s_C2DeletePodrivStoneDebrisFrames;
            if (!s_C2DeletePodrivStoneDebrisTried)
            {
                s_C2DeletePodrivStoneDebrisTried = true;
                s_C2DeletePodrivStoneDebrisFrames = C2BuildingDeleteRuntimeTryLoadParticleAtlasFramesLikeOriginal(
                    "pfx_stonedebris1.tga",
                    4,
                    4,
                    "C2_Delete_Podriv_StoneDebris1",
                    false);
            }

            if (s_C2DeletePodrivStoneDebrisFrames == null || s_C2DeletePodrivStoneDebrisFrames.Length == 0)
                s_C2DeletePodrivStoneDebrisFrames = C2BuildingDeleteRuntimeCreateProceduralFramesLikeOriginal(true, 4);
            return s_C2DeletePodrivStoneDebrisFrames;
        }

        private static Texture2D[] C2BuildingDeleteRuntimeGetPodrivWoodDebrisFramesLikeOriginal()
        {
            if (s_C2DeletePodrivWoodDebrisFrames != null && s_C2DeletePodrivWoodDebrisFrames.Length > 0)
                return s_C2DeletePodrivWoodDebrisFrames;
            if (!s_C2DeletePodrivWoodDebrisTried)
            {
                s_C2DeletePodrivWoodDebrisTried = true;
                s_C2DeletePodrivWoodDebrisFrames = C2BuildingDeleteRuntimeTryLoadParticleAtlasFramesLikeOriginal(
                    "woodchips.tga",
                    4,
                    4,
                    "C2_Delete_Podriv_WoodChips",
                    false);
            }

            if (s_C2DeletePodrivWoodDebrisFrames == null || s_C2DeletePodrivWoodDebrisFrames.Length == 0)
                s_C2DeletePodrivWoodDebrisFrames = C2BuildingDeleteRuntimeCreateProceduralFramesLikeOriginal(true, 4);
            return s_C2DeletePodrivWoodDebrisFrames;
        }

        private static Texture2D[] C2BuildingDeleteRuntimeGetOblWeaponFramesLikeOriginal(string weaponName)
        {
            string key = (weaponName ?? string.Empty).Trim();
            if (key.Length == 0) return null;

            Texture2D[] cached;
            if (s_C2DeleteOblWeaponFrames.TryGetValue(key, out cached))
                return cached;

            int first;
            int last;
            if (!C2BuildingDeleteRuntimeTryGetOblWeaponRangeLikeOriginal(key, out first, out last))
            {
                cached = C2BuildingDeleteRuntimeGetPodrivStoneDebrisFramesLikeOriginal();
                s_C2DeleteOblWeaponFrames[key] = cached;
                return cached;
            }

            cached = C2BuildingDeleteRuntimeTryLoadOblAllFramesLikeOriginal(key, first, last);
            if (cached == null || cached.Length == 0)
                cached = C2BuildingDeleteRuntimeGetPodrivStoneDebrisFramesLikeOriginal();

            s_C2DeleteOblWeaponFrames[key] = cached;
            return cached;
        }

        private static bool C2BuildingDeleteRuntimeTryGetOblWeaponRangeLikeOriginal(
            string weaponName,
            out int first,
            out int last)
        {
            first = 0;
            last = 0;
            if (string.Equals(weaponName, "obl16", StringComparison.OrdinalIgnoreCase))
            {
                first = 258;
                last = 274;
                return true;
            }
            if (string.Equals(weaponName, "obl23", StringComparison.OrdinalIgnoreCase))
            {
                first = 377;
                last = 393;
                return true;
            }
            if (string.Equals(weaponName, "obl38", StringComparison.OrdinalIgnoreCase))
            {
                first = 633;
                last = 649;
                return true;
            }
            return false;
        }

        private static Texture2D[] C2BuildingDeleteRuntimeTryLoadOblAllFramesLikeOriginal(
            string weaponName,
            int first,
            int last)
        {
            string[] roots = C2BuildingDeleteRuntimeDataRootCandidatesLikeOriginal();
            string lastError = string.Empty;
            for (int r = 0; r < roots.Length; r++)
            {
                string root = roots[r];
                if (string.IsNullOrEmpty(root)) continue;
                if (!File.Exists(Path.Combine(root, "oblall.g2d"))) continue;

                global::TemnyLessViewer.C2GpSystem gps = s_C2DeleteWeaponGps;
                if (gps == null)
                {
                    gps = new global::TemnyLessViewer.C2GpSystem();
                    gps.MaxCachedFrames = 256;
                    gps.MaxCachedBytes = 160L * 1024L * 1024L;
                }

                string preloadError;
                int gpId = gps.PreLoadGPImage("oblall", root, out preloadError);
                if (gpId < 0)
                {
                    lastError = preloadError ?? string.Empty;
                    continue;
                }

                int frameCount = gps.GetFrameCount(gpId);
                if (frameCount <= first)
                {
                    lastError = "frameCount=" + frameCount.ToString(CultureInfo.InvariantCulture);
                    continue;
                }

                int safeLast = Mathf.Min(last, frameCount - 1);
                List<Texture2D> frames = new List<Texture2D>();
                for (int frameId = first; frameId <= safeLast; frameId++)
                {
                    global::TemnyLessViewer.C2RenderedFrame rendered;
                    string frameError;
                    if (!gps.GetRenderedFrame(gpId, frameId, out rendered, out frameError) || rendered == null || rendered.Rgba == null)
                    {
                        lastError = frameError ?? string.Empty;
                        continue;
                    }

                    Texture2D tex = C2BuildingDeleteRuntimeCreateTextureFromGpFrameLikeOriginal("oblall", frameId, rendered);
                    if (tex != null) frames.Add(tex);
                }

                if (frames.Count > 0)
                {
                    s_C2DeleteWeaponGps = gps;
                    Debug.Log("[C2:BUILD DELETE V273 OBLALL TEX] loaded='" + gps.GetPackagePath(gpId) +
                              "' weapon='" + (weaponName ?? string.Empty) +
                              "' range=" + first.ToString(CultureInfo.InvariantCulture) + "-" + safeLast.ToString(CultureInfo.InvariantCulture) +
                              " frames=" + frames.Count.ToString(CultureInfo.InvariantCulture) +
                              " source=WEAPON.ADS @" + (weaponName ?? string.Empty));
                    return frames.ToArray();
                }
            }

            Debug.LogWarning("[C2:BUILD DELETE V273 OBLALL TEX FAIL] weapon='" + (weaponName ?? string.Empty) +
                             "' range=" + first.ToString(CultureInfo.InvariantCulture) + "-" + last.ToString(CultureInfo.InvariantCulture) +
                             " fallback=pfx_stonedebris1 reason='" + lastError + "'");
            return null;
        }

        private static Texture2D C2BuildingDeleteRuntimeCreateTextureFromGpFrameLikeOriginal(
            string package,
            int spriteId,
            global::TemnyLessViewer.C2RenderedFrame rendered)
        {
            if (rendered == null || rendered.Width <= 0 || rendered.Height <= 0 || rendered.Rgba == null) return null;
            int w = rendered.Width;
            int h = rendered.Height;
            if (rendered.Rgba.Length < w * h * 4) return null;

            byte[] unityRgba = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
            {
                int srcRow = y * w * 4;
                int dstRow = (h - 1 - y) * w * 4;
                Buffer.BlockCopy(rendered.Rgba, srcRow, unityRgba, dstRow, w * 4);
            }

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            tex.name = "C2_Delete_" + (package ?? "gp") + "_" + spriteId.ToString(CultureInfo.InvariantCulture);
            tex.LoadRawTextureData(unityRgba);
            tex.Apply(false, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        private static string[] C2BuildingDeleteRuntimeDataRootCandidatesLikeOriginal()
        {
            List<string> list = new List<string>();
            Action<string> add = delegate(string p)
            {
                if (string.IsNullOrEmpty(p)) return;
                for (int i = 0; i < list.Count; i++)
                    if (string.Equals(list[i], p, StringComparison.OrdinalIgnoreCase)) return;
                list.Add(p);
            };

            add(@"C:\GSC Game World\Cossacks II\Data");
            add(@"C:\GSC Game World\Cossacks II\Data1");
            add(@"C:\Program Files (x86)\GSC Game World\Cossacks II\Data");
            add(@"C:\Games\Cossacks II\Data");

            string app = Application.dataPath ?? string.Empty;
            if (!string.IsNullOrEmpty(app))
            {
                add(Path.Combine(app, "..", "Data"));
                add(Path.Combine(app, "..", "Data1"));
                add(Path.Combine(app, "Resources", "Data"));
                add(Path.Combine(app, "Resources", "Data1"));
                add(Path.Combine(app, "Cossacks2Bridge", "Resources", "Data"));
            }

            return list.ToArray();
        }

        private static Texture2D[] C2BuildingDeleteRuntimeTryLoadParticleAtlasFramesLikeOriginal(
            string fileName,
            int columns,
            int rows,
            string namePrefix,
            bool alphaFromBlack)
        {
            string[] candidates = C2BuildingDeleteRuntimeSmokeAtlasCandidatesLikeOriginal(fileName);
            for (int i = 0; i < candidates.Length; i++)
            {
                string path = candidates[i];
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

                int width;
                int height;
                Color32[] pixels;
                string audit;
                if (!C2BuildingDeleteRuntimeTryReadTgaRgba32LikeOriginal(path, out width, out height, out pixels, out audit))
                    continue;
                if (alphaFromBlack)
                    C2BuildingDeleteRuntimeFireAtlasApplyBlackAlphaCutoutLikeOriginal(pixels, width, height);

                Texture2D[][] rowClips = C2BuildingDeleteRuntimeSliceAtlasRowsTopLeftLikeOriginal(
                    pixels,
                    width,
                    height,
                    columns,
                    rows,
                    namePrefix);

                Texture2D[] frames = C2BuildingDeleteRuntimeFlattenRowsLikeOriginal(rowClips);
                if (frames != null && frames.Length > 0)
                {
                    Debug.Log("[C2:BUILD DELETE V267 PODRIV TEX] loaded='" + path + "' size=" +
                              width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) +
                              " frames=" + frames.Length.ToString(CultureInfo.InvariantCulture) +
                              " source=Podriv_Zdaniya.c2m/" + (fileName ?? string.Empty));
                    return frames;
                }
            }

            return null;
        }

        private static Texture2D[] C2BuildingDeleteRuntimeTryLoadParticleSmoke01FramesLikeOriginal()
        {
            string[] candidates = C2BuildingDeleteRuntimeSmokeAtlasCandidatesLikeOriginal("smoke_01.tga");
            for (int i = 0; i < candidates.Length; i++)
            {
                string path = candidates[i];
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

                int width;
                int height;
                Color32[] pixels;
                string audit;
                if (!C2BuildingDeleteRuntimeTryReadTgaRgba32LikeOriginal(path, out width, out height, out pixels, out audit))
                    continue;

                Texture2D[][] rowClips = C2BuildingDeleteRuntimeSliceAtlasRowsTopLeftLikeOriginal(
                    pixels,
                    width,
                    height,
                    4,
                    1,
                    "C2_Delete_Podriv_Smoke01");

                Texture2D[] frames = C2BuildingDeleteRuntimeNormalizeSmokeClipLikeOriginal(rowClips, false);
                if (frames != null && frames.Length > 0)
                {
                    Debug.Log("[C2:BUILD DELETE V267 PODRIV SMOKE TEX] loaded='" + path + "' size=" +
                              width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) +
                              " frames=" + frames.Length.ToString(CultureInfo.InvariantCulture) +
                              " source=Podriv_Zdaniya.c2m/smoke_01.tga");
                    return frames;
                }
            }

            return null;
        }

        private static Texture2D C2BuildingDeleteRuntimeTryLoadParticleSingleTextureLikeOriginal(
            string fileName,
            string textureName,
            bool alphaFromBlack)
        {
            string[] candidates = C2BuildingDeleteRuntimeSmokeAtlasCandidatesLikeOriginal(fileName);
            for (int i = 0; i < candidates.Length; i++)
            {
                string path = candidates[i];
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

                int width;
                int height;
                Color32[] pixels;
                string audit;
                if (!C2BuildingDeleteRuntimeTryReadTgaRgba32LikeOriginal(path, out width, out height, out pixels, out audit))
                    continue;
                if (alphaFromBlack)
                    C2BuildingDeleteRuntimeFireAtlasApplyBlackAlphaCutoutLikeOriginal(pixels, width, height);

                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.name = textureName ?? "C2_Delete_Podriv_Single";
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                tex.SetPixels32(pixels);
                tex.Apply(false, false);
                Debug.Log("[C2:BUILD DELETE V267 PODRIV TEX] loaded='" + path + "' size=" +
                          width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) +
                          " source=Podriv_Zdaniya.c2m/" + (fileName ?? string.Empty));
                return tex;
            }

            return null;
        }

        private static Texture2D[] C2BuildingDeleteRuntimeFlattenRowsLikeOriginal(Texture2D[][] rowClips)
        {
            if (rowClips == null || rowClips.Length == 0) return null;
            List<Texture2D> frames = new List<Texture2D>();
            for (int r = 0; r < rowClips.Length; r++)
            {
                Texture2D[] row = rowClips[r];
                if (row == null) continue;
                for (int c = 0; c < row.Length; c++)
                    if (row[c] != null) frames.Add(row[c]);
            }
            return frames.Count > 0 ? frames.ToArray() : null;
        }

        private static Texture2D[][] C2BuildingDeleteRuntimeTryLoadFireAtlasTgaClipsLikeOriginal()
        {
            if (s_C2DeleteFireAtlasTried) return null;
            s_C2DeleteFireAtlasTried = true;

            string[] candidates = C2BuildingDeleteRuntimeFireAtlasCandidatesLikeOriginal();
            for (int i = 0; i < candidates.Length; i++)
            {
                string path = candidates[i];
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

                int width;
                int height;
                Color32[] pixels;
                string audit;
                if (!C2BuildingDeleteRuntimeTryReadTgaRgba32LikeOriginal(path, out width, out height, out pixels, out audit))
                {
                    Debug.LogWarning("[C2:BUILD DELETE V15 FIRE ATLAS FAIL] path='" + path + "' " + audit);
                    continue;
                }

                C2BuildingDeleteRuntimeFireAtlasApplyBlackAlphaCutoutLikeOriginal(pixels, width, height);

                Texture2D[][] rowClips = C2BuildingDeleteRuntimeSliceAtlasRowsTopLeftLikeOriginal(
                    pixels,
                    width,
                    height,
                    11,
                    7,
                    "C2_Delete_FireAnim1_TGA_11x7");

                Texture2D[][] clips = C2BuildingDeleteRuntimeSelectC2MFrameAnimBuildingFireClipsLikeOriginal(rowClips);
                if (clips != null && clips.Length > 0)
                {
                    Debug.Log("[C2:BUILD DELETE V15 FIRE ATLAS] loaded='" + path + "' size=" +
                              width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) +
                              " clips=" + clips.Length.ToString(CultureInfo.InvariantCulture) +
                              " framesPerClip=" + clips[0].Length.ToString(CultureInfo.InvariantCulture) +
                              " rule=progressive_life_based_fire_count_full_11x7_no_initial_flash_bottom_centered_alpha_frames original=EngineSettings.FiresList/Gorenie_Zdaniya_Ogon*.c2m source=textures/particles/fire_anim1.tga");
                    return clips;
                }
            }

            return null;
        }

        private static Texture2D[][] C2BuildingDeleteRuntimeTryLoadSmokeAtlasClipsLikeOriginal()
        {
            if (s_C2DeleteSmokeTried) return null;
            s_C2DeleteSmokeTried = true;

            List<Texture2D[]> clips = new List<Texture2D[]>();

            string[] smoke01 = C2BuildingDeleteRuntimeSmokeAtlasCandidatesLikeOriginal("smoke_01.tga");
            for (int i = 0; i < smoke01.Length; i++)
            {
                string path = smoke01[i];
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

                int width;
                int height;
                Color32[] pixels;
                string audit;
                if (!C2BuildingDeleteRuntimeTryReadTgaRgba32LikeOriginal(path, out width, out height, out pixels, out audit))
                    continue;

                Texture2D[][] rowClips = C2BuildingDeleteRuntimeSliceAtlasRowsTopLeftLikeOriginal(
                    pixels,
                    width,
                    height,
                    4,
                    1,
                    "C2_Delete_Smoke01_4x1");

                Texture2D[] clip = C2BuildingDeleteRuntimeNormalizeSmokeClipLikeOriginal(rowClips, false);
                if (clip != null && clip.Length > 0)
                {
                    // EngineSettings SmokeList: Smock1..3 use smoke_01.tga.
                    clips.Add(clip);
                    clips.Add(clip);
                    clips.Add(clip);
                    Debug.Log("[C2:BUILD DELETE V15 SMOKE ATLAS] loaded='" + path + "' size=" +
                              width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) +
                              " variants=3 framesPerClip=" + clip.Length.ToString(CultureInfo.InvariantCulture) +
                              " source=textures/particles/smoke_01.tga original=EngineSettings.SmokeList/Gorenie_Zdaniya_Smock1..3.c2m");
                    break;
                }
            }

            string[] smoke02 = C2BuildingDeleteRuntimeSmokeAtlasCandidatesLikeOriginal("smoke_02.tga");
            for (int i = 0; i < smoke02.Length; i++)
            {
                string path = smoke02[i];
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

                int width;
                int height;
                Color32[] pixels;
                string audit;
                if (!C2BuildingDeleteRuntimeTryReadTgaRgba32LikeOriginal(path, out width, out height, out pixels, out audit))
                    continue;

                Texture2D[][] rowClips = C2BuildingDeleteRuntimeSliceAtlasRowsTopLeftLikeOriginal(
                    pixels,
                    width,
                    height,
                    2,
                    2,
                    "C2_Delete_Smoke02_2x2");

                Texture2D[] clip = C2BuildingDeleteRuntimeNormalizeSmokeClipLikeOriginal(rowClips, true);
                if (clip != null && clip.Length > 0)
                {
                    // EngineSettings SmokeList: Smock4 uses smoke_02.tga.
                    clips.Add(clip);
                    Debug.Log("[C2:BUILD DELETE V15 SMOKE ATLAS] loaded='" + path + "' size=" +
                              width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) +
                              " variants=1 framesPerClip=" + clip.Length.ToString(CultureInfo.InvariantCulture) +
                              " source=textures/particles/smoke_02.tga original=EngineSettings.SmokeList/Gorenie_Zdaniya_Smock4.c2m");
                    break;
                }
            }

            if (clips.Count > 0) return clips.ToArray();
            return null;
        }

        private static string[] C2BuildingDeleteRuntimeSmokeAtlasCandidatesLikeOriginal(string fileName)
        {
            List<string> list = new List<string>();
            Action<string> add = delegate(string p)
            {
                if (string.IsNullOrEmpty(p)) return;
                for (int i = 0; i < list.Count; i++)
                    if (string.Equals(list[i], p, StringComparison.OrdinalIgnoreCase)) return;
                list.Add(p);
            };

            string rel = Path.Combine("textures", "particles", fileName ?? string.Empty);

            add(Path.Combine(@"C:\GSC Game World\Cossacks II\Data", rel));
            add(Path.Combine(@"C:\GSC Game World\Cossacks II\Data1", rel));
            add(Path.Combine(@"C:\Program Files (x86)\GSC Game World\Cossacks II\Data", rel));
            add(Path.Combine(@"C:\Games\Cossacks II\Data", rel));

            string app = Application.dataPath ?? string.Empty;
            if (!string.IsNullOrEmpty(app))
            {
                add(Path.Combine(app, "..", "Data", rel));
                add(Path.Combine(app, "..", "Data1", rel));
                add(Path.Combine(app, "Resources", rel));
                add(Path.Combine(app, "Resources", "Data", rel));
                add(Path.Combine(app, "Resources", "Data1", rel));
                add(Path.Combine(app, "Cossacks2Bridge", "Resources", rel));
            }

            return list.ToArray();
        }

        private static Texture2D[] C2BuildingDeleteRuntimeNormalizeSmokeClipLikeOriginal(Texture2D[][] rowClips, bool brighten)
        {
            if (rowClips == null || rowClips.Length == 0) return null;
            List<Texture2D> frames = new List<Texture2D>();
            for (int r = 0; r < rowClips.Length; r++)
            {
                Texture2D[] row = rowClips[r];
                if (row == null || row.Length == 0) continue;
                for (int c = 0; c < row.Length; c++)
                {
                    Texture2D normalized = C2BuildingDeleteRuntimeNormalizeSmokeAtlasFrameLikeOriginal(
                        row[c],
                        "C2_Delete_Smoke_Frame_r" + r.ToString(CultureInfo.InvariantCulture) + "_c" + c.ToString(CultureInfo.InvariantCulture),
                        brighten);
                    if (normalized != null) frames.Add(normalized);
                }
            }
            return frames.Count > 0 ? frames.ToArray() : null;
        }

        private static Texture2D C2BuildingDeleteRuntimeNormalizeSmokeAtlasFrameLikeOriginal(Texture2D src, string name, bool brighten)
        {
            if (src == null) return null;

            Color32[] srcPixels = src.GetPixels32();
            int sw = src.width;
            int sh = src.height;
            if (srcPixels == null || srcPixels.Length == 0 || sw <= 0 || sh <= 0) return null;

            int minX = sw;
            int minY = sh;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < sh; y++)
            {
                int row = y * sw;
                for (int x = 0; x < sw; x++)
                {
                    Color32 c = srcPixels[row + x];
                    int max = Math.Max(c.r, Math.Max(c.g, c.b));
                    int brightness = (c.r * 54 + c.g * 183 + c.b * 19) >> 8;
                    if (c.a <= 2 || max <= 10 || brightness <= 10) continue;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
                return null;

            int cropW = maxX - minX + 1;
            int cropH = maxY - minY + 1;

            const int canvasW = 176;
            const int canvasH = 176;
            const int bottomPad = 8;

            Color32[] dst = new Color32[canvasW * canvasH];
            for (int i = 0; i < dst.Length; i++)
                dst[i] = new Color32(0, 0, 0, 0);

            int copyW = Mathf.Min(cropW, canvasW);
            int copyH = Mathf.Min(cropH, canvasH - bottomPad);
            int dstX0 = (canvasW - copyW) / 2;
            int dstY0 = bottomPad;

            for (int y = 0; y < copyH; y++)
            {
                int sy = minY + y;
                int dy = dstY0 + y;
                if (sy < 0 || sy >= sh || dy < 0 || dy >= canvasH) continue;

                int srcRow = sy * sw;
                int dstRow = dy * canvasW;
                for (int x = 0; x < copyW; x++)
                {
                    int sx = minX + x;
                    int dx = dstX0 + x;
                    if (sx < 0 || sx >= sw || dx < 0 || dx >= canvasW) continue;

                    Color32 c = srcPixels[srcRow + sx];
                    int brightness = (c.r * 54 + c.g * 183 + c.b * 19) >> 8;
                    int max = Math.Max(c.r, Math.Max(c.g, c.b));
                    if (c.a <= 2 || max <= 10 || brightness <= 10)
                    {
                        c.a = 0;
                    }
                    else
                    {
                        int aa = Mathf.Clamp(brightness * 2 + 24, 0, 255);
                        c.a = (byte)Mathf.Clamp(Math.Max((int)c.a, aa / 2), 0, 255);
                        if (brighten)
                        {
                            c.r = (byte)Mathf.Clamp((int)(c.r * 1.25f + 36), 0, 255);
                            c.g = (byte)Mathf.Clamp((int)(c.g * 1.25f + 36), 0, 255);
                            c.b = (byte)Mathf.Clamp((int)(c.b * 1.25f + 36), 0, 255);
                        }
                        else
                        {
                            c.r = (byte)Mathf.Clamp((int)(c.r * 1.15f + 20), 0, 255);
                            c.g = (byte)Mathf.Clamp((int)(c.g * 1.15f + 20), 0, 255);
                            c.b = (byte)Mathf.Clamp((int)(c.b * 1.15f + 20), 0, 255);
                        }
                    }
                    dst[dstRow + dx] = c;
                }
            }

            Texture2D tex = new Texture2D(canvasW, canvasH, TextureFormat.RGBA32, false);
            tex.name = name ?? "C2_Delete_SmokeAtlas_Normalized";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(dst);
            tex.Apply(false, false);
            return tex;
        }

        private static string[] C2BuildingDeleteRuntimeFireAtlasCandidatesLikeOriginal()
        {
            List<string> list = new List<string>();
            Action<string> add = delegate(string p)
            {
                if (string.IsNullOrEmpty(p)) return;
                for (int i = 0; i < list.Count; i++)
                    if (string.Equals(list[i], p, StringComparison.OrdinalIgnoreCase)) return;
                list.Add(p);
            };

            string rel = Path.Combine("textures", "particles", "fire_anim1.tga");

            add(Path.Combine(@"C:\GSC Game World\Cossacks II\Data", rel));
            add(Path.Combine(@"C:\GSC Game World\Cossacks II\Data1", rel));
            add(Path.Combine(@"C:\Program Files (x86)\GSC Game World\Cossacks II\Data", rel));
            add(Path.Combine(@"C:\Games\Cossacks II\Data", rel));

            string app = Application.dataPath ?? string.Empty;
            if (!string.IsNullOrEmpty(app))
            {
                add(Path.Combine(app, "..", "Data", rel));
                add(Path.Combine(app, "..", "Data1", rel));
                add(Path.Combine(app, "Resources", rel));
                add(Path.Combine(app, "Resources", "Data", rel));
                add(Path.Combine(app, "Resources", "Data1", rel));
                add(Path.Combine(app, "Cossacks2Bridge", "Resources", rel));
            }

            return list.ToArray();
        }

        private static bool C2BuildingDeleteRuntimeTryReadTgaRgba32LikeOriginal(
            string path,
            out int width,
            out int height,
            out Color32[] pixels,
            out string audit)
        {
            width = 0;
            height = 0;
            pixels = null;
            audit = "unknown";

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                audit = "read_exception=" + ex.GetType().Name + ":" + ex.Message;
                return false;
            }

            if (bytes == null || bytes.Length < 18)
            {
                audit = "too_small";
                return false;
            }

            int idLength = bytes[0];
            int colorMapType = bytes[1];
            int imageType = bytes[2];
            width = bytes[12] | (bytes[13] << 8);
            height = bytes[14] | (bytes[15] << 8);
            int bpp = bytes[16];
            int descriptor = bytes[17];

            if (colorMapType != 0)
            {
                audit = "colormap_not_supported";
                return false;
            }

            if (imageType != 2 && imageType != 3)
            {
                audit = "imageType_not_supported type=" + imageType.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            if (width <= 0 || height <= 0 || (bpp != 24 && bpp != 32 && bpp != 8))
            {
                audit = "bad_header width=" + width.ToString(CultureInfo.InvariantCulture) +
                        " height=" + height.ToString(CultureInfo.InvariantCulture) +
                        " bpp=" + bpp.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            int bytesPerPixel = bpp / 8;
            int offset = 18 + idLength;
            long need = (long)offset + (long)width * (long)height * (long)bytesPerPixel;
            if (need > bytes.Length)
            {
                audit = "truncated need=" + need.ToString(CultureInfo.InvariantCulture) +
                        " have=" + bytes.Length.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            bool originTop = (descriptor & 0x20) != 0;
            pixels = new Color32[width * height];

            int p = offset;
            for (int fileY = 0; fileY < height; fileY++)
            {
                int unityY = originTop ? (height - 1 - fileY) : fileY;
                int row = unityY * width;
                for (int x = 0; x < width; x++)
                {
                    byte r;
                    byte g;
                    byte b;
                    byte a;
                    if (bpp == 8)
                    {
                        r = g = b = bytes[p++];
                        a = 255;
                    }
                    else
                    {
                        b = bytes[p++];
                        g = bytes[p++];
                        r = bytes[p++];
                        a = bpp == 32 ? bytes[p++] : (byte)255;
                    }
                    pixels[row + x] = new Color32(r, g, b, a);
                }
            }

            audit = "ok";
            return true;
        }

        private static Texture2D[][] C2BuildingDeleteRuntimeSliceAtlasRowsTopLeftLikeOriginal(
            Color32[] atlasPixels,
            int atlasWidth,
            int atlasHeight,
            int columns,
            int rows,
            string namePrefix)
        {
            if (atlasPixels == null || atlasWidth <= 0 || atlasHeight <= 0 || columns <= 0 || rows <= 0)
                return null;

            Texture2D[][] rowClips = new Texture2D[rows][];
            for (int rowTop = 0; rowTop < rows; rowTop++)
            {
                Texture2D[] clip = new Texture2D[columns];

                int yTop0 = Mathf.RoundToInt(rowTop * (atlasHeight / (float)rows));
                int yTop1 = Mathf.RoundToInt((rowTop + 1) * (atlasHeight / (float)rows));
                int tileH = Mathf.Max(1, yTop1 - yTop0);

                for (int col = 0; col < columns; col++)
                {
                    int x0 = Mathf.RoundToInt(col * (atlasWidth / (float)columns));
                    int x1 = Mathf.RoundToInt((col + 1) * (atlasWidth / (float)columns));
                    int tileW = Mathf.Max(1, x1 - x0);

                    Color32[] tile = new Color32[tileW * tileH];

                    // Convert atlas top-row order to Unity bottom-left texture data.
                    int srcY = atlasHeight - yTop1;

                    for (int y = 0; y < tileH; y++)
                    {
                        int sy = srcY + y;
                        if (sy < 0 || sy >= atlasHeight) continue;

                        int srcRow = sy * atlasWidth + x0;
                        int dstRow = y * tileW;
                        Array.Copy(atlasPixels, srcRow, tile, dstRow, tileW);
                    }

                    Texture2D tex = new Texture2D(tileW, tileH, TextureFormat.RGBA32, false);
                    tex.name = namePrefix + "_row" + rowTop.ToString(CultureInfo.InvariantCulture) + "_" + col.ToString(CultureInfo.InvariantCulture);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    tex.SetPixels32(tile);
                    tex.Apply(false, false);
                    clip[col] = tex;
                }
                rowClips[rowTop] = clip;
            }

            return rowClips;
        }

        private static Texture2D[][] C2BuildingDeleteRuntimeSelectC2MFrameAnimBuildingFireClipsLikeOriginal(Texture2D[][] rowClips)
        {
            if (rowClips == null || rowClips.Length == 0) return null;

            // V10: use fire_anim1.tga as one full 11x7 frame animation.
            // Each atlas cell is normalized once to the same canvas, bottom-center anchored.
            // Runtime only swaps textures on the same fixed quad: no particles, no scale pulse, no recenter.
            List<Texture2D> full = new List<Texture2D>();

            for (int r = 0; r < rowClips.Length; r++)
            {
                Texture2D[] row = rowClips[r];
                if (row == null || row.Length == 0) continue;

                for (int c = 0; c < row.Length; c++)
                {
                    Texture2D normalized = C2BuildingDeleteRuntimeNormalizeFireAtlasFrameLikeOriginal(
                        row[c],
                        "C2_Delete_FireAnim1_FULL11x7_r" + r.ToString(CultureInfo.InvariantCulture) + "_c" + c.ToString(CultureInfo.InvariantCulture));
                    if (normalized != null)
                        full.Add(normalized);
                }
            }

            if (full.Count >= 4)
                return new Texture2D[][] { full.ToArray() };

            return null;
        }

        private static Texture2D C2BuildingDeleteRuntimeNormalizeFireAtlasFrameLikeOriginal(Texture2D src, string name)
        {
            if (src == null) return null;

            Color32[] srcPixels = src.GetPixels32();
            int sw = src.width;
            int sh = src.height;
            if (srcPixels == null || srcPixels.Length == 0 || sw <= 0 || sh <= 0) return null;

            int minX = sw;
            int minY = sh;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < sh; y++)
            {
                int row = y * sw;
                for (int x = 0; x < sw; x++)
                {
                    Color32 c = srcPixels[row + x];
                    if (c.a <= 3) continue;
                    if (c.r <= 8 && c.g <= 8 && c.b <= 8) continue;

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
                return null;

            int cropW = maxX - minX + 1;
            int cropH = maxY - minY + 1;

            const int canvasW = 160;
            const int canvasH = 192;
            const int bottomPad = 4;

            Color32[] dst = new Color32[canvasW * canvasH];
            for (int i = 0; i < dst.Length; i++)
                dst[i] = new Color32(0, 0, 0, 0);

            int copyW = Mathf.Min(cropW, canvasW);
            int copyH = Mathf.Min(cropH, canvasH - bottomPad);
            int dstX0 = (canvasW - copyW) / 2;
            int dstY0 = bottomPad;

            // Source texture data is bottom-left based. dstY0 keeps the flame base fixed.
            for (int y = 0; y < copyH; y++)
            {
                int sy = minY + y;
                int dy = dstY0 + y;
                if (sy < 0 || sy >= sh || dy < 0 || dy >= canvasH) continue;

                int srcRow = sy * sw;
                int dstRow = dy * canvasW;

                for (int x = 0; x < copyW; x++)
                {
                    int sx = minX + x;
                    int dx = dstX0 + x;
                    if (sx < 0 || sx >= sw || dx < 0 || dx >= canvasW) continue;

                    Color32 c = srcPixels[srcRow + sx];

                    int max = Math.Max(c.r, Math.Max(c.g, c.b));
                    int brightness = (c.r * 54 + c.g * 183 + c.b * 19) >> 8;
                    if (c.a <= 3 || max <= 20 || brightness <= 18)
                        c.a = 0;

                    dst[dstRow + dx] = c;
                }
            }

            Texture2D tex = new Texture2D(canvasW, canvasH, TextureFormat.RGBA32, false);
            tex.name = name ?? "C2_Delete_FireAnim1_FULL11x7_BottomCentered";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels32(dst);
            tex.Apply(false, false);
            return tex;
        }

        private static Texture2D[] C2BuildingDeleteRuntimeCreateProceduralFramesLikeOriginal(bool fire, int count)
        {
            Texture2D[] frames = new Texture2D[Mathf.Max(1, count)];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = C2BuildingDeleteRuntimeCreateProceduralEffectTextureLikeOriginal(fire, i, frames.Length);
            return frames;
        }

        private static Texture2D C2BuildingDeleteRuntimeCreateProceduralEffectTextureLikeOriginal(bool fire, int frame, int count)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.name = "C2_Delete_" + (fire ? "Fire" : "Smoke") + "_Proc_" + frame.ToString(CultureInfo.InvariantCulture);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            float t = count > 1 ? frame / (float)(count - 1) : 0.0f;
            float cx = 31.5f + Mathf.Sin(t * Mathf.PI * 2.0f) * (fire ? 3.5f : 2.0f);
            float cy = fire ? 22.0f + Mathf.Cos(t * Mathf.PI * 2.0f) * 3.0f : 30.0f + t * 6.0f;
            float rx = fire ? 15.0f + Mathf.Sin(t * Mathf.PI * 2.0f) * 2.0f : 22.0f + t * 5.0f;
            float ry = fire ? 27.0f + Mathf.Cos(t * Mathf.PI * 2.0f) * 3.0f : 18.0f + t * 7.0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - cx) / Mathf.Max(1.0f, rx);
                    float dy = (y - cy) / Mathf.Max(1.0f, ry);
                    float d = dx * dx + dy * dy;
                    float a = Mathf.Clamp01(1.0f - d);
                    a = a * a;

                    Color c;
                    if (fire)
                    {
                        float hot = Mathf.Clamp01(1.0f - Mathf.Abs(x - cx) / Mathf.Max(1.0f, rx * 0.45f));
                        c = Color.Lerp(new Color(1.0f, 0.18f, 0.02f, a), new Color(1.0f, 0.82f, 0.10f, a), hot * Mathf.Clamp01(1.1f - dy));
                    }
                    else
                    {
                        // V15: C2M-like smoke puff texture, not raw smoke_01 paper billboard.
                        // Several soft lobes + deterministic noise give a cloudy edge; ramps are handled by the animator.
                        float n1 = Mathf.PerlinNoise(x * 0.075f + frame * 0.173f, y * 0.075f + 3.17f);
                        float n2 = Mathf.PerlinNoise(x * 0.145f + 11.0f, y * 0.145f + frame * 0.091f);
                        float lobeA = Mathf.Clamp01(1.0f - ((x - (cx - 6.0f)) * (x - (cx - 6.0f)) / (rx * rx * 0.90f) + (y - (cy + 1.0f)) * (y - (cy + 1.0f)) / (ry * ry * 0.72f)));
                        float lobeB = Mathf.Clamp01(1.0f - ((x - (cx + 7.0f)) * (x - (cx + 7.0f)) / (rx * rx * 0.75f) + (y - (cy - 3.0f)) * (y - (cy - 3.0f)) / (ry * ry * 0.92f)));
                        float lobeC = Mathf.Clamp01(1.0f - ((x - cx) * (x - cx) / (rx * rx * 1.18f) + (y - (cy + 7.0f)) * (y - (cy + 7.0f)) / (ry * ry * 0.65f)));
                        float cloud = Mathf.Clamp01((lobeA * 0.52f + lobeB * 0.38f + lobeC * 0.44f) * (0.62f + 0.48f * n1) - (1.0f - n2) * 0.12f);
                        float soft = Mathf.Pow(cloud, 1.28f);
                        float edge = Mathf.Clamp01(a * 1.35f);
                        soft *= edge;
                        float gray = 0.46f + 0.38f * soft + 0.10f * n1;
                        c = new Color(gray, gray, gray * 0.98f, soft * 0.58f);
                    }
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply(false, false);
            return tex;
        }
    }

    public sealed class C2BuildingDeleteRuntimeLikeOriginal : MonoBehaviour
    {
        private static bool s_installed;
        private float _lastDeleteAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallLikeOriginal()
        {
            if (s_installed) return;
            s_installed = true;

            GameObject root = new GameObject("C2_BuildingDeleteRuntimeLikeOriginal");
            DontDestroyOnLoad(root);
            root.AddComponent<C2BuildingDeleteRuntimeLikeOriginal>();
        }

        private void Update()
        {
            if (!DeletePressedLikeOriginal()) return;
            if (Time.unscaledTime - _lastDeleteAt < 0.05f) return;
            _lastDeleteAt = Time.unscaledTime;

            ExecuteDeleteSelectedBuildingsLikeOriginal();
        }

        private static bool DeletePressedLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.deleteKey.wasPressedThisFrame)
                return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Delete))
                return true;
#endif

            return false;
        }

        private static void ExecuteDeleteSelectedBuildingsLikeOriginal()
        {
            C2SettlementBuildingSelectableV1LikeOriginal[] all =
                UnityEngine.Object.FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();

            if (all == null || all.Length == 0)
            {
                Debug.Log("[C2:BUILD DELETE V266D KEY] selected=0 candidates=0 reason='no_building_selectables'");
                return;
            }

            int selectedCount = 0;
            for (int si = 0; si < all.Length; si++)
                if (all[si] != null && all[si].IsSelected && !all[si].NotSelectable)
                    selectedCount++;

            Debug.Log("[C2:BUILD DELETE V266D KEY] selected=" + selectedCount.ToString(CultureInfo.InvariantCulture) +
                      " candidates=" + all.Length.ToString(CultureInfo.InvariantCulture));

            int deleted = 0;
            for (int i = 0; i < all.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = all[i];
                if (b == null) continue;
                if (!b.IsSelected) continue;
                if (b.NotSelectable) continue;

                if (TryDeleteBuildingLikeOriginal(b))
                    deleted++;
            }

            if (deleted > 0)
            {
                Debug.Log("[C2:BUILD DELETE V266D] deleted=" + deleted.ToString(CultureInfo.InvariantCulture) +
                          " contract=" + C2BattleTerrainMode.C2BuildingDeleteRuntimeContractLikeOriginal);
            }
            else
            {
                Debug.Log("[C2:BUILD DELETE V266D] deleted=0 reason='none_selected_or_blocked'");
            }
        }

        internal static bool C2BuildingDeleteRuntimeApplyCombatDamageLikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal source,
            int damage,
            out bool killed)
        {
            killed = false;
            if (source == null || damage <= 0) return false;

            C2RuntimeConstructionSiteProxyLikeOriginal proxy = source.GetComponentInParent<C2RuntimeConstructionSiteProxyLikeOriginal>();
            C2BattleTerrainMode mode = source.OwnerMode;
            if (mode == null && proxy != null) mode = proxy.OwnerMode;
            if (mode == null) mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            if (mode == null) return false;

            string mdName = ResolveMdNameLikeOriginal(source, proxy);
            bool immortal;
            bool slowDeath;
            int fireCount;
            int smokeCount;
            bool hasDestruct;
            string flagsAudit;
            mode.C2BuildingDeleteRuntimeTryGetMdFlagsLikeOriginal(
                mdName,
                out immortal,
                out slowDeath,
                out fireCount,
                out smokeCount,
                out hasDestruct,
                out flagsAudit);

            // Nation.cpp::OneObject::MakeDamage returns before changing Life for IMMORTAL.
            if (immortal)
            {
                Debug.Log("[C2:BUILD COMBAT V380 IMMORTAL] damage=" + damage.ToString(CultureInfo.InvariantCulture) + " " + flagsAudit);
                return true;
            }

            int lifeMax = mode.C2BuildingDeleteRuntimeLifeMaxLikeOriginal(mdName);
            if (source.LifeMaxLikeOriginal > 1) lifeMax = source.LifeMaxLikeOriginal;
            if (proxy != null && proxy.LifeMax > 1) lifeMax = proxy.LifeMax;
            lifeMax = Mathf.Max(1, lifeMax);

            int currentLife = lifeMax;
            if (source.LifeLikeOriginal > 1) currentLife = source.LifeLikeOriginal;
            if (proxy != null && proxy.Life > 1) currentLife = proxy.Life;
            int nextLife = Mathf.Max(0, currentLife - Mathf.Max(1, damage));

            source.LifeMaxLikeOriginal = lifeMax;
            source.LifeLikeOriginal = nextLife;
            if (proxy != null)
            {
                proxy.LifeMax = lifeMax;
                proxy.Life = nextLife;
            }

            if (nextLife > 0)
            {
                if (source.ReadyLikeOriginal)
                {
                    Transform fxRoot = ResolveLiveBuildingFxRootLikeOriginal(source, proxy, source.gameObject, mdName, out string _);
                    if (fxRoot != null)
                    {
                        var effects = fxRoot.GetComponent<C2BuildingDamageFireSmokeLikeOriginal>();
                        if (effects == null) effects = fxRoot.gameObject.AddComponent<C2BuildingDamageFireSmokeLikeOriginal>();
                        effects.Configure(mode, source, mdName);
                    }
                }
                Debug.Log("[C2:BUILD COMBAT V380 DAMAGE] md='" + (mdName ?? string.Empty) +
                          "' hp=" + nextLife.ToString(CultureInfo.InvariantCulture) +
                          "/" + lifeMax.ToString(CultureInfo.InvariantCulture) +
                          " damage=" + damage.ToString(CultureInfo.InvariantCulture));
                return true;
            }

            // A lethal MakeDamage call goes straight to OneObject::Die. SLOWDEATH is only
            // entered by DestructBuilding (the Delete/editor command), so combat must bypass it.
            killed = TryDeleteBuildingLikeOriginal(source, false, "combat_lethal_die");
            return true;
        }

        private static bool TryDeleteBuildingLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal source)
        {
            return TryDeleteBuildingLikeOriginal(source, true, "manual_delete");
        }

        private static bool TryDeleteBuildingLikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal source,
            bool allowSlowDeath,
            string reason)
        {
            if (source == null) return false;

            C2RuntimeConstructionSiteProxyLikeOriginal proxy = source.GetComponentInParent<C2RuntimeConstructionSiteProxyLikeOriginal>();
            C2BattleTerrainMode mode = source.OwnerMode;
            if (mode == null && proxy != null) mode = proxy.OwnerMode;
            if (mode == null) mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            if (mode == null) return false;

            string mdName = ResolveMdNameLikeOriginal(source, proxy);
            bool immortal;
            bool slowDeath;
            int fireCount;
            int smokeCount;
            bool hasDestruct;
            string flagsAudit;
            mode.C2BuildingDeleteRuntimeTryGetMdFlagsLikeOriginal(mdName, out immortal, out slowDeath, out fireCount, out smokeCount, out hasDestruct, out flagsAudit);

            if (immortal)
            {
                Debug.Log("[C2:BUILD DELETE V266D BLOCKED] reason=IMMORTAL " + flagsAudit);
                return false;
            }

            GameObject oldRoot = proxy != null ? proxy.gameObject : source.gameObject;
            string liveFxRootAudit;
            Transform visualRoot = ResolveLiveBuildingFxRootLikeOriginal(source, proxy, oldRoot, mdName, out liveFxRootAudit);
            if (visualRoot != null)
            {
                var damageEffects = visualRoot.GetComponent<C2BuildingDamageFireSmokeLikeOriginal>();
                if (damageEffects != null) damageEffects.StopForDeath();
            }
            mode.C2BuildingDeleteRuntimeCleanupBadFxArtifactsLikeOriginal(visualRoot);
            Transform deathParent = oldRoot != null && oldRoot.transform.parent != null ? oldRoot.transform.parent : mode.transform;
            int recordIndex = source.RecordIndex;
            int realX = proxy != null ? proxy.RealX : source.RealX;
            int realY = proxy != null ? proxy.RealY : source.RealY;
            bool underConstruction = proxy != null && !proxy.Ready;
            int currentLifeLikeOriginal = mode.C2BuildingDeleteRuntimeLifeMaxLikeOriginal(mdName);
            if (source.LifeLikeOriginal > 1) currentLifeLikeOriginal = source.LifeLikeOriginal;
            if (proxy != null && proxy.Life > 1) currentLifeLikeOriginal = proxy.Life;
            int effectSortingOrderLikeOriginal = mode.C2BuildingDeleteRuntimeResolveFxSortingOrderLikeOriginal(
                visualRoot,
                source != null ? source.SortKey : 10000);

            if (allowSlowDeath && slowDeath && !underConstruction)
            {
                const int minDeathLikeOriginal = 2000;
                // Original DestructBuilding on SLOWDEATH does not switch LoLayer immediately.
                // Keep the live building selectable/visible so the selected-building HUD can still show HP while fire/smoke runs.
                if (oldRoot == null)
                    return false;

                C2BuildingDeleteDyingRuntimeLikeOriginal alreadyDying = oldRoot.GetComponent<C2BuildingDeleteDyingRuntimeLikeOriginal>();
                if (alreadyDying != null)
                {
                    Debug.Log("[C2:BUILD DELETE V271 SLOWDEATH IGNORE] md='" + (mdName ?? string.Empty) +
                              "' hp=" + alreadyDying.LifeLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                              "/" + alreadyDying.LifeMaxLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                              " reason='already_burning_original_DestructBuilding_return'");
                    return false;
                }

                if (currentLifeLikeOriginal < minDeathLikeOriginal)
                {
                    Debug.Log("[C2:BUILD DELETE V271 SLOWDEATH IGNORE] md='" + (mdName ?? string.Empty) +
                              "' hp=" + currentLifeLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                              " minDeath=" + minDeathLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                              " reason='life_below_MinDeath_original_DestructBuilding_return'");
                    return false;
                }

                CancelBuilderOrdersForSiteLikeOriginal(oldRoot);

                int removedPassability = 0;

                C2BuildingDeleteDyingRuntimeLikeOriginal dying = oldRoot.AddComponent<C2BuildingDeleteDyingRuntimeLikeOriginal>();
                dying.Configure(mode, source, proxy, deathParent, mdName, removedPassability, visualRoot, effectSortingOrderLikeOriginal);

                Debug.Log("[C2:BUILD DELETE V271 SLOWDEATH] md='" + (mdName ?? string.Empty) + "' " + flagsAudit +
                          " hp=" + dying.LifeLikeOriginal.ToString(CultureInfo.InvariantCulture) + "/" + dying.LifeMaxLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " fxRoot='" + liveFxRootAudit + "'" +
                          " fxSort=" + effectSortingOrderLikeOriginal.ToString(CultureInfo.InvariantCulture));
                return true;
            }

            PrepareObjectForDeletionLikeOriginal(source, proxy, oldRoot);
            CancelBuilderOrdersForSiteLikeOriginal(oldRoot);

            int removedPassabilityDirect = mode.C2BuildingDeleteRuntimeRemovePassabilityLikeOriginal(
                recordIndex,
                mdName,
                realX,
                realY,
                underConstruction ? "deathlie2_under_construction" : "deathlie1_ready");

            FinishDeathLikeOriginal(
                mode,
                source,
                proxy,
                deathParent,
                mdName,
                underConstruction ? 2 : 1,
                oldRoot,
                removedPassabilityDirect,
                string.IsNullOrEmpty(reason) ? "direct_delete" : reason);
            return true;
        }

        internal static void FinishDeathLikeOriginal(
            C2BattleTerrainMode mode,
            C2SettlementBuildingSelectableV1LikeOriginal source,
            C2RuntimeConstructionSiteProxyLikeOriginal proxy,
            Transform deathParent,
            string mdName,
            int deathLieIndex,
            GameObject oldRoot,
            int removedPassability,
            string reason)
        {
            if (mode == null) return;
            if (deathParent == null) deathParent = mode.transform;

            string audit;
            GameObject deathVisual = mode.C2BuildingDeleteRuntimeCreateDeathLieVisualLikeOriginal(deathParent, source, proxy, deathLieIndex, out audit);

            if (deathVisual != null)
            {
                int sort = mode.C2BuildingDeleteRuntimeResolveFxSortingOrderLikeOriginal(
                    deathVisual.transform,
                    source != null ? source.SortKey : 10000);
                // Original drawing path: ShowFires() is called only while !OB->Sdoxlo.
                // Once DeathLie is active, IFire is erased and fire/smoke is NOT reattached to ruins.
                C2BuildingDeleteDeathLieDecayLikeOriginal decay = deathVisual.GetComponent<C2BuildingDeleteDeathLieDecayLikeOriginal>();
                if (decay == null) decay = deathVisual.AddComponent<C2BuildingDeleteDeathLieDecayLikeOriginal>();
                decay.Configure(18.0f, 2.2f);

                mode.C2BuildingDeleteRuntimeSpawnDestructLikeOriginal(deathVisual.transform, mdName, sort, reason);
            }

            if (oldRoot != null)
                Destroy(oldRoot);

            Debug.Log("[C2:BUILD DELETE V266D FINISH] md='" + (mdName ?? string.Empty) +
                      "' deathlie=" + deathLieIndex.ToString(CultureInfo.InvariantCulture) +
                      " visual=" + (deathVisual != null) +
                      " removedPassability=" + removedPassability.ToString(CultureInfo.InvariantCulture) +
                      " audit='" + audit + "'");
        }

        private static Transform ResolveLiveBuildingFxRootLikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal source,
            C2RuntimeConstructionSiteProxyLikeOriginal proxy,
            GameObject oldRoot,
            string mdName,
            out string audit)
        {
            Transform fallback = source != null
                ? source.transform
                : (oldRoot != null ? oldRoot.transform : null);
            audit = fallback != null ? "fallback:" + fallback.name : "none";
            if (oldRoot == null)
                return fallback;

            C2BuildingRuntimeInfoV247LikeOriginal[] infos =
                oldRoot.GetComponentsInChildren<C2BuildingRuntimeInfoV247LikeOriginal>(true);
            if (infos == null || infos.Length == 0)
                return fallback;

            C2BuildingRuntimeInfoV247LikeOriginal best = null;
            int bestScore = int.MinValue;
            string wantedMd = (mdName ?? string.Empty).Trim();
            int wantedRecord = source != null ? source.RecordIndex : -1;

            for (int i = 0; i < infos.Length; i++)
            {
                C2BuildingRuntimeInfoV247LikeOriginal info = infos[i];
                if (info == null || info.transform == null)
                    continue;

                MeshRenderer[] renderers = info.GetComponentsInChildren<MeshRenderer>(true);
                if (renderers == null || renderers.Length == 0)
                    continue;

                int score = 0;
                if (wantedRecord >= 0 && info.RecordIndex == wantedRecord)
                    score += 1000;
                if (!string.IsNullOrWhiteSpace(wantedMd) &&
                    (string.Equals(info.MdName, wantedMd, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(info.SourceMonsterId, wantedMd, StringComparison.OrdinalIgnoreCase)))
                    score += 500;
                if (source != null && info.transform != source.transform)
                    score += 120;
                if ((info.gameObject.name ?? string.Empty).IndexOf("C2_Building_", StringComparison.OrdinalIgnoreCase) >= 0)
                    score += 80;
                if (!info.NotSelectable)
                    score += 20;
                score += Mathf.Clamp(renderers.Length, 1, 16);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = info;
                }
            }

            if (best == null)
                return fallback;

            audit =
                "runtimeVisual:" + best.gameObject.name +
                " record=" + best.RecordIndex.ToString(CultureInfo.InvariantCulture) +
                " md='" + (best.MdName ?? string.Empty) + "'" +
                " score=" + bestScore.ToString(CultureInfo.InvariantCulture);
            return best.transform;
        }

        private static void PrepareObjectForDeletionLikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal source,
            C2RuntimeConstructionSiteProxyLikeOriginal proxy,
            GameObject oldRoot)
        {
            if (source != null)
            {
                source.ClearRallyPointV155LikeOriginal();
                source.SetSelected(false);
                                source.NotSelectable = true;
                source.enabled = false;
            }

            if (proxy != null)
                proxy.NotSelectable = true;

            if (oldRoot != null)
            {
                C2SettlementBuildingSelectableV1LikeOriginal[] selectables = oldRoot.GetComponentsInChildren<C2SettlementBuildingSelectableV1LikeOriginal>(true);
                for (int i = 0; i < selectables.Length; i++)
                {
                    if (selectables[i] == null) continue;
                    selectables[i].SetSelected(false);
                                        selectables[i].NotSelectable = true;
                    selectables[i].enabled = false;
                }

                C2BuildingProductionCardsRuntimeV114[] cards = oldRoot.GetComponentsInChildren<C2BuildingProductionCardsRuntimeV114>(true);
                for (int i = 0; i < cards.Length; i++)
                    if (cards[i] != null) Destroy(cards[i]);
            }
        }

        private static void CancelBuilderOrdersForSiteLikeOriginal(GameObject buildingRoot)
        {
            var site = buildingRoot != null ? buildingRoot.GetComponent<C2RuntimeConstructionSitePseudo3DV245LikeOriginal>() : null;
            if (site == null) return;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] units =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            if (units == null) return;

            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] == null) continue;
                var build = units[i].GetComponent<C2BuildWorkerOrderV245LikeOriginal>();
                if (build != null) build.CancelForSiteLikeOriginal(site);
            }
        }

        private static string ResolveMdNameLikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal source,
            C2RuntimeConstructionSiteProxyLikeOriginal proxy)
        {
            if (proxy != null && !string.IsNullOrEmpty(proxy.MdName)) return proxy.MdName;
            if (source != null && !string.IsNullOrEmpty(source.SourceMonsterId)) return source.SourceMonsterId;
            if (proxy != null && !string.IsNullOrEmpty(proxy.UnitId)) return proxy.UnitId;
            return string.Empty;
        }
    }

    // MiniMap4X::ShowFiresNearBuilding applies to any damaged READY building,
    // not just a building executing the manual SLOWDEATH command.
    public sealed class C2BuildingDamageFireSmokeLikeOriginal : MonoBehaviour
    {
        private C2BattleTerrainMode _mode;
        private C2SettlementBuildingSelectableV1LikeOriginal _source;
        private string _mdName;
        private Transform _emitters;
        private int _fires, _smoke, _lastLife = -1, _lastMax = -1;
        private float _nextCheck;

        public void Configure(C2BattleTerrainMode mode, C2SettlementBuildingSelectableV1LikeOriginal source, string mdName)
        {
            _mode = mode; _source = source; _mdName = mdName;
            enabled = true;
            Refresh();
        }

        private void Update()
        {
            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + 0.2f;
            Refresh();
        }

        private void ClearEmitters()
        {
            if (_emitters != null) { _emitters.gameObject.SetActive(false); Destroy(_emitters.gameObject); }
            _emitters = null; _fires = 0; _smoke = 0;
        }

        public void StopForDeath() { ClearEmitters(); enabled = false; }
        private void OnDestroy() { ClearEmitters(); }

        private void Refresh()
        {
            if (_source == null || _mode == null) { StopForDeath(); return; }
            int life = _source.LifeLikeOriginal, max = _source.LifeMaxLikeOriginal;
            if (!_source.ReadyLikeOriginal || _source.NotSelectable || life <= 0 || max <= 0 || life >= max)
            {
                ClearEmitters(); _lastLife = life; _lastMax = max; return;
            }
            if (_lastLife == life && _lastMax == max) return;
            // Repair can remove emitters. Rebuild only when HP increases/max HP changes.
            if (life > _lastLife && _lastLife >= 0 || max != _lastMax) ClearEmitters();
            if (_emitters == null)
            {
                _emitters = new GameObject("C2_DamageFireSmoke").transform;
                _emitters.SetParent(transform, false);
            }
            int sort = _mode.C2BuildingDeleteRuntimeResolveFxSortingOrderLikeOriginal(transform, _source.SortKey);
            _mode.C2BuildingDeleteRuntimeAttachFireSmokeForLifeLikeOriginal(_emitters, _mdName, sort,
                life, max, ref _fires, ref _smoke, "combat_damage");
            _lastLife = life; _lastMax = max;
        }
    }

    public sealed class C2BuildingDeleteDyingRuntimeLikeOriginal : MonoBehaviour
    {
        private const int SlowDeathStartLifeLikeOriginal = 1999;
        private const int SlowDeathFinishLifeLikeOriginal = 10;
        private const int SlowDeathDecLikeOriginal = 5;
        private const float SlowDeathSecondsLikeOriginal = 20.0f;

        private C2BattleTerrainMode _mode;
        private C2SettlementBuildingSelectableV1LikeOriginal _source;
        private C2RuntimeConstructionSiteProxyLikeOriginal _proxy;
        private Transform _deathParent;
        private Transform _visualRoot;
        private string _mdName;
        private int _removedPassability;
        private int _sortingOrder;
        private int _createdFirePoints;
        private int _createdSmokePoints;
        private float _finishAt;
        private float _startAt;
        private bool _finished;
        private float _nextHpLogAt;

        public int LifeLikeOriginal { get; private set; }
        public int LifeMaxLikeOriginal { get; private set; }
        public bool FinishedLikeOriginal { get { return _finished; } }

        public void Configure(
            C2BattleTerrainMode mode,
            C2SettlementBuildingSelectableV1LikeOriginal source,
            C2RuntimeConstructionSiteProxyLikeOriginal proxy,
            Transform deathParent,
            string mdName,
            int removedPassability,
            Transform visualRoot,
            int sortingOrder)
        {
            _mode = mode;
            _source = source;
            _proxy = proxy;
            _deathParent = deathParent;
            _visualRoot = visualRoot;
            _mdName = mdName ?? string.Empty;
            _removedPassability = removedPassability;
            _sortingOrder = sortingOrder;
            _createdFirePoints = 0;
            _createdSmokePoints = 0;
            _startAt = Time.time;
            _finishAt = Time.time + SlowDeathSecondsLikeOriginal;
            LifeMaxLikeOriginal = mode != null ? mode.C2BuildingDeleteRuntimeLifeMaxLikeOriginal(_mdName) : 2000;
            LifeLikeOriginal = Mathf.Min(SlowDeathStartLifeLikeOriginal, Mathf.Max(1, LifeMaxLikeOriginal - 1));
            _finished = false;
            _nextHpLogAt = Time.time;
            SyncLifeMetadataLikeOriginal();
            SyncFireSmokeLikeOriginal("slowdeath_start");
        }

        private void SyncLifeMetadataLikeOriginal()
        {
            if (_source != null)
            {
                _source.LifeMaxLikeOriginal = Mathf.Max(1, LifeMaxLikeOriginal);
                _source.LifeLikeOriginal = Mathf.Clamp(LifeLikeOriginal, 0, Mathf.Max(1, LifeMaxLikeOriginal));
            }

            if (_proxy != null)
            {
                _proxy.LifeMax = Mathf.Max(1, LifeMaxLikeOriginal);
                _proxy.Life = Mathf.Clamp(LifeLikeOriginal, 0, Mathf.Max(1, LifeMaxLikeOriginal));
            }
        }

        private void SyncFireSmokeLikeOriginal(string reason)
        {
            if (_mode == null || _visualRoot == null) return;

            _mode.C2BuildingDeleteRuntimeAttachFireSmokeForLifeLikeOriginal(
                _visualRoot,
                _mdName,
                _sortingOrder,
                LifeLikeOriginal,
                LifeMaxLikeOriginal,
                ref _createdFirePoints,
                ref _createdSmokePoints,
                reason);
        }

        private void Update()
        {
            if (_finished) return;

            float duration = Mathf.Max(0.01f, _finishAt - _startAt);
            float age = Mathf.Clamp(Time.time - _startAt, 0.0f, duration);
            int totalSteps = Mathf.Max(1, Mathf.CeilToInt((SlowDeathStartLifeLikeOriginal - SlowDeathFinishLifeLikeOriginal) / (float)SlowDeathDecLikeOriginal));
            float tickSeconds = duration / totalSteps;
            int elapsedSteps = Mathf.Clamp(Mathf.FloorToInt(age / Mathf.Max(0.0001f, tickSeconds)), 0, totalSteps);
            LifeLikeOriginal = Mathf.Max(0, SlowDeathStartLifeLikeOriginal - elapsedSteps * SlowDeathDecLikeOriginal);
            float remain01 = Mathf.Clamp01(LifeLikeOriginal / (float)SlowDeathStartLifeLikeOriginal);
            SyncLifeMetadataLikeOriginal();
            SyncFireSmokeLikeOriginal("slowdeath_hp");

            if (Time.time >= _nextHpLogAt)
            {
                _nextHpLogAt = Time.time + 2.0f;
                Debug.Log("[C2:BUILD DELETE V266D HP] md='" + (_mdName ?? string.Empty) +
                          "' hp=" + LifeLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          "/" + LifeMaxLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                          " remain01=" + remain01.ToString("0.###", CultureInfo.InvariantCulture));
            }

            if (Time.time < _finishAt && LifeLikeOriginal > SlowDeathFinishLifeLikeOriginal) return;

            _finished = true;
            LifeLikeOriginal = 0;
            SyncLifeMetadataLikeOriginal();
            C2BuildingDeleteRuntimeLikeOriginal.FinishDeathLikeOriginal(
                _mode,
                _source,
                _proxy,
                _deathParent,
                _mdName,
                1,
                gameObject,
                _removedPassability,
                "slowdeath_finish");
        }
    }

    public sealed class C2BuildingDeleteAutoDestroyLikeOriginal : MonoBehaviour
    {
        public float Delay = 1.0f;
        private float _startTime;

        private void OnEnable()
        {
            _startTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - _startTime >= Delay)
                Destroy(gameObject);
        }
    }

    public sealed class C2BuildingDeleteEffectAnimatorLikeOriginal : MonoBehaviour
    {
        public MeshRenderer Renderer;
        public Mesh OwnedMesh;
        public Texture2D[] Frames;
        public float Fps = 10.0f;
        public bool Loop = true;
        public Vector3 FloatPerSecond;
        public Vector3 AccelerationPerSecondSquared;
        public float InitialDelay;
        public float AutoDestroyAfter = -1.0f;
        public bool RestartMotionEachLoop;
        public bool FadeOutPerLoop;
        public Color BaseColor = Color.white;
        public float ScalePulse;
        public float AlphaPulse;
        public float PulseSpeed;
        public float PulsePhase;
        public int FrameOffset;
        public float FadeInSeconds = 0.0f;
        public float GrowInStartScale = 1.0f;
        public bool FaceMainCamera;
        public Camera FacingCamera;

        private int _lastFrame = -1;
        private Vector3 _startLocal;
        private Vector3 _baseScale;
        private float _startTime;
        private Material _runtimeMaterial;

        private void OnEnable()
        {
            _startLocal = transform.localPosition;
            _baseScale = transform.localScale;
            _startTime = Time.time;
            if (Renderer != null)
            {
                _runtimeMaterial = Renderer.sharedMaterial;
                if (_runtimeMaterial != null) _runtimeMaterial.color = BaseColor;
                Renderer.enabled = false;
            }
        }

        private void OnDestroy()
        {
            // Factories give each particle its own material/mesh. Frame textures are cached/shared.
            Material owned = _runtimeMaterial != null ? _runtimeMaterial : (Renderer != null ? Renderer.sharedMaterial : null);
            if (owned != null) Destroy(owned);
            if (OwnedMesh != null) Destroy(OwnedMesh);
        }

        private void Start()
        {
            // V12: properties are assigned after AddComponent(), so Renderer can be null in OnEnable().
            // Force the initial hidden state once assignments are complete.
            if (Renderer != null)
            {
                if (_runtimeMaterial == null) _runtimeMaterial = Renderer.sharedMaterial;
                if (_runtimeMaterial != null)
                {
                    Color c = BaseColor;
                    c.a = 0.0f;
                    _runtimeMaterial.color = c;
                }
                Renderer.enabled = false;
            }
        }

        private void Update()
        {
            if (Frames == null || Frames.Length == 0 || Renderer == null) return;

            float rawAge = Time.time - _startTime;
            if (rawAge < InitialDelay)
            {
                if (Renderer.enabled) Renderer.enabled = false;
                return;
            }

            if (!Renderer.enabled) Renderer.enabled = true;

            float age = rawAge - InitialDelay;
            if (AutoDestroyAfter > 0.0f && age >= AutoDestroyAfter)
            {
                Destroy(gameObject);
                return;
            }

            float fps = Mathf.Max(1.0f, Fps);
            int frame = Mathf.FloorToInt(age * fps) + FrameOffset;
            float cycleDuration = Frames.Length / fps;
            float cycleAge = cycleDuration > 0.0001f ? Mathf.Repeat(age, cycleDuration) : 0.0f;
            float cycle01 = cycleDuration > 0.0001f ? Mathf.Clamp01(cycleAge / cycleDuration) : 0.0f;

            if (Loop) frame %= Frames.Length;
            else frame = Mathf.Min(frame, Frames.Length - 1);

            Material mat = _runtimeMaterial != null ? _runtimeMaterial : Renderer.sharedMaterial;
            if (frame != _lastFrame)
            {
                _lastFrame = frame;
                if (mat != null)
                    mat.mainTexture = Frames[frame];
            }

            float moveTime = RestartMotionEachLoop ? cycle01 : age;
            transform.localPosition =
                _startLocal +
                FloatPerSecond * moveTime +
                AccelerationPerSecondSquared * (0.5f * moveTime * moveTime);

            if (FaceMainCamera)
            {
                Camera cam = FacingCamera != null ? FacingCamera : Camera.main;
                if (cam != null)
                    transform.rotation = Quaternion.LookRotation(-cam.transform.forward, cam.transform.up);
            }

            float pulse01 = PulseSpeed > 0.0001f ? (0.5f + 0.5f * Mathf.Sin(age * PulseSpeed + PulsePhase)) : 0.5f;
            float fadeIn01 = FadeInSeconds > 0.0001f ? Mathf.Clamp01(age / FadeInSeconds) : 1.0f;

            float scale = 1.0f;
            if (GrowInStartScale < 0.999f)
                scale *= Mathf.Lerp(Mathf.Max(0.05f, GrowInStartScale), 1.0f, fadeIn01);
            if (ScalePulse > 0.0001f)
                scale *= 1.0f + (pulse01 - 0.5f) * 2.0f * ScalePulse;
            transform.localScale = _baseScale * scale;

            if (mat != null)
            {
                Color c = BaseColor;
                c.a *= fadeIn01;
                if (AlphaPulse > 0.0001f)
                    c.a *= Mathf.Clamp01(1.0f + (pulse01 - 0.5f) * 2.0f * AlphaPulse);
                if (FadeOutPerLoop)
                    c.a *= Mathf.Clamp01(1.0f - cycle01);
                if (AutoDestroyAfter > 0.0f)
                    c.a *= Mathf.Clamp01((AutoDestroyAfter - age) / 0.35f);
                mat.color = c;
            }
        }
    }

    public sealed class C2BuildingDeleteDeathLieDecayLikeOriginal : MonoBehaviour
    {
        private float _startTime;
        private float _lifeSeconds = 5.8f;
        private float _fadeSeconds = 0.65f;
        private Renderer[] _renderers;
        private readonly List<Material> _materials = new List<Material>();

        public void Configure(float lifeSeconds, float fadeSeconds)
        {
            _lifeSeconds = Mathf.Max(0.25f, lifeSeconds);
            _fadeSeconds = Mathf.Max(0.05f, fadeSeconds);
            _startTime = Time.time;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _materials.Clear();

            if (_renderers != null)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] == null) continue;
                    Material shared = _renderers[i].sharedMaterial;
                    if (shared != null && shared.HasProperty("_Color"))
                        _materials.Add(_renderers[i].material);
                }
            }
        }

        private void OnEnable()
        {
            if (_startTime <= 0.0f) _startTime = Time.time;
            if (_renderers == null) _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            float age = Time.time - _startTime;
            if (age >= _lifeSeconds)
            {
                Destroy(gameObject);
                return;
            }

            float fadeStart = Mathf.Max(0.0f, _lifeSeconds - _fadeSeconds);
            if (age < fadeStart) return;

            float a = Mathf.Clamp01((_lifeSeconds - age) / Mathf.Max(0.05f, _fadeSeconds));
            for (int i = 0; i < _materials.Count; i++)
            {
                Material mat = _materials[i];
                if (mat == null) continue;
                Color c = mat.color;
                c.a = a;
                mat.color = c;
            }
        }
    }
}
