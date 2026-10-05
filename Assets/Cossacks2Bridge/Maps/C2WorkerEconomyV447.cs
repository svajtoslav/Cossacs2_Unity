using System.Collections.Generic;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2GameplayUnitTaskV1
    {
        private readonly Dictionary<long, float> _failedResourcesV447 = new Dictionary<long, float>();
        private readonly Dictionary<C2SettlementBuildingSelectableV1LikeOriginal, float> _failedStoresV447 = new Dictionary<C2SettlementBuildingSelectableV1LikeOriginal, float>();
        private float _retryResourceAtV447, _travelProgressAtV447;
        private Vector2 _travelPositionV447;
        internal bool TaskActiveV447 => _active;
        internal int CarriedAmountV447 => _carriedResourceAmountV348;
        internal byte ResourceIdV447 => _resourceIdV222;

        private bool ResourceFailedV447(int x, int y) =>
            _failedResourcesV447.TryGetValue(((long)x << 32) | (uint)y, out float until) && Time.time < until;

        private void ResetTravelWatchV447()
        {
            _travelProgressAtV447 = Time.time;
            if (Unit != null) _travelPositionV447 = new Vector2(Unit.RealX, Unit.RealY);
        }

        private bool RetryStalledTravelV447()
        {
            if (Unit.SettlementAiControlledLikeOriginal ||
                (_resourcePhaseV222 != ResourcePhaseV222.MoveToResource &&
                 _resourcePhaseV222 != ResourcePhaseV222.MoveToStore &&
                 _resourcePhaseV222 != ResourcePhaseV222.MoveIntoStore)) return false;
            Vector2 now = new Vector2(Unit.RealX, Unit.RealY);
            if ((now - _travelPositionV447).sqrMagnitude >= 128 * 128) ResetTravelWatchV447();
            if (Time.time - _travelProgressAtV447 < 15) return false;
            // No progress, rather than a travel-time limit: long detours are valid.
            // Retry the obstruction later; never credit a load on a timeout.
            C2OriginalOrderChainV352.StopTaskMoveV433LikeOriginal(Unit);
            Unit.StopMoveAndFaceDirectionLikeOriginal(Unit.RealDir);
            if (_resourcePhaseV222 == ResourcePhaseV222.MoveToResource) {
                _failedResourcesV447[((long)_resourceOriginalXV222 << 32) | (uint)_resourceOriginalYV222] = Time.time + 30;
                MoveToResourceV222LikeOriginal("blocked_resource_retry");
            } else {
                if (_storeBuildingV445 != null) _failedStoresV447[_storeBuildingV445] = Time.time + 30;
                MoveToStoreV222LikeOriginal();
            }
            return true;
        }

        internal static bool AssignResourceV447(C2NeutralPeasantUnitInfoV2LikeOriginal unit, byte resource)
        {
            if (unit == null || unit.OwnerMode == null || resource >= 6) return false;
            var map = unit.OwnerMode;
            if (!map.C2OriginalResourceMapV1TryFindResourceWorkTargetLikeOriginal(resource, unit.RealX / 16, unit.RealY / 16,
                2000, unit, out int rx, out int ry, out _, out _, out int radius, out _, out _)) return false;
            var store = C2ResourceStorageV445.Nearest(map, unit.Nation, resource, unit.RealX, unit.RealY);
            if (!C2ResourceStorageV445.Paths(store, out var entry, out var exit)) {
                map.C2OriginalResourceMapV1ReleaseWorkPointLikeOriginal(unit);
                return false;
            }
            var proxy = unit.EnsureUnityProxyLikeOriginal();
            if (proxy == null) { map.C2OriginalResourceMapV1ReleaseWorkPointLikeOriginal(unit); return false; }
            var task = proxy.GetComponent<C2GameplayUnitTaskV1>() ?? proxy.AddComponent<C2GameplayUnitTaskV1>();
            var gate = entry[0]; var deposit = entry[entry.Length - 1];
            task.BeginTakeResourceV222LikeOriginal(unit, resource == 0 ? C2GameplayTargetKindV1.Tree :
                resource == 3 ? C2GameplayTargetKindV1.Field : C2GameplayTargetKindV1.Stone,
                resource, rx, ry, radius, map.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(rx, ry), true,
                (int)gate.x, (int)gate.y, (int)deposit.x, (int)deposit.y,
                map.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(gate.x / 16, gate.y / 16), entry, exit);
            return true;
        }
    }

    // PlaceInAJob.cpp: eight seconds idle, four-second scans, existing resource
    // proportions, within 2000 original pixels of the workers' centre.
    internal sealed class C2PeasantAutoWorkV447 : MonoBehaviour
    {
        internal static bool Enabled = true;
        private readonly Dictionary<int, float> _idleSince = new Dictionary<int, float>();
        private readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> _idle = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _removed = new List<int>();
        private float _nextScan;
        internal void ResetMap() { _idleSince.Clear(); _nextScan = Time.time + 4; }

        private void Update()
        {
            if (Time.timeScale <= 0 || Time.time < _nextScan) return;
            _nextScan = Time.time + 4;
            Tick(Time.time);
        }

        internal static bool CanAutoAssign(C2NeutralPeasantUnitInfoV2LikeOriginal u)
        {
            return u!=null&&u.CanReceivePlayerOrdersLikeOriginal()&&CanAutoAssignForNationV451(u,u.Nation);
        }

        internal static bool CanAutoAssignForNationV451(C2NeutralPeasantUnitInfoV2LikeOriginal u,int nation)
        {
            if (u == null || u.Nation!=nation || u.SettlementAiControlledLikeOriginal || u.IsDeadLikeOriginal || !u.isActiveAndEnabled || !u.CanReceiveOrdersLikeOriginal() ||
                !C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(u).Peasant) return false;
            var task = u.GetComponent<C2GameplayUnitTaskV1>();
            var build = u.GetComponent<C2BuildWorkerOrderV245LikeOriginal>();
            var rt = u.RuntimeLinkCachedLikeOriginal?.Runtime;
            return !(task != null && task.TaskActiveV447) && !(build != null && build.isActiveAndEnabled) &&
                !u.IsBusyWithBornExitOrMoveLikeOriginal() && !C2OriginalOrderChainV352.HasLocalMoveOrderLikeOriginal(u) &&
                !C2CombatRuntimeV334LikeOriginal.IsAttackOrderActiveV403LikeOriginal(u) &&
                !(rt != null && (rt.HiddenInsideBuildingLikeOriginal || rt.MoveDeferredUntilNeutralStandLikeOriginal || rt.OriginalPathRequestPendingV425LikeOriginal));
        }

        internal void Tick(float now)
        {
            if (!Enabled) { _idleSince.Clear(); return; }
            _seen.Clear();
            var units = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int nation = 0; nation < 7; nation++) {
                if (!C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(nation)) continue;
                int[] assigned = new int[6]; int working = 0; Vector2 centre = Vector2.zero;
                _idle.Clear();
                foreach (var u in units) {
                    if (u == null || u.Nation != nation || u.IsDeadLikeOriginal || u.SettlementAiControlledLikeOriginal ||
                        !C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(u).Peasant) continue;
                    if(C2EconomicAiDriverV451.Owns(u.OwnerMode,nation))continue;
                    var task = u.GetComponent<C2GameplayUnitTaskV1>();
                    if (task != null && task.ResourceTaskActiveV445 && task.ResourceIdV447 < 6) {
                        assigned[task.ResourceIdV447]++; working++; centre += new Vector2(u.RealX / 16f, u.RealY / 16f);
                    }
                    if (!CanAutoAssign(u)) { _idleSince.Remove(u.GetInstanceID()); continue; }
                    int id = u.GetInstanceID(); _seen.Add(id);
                    if (!_idleSince.TryGetValue(id, out float since)) _idleSince[id] = now;
                    else if (now - since >= 8) _idle.Add(u);
                }
                if (working == 0 || _idle.Count == 0) continue;
                centre /= working;
                int[] added = new int[6]; int total = _idle.Count;
                // Limit new path searches per scan; hundreds of idle workers must
                // not all start expensive pathfinding in a single frame.
                int budget = 8;
                foreach (var u in _idle) {
                    if (budget <= 0) break;
                    if ((new Vector2(u.RealX / 16f, u.RealY / 16f) - centre).sqrMagnitude >= 2000 * 2000) continue;
                    int resource = -1; float deficit = float.NegativeInfinity;
                    for (int r = 0; r < 6; r++) if (assigned[r] > 0) {
                        float d = (float)assigned[r] * total / working - added[r];
                        if (d > deficit) { deficit = d; resource = r; }
                    }
                    if (resource < 0) continue;
                    budget--; added[resource]++;
                    if (C2GameplayUnitTaskV1.AssignResourceV447(u, (byte)resource)) _idleSince.Remove(u.GetInstanceID());
                }
            }
            _removed.Clear();
            foreach (int id in _idleSince.Keys) if (!_seen.Contains(id)) _removed.Add(id);
            foreach (int id in _removed) _idleSince.Remove(id);
        }
    }
}
