// C2BuildingConstructionRuntimeLikeOriginal.cs
// Runtime construction bridge:
// original path is UI BuildMode -> ShowBuildingPreview -> CmdCreateBuilding -> CreateBuilding -> BuildWithSelected -> BuildObj/NextStage.
// This file reuses the already transferred settlement MD/G16 composite renderer so preview and construction stages
// are drawn from the same #STANDLO/#BUILDLO parts as saved buildings, not from a single fallback quad.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const string C2BuildRuntimeContractLikeOriginal = "V117_SELECTED_FLAG_CARRIED_ACROSS_STAGE_VISUAL_REBUILD";
        private static int s_C2BuildRuntimeNextIndexLikeOriginal = 900000;
        private GameObject _c2BuildRuntimeRootLikeOriginal;

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

            C2BuildRuntimeClearChildrenLikeOriginal(root);

            C2Settlement3InuMdV2Info md = C2Settlement3InuMdV2ResolveMdLikeOriginal(mdName);
            if (md == null || !md.Found)
            {
                audit = "md_not_found name='" + (mdName ?? string.Empty) + "'";
                return false;
            }

            C2Settlement3InuMdV2Record r = C2BuildRuntimeRecordLikeOriginal(mdName, nation, realX, realY, -1, 0);
            C2Settlement3InuMdV2Kind kind = C2BuildRuntimeKindLikeOriginal(md);
            List<C2Settlement3InuMdV2LoadedFrame> frames;
            string visualAudit;
            bool ok = C2Settlement3InuMdV2TryLoadVisualFramesLikeOriginal(md, r, kind, out frames, out visualAudit);
            if (!ok || frames == null || frames.Count == 0)
            {
                audit = "visual_missing md='" + (md.MdPath ?? string.Empty) + "' visual=" + visualAudit;
                return false;
            }

            C2Settlement3InuMdV2CreateSpriteObjectCompositeLikeOriginal(root, r, md, kind, frames, visualAudit);

            // V111: disable selectable scripts before tinting the placement ghost.
            // Otherwise selectable.OnDisable may overwrite the red/white preview tint.
            C2BuildRuntimeDisablePreviewSelectionLikeOriginal(root);
            C2BuildRuntimeApplyGhostTintLikeOriginal(root, valid);

            audit = "contract=" + C2BuildRuntimeContractLikeOriginal +
                    " md='" + (md.MdPath ?? string.Empty) + "'" +
                    " kind=" + kind +
                    " real=(" + realX.ToString(CultureInfo.InvariantCulture) + "," + realY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " valid=" + valid +
                    " parts=" + frames.Count.ToString(CultureInfo.InvariantCulture) +
                    " visual=[" + visualAudit + "]" +
                    " source='" + (source ?? string.Empty) + "'";
            return true;
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

            C2Settlement3InuMdV2Info md = C2Settlement3InuMdV2ResolveMdLikeOriginal(mdName);
            if (md == null || !md.Found)
            {
                audit = "md_not_found name='" + (mdName ?? string.Empty) + "'";
                return false;
            }

            if (_c2BuildRuntimeRootLikeOriginal == null)
            {
                _c2BuildRuntimeRootLikeOriginal = new GameObject("C2_RuntimeConstructionRoot_LikeOriginal");
                _c2BuildRuntimeRootLikeOriginal.transform.SetParent(transform, false);
            }

            site = new GameObject("C2_RuntimeConstructionSite_" + C2Settlement3InuMdV2SanitizeNameLikeOriginal(string.IsNullOrEmpty(unitId) ? mdName : unitId));
            site.transform.SetParent(_c2BuildRuntimeRootLikeOriginal.transform, false);

            C2RuntimeConstructionSiteLikeOriginal behaviour = site.AddComponent<C2RuntimeConstructionSiteLikeOriginal>();
            behaviour.Initialize(this, mdName, unitId, nation, realX, realY, source);

            audit = "contract=" + C2BuildRuntimeContractLikeOriginal +
                    " created md='" + (md.MdPath ?? string.Empty) + "'" +
                    " name='" + (mdName ?? string.Empty) + "'" +
                    " unit='" + (unitId ?? string.Empty) + "'" +
                    " real=(" + realX.ToString(CultureInfo.InvariantCulture) + "," + realY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " buildStages=" + md.BuildStages.ToString(CultureInfo.InvariantCulture) +
                    " source='" + (source ?? string.Empty) + "'";
            return true;
        }

        public int C2BuildRuntimeAssignSelectedBuildersLikeOriginal(GameObject site, int realX, int realY, string source, out string audit)
        {
            return C2BuildRuntimeAssignBuildersSnapshotLikeOriginal(site, realX, realY, null, source, out audit);
        }

        public static int C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, string source)
        {
            if (unit == null) return 0;
            C2BuildWorkerOrderLikeOriginal order = unit.GetComponent<C2BuildWorkerOrderLikeOriginal>();
            if (order == null || !order.enabled) return 0;
            order.CancelFromExternalOrderLikeOriginal(source ?? "external_order");
            return 1;
        }

        public static bool C2BuildRuntimeUnitCanBuildLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit)
        {
            if (unit == null || !unit.isActiveAndEnabled || !unit.CanReceiveOrdersLikeOriginal())
                return false;

            string id = ((unit.SourceMonsterId ?? string.Empty) + " " + (unit.ResolvedMd ?? string.Empty)).ToLowerInvariant();
            return id.IndexOf("kri", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   id.IndexOf("peasant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   id.IndexOf("worker", StringComparison.OrdinalIgnoreCase) >= 0;
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
            C2RuntimeConstructionSiteLikeOriginal construction = site != null ? site.GetComponent<C2RuntimeConstructionSiteLikeOriginal>() : null;
            if (construction == null)
            {
                audit = "no_runtime_construction_site";
                return 0;
            }

            if (construction.ReadyLikeOriginal || construction.DeadLikeOriginal)
            {
                audit = "assigned=0 reason=site_not_buildable ready=" + construction.ReadyLikeOriginal +
                        " dead=" + construction.DeadLikeOriginal +
                        " source='" + (source ?? string.Empty) + "'";
                return 0;
            }

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> selected = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(32);
            int snapshotSeen = 0;
            int snapshotAccepted = 0;
            int liveSeen = 0;
            int liveAccepted = 0;

            if (builderSnapshot != null)
            {
                for (int i = 0; i < builderSnapshot.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = builderSnapshot[i];
                    snapshotSeen++;
                    if (!C2BuildRuntimeUnitCanBuildLikeOriginal(u))
                        continue;
                    if (selected.Contains(u))
                        continue;
                    selected.Add(u);
                    snapshotAccepted++;
                }
            }

            // V44: placing the building is a map click. In our Unity selection layer that click may clear IsSelected
            // before ConfirmConstruction calls into runtime. Original BuildWithSelected uses the selection captured
            // when BuildMode was entered, so snapshot must win. If snapshot is empty, fall back to the old live scan.
            bool usedLiveFallback = selected.Count == 0;
            if (usedLiveFallback)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal[] all = FindObjectsOfType<C2NeutralPeasantUnitInfoV2LikeOriginal>();
                for (int i = 0; all != null && i < all.Length; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                    liveSeen++;
                    if (u == null || !u.IsSelected || !C2BuildRuntimeUnitCanBuildLikeOriginal(u))
                        continue;
                    if (selected.Contains(u))
                        continue;
                    selected.Add(u);
                    liveAccepted++;
                }
            }

            if (selected.Count == 0)
            {
                audit = "assigned=0 real=(" + realX.ToString(CultureInfo.InvariantCulture) + "," + realY.ToString(CultureInfo.InvariantCulture) + ")" +
                        " buildPoints=" + construction.BuildPointCountLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                        " source='" + (source ?? string.Empty) + "'" +
                        " snapshotSeen=" + snapshotSeen.ToString(CultureInfo.InvariantCulture) +
                        " snapshotAccepted=" + snapshotAccepted.ToString(CultureInfo.InvariantCulture) +
                        " liveFallback=" + usedLiveFallback +
                        " liveSeen=" + liveSeen.ToString(CultureInfo.InvariantCulture) +
                        " liveAccepted=" + liveAccepted.ToString(CultureInfo.InvariantCulture) +
                        " reason='no_builder_snapshot_and_no_live_selected'";
                return 0;
            }

            selected.Sort((a, b) =>
            {
                float da = C2BuildRuntimeRealDistanceSqLikeOriginal(a.RealXFloat, a.RealYFloat, realX, realY);
                float db = C2BuildRuntimeRealDistanceSqLikeOriginal(b.RealXFloat, b.RealYFloat, realX, realY);
                return da.CompareTo(db);
            });

            // V44: original BuildObj uses FindPoint(... FP_FIND_WORKPOINT | FP_UNLOCKED_POINT)
            // and BSetPt locks the chosen work point during assignment. If no free BUILDPOINT exists,
            // BuildObj returns false and that peasant is not sent. Therefore one real BUILDPOINT/cell
            // may be occupied by only one builder order; extra selected peasants are skipped.
            HashSet<int> usedBuildPoints = new HashSet<int>();
            HashSet<long> usedBuildPointCells = new HashSet<long>();
            construction.FillReservedBuildPointsLikeOriginal(usedBuildPoints, usedBuildPointCells);
            int alreadyReservedBuildPoints = usedBuildPoints.Count;
            int assigned = 0;
            int skippedNoFreeBuildPoint = 0;
            StringBuilderLite auditSlots = new StringBuilderLite();
            StringBuilderLite auditSkipped = new StringBuilderLite();
            for (int i = 0; i < selected.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = selected[i];
                if (u == null) continue;

                int targetRealX;
                int targetRealY;
                int buildPointIndex;
                long buildPointCellKey;
                string pointAudit;
                bool hasPoint = construction.TryGetNearestFreeBuildPointForWorkerLikeOriginal(
                    u.RealXFloat,
                    u.RealYFloat,
                    usedBuildPoints,
                    usedBuildPointCells,
                    out buildPointIndex,
                    out buildPointCellKey,
                    out targetRealX,
                    out targetRealY,
                    out pointAudit);

                if (!hasPoint)
                {
                    skippedNoFreeBuildPoint++;
                    if (skippedNoFreeBuildPoint <= 12)
                    {
                        if (auditSkipped.Length > 0) auditSkipped.Append(";");
                        auditSkipped.Append("#").Append(i).Append(" ").Append(pointAudit);
                    }
                    continue;
                }

                C2GameplayUnitTaskV1 oldTask = u.GetComponent<C2GameplayUnitTaskV1>();
                if (oldTask != null) oldTask.enabled = false;

                // Reassigning a worker from another building must first detach from the old BuildObjLink.
                C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(u, "reassign_to_building_v67");

                construction.ReserveBuildPointForWorkerLikeOriginal(u, buildPointIndex, buildPointCellKey);

                C2BuildWorkerOrderLikeOriginal order = u.GetComponent<C2BuildWorkerOrderLikeOriginal>();
                if (order == null) order = u.gameObject.AddComponent<C2BuildWorkerOrderLikeOriginal>();
                // V44: if this worker finished a previous building, the component exists but is disabled.
                // Re-enable it before Begin(), otherwise later buildings are assigned in logs but ignored in-game.
                order.enabled = true;
                order.Begin(u, construction, targetRealX, targetRealY, buildPointIndex, buildPointCellKey, assigned, pointAudit, source);

                if (assigned < 12)
                {
                    if (auditSlots.Length > 0) auditSlots.Append(";");
                    auditSlots.Append("#").Append(assigned).Append(" ").Append(pointAudit);
                }
                assigned++;
            }

            audit = "assigned=" + assigned.ToString(CultureInfo.InvariantCulture) +
                    " real=(" + realX.ToString(CultureInfo.InvariantCulture) + "," + realY.ToString(CultureInfo.InvariantCulture) + ")" +
                    " buildPoints=" + construction.BuildPointCountLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                    " alreadyReservedBuildPoints=" + alreadyReservedBuildPoints.ToString(CultureInfo.InvariantCulture) +
                    " skippedNoFreeBuildPoint=" + skippedNoFreeBuildPoint.ToString(CultureInfo.InvariantCulture) +
                    " source='" + (source ?? string.Empty) + "'" +
                    " snapshotSeen=" + snapshotSeen.ToString(CultureInfo.InvariantCulture) +
                    " snapshotAccepted=" + snapshotAccepted.ToString(CultureInfo.InvariantCulture) +
                    " liveFallback=" + usedLiveFallback +
                    " liveSeen=" + liveSeen.ToString(CultureInfo.InvariantCulture) +
                    " liveAccepted=" + liveAccepted.ToString(CultureInfo.InvariantCulture) +
                    " slots='" + auditSlots.ToString() + "'" +
                    " skippedSlots='" + auditSkipped.ToString() + "'";
            return assigned;
        }

        private static float C2BuildRuntimeRealDistanceSqLikeOriginal(float ax, float ay, float bx, float by)
        {
            float dx = ax - bx;
            float dy = ay - by;
            return dx * dx + dy * dy;
        }

        private static byte C2BuildRuntimeDirectionFromRealDeltaLikeOriginal(float dx, float dy)
        {
            if (Mathf.Abs(dx) < 0.0001f && Mathf.Abs(dy) < 0.0001f) return 0;
            float angle = Mathf.Atan2(-dy, dx) * Mathf.Rad2Deg;
            int raw = Mathf.RoundToInt(Mathf.Repeat(angle / 360.0f * 256.0f, 256.0f));
            return (byte)(raw & 255);
        }

        private sealed class StringBuilderLite
        {
            private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder(256);
            public int Length { get { return _sb.Length; } }
            public StringBuilderLite Append(string v) { _sb.Append(v); return this; }
            public StringBuilderLite Append(int v) { _sb.Append(v.ToString(CultureInfo.InvariantCulture)); return this; }
            public override string ToString() { return _sb.ToString(); }
        }

        private sealed class C2BuildWorkerOrderLikeOriginal : MonoBehaviour
        {
            private C2NeutralPeasantUnitInfoV2LikeOriginal _unit;
            private C2RuntimeConstructionSiteLikeOriginal _site;
            private int _targetRealX;
            private int _targetRealY;
            private int _buildPointIndex = -1;
            private long _buildPointCellKey;
            private int _slotIndex;
            private float _nextRepathAt;
            private float _nextWorkAt;
            private float _workPhase;
            private bool _arrived;
            private bool _working;
            // V45: original BuildObjLink rotates once toward the target building and then uses that
            // stable RealDir/GraphDir while anm_Work is playing. Recomputing direction every Unity
            // frame from small floating-point position changes made the worker flicker between banks.
            private bool _hasLockedWorkDir;
            private byte _lockedWorkDir;
            private int _lastWorkFrameIndex = -1;
            private string _pointAudit = string.Empty;
            private string _source = string.Empty;

            public void Begin(
                C2NeutralPeasantUnitInfoV2LikeOriginal unit,
                C2RuntimeConstructionSiteLikeOriginal site,
                int targetRealX,
                int targetRealY,
                int buildPointIndex,
                long buildPointCellKey,
                int slotIndex,
                string pointAudit,
                string source)
            {
                enabled = true;
                _unit = unit != null ? unit : GetComponent<C2NeutralPeasantUnitInfoV2LikeOriginal>();
                _site = site;
                _targetRealX = targetRealX;
                _targetRealY = targetRealY;
                _buildPointIndex = buildPointIndex;
                _buildPointCellKey = buildPointCellKey;
                _slotIndex = slotIndex;
                _pointAudit = pointAudit ?? string.Empty;
                _source = source ?? string.Empty;
                _arrived = false;
                _working = false;
                _hasLockedWorkDir = false;
                _lockedWorkDir = 0;
                _workPhase = 0.0f;
                _lastWorkFrameIndex = -1;
                _nextRepathAt = 0.0f;
                _nextWorkAt = Time.realtimeSinceStartup + 0.20f;

                if (_unit != null)
                {
                    byte face = _site != null ? _site.DirectionFromPointToBuildingLikeOriginal(_targetRealX, _targetRealY) : (byte)0;
                    _unit.SetMoveDestinationRealLikeOriginal(
                        _targetRealX,
                        _targetRealY,
                        C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                        true,
                        face);
                }
            }

            public void CancelFromExternalOrderLikeOriginal(string reason)
            {
                FinishOrderLikeOriginal("cancel_" + (reason ?? "external_order"));
            }

            private void Update()
            {
                if (_unit == null || _site == null || !_unit.isActiveAndEnabled)
                {
                    enabled = false;
                    return;
                }

                if (_site.ReadyLikeOriginal || _site.DeadLikeOriginal)
                {
                    FinishOrderLikeOriginal("site_finished");
                    return;
                }

                float dx = _unit.RealXFloat - _targetRealX;
                float dy = _unit.RealYFloat - _targetRealY;
                float distReal = Mathf.Sqrt(dx * dx + dy * dy);

                // Original BuildObjLink works in map cells and starts building when dst<=1 cell.
                // Unity movement snaps within ~64 real units, so <=96 keeps the same practical threshold
                // without forcing the worker to fight its own movement redirect every frame.
                bool near = distReal <= 96.0f;
                if (!near)
                {
                    _arrived = false;
                    _working = false;
                    _hasLockedWorkDir = false;
                    if (_unit.SpriteAnimator != null) _unit.SpriteAnimator.StopWorkAnimationLikeOriginal();
                    if (Time.realtimeSinceStartup >= _nextRepathAt)
                    {
                        _nextRepathAt = Time.realtimeSinceStartup + 1.25f;
                        byte face = _site.DirectionFromPointToBuildingLikeOriginal(_targetRealX, _targetRealY);
                        _unit.SetMoveDestinationRealLikeOriginal(
                            _targetRealX,
                            _targetRealY,
                            C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                            true,
                            face);
                    }
                    return;
                }

                if (!_hasLockedWorkDir)
                {
                    // Stable original-facing direction: from the assigned BUILDPOINT to the building center.
                    // Do not recompute this from the moving sprite every frame.
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
                    _nextWorkAt = Time.realtimeSinceStartup + 0.18f;
                    _unit.StopMoveAndFaceDirectionLikeOriginal(dir);
                    return;
                }

                if (Time.realtimeSinceStartup < _nextWorkAt)
                    return;

                if (!_working)
                    _unit.StopMoveAndFaceDirectionLikeOriginal(dir);
                _working = true;

                int workFrames = _unit.SpriteAnimator != null ? _unit.SpriteAnimator.GetWorkFrameCountLikeOriginal(dir) : 0;
                if (workFrames <= 0) workFrames = _site.WorkFrameCountLikeOriginal;
                workFrames = Mathf.Max(1, workFrames);

                float beforePhase = _workPhase;
                _workPhase += Time.deltaTime * _site.WorkFpsLikeOriginal;
                int frameIndex = Mathf.FloorToInt(_workPhase) % workFrames;
                if (frameIndex < 0) frameIndex += workFrames;

                bool displayedWork = false;
                if (_unit.SpriteAnimator != null)
                    displayedWork = _unit.SpriteAnimator.SetWorkFramePhaseLikeOriginal(dir, _workPhase, frameIndex != _lastWorkFrameIndex);
                _lastWorkFrameIndex = frameIndex;

                bool cycleFinished = Mathf.FloorToInt(beforePhase / workFrames) != Mathf.FloorToInt(_workPhase / workFrames);
                if (!cycleFinished)
                    return;

                _workPhase = 0.0f;
                _lastWorkFrameIndex = -1;
                bool advanced = _site.NextStageFromWorkerLikeOriginal(_unit, _slotIndex, _pointAudit);
                if (!advanced)
                    FinishOrderLikeOriginal("nextstage_false");
            }

            private void FinishOrderLikeOriginal(string reason)
            {
                C2RuntimeConstructionSiteLikeOriginal site = _site;
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = _unit;

                if (site != null && _buildPointIndex >= 0)
                    site.ReleaseBuildPointForWorkerLikeOriginal(unit, _buildPointIndex, _buildPointCellKey, reason ?? string.Empty);

                if (unit != null && unit.SpriteAnimator != null)
                {
                    unit.SpriteAnimator.StopWorkAnimationLikeOriginal();
                    unit.SpriteAnimator.SetMovingLikeOriginal(false);
                }
                _site = null;
                _buildPointIndex = -1;
                _buildPointCellKey = 0;
                enabled = false;
            }
        }

        private static C2Settlement3InuMdV2Kind C2BuildRuntimeKindLikeOriginal(C2Settlement3InuMdV2Info md)
        {
            C2Settlement3InuMdV2Kind kind = md != null ? md.Kind : C2Settlement3InuMdV2Kind.Unknown;
            if (kind == C2Settlement3InuMdV2Kind.SettlementBuilding ||
                kind == C2Settlement3InuMdV2Kind.Building ||
                kind == C2Settlement3InuMdV2Kind.ResourceBuilding ||
                kind == C2Settlement3InuMdV2Kind.SpriteObject)
                return kind;
            return C2Settlement3InuMdV2Kind.Building;
        }

        private static C2Settlement3InuMdV2Record C2BuildRuntimeRecordLikeOriginal(string mdName, int nation, int realX, int realY, int builtStage, ushort life)
        {
            C2Settlement3InuMdV2Record r = new C2Settlement3InuMdV2Record();
            r.Index = s_C2BuildRuntimeNextIndexLikeOriginal++;
            r.Nation = (byte)Mathf.Clamp(nation, 0, 255);
            r.NIndex = 0;
            r.RealX = realX;
            r.RealY = realY;
            r.Life = life;
            r.Stage = C2BuildRuntimeSavedStageFromBuildProgressLikeOriginal(builtStage);
            r.WallX = 0;
            r.WallY = 0;
            r.RealDir = 0;
            r.Flags = 0;
            r.MonsterId = mdName ?? string.Empty;
            return r;
        }

        private static ushort C2BuildRuntimeSavedStageFromBuildProgressLikeOriginal(int builtStage)
        {
            if (builtStage <= 0)
            {
                if (builtStage < 0)
                    return 0;
                return 0xFFFF;
            }
            if (builtStage >= 0x7FFE)
                return 0;
            int saved = Mathf.Clamp(0xFFFF - builtStage, 0x8001, 0xFFFF);
            return (ushort)saved;
        }

        private static void C2BuildRuntimeClearChildrenLikeOriginal(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                if (Application.isPlaying) UnityEngine.Object.Destroy(child);
                else UnityEngine.Object.DestroyImmediate(child);
            }
        }

        private static void C2BuildRuntimeApplyGhostTintLikeOriginal(Transform root, bool valid)
        {
            Color c = valid ? new Color(1.0f, 1.0f, 1.0f, 0.78f) : new Color(1.0f, 0.0f, 0.0f, 0.62f);
            Renderer[] rr = root != null ? root.GetComponentsInChildren<Renderer>(true) : null;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            for (int i = 0; rr != null && i < rr.Length; i++)
            {
                Renderer r = rr[i];
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_Color", c);
                block.SetColor("_BaseColor", c);
                r.SetPropertyBlock(block);
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        private static void C2BuildRuntimeDisablePreviewSelectionLikeOriginal(Transform root)
        {
            C2SettlementBuildingSelectableV1LikeOriginal[] selectables = root != null ? root.GetComponentsInChildren<C2SettlementBuildingSelectableV1LikeOriginal>(true) : null;
            for (int i = 0; selectables != null && i < selectables.Length; i++)
            {
                if (selectables[i] != null)
                {
                    selectables[i].SetSuppressVisualResetOnDisableV111LikeOriginal(true);
                    selectables[i].SetHovered(false);
                    selectables[i].SetSelected(false);
                    selectables[i].enabled = false;
                }
            }
        }

        private int C2BuildRuntimeRegisterFoundationLocksLikeOriginal(string mdName, int nation, int realX, int realY, int stableRecordIndex)
        {
            C2Settlement3InuMdV2Info md = C2Settlement3InuMdV2ResolveMdLikeOriginal(mdName);
            if (md == null || !md.Found || md.Zones == null)
                return 0;

            C2Settlement3InuMdV2Record r = C2BuildRuntimeRecordLikeOriginal(mdName, nation, realX, realY, 0, 2);
            if (stableRecordIndex >= 0)
                r.Index = stableRecordIndex;

            C2Settlement3InuMdV2Kind kind = C2BuildRuntimeKindLikeOriginal(md);

            // Original CreateNewMonsterAt locks BUILDLOCKPOINTS while NewBuilding is not ready.
            // This keeps the newly placed foundation from accepting another building on top of it.
            int removedOld = C2Settlement3InuMdV2RemoveMotionBlocksByRecordIndexLikeOriginal(r.Index);
            C2Settlement3InuMdV2RegisterBuildingZonesLikeOriginal(r, md, kind, true);
            C2Settlement3InuMdV2RefreshPassabilityOverlayLikeOriginal("runtime_foundation_buildlock md='" + (mdName ?? string.Empty) + "' removedOld=" + removedOld.ToString(CultureInfo.InvariantCulture));
            return md.Zones.BuildLockPoints != null && md.Zones.BuildLockPoints.Count > 0
                ? md.Zones.BuildLockPoints.Count
                : md.Zones.LockPoints.Count;
        }

        private int C2BuildRuntimeRegisterReadyLocksLikeOriginal(string mdName, int nation, int realX, int realY, int stableRecordIndex)
        {
            C2Settlement3InuMdV2Info md = C2Settlement3InuMdV2ResolveMdLikeOriginal(mdName);
            if (md == null || !md.Found || md.Zones == null)
                return 0;

            C2Settlement3InuMdV2Record r = C2BuildRuntimeRecordLikeOriginal(mdName, nation, realX, realY, 0, 1);
            if (stableRecordIndex >= 0)
                r.Index = stableRecordIndex;

            C2Settlement3InuMdV2Kind kind = C2BuildRuntimeKindLikeOriginal(md);

            // Stage >= ProduceStages: original stops using BUILDLOCKPOINTS and switches to final LOCKPOINTS.
            int removedBuildLock = C2Settlement3InuMdV2RemoveMotionBlocksByRecordIndexLikeOriginal(r.Index);
            C2Settlement3InuMdV2RegisterBuildingZonesLikeOriginal(r, md, kind, false);
            C2Settlement3InuMdV2RefreshPassabilityOverlayLikeOriginal("runtime_ready_lockpoints md='" + (mdName ?? string.Empty) + "' removedBuildLock=" + removedBuildLock.ToString(CultureInfo.InvariantCulture));
            return md.Zones.LockPoints != null && md.Zones.LockPoints.Count > 0
                ? md.Zones.LockPoints.Count
                : md.Zones.BuildLockPoints.Count;
        }

        private sealed class C2RuntimeConstructionSiteLikeOriginal : MonoBehaviour
        {
            private C2BattleTerrainMode _mode;
            private string _mdName = string.Empty;
            private string _unitId = string.Empty;
            private string _source = string.Empty;
            private int _nation;
            private int _realX;
            private int _realY;
            private int _buildStages = 64;
            private int _stage;
            private int _life;
            private bool _ready;
            private bool _dead;
            private int _lockedBuildCells;
            private int _finalLockCells;
            private Transform _visualRoot;
            private bool _complete;
            private int _lastVisualPhase = -1;
            private int _nextVisualStatus = -1;
            private float _lastNextStageLogAt;
            private C2Settlement3InuMdV2Info _md;
            private C2Settlement3InuMdV2Record _recordForPoints;
            private int _cornerX;
            private int _cornerY;
            private int _workerWorkFrames = 9;
            private int _workerWorkRotations = 9;
            private readonly HashSet<int> _reservedBuildPointIndices = new HashSet<int>();
            private readonly HashSet<long> _reservedBuildPointCells = new HashSet<long>();
            private readonly Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, int> _workerReservedIndex = new Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, int>();
            private readonly Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, long> _workerReservedCell = new Dictionary<C2NeutralPeasantUnitInfoV2LikeOriginal, long>();
            private C2RuntimeConstructionSiteProxyLikeOriginal _proxy;

            public bool ReadyLikeOriginal { get { return _ready; } }
            public bool DeadLikeOriginal { get { return _dead; } }
            public int BuildPointCountLikeOriginal
            {
                get { return _md != null && _md.Zones != null && _md.Zones.BuildPoints != null ? _md.Zones.BuildPoints.Count : 0; }
            }
            public int WorkFrameCountLikeOriginal
            {
                get { return Mathf.Max(1, _workerWorkFrames); }
            }

            public float WorkFpsLikeOriginal
            {
                get { return 12.0f; }
            }

            public float WorkCycleSecondsLikeOriginal
            {
                get { return Mathf.Clamp(WorkFrameCountLikeOriginal / WorkFpsLikeOriginal, 0.25f, 2.0f); }
            }

            public void Initialize(C2BattleTerrainMode mode, string mdName, string unitId, int nation, int realX, int realY, string source)
            {
                _mode = mode;
                _mdName = mdName ?? string.Empty;
                _unitId = unitId ?? string.Empty;
                _nation = nation;
                _realX = realX;
                _realY = realY;
                _source = source ?? string.Empty;

                GameObject visual = new GameObject("visual");
                visual.transform.SetParent(transform, false);
                _visualRoot = visual.transform;

                _md = _mode != null ? C2Settlement3InuMdV2ResolveMdLikeOriginal(_mdName) : null;
                _buildStages = _md != null && _md.BuildStages > 0 ? _md.BuildStages : 64;
                _recordForPoints = C2BuildRuntimeRecordLikeOriginal(_mdName, _nation, _realX, _realY, 0, 2);
                if (_md != null)
                    C2Settlement3InuMdV2BuildingCornerCellLikeOriginal(_recordForPoints, _md, out _cornerX, out _cornerY);

                // V82: original Nation::CreateNewMonsterAt applies NewMonster::PieceName immediately
                // via RM_GetObjVector + RM_LoadNotObj before the building body appears.
                // Do it once for the runtime construction site, not for mouse-preview ghost.
                if (_mode != null && _md != null)
                    _mode.C2SmpRuntimeApplyBuildingPieceFromMdLikeOriginal(_md, _recordForPoints, _realX, _realY, "construction_start");

                _proxy = gameObject.GetComponent<C2RuntimeConstructionSiteProxyLikeOriginal>();
                if (_proxy == null) _proxy = gameObject.AddComponent<C2RuntimeConstructionSiteProxyLikeOriginal>();

                // Original Nation::CreateNewMonsterAt starts a NewBuilding as foundation:
                // Stage=0, Life=2, Ready=false. Progress happens later through BuildObjLink -> OB->NextStage().
                _stage = 0;
                _life = 2;
                _ready = false;
                _dead = false;
                _complete = false;
                SyncProxyLikeOriginal();

                RebuildVisualLikeOriginal("init_foundation");
                _lockedBuildCells = _mode != null ? _mode.C2BuildRuntimeRegisterFoundationLocksLikeOriginal(_mdName, _nation, _realX, _realY, _recordForPoints.Index) : 0;
            }

            public void FillReservedBuildPointsLikeOriginal(HashSet<int> usedBuildPoints, HashSet<long> usedBuildPointCells)
            {
                if (usedBuildPoints != null)
                {
                    foreach (int idx in _reservedBuildPointIndices)
                        usedBuildPoints.Add(idx);
                }

                if (usedBuildPointCells != null)
                {
                    foreach (long key in _reservedBuildPointCells)
                        usedBuildPointCells.Add(key);
                }
            }

            public void ReserveBuildPointForWorkerLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal worker, int buildPointIndex, long buildPointCellKey)
            {
                if (buildPointIndex < 0) return;

                _reservedBuildPointIndices.Add(buildPointIndex);
                _reservedBuildPointCells.Add(buildPointCellKey);

                if (worker != null)
                {
                    _workerReservedIndex[worker] = buildPointIndex;
                    _workerReservedCell[worker] = buildPointCellKey;
                }
            }

            public void ReleaseBuildPointForWorkerLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal worker, int buildPointIndex, long buildPointCellKey, string reason)
            {
                if (worker != null)
                {
                    int idx;
                    if (_workerReservedIndex.TryGetValue(worker, out idx))
                    {
                        _reservedBuildPointIndices.Remove(idx);
                        _workerReservedIndex.Remove(worker);
                    }

                    long key;
                    if (_workerReservedCell.TryGetValue(worker, out key))
                    {
                        _reservedBuildPointCells.Remove(key);
                        _workerReservedCell.Remove(worker);
                    }
                }
                else
                {
                    if (buildPointIndex >= 0) _reservedBuildPointIndices.Remove(buildPointIndex);
                    if (buildPointCellKey != 0) _reservedBuildPointCells.Remove(buildPointCellKey);
                }
            }

            public bool TryGetNearestFreeBuildPointForWorkerLikeOriginal(
                float workerRealX,
                float workerRealY,
                HashSet<int> usedBuildPoints,
                HashSet<long> usedBuildPointCells,
                out int buildPointIndex,
                out long buildPointCellKey,
                out int targetRealX,
                out int targetRealY,
                out string audit)
            {
                buildPointIndex = -1;
                buildPointCellKey = 0;
                targetRealX = _realX;
                targetRealY = _realY;
                audit = "no_free_buildpoint";

                int count = BuildPointCountLikeOriginal;
                if (_md == null || _md.Zones == null || count <= 0)
                {
                    audit = "no_buildpoints_in_md";
                    return false;
                }

                int bestIdx = -1;
                int bestCellX = 0;
                int bestCellY = 0;
                float bestDist = float.PositiveInfinity;

                for (int i = 0; i < count; i++)
                {
                    if (usedBuildPoints != null && usedBuildPoints.Contains(i))
                        continue;

                    var p = _md.Zones.BuildPoints[i];
                    int cellX = _cornerX + p.X;
                    int cellY = _cornerY + p.Y;
                    long cellKey = (((long)cellX) << 32) ^ (uint)cellY;
                    if (usedBuildPointCells != null && usedBuildPointCells.Contains(cellKey))
                        continue;

                    int rx = cellX << 8;
                    int ry = cellY << 8;
                    bool blocked = C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedRealLikeOriginal(rx, ry);
                    if (blocked)
                        continue;

                    float d = C2BuildRuntimeRealDistanceSqLikeOriginal(workerRealX, workerRealY, rx, ry);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestIdx = i;
                        bestCellX = cellX;
                        bestCellY = cellY;
                    }
                }

                if (bestIdx < 0)
                {
                    int usedIdx = usedBuildPoints != null ? usedBuildPoints.Count : 0;
                    int usedCell = usedBuildPointCells != null ? usedBuildPointCells.Count : 0;
                    audit = "no_free_unique_BUILDPOINT count=" + count.ToString(CultureInfo.InvariantCulture) +
                            " usedIndex=" + usedIdx.ToString(CultureInfo.InvariantCulture) +
                            " usedCell=" + usedCell.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                long bestKey = (((long)bestCellX) << 32) ^ (uint)bestCellY;
                if (usedBuildPoints != null) usedBuildPoints.Add(bestIdx);
                if (usedBuildPointCells != null) usedBuildPointCells.Add(bestKey);

                buildPointIndex = bestIdx;
                buildPointCellKey = bestKey;
                targetRealX = bestCellX << 8;
                targetRealY = bestCellY << 8;
                var raw = _md.Zones.BuildPoints[bestIdx];
                audit = "BUILDPOINTS[" + bestIdx.ToString(CultureInfo.InvariantCulture) + "] cell=" +
                        bestCellX.ToString(CultureInfo.InvariantCulture) + "/" + bestCellY.ToString(CultureInfo.InvariantCulture) +
                        " raw=" + raw.X.ToString(CultureInfo.InvariantCulture) + "/" + raw.Y.ToString(CultureInfo.InvariantCulture) +
                        " nearestToWorkerReal=(" + Mathf.RoundToInt(workerRealX).ToString(CultureInfo.InvariantCulture) + "," + Mathf.RoundToInt(workerRealY).ToString(CultureInfo.InvariantCulture) + ")" +
                        " unique=True blocked=False";
                return true;
            }

            public void GetNearestBuildPointForWorkerLikeOriginal(
                float workerRealX,
                float workerRealY,
                HashSet<int> usedBuildPoints,
                out int targetRealX,
                out int targetRealY,
                out string audit)
            {
                HashSet<long> usedCells = new HashSet<long>();
                int dummyIdx;
                long dummyKey;
                TryGetNearestFreeBuildPointForWorkerLikeOriginal(
                    workerRealX,
                    workerRealY,
                    usedBuildPoints,
                    usedCells,
                    out dummyIdx,
                    out dummyKey,
                    out targetRealX,
                    out targetRealY,
                    out audit);
            }

            public void GetBuildPointForWorkerLikeOriginal(int workerIndex, out int targetRealX, out int targetRealY, out string audit)
            {
                targetRealX = _realX;
                targetRealY = _realY;
                audit = "fallback_center";

                int count = BuildPointCountLikeOriginal;
                if (_md != null && _md.Zones != null && count > 0)
                {
                    int idx = Mathf.Abs(workerIndex) % count;
                    var p = _md.Zones.BuildPoints[idx];
                    int cellX = _cornerX + p.X;
                    int cellY = _cornerY + p.Y;
                    // V42: the cell logged here is the same placement/footprint cell used by the
                    // construction validator (real >> 8). V40 incorrectly used << 4 and sent
                    // peasants to coordinates near the map origin: e.g. cell=79/536 became
                    // targetReal=1264/8576 instead of 20224/137216. That caused long/impossible
                    // paths and the 1 FPS freeze after placing a foundation.
                    targetRealX = cellX << 8;
                    targetRealY = cellY << 8;
                    audit = "BUILDPOINTS[" + idx.ToString(CultureInfo.InvariantCulture) + "] cell=" +
                            cellX.ToString(CultureInfo.InvariantCulture) + "/" + cellY.ToString(CultureInfo.InvariantCulture) +
                            " raw=" + p.X.ToString(CultureInfo.InvariantCulture) + "/" + p.Y.ToString(CultureInfo.InvariantCulture);
                    return;
                }

                // Safe fallback for malformed MD: ring around building center. Normal Cossacks 2 buildings have BUILDPOINTS.
                int ring = workerIndex / 8;
                int pos = workerIndex % 8;
                int radius = 18 + ring * 7;
                int ox = 0;
                int oy = 0;
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
                // V42: fallback ring also works in validator cells, not 1/16-real pixels.
                int baseCellX = _realX >> 8;
                int baseCellY = _realY >> 8;
                targetRealX = (baseCellX + ox) << 8;
                targetRealY = (baseCellY + oy) << 8;
                audit = "fallback_ring cell=" + (targetRealX >> 8).ToString(CultureInfo.InvariantCulture) + "/" +
                        (targetRealY >> 8).ToString(CultureInfo.InvariantCulture);
            }

            private static byte C2BuildRuntimeMirrorLeftRightDirectionLikeOriginal(byte dir)
            {
                // V47: after V46 the vertical facing was correct, but left/right banks were mirrored.
                // Mirror across the vertical screen axis: up/down stay unchanged, left/right swap.
                return (byte)((128 - dir) & 255);
            }

            private static byte C2BuildRuntimeWorkBankDirectionLikeOriginal(byte rawDir)
            {
                // Keep V46 face-toward-building correction, then mirror X only.
                // Equivalent: mirrorX(raw + 128). This preserves up/down and fixes right<->left.
                byte faceToward = (byte)((rawDir + 128) & 255);
                return C2BuildRuntimeMirrorLeftRightDirectionLikeOriginal(faceToward);
            }

            public byte DirectionFromPointToBuildingLikeOriginal(int pointRealX, int pointRealY)
            {
                // Original BuildObjLink faces the builder toward the building:
                //     GetDir(OB->RealX - OBJ->RealX, OB->RealY - OBJ->RealY)
                // Unity bank mapping is horizontally mirrored for @WORK: vertical directions were already correct,
                // but right/left were swapped. Do not unlock/recompute; only correct the locked sprite bank.
                byte raw = C2BuildRuntimeDirectionFromRealDeltaLikeOriginal(_realX - pointRealX, _realY - pointRealY);
                return C2BuildRuntimeWorkBankDirectionLikeOriginal(raw);
            }

            public byte DirectionFromWorkerToBuildingLikeOriginal(float workerRealX, float workerRealY)
            {
                byte raw = C2BuildRuntimeDirectionFromRealDeltaLikeOriginal(_realX - workerRealX, _realY - workerRealY);
                return C2BuildRuntimeWorkBankDirectionLikeOriginal(raw);
            }

            public bool NextStageFromWorkerLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal worker, int slotIndex, string pointAudit)
            {
                if (_ready || _dead || _complete || _mode == null)
                    return false;

                int beforeStage = _stage;
                int beforeStatus = ConstructionStatusLikeOriginal;
                _stage = Mathf.Clamp(_stage + 1, 0, Mathf.Max(1, _buildStages));
                if (_stage >= _buildStages)
                {
                    _stage = _buildStages;
                    _ready = true;
                    _complete = true;
                    _life = 1;
                    // Ready building uses final LOCKPOINTS in the original; BUILDLOCKPOINTS were temporary.
                    _finalLockCells = _mode != null ? _mode.C2BuildRuntimeRegisterReadyLocksLikeOriginal(_mdName, _nation, _realX, _realY, _recordForPoints.Index) : 0;
                }
                else
                {
                    _life = 2;
                }

                SyncProxyLikeOriginal();

                int afterStatus = ConstructionStatusLikeOriginal;
                int afterVisualPhase = VisualBuildPhaseLikeOriginal;
                bool visualChanged = _ready || afterVisualPhase != _lastVisualPhase;
                if (visualChanged)
                    RebuildVisualLikeOriginal(_ready ? "complete_ready" : "nextstage_phase_" + afterVisualPhase.ToString(CultureInfo.InvariantCulture));

                if (Time.realtimeSinceStartup - _lastNextStageLogAt > 0.25f || visualChanged)
                {
                    _lastNextStageLogAt = Time.realtimeSinceStartup;
                }
                return !_ready;
            }

            private int ConstructionStatusLikeOriginal
            {
                get
                {
                    if (_dead) return 4;
                    if (_ready || _complete || _stage >= _buildStages) return 3;
                    if (_stage <= 0) return 0;
                    if (_stage * 4 >= _buildStages * 3) return 2;
                    return 1;
                }
            }

            private int VisualBuildPhaseLikeOriginal
            {
                get
                {
                    if (_dead || _ready || _complete || _stage >= _buildStages) return 3;
                    return Mathf.Clamp((_stage * 4) / Mathf.Max(1, _buildStages), 0, 3);
                }
            }

            private void SyncProxyLikeOriginal()
            {
                if (_proxy == null)
                {
                    _proxy = gameObject.GetComponent<C2RuntimeConstructionSiteProxyLikeOriginal>();
                    if (_proxy == null) _proxy = gameObject.AddComponent<C2RuntimeConstructionSiteProxyLikeOriginal>();
                }

                if (_proxy != null)
                    _proxy.Configure(_mode, _mdName, _unitId, _nation, _realX, _realY, _buildStages, _stage, _ready, _dead, _md != null && _md.NotSelectable);
            }

            private void RebuildVisualLikeOriginal(string reason)
            {
                if (_mode == null || _visualRoot == null) return;

                // V117: stage changes rebuild the whole composite. If the site was selected before
                // this visual swap, transfer only that real selected flag to the freshly created
                // selectable. This keeps HUD progress alive for 0->1->2 transitions without using a
                // broad "keep selection after ground click" fallback.
                bool selectedBeforeVisualSwap = C2BuildRuntimeWasSelectedBeforeVisualRebuildV117LikeOriginal();

                C2BuildRuntimeClearChildrenLikeOriginal(_visualRoot);

                C2Settlement3InuMdV2Info md = C2Settlement3InuMdV2ResolveMdLikeOriginal(_mdName);
                if (md == null || !md.Found)
                {
                    Debug.LogWarning("[C2:BUILD RUNTIME V44 SITE VISUAL MISS] md='" + _mdName + "' reason=md_not_found");
                    return;
                }

                int status = ConstructionStatusLikeOriginal;
                int visualPhase = VisualBuildPhaseLikeOriginal;
                // Original visual selection is stage-driven: phase=(Stage*4)/NStages selects #BUILDLO_0..3,
                // and ready uses normal StandLo/StandHi/Work. Passing -1 creates a saved Stage=0 record,
                // so the existing 3INU renderer selects StandLo instead of accidentally returning #BUILDLO_0.
                int mdStage = (_complete || _ready) ? -1 : Mathf.Clamp(_stage, 0, Mathf.Max(1, _buildStages - 1));
                C2Settlement3InuMdV2Record r = C2BuildRuntimeRecordLikeOriginal(_mdName, _nation, _realX, _realY, mdStage, (ushort)((_complete || _ready) ? 1 : _life));
                C2Settlement3InuMdV2Kind kind = C2BuildRuntimeKindLikeOriginal(md);
                List<C2Settlement3InuMdV2LoadedFrame> frames;
                string visualAudit;
                bool ok = _mode.C2Settlement3InuMdV2TryLoadVisualFramesLikeOriginal(md, r, kind, out frames, out visualAudit);
                if (!ok || frames == null || frames.Count == 0)
                {
                    Debug.LogWarning("[C2:BUILD RUNTIME V44 SITE VISUAL MISS] md='" + _mdName +
                                     "' stage=" + _stage.ToString(CultureInfo.InvariantCulture) +
                                     " status=" + status.ToString(CultureInfo.InvariantCulture) +
                                     " savedStage=" + r.Stage.ToString(CultureInfo.InvariantCulture) +
                                     " audit=" + visualAudit);
                    return;
                }

                _mode.C2Settlement3InuMdV2CreateSpriteObjectCompositeLikeOriginal(_visualRoot, r, md, kind, frames, visualAudit);
                int restoredSelection = C2BuildRuntimeRestoreSelectedAfterVisualRebuildV117LikeOriginal(selectedBeforeVisualSwap, reason);

                _nextVisualStatus = status;
                _lastVisualPhase = visualPhase;
            }

            private bool C2BuildRuntimeWasSelectedBeforeVisualRebuildV117LikeOriginal()
            {
                if (_visualRoot == null) return false;
                C2SettlementBuildingSelectableV1LikeOriginal[] selectables =
                    _visualRoot.GetComponentsInChildren<C2SettlementBuildingSelectableV1LikeOriginal>(true);
                for (int i = 0; selectables != null && i < selectables.Length; i++)
                {
                    C2SettlementBuildingSelectableV1LikeOriginal s = selectables[i];
                    if (s != null && s.IsSelected && !s.NotSelectable)
                        return true;
                }
                return false;
            }

            private int C2BuildRuntimeRestoreSelectedAfterVisualRebuildV117LikeOriginal(bool selectedBeforeVisualSwap, string reason)
            {
                if (_proxy != null)
                    _proxy.ClearRuntimeSelectionCarryV117LikeOriginal();

                if (!selectedBeforeVisualSwap || _visualRoot == null)
                    return 0;

                C2SettlementBuildingSelectableV1LikeOriginal[] selectables =
                    _visualRoot.GetComponentsInChildren<C2SettlementBuildingSelectableV1LikeOriginal>(true);

                C2SettlementBuildingSelectableV1LikeOriginal best = null;
                for (int i = 0; selectables != null && i < selectables.Length; i++)
                {
                    C2SettlementBuildingSelectableV1LikeOriginal s = selectables[i];
                    if (s == null || s.NotSelectable || !s.isActiveAndEnabled)
                        continue;
                    if (best == null || s.SortKey < best.SortKey)
                        best = s;
                }

                if (best == null)
                    return 0;

                best.SetSelected(true);

                if (_proxy != null)
                    _proxy.MarkRuntimeSelectionCarryV117LikeOriginal(1, reason ?? string.Empty);

                return 1;
            }
        }

    }


    public sealed class C2RuntimeConstructionSiteProxyLikeOriginal : MonoBehaviour
    {
        public C2BattleTerrainMode OwnerMode;
        public string MdName = string.Empty;
        public string UnitId = string.Empty;
        public int Nation;
        public int RealX;
        public int RealY;
        public int BuildStages;
        public int Stage;
        public bool Ready;
        public bool Dead;
        public bool NotSelectable;
        public bool RuntimeSelectionCarriedV117LikeOriginal { get; private set; }
        public float RuntimeSelectionCarriedAtV117LikeOriginal { get; private set; }
        public int RuntimeSelectionCarryRestoredCountV117LikeOriginal { get; private set; }
        public string RuntimeSelectionCarryReasonV117LikeOriginal { get; private set; }

        public bool CanAcceptBuildersLikeOriginal
        {
            get { return !Ready && !Dead && !NotSelectable; }
        }

        public void Configure(
            C2BattleTerrainMode owner,
            string mdName,
            string unitId,
            int nation,
            int realX,
            int realY,
            int buildStages,
            int stage,
            bool ready,
            bool dead,
            bool notSelectable)
        {
            OwnerMode = owner;
            MdName = mdName ?? string.Empty;
            UnitId = unitId ?? string.Empty;
            Nation = nation;
            RealX = realX;
            RealY = realY;
            BuildStages = buildStages;
            Stage = stage;
            Ready = ready;
            Dead = dead;
            NotSelectable = notSelectable;
        }

        public void MarkRuntimeSelectionCarryV117LikeOriginal(int restoredCount, string reason)
        {
            RuntimeSelectionCarriedV117LikeOriginal = restoredCount > 0;
            RuntimeSelectionCarriedAtV117LikeOriginal = Time.realtimeSinceStartup;
            RuntimeSelectionCarryRestoredCountV117LikeOriginal = Mathf.Max(0, restoredCount);
            RuntimeSelectionCarryReasonV117LikeOriginal = reason ?? string.Empty;
        }

        public void ClearRuntimeSelectionCarryV117LikeOriginal()
        {
            RuntimeSelectionCarriedV117LikeOriginal = false;
            RuntimeSelectionCarriedAtV117LikeOriginal = 0.0f;
            RuntimeSelectionCarryRestoredCountV117LikeOriginal = 0;
            RuntimeSelectionCarryReasonV117LikeOriginal = string.Empty;
        }

        public int AssignSelectedBuildersLikeOriginal(string source, out string audit)
        {
            audit = "not_started";
            if (OwnerMode == null)
            {
                audit = "no_owner_mode";
                return 0;
            }

            if (!CanAcceptBuildersLikeOriginal)
            {
                audit = "not_accepting_builders ready=" + Ready + " dead=" + Dead + " notSelectable=" + NotSelectable;
                return 0;
            }

            return OwnerMode.C2BuildRuntimeAssignBuildersSnapshotLikeOriginal(
                gameObject,
                RealX,
                RealY,
                null,
                source ?? "right_click_mend",
                out audit);
        }
    }
}
