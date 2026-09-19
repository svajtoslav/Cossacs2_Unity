// C2BuildingConstructionPseudo3DV245.cs
// V247: original BuildObjLink direction + no-stacked BUILDLO phase rebuild.
// V245: construction runtime ported onto the CURRENT pseudo-3D building renderer.
// This keeps the working V232/V234 building pipeline: DrawSpriteBuilding/LINESORT/tight-crop/work animation.
// It does NOT bring back the old flat 2D construction renderer.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private GameObject _c2BuildRuntimeRootV245LikeOriginal;
        private static int s_C2BuildRuntimeNextIndexV245LikeOriginal = 200000;

        public bool C2BuildRuntimeDrawGhostCompositeLikeOriginal(
            Transform root,
            string mdName,
            int nation,
            int realX,
            int realY,
            bool valid,
            string source,
            out string audit)
        {
            audit = "not_started";
            if (root == null)
            {
                audit = "no_root";
                return false;
            }

            C2BuildRuntimeClearChildrenV245LikeOriginal(root);

            GameObject visual;
            bool ok = C2BuildRuntimeCreatePseudo3DCompositeV245LikeOriginal(
                root,
                mdName,
                nation,
                realX,
                realY,
                -1,
                true,
                "preview_" + (source ?? string.Empty),
                out visual,
                out audit,
                null,
                false);

            if (ok)
                C2BuildRuntimeApplyGhostTintV245LikeOriginal(root, valid);

            return ok;
        }

        public bool C2BuildRuntimeCreateConstructionLikeOriginal(
            string mdName,
            string unitId,
            int nation,
            int realX,
            int realY,
            string source,
            out GameObject site,
            out string audit)
        {
            site = null;
            audit = "not_started";

            // V246: no preview root exists in CreateConstruction; clearing belongs only to ghost preview.
            var placementClock = global::System.Diagnostics.Stopwatch.StartNew();
            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found)
            {
                audit = "md_not_found name='" + (mdName ?? string.Empty) + "'";
                return false;
            }

            string smpAuditV1;
            int requestedRealXV1 = realX;
            int requestedRealYV1 = realY;
            bool smpOkV1 = C2SmpApplyBuildingPieceV1LikeOriginal(
                md,
                ref realX,
                ref realY,
                source ?? "runtime-construction",
                out smpAuditV1);
            double surfaceMs = placementClock.Elapsed.TotalMilliseconds;

            if (_c2BuildRuntimeRootV245LikeOriginal == null)
            {
                _c2BuildRuntimeRootV245LikeOriginal = new GameObject("C2_RuntimeConstructionRoot_Pseudo3D_V247");
                _c2BuildRuntimeRootV245LikeOriginal.transform.SetParent(_terrainRoot != null ? _terrainRoot.transform : transform, false);
            }

            site = new GameObject("C2_RuntimeConstructionSite_Pseudo3D_" + SanitizeNameLikeOriginal(string.IsNullOrEmpty(unitId) ? mdName : unitId));
            site.transform.SetParent(_c2BuildRuntimeRootV245LikeOriginal.transform, false);

            var behaviour = site.AddComponent<C2RuntimeConstructionSitePseudo3DV245LikeOriginal>();
            behaviour.Initialize(this, mdName, unitId, nation, realX, realY, source);
            Debug.Log("[C2 BUILD PLACEMENT TIMING] md=" + mdName + " surfaceMs=" +
                surfaceMs.ToString("0.0", CultureInfo.InvariantCulture) + " siteAndVisualMs=" +
                (placementClock.Elapsed.TotalMilliseconds - surfaceMs).ToString("0.0", CultureInfo.InvariantCulture));

            audit = "contract=V247_PSEUDO3D_BUILD_RUNTIME_ORIGINAL_STAGE_DIR created md='" + (md.MdPath ?? string.Empty) + "'" +
                    " name='" + (mdName ?? string.Empty) + "'" +
                    " unit='" + (unitId ?? string.Empty) + "'" +
                    " requestedReal=(" + requestedRealXV1.ToString(CultureInfo.InvariantCulture) + "," + requestedRealYV1.ToString(CultureInfo.InvariantCulture) + ")" +
                    " real=(" + realX.ToString(CultureInfo.InvariantCulture) + "," + realY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " smpOk=" + smpOkV1.ToString() + " smp=[" + smpAuditV1 + "]" +
                    " buildStages=" + Mathf.Max(1, md.BuildStages).ToString(CultureInfo.InvariantCulture) +
                    " source='" + (source ?? string.Empty) + "'";
            return true;
        }

        public int C2BuildRuntimeAssignSelectedBuildersLikeOriginal(GameObject site, int realX, int realY, string source, out string audit)
        {
            return C2BuildRuntimeAssignBuildersSnapshotLikeOriginal(site, realX, realY, null, source, out audit);
        }

        public int C2BuildRuntimeAssignBuildersSnapshotLikeOriginal(
            GameObject site,
            int realX,
            int realY,
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> builderSnapshot,
            string source,
            out string audit)
        {
            audit = "not_started";
            var construction = site != null ? site.GetComponent<C2RuntimeConstructionSitePseudo3DV245LikeOriginal>() : null;
            if (construction == null)
            {
                audit = "no_construction_site";
                return 0;
            }

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> selected = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(64);
            if (builderSnapshot != null)
            {
                for (int i = 0; i < builderSnapshot.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = builderSnapshot[i];
                    if (u != null && u.isActiveAndEnabled && u.CanBuildOrRepairLikeOriginal() && !selected.Contains(u))
                        selected.Add(u);
                }
            }
            else
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal[] all = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
                for (int i = 0; all != null && i < all.Length; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                    if (u != null && u.isActiveAndEnabled && u.IsSelected && u.CanBuildOrRepairLikeOriginal())
                        selected.Add(u);
                }
            }

            int issued = 0;
            List<string> samples = new List<string>();
            for (int i = 0; i < selected.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = selected[i];
                if (u == null) continue;

                // Original Multi.cpp::BuildWithSelected(NI,...) begins with
                // DiscardUnitsOfOtherPlayer(NI).  A blue/EN building must not be
                // built/finished by red/AU selected peasants just because both are
                // selectable in the bridge sandbox.
                if (u.Nation != (byte)Mathf.Clamp(construction.Nation, 0, 255))
                {
                    if (samples.Count < 8)
                        samples.Add("skip:foreign_worker unit='" + (u.SourceMonsterId ?? string.Empty) +
                                    "' unitNation=" + u.Nation.ToString(CultureInfo.InvariantCulture) +
                                    " siteNation=" + construction.Nation.ToString(CultureInfo.InvariantCulture));
                    continue;
                }

                C2BuildWorkerOrderV245LikeOriginal oldOrder = u.GetComponent<C2BuildWorkerOrderV245LikeOriginal>();
                // COSSACKS2/NewMon.cpp::BuildObj appends another BuildObj while
                // one is active; Multi.cpp::BuildWithSelected preserves that chain.
                if (oldOrder != null && oldOrder.QueueFollowingSiteLikeOriginal(construction))
                {
                    issued++;
                    if (samples.Count < 8) samples.Add("queued_after_current_build");
                    continue;
                }

                // Original BuildWithSelected installs BuildObj as the unit's new
                // LocalOrder.  The Unity resource/task component is independent,
                // so it must be stopped explicitly or it keeps overwriting the
                // builder's movement/WORK state every Update (typically affecting
                // only the peasants that had a previous gathering order).
                C2GameplayUnitTaskV1 previousTask = u.GetComponent<C2GameplayUnitTaskV1>();
                if (previousTask != null)
                    previousTask.CancelForExternalOrderLikeOriginal("reassign_to_build_site");

                if (oldOrder != null && oldOrder.enabled)
                    oldOrder.CancelFromExternalOrderLikeOriginal("reassign_to_build_site");
                C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(u);

                int targetRealX;
                int targetRealY;
                int slot;
                string pointAudit;
                if (!construction.TryReserveNearestBuildPointLikeOriginal(u, out slot, out targetRealX, out targetRealY, out pointAudit))
                {
                    if (samples.Count < 8) samples.Add("skip:" + pointAudit);
                    continue;
                }

                C2BuildWorkerOrderV245LikeOriginal order = u.GetComponent<C2BuildWorkerOrderV245LikeOriginal>();
                GameObject unitProxy = order == null ? u.EnsureUnityProxyLikeOriginal() : null;
                if (order == null && unitProxy != null) order = unitProxy.AddComponent<C2BuildWorkerOrderV245LikeOriginal>();
                if (order == null) continue;
                order.Begin(u, construction, slot, targetRealX, targetRealY, source ?? "assign_builders_v245", pointAudit);
                issued++;
                if (samples.Count < 8)
                    samples.Add("#" + i.ToString(CultureInfo.InvariantCulture) + " slot=" + slot.ToString(CultureInfo.InvariantCulture) + " " + pointAudit);
            }

            audit = "contract=V247_PSEUDO3D_BUILD_ASSIGN_ORIGINAL_STAGE_DIR selected=" + selected.Count.ToString(CultureInfo.InvariantCulture) +
                    " issued=" + issued.ToString(CultureInfo.InvariantCulture) +
                    " site='" + (construction != null ? construction.MdName : string.Empty) + "'" +
                    " samples=" + string.Join(" | ", samples.ToArray());
            Debug.Log("[C2:BUILD RUNTIME V247 ASSIGN] " + audit);
            return issued;
        }

        internal bool C2BuildRuntimeCreatePseudo3DCompositeV245LikeOriginal(
            Transform root,
            string mdName,
            int nation,
            int realX,
            int realY,
            int buildStage,
            bool includeWorkWhenReady,
            string source,
            out GameObject visual,
            out string audit,
            string recordMonsterIdOverrideV260C = null,
            bool attachGameplayRuntimeV303LikeOriginal = true)
        {
            visual = null;
            audit = "not_started";
            if (root == null)
            {
                audit = "no_root";
                return false;
            }

            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found)
            {
                audit = "md_not_found name='" + (mdName ?? string.Empty) + "'";
                return false;
            }

            C2Building3InuRecordLikeOriginal r = C2BuildRuntimeRecordV245LikeOriginal(
                !string.IsNullOrWhiteSpace(recordMonsterIdOverrideV260C) ? recordMonsterIdOverrideV260C : mdName,
                nation,
                realX,
                realY,
                buildStage,
                md.BuildStages,
                md.Life);
            List<C2BuildingLoadedPartLikeOriginal> parts;
            string visualAudit;
            if (!TryLoadBuildingPartsLikeOriginal(md, r, out parts, out visualAudit) || parts == null || parts.Count == 0)
            {
                audit = "visual_missing " + visualAudit;
                return false;
            }

            int before = root.childCount;
            CreateBuildingCompositeLikeOriginal(root, r, md, parts, attachGameplayRuntimeV303LikeOriginal);
            if (root.childCount > before)
                visual = root.GetChild(root.childCount - 1).gameObject;

            audit = "contract=V247_PSEUDO3D_COMPOSITE_ORIGINAL_STAGE_DIR md='" + (md.MdPath ?? string.Empty) + "'" +
                    " real=(" + realX.ToString(CultureInfo.InvariantCulture) + "," + realY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " stage=" + buildStage.ToString(CultureInfo.InvariantCulture) +
                    " savedStage=" + r.Stage.ToString(CultureInfo.InvariantCulture) +
                    " parts=" + parts.Count.ToString(CultureInfo.InvariantCulture) +
                    " gameplayRuntimeV303=" + (attachGameplayRuntimeV303LikeOriginal ? "1" : "0") +
                    " source='" + (source ?? string.Empty) + "' visual=[" + visualAudit + "]";
            return true;
        }

        internal int C2BuildRuntimeGetBuildStagesV245LikeOriginal(string mdName)
        {
            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found) return 64;
            return Mathf.Max(1, md.BuildStages > 0 ? md.BuildStages : 64);
        }

        internal int C2BuildRuntimeGetLifeMaxV245LikeOriginal(string mdName)
        {
            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found) return 1;
            return Mathf.Max(1, md.Life > 0 ? md.Life : 1);
        }

        internal int C2BuildRuntimeGetBuildPointCountV253LikeOriginal(string mdName)
        {
            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found || md.BuildPoints == null) return 0;
            return Mathf.Max(0, md.BuildPoints.Count);
        }

        internal bool C2BuildRuntimeGetBuildPointRealV245LikeOriginal(
            string mdName,
            int realX,
            int realY,
            int pointIndex,
            out int targetRealX,
            out int targetRealY,
            out string audit)
        {
            targetRealX = realX;
            targetRealY = realY;
            audit = "fallback_center";

            C2BuildingMdInfoLikeOriginal md = ResolveBuildingMdLikeOriginal(mdName);
            if (md == null || !md.Found)
            {
                audit = "md_not_found";
                return false;
            }

            int count = md.BuildPoints != null ? md.BuildPoints.Count : 0;
            if (count > 0)
            {
                int idx = Mathf.Abs(pointIndex) % count;
                Vector2 p = md.BuildPoints[idx];

                int cornerX = (realX + (md.PicDx << 4)) >> 8;
                int cornerY = (realY + (md.PicDy << 5)) >> 8;

                int rawCellX = Mathf.RoundToInt(p.x);
                int rawCellY = Mathf.RoundToInt(p.y);
                int cellX = cornerX + rawCellX;
                int cellY = cornerY + rawCellY;
                // COSSACKS2 OneObject::FindPoint supplies original MD gameplay
                // cells directly relative to GetCornerXY. V379 deliberately keeps
                // them independent of Unity visual sprite scale.
                Vector2 target = C2BuildingFootprintPointLikeOriginal(realX, realY, cellX * 16.0f, cellY * 16.0f);
                targetRealX = Mathf.RoundToInt(target.x * 16.0f);
                targetRealY = Mathf.RoundToInt(target.y * 16.0f);
                audit = "BUILDPOINTS[" + idx.ToString(CultureInfo.InvariantCulture) + "] cell=" +
                        cellX.ToString(CultureInfo.InvariantCulture) + "/" + cellY.ToString(CultureInfo.InvariantCulture) +
                        " source=C2_MD_build_cells_original_gameplay_v379";
                return true;
            }

            int radius = 18 + (Mathf.Abs(pointIndex) / 8) * 7;
            int pos = Mathf.Abs(pointIndex) % 8;
            int ox = 0, oy = 0;
            switch (pos)
            {
                case 0: ox = -radius; break;
                case 1: ox = radius; break;
                case 2: oy = -radius; break;
                case 3: oy = radius; break;
                case 4: ox = -radius; oy = -radius; break;
                case 5: ox = radius; oy = -radius; break;
                case 6: ox = -radius; oy = radius; break;
                default: ox = radius; oy = radius; break;
            }

            int baseCellX = realX >> 8;
            int baseCellY = realY >> 8;
            targetRealX = (baseCellX + ox) << 8;
            targetRealY = (baseCellY + oy) << 8;
            audit = "fallback_ring cell=" + (targetRealX >> 8).ToString(CultureInfo.InvariantCulture) + "/" +
                    (targetRealY >> 8).ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static C2Building3InuRecordLikeOriginal C2BuildRuntimeRecordV245LikeOriginal(
            string mdName,
            int nation,
            int realX,
            int realY,
            int builtStage,
            int buildStages,
            int lifeMax)
        {
            C2Building3InuRecordLikeOriginal r = new C2Building3InuRecordLikeOriginal();
            r.Index = s_C2BuildRuntimeNextIndexV245LikeOriginal++;
            r.Nation = (byte)Mathf.Clamp(nation, 0, 255);
            r.NIndex = 0;
            r.RealX = realX;
            r.RealY = realY;
            int stages = Mathf.Max(1, buildStages > 0 ? buildStages : 64);
            int maxLife = Mathf.Max(1, lifeMax > 0 ? lifeMax : 1);
            int originalLife = builtStage < 0 ? maxLife : (Mathf.Clamp(builtStage, 0, stages) * maxLife) / stages;
            r.Life = (ushort)Mathf.Clamp(originalLife, 0, 65535);
            r.Stage = C2BuildRuntimeSavedStageFromBuildProgressV245LikeOriginal(builtStage);
            r.WallX = 0;
            r.WallY = 0;
            r.RealDir = 0;
            r.Flags = 0;
            r.MonsterId = mdName ?? string.Empty;
            return r;
        }

        private static ushort C2BuildRuntimeSavedStageFromBuildProgressV245LikeOriginal(int builtStage)
        {
            if (builtStage < 0)
                return 0;
            if (builtStage <= 0)
                return 0xFFFF;
            if (builtStage >= 0x7FFE)
                return 0;
            int saved = Mathf.Clamp(0xFFFF - builtStage, 0x8001, 0xFFFF);
            return (ushort)saved;
        }

        internal static void C2BuildRuntimeClearChildrenV245LikeOriginal(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform c = root.GetChild(i);
                if (c == null) continue;

                // V303: Destroy() is delayed until the end of frame.  During that frame the old
                // preview/build-stage child still has active C2BuildingRuntimeInfo and can be
                // matched by production as another identical barracks.  Disable it first so
                // FindObjectsOfType/selection cannot use stale BORNPOINTS.
                if (c.gameObject != null)
                    c.gameObject.SetActive(false);
#if UNITY_EDITOR
                if (!Application.isPlaying) UnityEngine.Object.DestroyImmediate(c.gameObject);
                else UnityEngine.Object.Destroy(c.gameObject);
#else
                UnityEngine.Object.Destroy(c.gameObject);
#endif
            }
        }

        private static void C2BuildRuntimeApplyGhostTintV245LikeOriginal(Transform root, bool valid)
        {
            if (root == null) return;
            Color c = valid ? new Color(1.0f, 1.0f, 1.0f, 0.78f) : new Color(1.0f, 0.0f, 0.0f, 0.62f);
            Renderer[] rr = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; rr != null && i < rr.Length; i++)
            {
                Renderer r = rr[i];
                if (r == null) continue;
                Material[] mats = r.materials;
                for (int m = 0; mats != null && m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null) continue;
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                    mat.renderQueue = C2BuildingsRenderQueueLikeOriginal + 20;
                    if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
                    if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.Always);
                }
            }
        }

        internal static byte C2BuildRuntimeDirectionFromRealDeltaV245LikeOriginal(float dx, float dy)
        {
            // V247: match original Cossacks2 GetDir(dx,dy) convention used by BuildObjLink.
            // Original line:
            //     char dir=char(GetDir(OB->RealX-OBJ->RealX,OB->RealY-OBJ->RealY));
            // In Unity real-map Y is inverted relative to screen angle, so use atan2(-dy, dx).
            if (Mathf.Abs(dx) < 0.0001f && Mathf.Abs(dy) < 0.0001f) return 0;
            float angle = Mathf.Atan2(-dy, dx) * Mathf.Rad2Deg;
            int raw = Mathf.RoundToInt(Mathf.Repeat(angle / 360.0f * 256.0f, 256.0f));
            return (byte)(raw & 255);
        }

        private static byte C2BuildRuntimeMirrorLeftRightDirectionV247LikeOriginal(byte dir)
        {
            // Same correction used by the old working construction port:
            // vertical directions stay, left/right work-animation banks are mirrored.
            return (byte)((128 - dir) & 255);
        }

        internal static byte C2BuildRuntimeWorkBankDirectionV245LikeOriginal(byte rawDir)
        {
            // Face toward building first, then fix left/right bank mapping.
            byte faceToward = (byte)((rawDir + 128) & 255);
            return C2BuildRuntimeMirrorLeftRightDirectionV247LikeOriginal(faceToward);
        }
    }

    public sealed class C2RuntimeConstructionSitePseudo3DV245LikeOriginal : MonoBehaviour
    {
        public C2BattleTerrainMode OwnerMode;
        public string MdName = string.Empty;
        public string UnitId = string.Empty;
        public int Nation;
        public int RealX;
        public int RealY;
        public int BuildStages = 64;
        public int Stage;
        public int LifeMax = 1;
        public int Life;
        public bool Ready;
        public bool Dead;

        private Transform _visualRoot;
        private C2RuntimeConstructionSiteProxyLikeOriginal _proxy;
        private C2SettlementBuildingSelectableV1LikeOriginal _selectable;
        private readonly HashSet<int> _reservedSlots = new HashSet<int>();
        private int _lastVisualPhase = -999;
        private float _lastLogAt;

        // V260C: keep gameplay/build logic untouched, but stop per-worker/per-frame log spam.
        // The previous build could drop FPS during construction mostly because every worker
        // wrote WORK_CYCLE/NEXTSTAGE/BORN/cache logs while dozens of peasants were building.
        private const bool C2BuildRuntimeVerboseWorkerLogV260C = false;
        private const bool C2BuildRuntimeVerboseStageLogV260C = false;

        // Cossacks II keeps #BUILDLO_3 until the final NextStage switches the object to #STANDLO.
        // Showing #STANDLO during the last unfinished quarter mixes the complete building with
        // scaffolding and produces the broken construction image seen in the test scene.
        private const bool C2BuildRuntimeUseStandLoForFinalBuildPhaseV260C = false;
        internal static bool C2BuildRuntimeVerboseWorkerLogV260CAccessor
        {
            get { return C2BuildRuntimeVerboseWorkerLogV260C; }
        }

        public int WorkFrameCountLikeOriginal
        {
            get { return 9; }
        }

        public float WorkFpsLikeOriginal
        {
            get { return 12.0f; }
        }

        public void Initialize(C2BattleTerrainMode mode, string mdName, string unitId, int nation, int realX, int realY, string source)
        {
            OwnerMode = mode;
            MdName = mdName ?? string.Empty;
            UnitId = unitId ?? string.Empty;
            Nation = nation;
            RealX = realX;
            RealY = realY;
            BuildStages = mode != null ? mode.C2BuildRuntimeGetBuildStagesV245LikeOriginal(MdName) : 64;
            LifeMax = mode != null ? mode.C2BuildRuntimeGetLifeMaxV245LikeOriginal(MdName) : 1;
            Stage = 0;
            Life = 0;
            Ready = false;
            Dead = false;

            _visualRoot = new GameObject("visual_pseudo3d").transform;
            _visualRoot.SetParent(transform, false);

            _selectable = gameObject.GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
            if (_selectable == null) _selectable = gameObject.AddComponent<C2SettlementBuildingSelectableV1LikeOriginal>();

            _proxy = gameObject.GetComponent<C2RuntimeConstructionSiteProxyLikeOriginal>();
            if (_proxy == null) _proxy = gameObject.AddComponent<C2RuntimeConstructionSiteProxyLikeOriginal>();

            SyncProxyLikeOriginal();
            RebuildVisualLikeOriginal("init_" + (source ?? string.Empty));

            Debug.Log("[C2:BUILD RUNTIME V247 SITE CREATE] md='" + MdName +
                      "' real=(" + RealX.ToString(CultureInfo.InvariantCulture) + "," + RealY.ToString(CultureInfo.InvariantCulture) + ")" +
                      " stages=" + BuildStages.ToString(CultureInfo.InvariantCulture) +
                      " source='" + (source ?? string.Empty) + "'");
        }

        public bool TryReserveNearestBuildPointLikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal worker,
            out int slot,
            out int targetRealX,
            out int targetRealY,
            out string audit)
        {
            slot = -1;
            targetRealX = RealX;
            targetRealY = RealY;
            audit = "no_worker";
            if (worker == null || OwnerMode == null || Ready || Dead) return false;

            CleanupStaleBuildPointReservationsV259LikeOriginal();

            float wx = worker.RealXFloat != 0.0f ? worker.RealXFloat : worker.RealX;
            float wy = worker.RealYFloat != 0.0f ? worker.RealYFloat : worker.RealY;

            float best = float.PositiveInfinity;
            int bestSlot = -1;
            int bestX = RealX;
            int bestY = RealY;
            string bestAudit = string.Empty;
            int blockedBuildPointCandidatesV278 = 0;
            int snappedFreeBuildPointCandidatesV278 = 0;
            int ownServicePointSelfBlockIgnoredV290 = 0;

            int declaredBuildPoints = OwnerMode.C2BuildRuntimeGetBuildPointCountV253LikeOriginal(MdName);
            bool hasMdBuildPointsV290 = declaredBuildPoints > 0;
            int probeCount = hasMdBuildPointsV290 ? Mathf.Clamp(declaredBuildPoints, 1, 512) : 32;

            // COSSACKS2::FindPoint(FP_UNLOCKED_POINT) / BuildObjLink never
            // assigns a blocked working cell. Expanded raster cells and the
            // unit radius can cover a source MD point; use a nearby free cell,
            // with a bounded search, rather than bypassing the building wall.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < probeCount; i++)
                {
                    if (pass == 0 && _reservedSlots.Contains(i)) continue;

                    int rx, ry;
                    string pa;
                    if (!OwnerMode.C2BuildRuntimeGetBuildPointRealV245LikeOriginal(MdName, RealX, RealY, i, out rx, out ry, out pa))
                        continue;

                    int candidateX = rx;
                    int candidateY = ry;
                    string candidateAudit = pa + (pass == 1 ? " shared_slot=True" : string.Empty);

                    if (C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(candidateX, candidateY, 1))
                    {
                        blockedBuildPointCandidatesV278++;

                        float freeX, freeY;
                        if (!C2BattleTerrainMode.C2BuildingMotionFieldV1TryFindNearestFreeRealLikeOriginal(
                                candidateX, candidateY, out freeX, out freeY, 4))
                            continue;
                        candidateX = Mathf.RoundToInt(freeX);
                        candidateY = Mathf.RoundToInt(freeY);
                        snappedFreeBuildPointCandidatesV278++;
                        candidateAudit += " workingCellAdjustedForClearance=True";
                    }

                    float dx = wx - candidateX;
                    float dy = wy - candidateY;
                    float d = dx * dx + dy * dy;
                    if (d < best)
                    {
                        best = d;
                        bestSlot = i;
                        bestX = candidateX;
                        bestY = candidateY;
                        bestAudit = candidateAudit;
                    }
                }

                if (bestSlot >= 0)
                    break;
            }

            if (bestSlot < 0)
            {
                audit = "no_free_buildpoint blockedCandidates=" +
                        blockedBuildPointCandidatesV278.ToString(CultureInfo.InvariantCulture) +
                        " snappedFreeCandidates=" +
                        snappedFreeBuildPointCandidatesV278.ToString(CultureInfo.InvariantCulture) +
                        " ownServicePointSelfBlockIgnoredV290=" +
                        ownServicePointSelfBlockIgnoredV290.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            _reservedSlots.Add(bestSlot);
            slot = bestSlot;
            targetRealX = bestX;
            targetRealY = bestY;
            audit = bestAudit + " reserved=True";
            return true;
        }

        private void CleanupStaleBuildPointReservationsV259LikeOriginal()
        {
            if (_reservedSlots.Count == 0)
                return;

            C2BuildWorkerOrderV245LikeOriginal[] orders = UnityEngine.Object.FindObjectsOfType<C2BuildWorkerOrderV245LikeOriginal>();
            List<int> remove = null;

            foreach (int slot in _reservedSlots)
            {
                bool live = false;
                for (int i = 0; orders != null && i < orders.Length; i++)
                {
                    C2BuildWorkerOrderV245LikeOriginal order = orders[i];
                    if (order != null && order.IsLiveOrderForSiteSlotV259LikeOriginal(this, slot))
                    {
                        live = true;
                        break;
                    }
                }

                if (!live)
                {
                    if (remove == null) remove = new List<int>(4);
                    remove.Add(slot);
                }
            }

            if (remove == null || remove.Count == 0)
                return;

            for (int i = 0; i < remove.Count; i++)
                _reservedSlots.Remove(remove[i]);

            Debug.Log("[C2:BUILD RUNTIME V259 RESERVE CLEANUP] site='" + (MdName ?? string.Empty) +
                      "' releasedStale=" + remove.Count.ToString(CultureInfo.InvariantCulture));
        }

        public void ReleaseBuildSlotLikeOriginal(int slot)
        {
            if (slot >= 0) _reservedSlots.Remove(slot);
        }

        public byte DirectionFromPointToBuildingLikeOriginal(int pointRealX, int pointRealY)
        {
            byte raw = C2BattleTerrainMode.C2BuildRuntimeDirectionFromRealDeltaV245LikeOriginal(RealX - pointRealX, RealY - pointRealY);
            return C2BattleTerrainMode.C2BuildRuntimeWorkBankDirectionV245LikeOriginal(raw);
        }

        public void CompleteInstantForEditorV332LikeOriginal()
        {
            if (Dead) return;
            Stage = Mathf.Max(1, BuildStages);
            Life = Mathf.Max(1, LifeMax);
            Ready = true;
            RebuildVisualLikeOriginal("editor_instant_complete_v332");
            SyncProxyLikeOriginal();
        }

        public bool NextStageFromWorkerLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal worker, int slotIndex, string pointAudit)
        {
            if (Ready || Dead || OwnerMode == null)
                return false;

            int before = Stage;
            Stage = Mathf.Clamp(Stage + 1, 0, Mathf.Max(1, BuildStages));
            Life = (Stage * Mathf.Max(1, LifeMax)) / Mathf.Max(1, BuildStages);
            if (Stage >= BuildStages)
            {
                Stage = BuildStages;
                Life = Mathf.Max(1, LifeMax);
                Ready = true;
            }

            int phase = VisualBuildPhaseLikeOriginal;
            if (Ready || phase != _lastVisualPhase)
                RebuildVisualLikeOriginal(Ready ? "ready" : "phase_" + phase.ToString(CultureInfo.InvariantCulture));

            SyncProxyLikeOriginal();

            if (Ready || (C2BuildRuntimeVerboseStageLogV260C && Time.realtimeSinceStartup - _lastLogAt > 0.25f))
            {
                _lastLogAt = Time.realtimeSinceStartup;
                Debug.Log("[C2:BUILD RUNTIME V247 NEXTSTAGE] md='" + MdName +
                          "' worker='" + (worker != null ? worker.SourceMonsterId : "<null>") +
                          "' slot=" + slotIndex.ToString(CultureInfo.InvariantCulture) +
                          " stage=" + before.ToString(CultureInfo.InvariantCulture) + "->" + Stage.ToString(CultureInfo.InvariantCulture) +
                          "/" + BuildStages.ToString(CultureInfo.InvariantCulture) +
                          " phase=" + phase.ToString(CultureInfo.InvariantCulture) +
                          " ready=" + Ready +
                          " point='" + (pointAudit ?? string.Empty) + "'");
            }

            return !Ready;
        }

        private int VisualBuildPhaseLikeOriginal
        {
            get
            {
                if (Ready || Stage >= BuildStages) return 3;
                return Mathf.Clamp((Stage * 4) / Mathf.Max(1, BuildStages), 0, 3);
            }
        }

        private void SyncProxyLikeOriginal()
        {
            if (_proxy == null) _proxy = gameObject.GetComponent<C2RuntimeConstructionSiteProxyLikeOriginal>();
            if (_proxy == null) _proxy = gameObject.AddComponent<C2RuntimeConstructionSiteProxyLikeOriginal>();
            if (_selectable == null) _selectable = gameObject.GetComponent<C2SettlementBuildingSelectableV1LikeOriginal>();
            if (_selectable == null) _selectable = gameObject.AddComponent<C2SettlementBuildingSelectableV1LikeOriginal>();

            _proxy.Building = _selectable;
            _proxy.Configure(OwnerMode, MdName, UnitId, Nation, RealX, RealY, BuildStages, Stage, Ready, Dead, false, LifeMax, Life);

            // V257:
            // The real gameplay selectable for a runtime construction site is the site root,
            // not the rebuilt visual child.  V248 comments assumed this root already carried
            // SourceMonsterId/KindName/Nation, but it was not actually synchronized here.
            // Result: after constructing a British/English barracks, the root could keep stale
            // BldKaz(AU) identity, so the building HUD queued AU units while BORNPOINTS came
            // from EngKaz.  Keep the root authoritative and refresh it every phase.
            _selectable.OwnerMode = OwnerMode;
            _selectable.SourceMonsterId = !string.IsNullOrWhiteSpace(UnitId) ? UnitId : MdName;
            _selectable.KindName = !string.IsNullOrWhiteSpace(MdName) ? MdName : UnitId;
            _selectable.RecordIndex = 200000 + Mathf.Abs(GetEntityId().GetHashCode() % 100000);
            _selectable.RealX = RealX;
            _selectable.RealY = RealY;
            _selectable.RealDir = 0;
            _selectable.Nation = Nation;
            _selectable.LifeMaxLikeOriginal = Mathf.Max(1, LifeMax);
            _selectable.LifeLikeOriginal = Ready ? Mathf.Max(1, LifeMax) : Mathf.Clamp(Life, 0, Mathf.Max(1, LifeMax));
            _selectable.StageMaxLikeOriginal = Mathf.Max(1, BuildStages);
            _selectable.StageLikeOriginal = Mathf.Clamp(Stage, 0, Mathf.Max(1, BuildStages));
            _selectable.ReadyLikeOriginal = Ready;
            _selectable.NotSelectable = Dead;
            _selectable.SortKey = 12000 + (RealY >> 8) * 32 + _selectable.RecordIndex;
            // V277: this is the map/pipeline pixel scale only.
            // Do not store native visual sprite scale here; produced units must not inherit building visual size.
            _selectable.MapPixelToWorld = OwnerMode != null ? OwnerMode.C2MapPixelToWorldScaleV277LikeOriginal() : 1.0f;
            _selectable.SelectionHalfPixelsX = Mathf.Max(48.0f, 96.0f);
            _selectable.SelectionHalfPixelsY = Mathf.Max(32.0f, 64.0f);

            // V260: construction site is also a separate original-like object;
            // it owns its own BUILDPOINTS/BORNPOINTS/production/destruction state.
            if (OwnerMode != null)
                OwnerMode.C2BuildingDestructionV260AttachConstructionSiteLikeOriginal(gameObject, this);
        }

        private void RebuildVisualLikeOriginal(string reason)
        {
            if (OwnerMode == null || _visualRoot == null) return;

            // V247: old V245 appended every #BUILDLO phase under visual_pseudo3d.
            // Original OneObject::NextStage replaces LoLayer; it does not stack phases.
            C2BattleTerrainMode.C2BuildRuntimeClearChildrenV245LikeOriginal(_visualRoot);

            int visualPhaseV260C = VisualBuildPhaseLikeOriginal;
            int buildStage = (Ready || (C2BuildRuntimeUseStandLoForFinalBuildPhaseV260C && visualPhaseV260C >= 3))
                ? -1
                : Mathf.Clamp(Stage, 0, Mathf.Max(1, BuildStages - 1));
            GameObject visual;
            string audit;
            bool ok = OwnerMode.C2BuildRuntimeCreatePseudo3DCompositeV245LikeOriginal(
                _visualRoot,
                MdName,
                Nation,
                RealX,
                RealY,
                buildStage,
                Ready,
                "site_" + reason,
                out visual,
                out audit,
                !string.IsNullOrWhiteSpace(UnitId) ? UnitId : MdName,
                false);

            _lastVisualPhase = VisualBuildPhaseLikeOriginal;
            if (!ok)
                Debug.LogWarning("[C2:BUILD RUNTIME V247 VISUAL MISS] md='" + MdName + "' " + audit);
            else if (Ready || (reason != null && reason.IndexOf("init", StringComparison.OrdinalIgnoreCase) >= 0) || C2BuildRuntimeVerboseStageLogV260C)
                Debug.Log("[C2:BUILD RUNTIME V247 VISUAL V260C] md='" + MdName +
                          "' reason='" + (reason ?? string.Empty) +
                          "' stage=" + Stage.ToString(CultureInfo.InvariantCulture) + "/" + BuildStages.ToString(CultureInfo.InvariantCulture) +
                          " phase=" + _lastVisualPhase.ToString(CultureInfo.InvariantCulture) +
                          " visualStage=" + buildStage.ToString(CultureInfo.InvariantCulture) +
                          " ready=" + Ready + " " + audit);

            SyncProxyLikeOriginal();

            string zoneAudit;
            bool zonesOk = OwnerMode.C2BuildingRuntimeV303AttachConstructionSiteZonesLikeOriginal(
                gameObject,
                MdName,
                UnitId,
                Nation,
                RealX,
                RealY,
                BuildStages,
                Stage,
                Ready,
                out zoneAudit);
            if (!zonesOk)
            {
                Debug.LogWarning("[C2:BUILD RUNTIME V303 SITE ZONES MISS] md='" + MdName + "' " + zoneAudit);
            }
            else if (Ready || (reason != null && reason.IndexOf("init", StringComparison.OrdinalIgnoreCase) >= 0) || C2BuildRuntimeVerboseStageLogV260C)
            {
                Debug.Log("[C2:BUILD RUNTIME V303 SITE ZONES] md='" + MdName +
                          "' reason='" + (reason ?? string.Empty) +
                          "' " + zoneAudit);
            }
        }
    }

    public sealed class C2BuildWorkerOrderV245LikeOriginal : MonoBehaviour
    {
        internal static bool C2BuildWorkerOrderInternalMoveV258LikeOriginal;

        private C2NeutralPeasantUnitInfoV2LikeOriginal _unit;
        private C2RuntimeConstructionSitePseudo3DV245LikeOriginal _site;
        private int _slot;
        private int _targetRealX;
        private int _targetRealY;
        private byte _lockedWorkDir;
        private string _pointAudit = string.Empty;
        private float _nextRepathAt;
        private float _nextProgressSampleAt;
        private float _bestTravelDistanceReal;
        private float _stuckSince;
        private float _workPhase;
        private int _lastWorkFrameIndex = -1;
        private bool _approachIssued;
        private bool _arrived;
        private bool _working;
        private bool _hasLockedWorkDir;
        private bool _finished;
        private readonly Queue<C2RuntimeConstructionSitePseudo3DV245LikeOriginal> _followingSites =
            new Queue<C2RuntimeConstructionSitePseudo3DV245LikeOriginal>();

        public bool QueueFollowingSiteLikeOriginal(C2RuntimeConstructionSitePseudo3DV245LikeOriginal site)
        {
            if (_finished || !enabled || _site == null || _site.Ready || _site.Dead || site == null)
                return false;
            if (site != _site && !_followingSites.Contains(site)) _followingSites.Enqueue(site);
            return true;
        }

        public void CancelForSiteLikeOriginal(C2RuntimeConstructionSitePseudo3DV245LikeOriginal site)
        {
            if (site == null) return;
            int count = _followingSites.Count;
            for (int i = 0; i < count; i++)
            {
                var pending = _followingSites.Dequeue();
                if (pending != null && pending != site) _followingSites.Enqueue(pending);
            }
            if (!_finished && _site == site) FinishOrderLikeOriginal("site_deleted", true);
        }

        private bool StartFollowingSiteLikeOriginal()
        {
            while (_followingSites.Count > 0 && _unit != null && _unit.isActiveAndEnabled)
            {
                var next = _followingSites.Dequeue();
                if (next == null || next.Ready || next.Dead || next.Nation != _unit.Nation) continue;
                if (next.TryReserveNearestBuildPointLikeOriginal(_unit, out int slot, out int x, out int y, out string audit))
                {
                    Begin(_unit, next, slot, x, y, "next_queued_build", audit);
                    return true;
                }
            }
            return false;
        }

        public void Begin(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2RuntimeConstructionSitePseudo3DV245LikeOriginal site,
            int slot,
            int targetRealX,
            int targetRealY,
            string source,
            string pointAudit)
        {
            enabled = true;
            _unit = unit != null ? unit :
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2FindForGameObjectV365LikeOriginal(gameObject);
            _site = site;
            _slot = slot;
            _targetRealX = targetRealX;
            _targetRealY = targetRealY;
            _pointAudit = pointAudit ?? string.Empty;
            _approachIssued = false;
            _arrived = false;
            _working = false;
            _hasLockedWorkDir = false;
            _lockedWorkDir = 0;
            _finished = false;
            _workPhase = 0.0f;
            _lastWorkFrameIndex = -1;
            _nextRepathAt = Time.realtimeSinceStartup + 1.50f;
            _nextProgressSampleAt = Time.realtimeSinceStartup + 0.35f;
            _bestTravelDistanceReal = float.PositiveInfinity;
            _stuckSince = 0.0f;

            TryIssueApproachAfterRuntimeReadyLikeOriginal();

            Debug.Log("[C2:BUILD WORKER V247 ASSIGN] unit='" + (_unit != null ? _unit.SourceMonsterId : "<null>") +
                      "' slot=" + _slot.ToString(CultureInfo.InvariantCulture) +
                      " targetReal=(" + _targetRealX.ToString(CultureInfo.InvariantCulture) + "," + _targetRealY.ToString(CultureInfo.InvariantCulture) + ")" +
                      " point='" + _pointAudit + "' source='" + (source ?? string.Empty) + "'");
        }

        private bool TryIssueApproachAfterRuntimeReadyLikeOriginal()
        {
            if (_approachIssued)
                return true;
            if (_unit == null || _site == null)
                return false;

            C2UnitOriginalRuntimeLinkLikeOriginal runtimeLink =
                _unit.RuntimeLinkCachedLikeOriginal;
            if (runtimeLink == null || !runtimeLink.IsReadyLikeOriginal)
                return false;

            try
            {
                C2BuildWorkerOrderInternalMoveV258LikeOriginal = true;
                _unit.SetMoveDestinationRealLikeOriginal(
                    _targetRealX,
                    _targetRealY,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    false,
                    0);
            }
            finally
            {
                C2BuildWorkerOrderInternalMoveV258LikeOriginal = false;
            }
            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                _unit, C2UnitOrderKindV325LikeOriginal.BuildApproach, "build_order", _pointAudit);
            _approachIssued = true;
            return true;
        }

        private void Update()
        {
            if (_finished || _unit == null || !_unit.isActiveAndEnabled)
            {
                enabled = false;
                return;
            }

            if (_site == null || _site.Ready || _site.Dead)
            {
                FinishOrderLikeOriginal("site_done", true);
                return;
            }

            if (!TryIssueApproachAfterRuntimeReadyLikeOriginal())
                return;

            float ux = _unit.RealXFloat != 0.0f ? _unit.RealXFloat : _unit.RealX;
            float uy = _unit.RealYFloat != 0.0f ? _unit.RealYFloat : _unit.RealY;
            float dx = ux - _targetRealX;
            float dy = uy - _targetRealY;
            float distReal = Mathf.Sqrt(dx * dx + dy * dy);

            // Original BuildObjLink starts building at dst<=1 map cell.
            // Our Real coordinates are original-pixel*16, so one 16px map cell = 256 real units.
            bool near = distReal <= 256.0f;
            if (!near)
            {
                _arrived = false;
                _working = false;
                _hasLockedWorkDir = false;
                C2UnitOriginalRuntimeLinkLikeOriginal movingLink = _unit.RuntimeLinkCachedLikeOriginal;
                if (movingLink != null) movingLink.StopWorkAnimationLikeOriginal();

                float now = Time.realtimeSinceStartup;
                if (now >= _nextProgressSampleAt)
                {
                    _nextProgressSampleAt = now + 0.35f;
                    if (distReal + 64.0f < _bestTravelDistanceReal)
                    {
                        _bestTravelDistanceReal = distReal;
                        _stuckSince = 0.0f;
                    }
                    else if (_stuckSince <= 0.0f)
                    {
                        _stuckSince = now;
                    }
                }

                // Rebuild the path only after the worker has actually stopped making progress.
                // Reissuing the full path every 1.25 seconds reset motion and caused freeze/slide loops.
                bool stuckLongEnough = _stuckSince > 0.0f && now - _stuckSince >= 1.25f;
                if (stuckLongEnough && now >= _nextRepathAt)
                {
                    _nextRepathAt = now + 1.25f;
                    _stuckSince = now;
                    int replacementSlot;
                    int replacementX;
                    int replacementY;
                    string replacementAudit;
                    if (_site.TryReserveNearestBuildPointLikeOriginal(
                            _unit,
                            out replacementSlot,
                            out replacementX,
                            out replacementY,
                            out replacementAudit) &&
                        replacementSlot != _slot)
                    {
                        int abandonedSlot = _slot;
                        _site.ReleaseBuildSlotLikeOriginal(abandonedSlot);
                        _slot = replacementSlot;
                        _targetRealX = replacementX;
                        _targetRealY = replacementY;
                        _pointAudit = replacementAudit + " reassignedAfterStuck=True abandonedSlot=" +
                                      abandonedSlot.ToString(CultureInfo.InvariantCulture);
                        _bestTravelDistanceReal = float.PositiveInfinity;
                        _hasLockedWorkDir = false;

                        Debug.Log("[C2:BUILD WORKER V332 REASSIGN STUCK] unit='" +
                                  (_unit.SourceMonsterId ?? string.Empty) + "' oldSlot=" +
                                  abandonedSlot.ToString(CultureInfo.InvariantCulture) + " newSlot=" +
                                  _slot.ToString(CultureInfo.InvariantCulture) + " targetReal=(" +
                                  _targetRealX.ToString(CultureInfo.InvariantCulture) + "," +
                                  _targetRealY.ToString(CultureInfo.InvariantCulture) + ") point='" +
                                  _pointAudit + "'");
                    }
                    try
                    {
                        C2BuildWorkerOrderInternalMoveV258LikeOriginal = true;
                        _unit.SetMoveDestinationRealLikeOriginal(
                            _targetRealX,
                            _targetRealY,
                            C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                            false,
                            0);
                    }
                    finally
                    {
                        C2BuildWorkerOrderInternalMoveV258LikeOriginal = false;
                    }
                }
                return;
            }

            if (!_hasLockedWorkDir)
            {
                // Original locks direction toward the building, then plays anm_Work in that bank.
                _lockedWorkDir = _site.DirectionFromPointToBuildingLikeOriginal(_targetRealX, _targetRealY);
                _hasLockedWorkDir = true;
            }

            byte dir = _lockedWorkDir;

            if (!_arrived)
            {
                _arrived = true;
                _working = false;
                _workPhase = 0.0f;
                _lastWorkFrameIndex = -1;
                C2UnitOriginalRuntimeLinkLikeOriginal arrivedLink =
                    _unit.RuntimeLinkCachedLikeOriginal;
                if (arrivedLink != null)
                    arrivedLink.SetMovingLikeOriginal(false);
                _unit.C2NeutralPeasantUnitsV15SetMovingFlagLikeOriginal(false, false);
                C2NeutralPeasantFallbackMoveV311LikeOriginal fallback =
                    _unit.GetComponent<C2NeutralPeasantFallbackMoveV311LikeOriginal>();
                if (fallback != null)
                    fallback.CancelLikeOriginal();
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    _unit, C2UnitOrderKindV325LikeOriginal.BuildWork, "build_order", _pointAudit);

                if (C2RuntimeConstructionSitePseudo3DV245LikeOriginal.C2BuildRuntimeVerboseWorkerLogV260CAccessor)
                    Debug.Log("[C2:BUILD WORKER V247 ARRIVED] unit='" + (_unit != null ? _unit.SourceMonsterId : "<null>") +
                              "' slot=" + _slot.ToString(CultureInfo.InvariantCulture) +
                              " targetReal=(" + _targetRealX.ToString(CultureInfo.InvariantCulture) + "," + _targetRealY.ToString(CultureInfo.InvariantCulture) + ")" +
                              " dist=" + distReal.ToString("0.0", CultureInfo.InvariantCulture) +
                              " lockedWorkDir=" + dir.ToString(CultureInfo.InvariantCulture) +
                              " point='" + _pointAudit + "'");
            }

            if (!_working)
            {
                int directionDelta = (sbyte)(dir - _unit.RealDir);
                if (Mathf.Abs(directionDelta) >= 16)
                {
                    int directionStep = directionDelta > 0 ? 16 : -16;
                    _unit.SetFacingDirectionLikeOriginal((byte)(_unit.RealDir + directionStep));
                    return;
                }

                C2UnitOriginalRuntimeLinkLikeOriginal readyLink =
                    _unit.RuntimeLinkCachedLikeOriginal;
                if (readyLink == null || !readyLink.IsFrameFinishedLikeOriginal)
                    return;

                _unit.SetFacingDirectionLikeOriginal(dir);
                if (!readyLink.SetWorkFramePhaseLikeOriginal(dir, 0.0f, true))
                    return;
                _workPhase = 0.0f;
                _lastWorkFrameIndex = 0;
                _working = true;
                return;
            }

            C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                _unit, C2UnitOrderKindV325LikeOriginal.BuildWork, "build_order", _pointAudit);

            C2UnitOriginalRuntimeLinkLikeOriginal link = _unit.RuntimeLinkCachedLikeOriginal;
            int workFrames = link != null ? link.GetWorkFrameCountLikeOriginal(dir) : 0;
            if (workFrames <= 0) workFrames = _site.WorkFrameCountLikeOriginal;
            workFrames = Mathf.Max(1, workFrames);

            float beforePhase = _workPhase;
            _workPhase += Time.deltaTime * _site.WorkFpsLikeOriginal;
            int frameIndex = Mathf.FloorToInt(_workPhase) % workFrames;
            if (frameIndex < 0) frameIndex += workFrames;

            bool displayedWork = false;
            if (link != null)
                displayedWork = link.SetWorkFramePhaseLikeOriginal(dir, _workPhase, frameIndex != _lastWorkFrameIndex);
            _lastWorkFrameIndex = frameIndex;

            // Original OB->NextStage() is called when the work animation frame cycle finishes.
            bool cycleFinished = Mathf.FloorToInt(beforePhase / workFrames) != Mathf.FloorToInt(_workPhase / workFrames);
            if (!cycleFinished)
                return;

            _workPhase = 0.0f;
            _lastWorkFrameIndex = -1;

            bool keep = _site.NextStageFromWorkerLikeOriginal(_unit, _slot, _pointAudit);
            if (C2RuntimeConstructionSitePseudo3DV245LikeOriginal.C2BuildRuntimeVerboseWorkerLogV260CAccessor)
                Debug.Log("[C2:BUILD WORKER V247 WORK_CYCLE] unit='" + (_unit != null ? _unit.SourceMonsterId : "<null>") +
                          "' slot=" + _slot.ToString(CultureInfo.InvariantCulture) +
                          " lockedWorkDir=" + dir.ToString(CultureInfo.InvariantCulture) +
                          " workFrames=" + workFrames.ToString(CultureInfo.InvariantCulture) +
                          " fps=" + _site.WorkFpsLikeOriginal.ToString("0.##", CultureInfo.InvariantCulture) +
                          " displayedWork=" + displayedWork +
                          " advanced=" + keep);
            if (!keep)
            {
                FinishOrderLikeOriginal("construction_ready", true);
                return;
            }
        }

        public bool IsLiveOrderForSiteSlotV259LikeOriginal(C2RuntimeConstructionSitePseudo3DV245LikeOriginal site, int slot)
        {
            return !_finished &&
                   enabled &&
                   _site == site &&
                   _slot == slot &&
                   _unit != null &&
                   _unit.isActiveAndEnabled;
        }

        public void CancelFromExternalOrderLikeOriginal(string reason)
        {
            FinishOrderLikeOriginal("cancel_" + (reason ?? "external"));
        }

        private void OnDisable()
        {
            if (!_finished && _site != null)
                FinishOrderLikeOriginal("disabled");
        }

        private void OnDestroy()
        {
            if (!_finished && _site != null)
                FinishOrderLikeOriginal("destroyed");
        }

        private void FinishOrderLikeOriginal(string reason, bool continueBuildQueue = false)
        {
            if (_finished) return;
            _finished = true;

            if (_site != null)
                _site.ReleaseBuildSlotLikeOriginal(_slot);

            if (_unit != null)
            {
                C2UnitOriginalRuntimeLinkLikeOriginal link = _unit.RuntimeLinkCachedLikeOriginal;
                if (link != null) link.StopWorkAnimationLikeOriginal();
                C2UnitOrderRuntimeV325LikeOriginal.IssueLikeOriginal(
                    _unit, C2UnitOrderKindV325LikeOriginal.Stand, "build_finish", reason);
            }

            if (continueBuildQueue && StartFollowingSiteLikeOriginal()) return;
            _followingSites.Clear();
            enabled = false;
            if (C2RuntimeConstructionSitePseudo3DV245LikeOriginal.C2BuildRuntimeVerboseWorkerLogV260CAccessor || string.Equals(reason, "construction_ready", StringComparison.OrdinalIgnoreCase))
                Debug.Log("[C2:BUILD WORKER V247 FINISH] unit='" + (_unit != null ? _unit.SourceMonsterId : "<null>") +
                          "' reason='" + (reason ?? string.Empty) + "'");
        }
    }
}
