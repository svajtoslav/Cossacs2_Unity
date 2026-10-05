using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2SettlementDipVillageV336LikeOriginal
    {
        private bool _attackedV445, _replenishingV445;
        private float _nextCaravanTickV445;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _caravanUnitV445;
        private C2SettlementBuildingSelectableV1LikeOriginal _caravanDestinationV445;
        private readonly Dictionary<C2SettlementBuildingSelectableV1LikeOriginal,float> _caravanBlockedStoresV447 = new Dictionary<C2SettlementBuildingSelectableV1LikeOriginal,float>();
        private Vector2 _caravanProgressPositionV447;
        private float _caravanProgressAtV447;
        private int _caravanRouteOwnerV447 = -1;
        private C2SettlementBuildingSelectableV1LikeOriginal _mineV445;
        private float _nextMineScanV445;
        private readonly Dictionary<int, MineVisitV445> _mineVisitsV445 = new Dictionary<int, MineVisitV445>();
        private sealed class MineVisitV445 { internal int Phase; internal float Until; }
        internal readonly List<SettlementUpgradeV445> UpgradesV445 = new List<SettlementUpgradeV445>();
        internal sealed class SettlementUpgradeV445
        {
            internal string IconFile, Hint;
            internal int Icon, X, Y, ExtraWorkers;
            internal int[] Price = new int[8];
            internal float Duration, Started;
            internal bool Running, Done;
        }
        internal static bool IsVillageSelectionV445(C2SettlementBuildingSelectableV1LikeOriginal b) =>
            b != null && b.IsSelected && _selectedSettlement != null && _selectedSettlement._buildings.Contains(b);
        internal int OwnerV445 => _owner;
        internal C2BattleTerrainMode MapV451 => _mode;
        internal Vector2 CentreV451 => new Vector2(_data.CenterX,_data.CenterY);
        internal int WorkerCountV445 => LiveCountV445(_peasants);
        internal int WorkerLimitV445 => _data.PeasantMax;
        internal int StockV445 => Mathf.FloorToInt(_storedResource);
        internal int StockLimitV445 => _data.ResourceMax;
        internal int ResourceV445 => _data.ResourceType;
        internal int CargoV445 => _caravanShipmentAmount;
        internal int MineVisitsV445 => _mineVisitsV445.Count;
        internal int DefendersV447 => LiveCountV445(_defenders);
        internal bool AttackedV447 => _attackedV445;
        internal float PerMinuteV447 => _attackedV445||_data.PeasantProduceTicks<=0?0:Mathf.Min(WorkerCountV445,WorkerLimitV445)*1500f/_data.PeasantProduceTicks;
        internal string CaravanStatusV447 => _caravanInTransit ? (_caravanShipmentAmount>0 ? (_caravanDestinationV445==null?"нет склада":"везёт ресурсы") : "возвращается") : "ожидает загрузки";
        internal static int LiveCountV445(List<C2NeutralPeasantUnitInfoV2LikeOriginal> units)
        {
            int n = 0;
            for (int i = 0; i < units.Count; i++) if (units[i] != null && !units[i].IsDeadLikeOriginal) n++;
            return n;
        }

        // LastProduce runs regardless of neutral ownership, storage availability,
        // or a caravan being on the road. Native DIP Process caps the local stock.
        private void TickEconomyV445()
        {
            if (!_populationInitialized || _data == null) return;
            float now = Time.time;
            int live = Mathf.Min(WorkerCountV445,WorkerLimitV445);
            if (_attackedV445 || live == 0 || _data.PeasantProduceTicks <= 0)
                _lastProduceRealtime = now;
            else
            {
                float period = Mathf.Max(.04f, _data.PeasantProduceTicks / 25f);
                int periods = Mathf.FloorToInt(Mathf.Max(0, now - _lastProduceRealtime) / period);
                if (periods > 0)
                {
                    _lastProduceRealtime += periods * period;
                    _storedResource += (float)((long)periods * live);
                }
            }
            if (now >= _nextCaravanTickV445)
            {
                _nextCaravanTickV445 = now + .25f;
                TickCaravanV445();
            }
            if (_data.ResourceMax > 0) _storedResource = Mathf.Min(_storedResource, _data.ResourceMax);
        }

        private void TickCaravanV445()
        {
            if (_caravanUnitV445 != null && _caravanUnitV445.IsDeadLikeOriginal)
            {
                // A destroyed cart loses its cargo; there is no remote credit/refund.
                _caravanUnitV445 = null;
                _caravanShipmentAmount = 0;
                _caravanInTransit = false;
                _caravanDestinationV445 = null;
            }
            if (_caravanInTransit)
            {
                if (_caravanUnitV445 == null) { _caravanShipmentAmount = 0; _caravanInTransit = false; return; }
                if (_caravanUnitV445.IsBusyWithBornExitOrMoveLikeOriginal() &&
                    _caravanUnitV445.RuntimeLinkCachedLikeOriginal?.Runtime?.PreciseBornPathLikeOriginal == true) return;
                if (_caravanRouteOwnerV447 != _owner) {
                    _caravanRouteOwnerV447 = _owner;
                    _caravanBlockedStoresV447.Clear();
                    _caravanDestinationV445 = null;
                    StopCaravanRouteV447();
                }
                if (_caravanShipmentAmount > 0)
                {
                    if (!C2ResourceStorageV445.Accepts(_caravanDestinationV445, _mode, _owner, _data.ResourceType)) {
                        StopCaravanRouteV447();
                        _caravanDestinationV445 = C2ResourceStorageV445.Nearest(_mode, _owner, _data.ResourceType,
                            _caravanUnitV445.RealX, _caravanUnitV445.RealY, _caravanBlockedStoresV447);
                    }
                    if (_caravanDestinationV445 == null ||
                        !C2ResourceStorageV445.Paths(_caravanDestinationV445, out var entry, out _)) return;
                    // DipCaravan unloads at the storage's exterior entry, within 100px.
                    if (NearV445(_caravanUnitV445, entry[0], 100))
                    {
                        C2NationResourceEconomyV348LikeOriginal.AddResourceLikeOriginal(_owner, _data.ResourceType,
                            _caravanShipmentAmount, "DIP_caravan_delivery");
                        _caravanShipmentAmount = 0;
                        _caravanDestinationV445 = null;
                        StopCaravanRouteV447();
                    }
                    else if (!RetryCaravanRouteV447()) MoveV445(_caravanUnitV445, entry[0]);
                }
                else
                {
                    if (!TryVillageGateV445(out var gate)) return;
                    if (NearV445(_caravanUnitV445, gate, 100))
                    {
                        StopCaravanRouteV447();
                        _caravanInTransit = false;
                    }
                    else if (!RetryCaravanRouteV447()) MoveV445(_caravanUnitV445, gate);
                }
                return;
            }
            if (_owner == 7 || _attackedV445 || _caravanItem == null || _data.CaravanCapacity <= 0) return;
            int amount = Mathf.FloorToInt(_storedResource / _data.CaravanCapacity) * _data.CaravanCapacity;
            if (amount <= 0 || !TryFindNearestResourceStorageLikeOriginal(out var destination)) return;
            if (_caravanUnitV445 == null)
            {
                foreach (var building in _buildings)
                {
                    if (!C2ResourceStorageV445.Ready(building) || !HasProductionBornPathLikeOriginal(building)) continue;
                    if (C2UnitOriginalRuntimeAndRendererV1.TrySpawnProducedUnitFromBuildingLikeOriginal(
                        _mode, building, _caravanItem, 7, out _caravanUnitV445, out _)) break;
                }
                if (_caravanUnitV445 == null) return;
                _caravanUnitV445.SettlementAiControlledLikeOriginal = true;
                _caravanUnitV445.ControllableByPlayer = false;
                _caravans.Add(_caravanUnitV445);
            }
            _caravanUnitV445.SettlementAllegianceNationLikeOriginal = _owner;
            _caravanOwner = _owner;
            _caravanShipmentAmount = amount;
            _storedResource -= amount;
            _caravanDestinationV445 = destination;
            _caravanInTransit = true;
            _caravanProgressAtV447 = Time.time;
            _caravanProgressPositionV447 = new Vector2(_caravanUnitV445.RealX, _caravanUnitV445.RealY);
        }

        private void StopCaravanRouteV447()
        {
            C2OriginalOrderChainV352.ClearMoveChainForExternalOrder(_caravanUnitV445);
            _caravanUnitV445.StopMoveAndFaceDirectionLikeOriginal(_caravanUnitV445.RealDir);
            _caravanProgressAtV447 = Time.time;
            _caravanProgressPositionV447 = new Vector2(_caravanUnitV445.RealX, _caravanUnitV445.RealY);
        }
        private bool RetryCaravanRouteV447()
        {
            var position = new Vector2(_caravanUnitV445.RealX, _caravanUnitV445.RealY);
            if ((position - _caravanProgressPositionV447).sqrMagnitude >= 128 * 128) {
                _caravanProgressPositionV447 = position;
                _caravanProgressAtV447 = Time.time;
            }
            if (Time.time - _caravanProgressAtV447 < 15) return false;
            if (_caravanDestinationV445 != null) _caravanBlockedStoresV447[_caravanDestinationV445] = Time.time + 30;
            _caravanDestinationV445 = null;
            StopCaravanRouteV447();
            return true;
        }

        private bool TryVillageGateV445(out Vector2 gate)
        {
            foreach (var b in _buildings)
                if (C2ResourceStorageV445.Ready(b) && C2ResourceStorageV445.Paths(b, out var entry, out _))
                { gate = entry[0]; return true; }
            gate = default; return false;
        }
        private static bool NearV445(C2NeutralPeasantUnitInfoV2LikeOriginal u, Vector2 p, float pixels) =>
            (new Vector2(u.RealX, u.RealY) - p).sqrMagnitude <= pixels * pixels * 256f;
        private static void MoveV445(C2NeutralPeasantUnitInfoV2LikeOriginal u, Vector2 p)
        {
            if (!u.IsBusyWithBornExitOrMoveLikeOriginal()) u.SetMoveDestinationRealLikeOriginal(p.x, p.y,
                C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal);
        }

        private void ReplenishPopulationV445()
        {
            if (!_populationSpawned || _attackedV445 || Time.time < _nextPopulationProduceAt) return;
            _peasantsToProduce = _peasantItem != null ? Mathf.Max(0, _data.PeasantMax - WorkerCountV445) : 0;
            _defendersToProduce = _defenderItem != null ? Mathf.Max(0, _data.CopMax - LiveCountV445(_defenders)) : 0;
            if (_peasantsToProduce + _defendersToProduce == 0) return;
            _populationSpawned = false;
            _replenishingV445 = true;
            _nextPopulationProduceAt = Time.time + PopulationDelayV445();
        }
        private float PopulationDelayV445()
        {
            if (!_replenishingV445) return InitialPopulationOrderSecondsLikeOriginal;
            int ticks = _defendersToProduce > 0 ? _data.CopBuildTicks : _data.PeasantBuildTicks;
            int count = _defendersToProduce > 0 ? LiveCountV445(_defenders) : WorkerCountV445;
            int limit = _defendersToProduce > 0 ? _data.CopMax : _data.PeasantMax;
            return count < limit / 2 ? .8f : Mathf.Max(1, ticks) / 50f;
        }

        private void SetUnderAttackV445(bool attacked)
        {
            if (_attackedV445 == attacked) return;
            _attackedV445 = attacked;
            _lastProduceRealtime = Time.time;
            _mineVisitsV445.Clear();
            _nextPeasantWorkAt.Clear();
            foreach (var p in _peasants)
            {
                if (p == null || p.IsDeadLikeOriginal) continue;
                if (attacked) p.GetComponent<C2GameplayUnitTaskV1>()?.CancelForExternalOrderLikeOriginal("village_under_attack");
                p.StopMoveAndFaceDirectionLikeOriginal(p.RealDir);
                p.NotSelectable = attacked;
                p.RuntimeLinkCachedLikeOriginal?.SetHiddenInsideBuildingLikeOriginal(attacked);
            }
        }

        private void TickMineWorkersV445()
        {
            if (_attackedV445 || !_populationInitialized || _data.ResourceType == 0 || _data.ResourceType == 2 || _data.ResourceType == 3) return;
            if (!C2ResourceStorageV445.Ready(_mineV445) && Time.time >= _nextMineScanV445)
            {
                _nextMineScanV445 = Time.time + 3;
                // Release civilians if a mine was destroyed while they were inside.
                foreach (var p in _peasants)
                    if (p != null && !p.IsDeadLikeOriginal && _mineVisitsV445.ContainsKey(p.GetInstanceID()))
                    { p.RuntimeLinkCachedLikeOriginal?.SetHiddenInsideBuildingLikeOriginal(false); p.StopMoveAndFaceDirectionLikeOriginal(p.RealDir); }
                _mineVisitsV445.Clear();
                _mineV445 = null;
                FindVillageBuildingsLikeOriginal();
                foreach (var b in _buildings)
                {
                    var info = C2ResourceStorageV445.Info(b);
                    if (info == null || info.ConcentratorPathReal.Count < 1 || info.BornExitPathReal.Count < 1) continue;
                    var md = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedBuilding(b);
                    if (string.IsNullOrEmpty(md.Path) || !File.Exists(md.Path)) continue;
                    string token = new[] { "WOOD", "GOLD", "STONE", "FOOD", "IRON", "COAL" }[_data.ResourceType];
                    if (File.ReadLines(md.Path).Any(l => l.TrimStart().StartsWith("PRODUCER ", StringComparison.Ordinal) && l.Contains(token)))
                    { _mineV445 = b; break; }
                }
            }
            if (_mineV445 == null) return;
            var mine = C2ResourceStorageV445.Info(_mineV445);
            foreach (var p in _peasants)
            {
                if (p == null || p.IsDeadLikeOriginal) continue;
                int id = p.GetInstanceID();
                if (!_mineVisitsV445.TryGetValue(id, out var visit))
                {
                    if (p.IsBusyWithBornExitOrMoveLikeOriginal()) continue;
                    _mineVisitsV445[id] = visit = new MineVisitV445 { Until = Time.time + (id & 7) * .3f };
                }
                if (Time.time < visit.Until) continue;
                var entry = mine.ConcentratorPathReal;
                var exit = mine.BornExitPathReal;
                switch (visit.Phase)
                {
                    case 0:
                        if (!NearV445(p, entry[0], 50)) { MoveV445(p, entry[0]); break; }
                        C2OriginalOrderChainV352.SubmitBornExitV433LikeOriginal(p, entry.ToArray(), entry.Count, true, false);
                        visit.Phase = 1;
                        break;
                    case 1:
                        if (p.IsBusyWithBornExitOrMoveLikeOriginal() || !NearV445(p, entry[entry.Count-1], 40)) break;
                        p.RuntimeLinkCachedLikeOriginal?.SetHiddenInsideBuildingLikeOriginal(true);
                        // Presentation cycle only; all ore is accounted by DIP LastProduce.
                        visit.Until = Time.time + Mathf.Max(2, _data.PeasantProduceTicks / 25f) + (id & 3);
                        visit.Phase = 2;
                        break;
                    case 2:
                        p.RuntimeLinkCachedLikeOriginal?.SetHiddenInsideBuildingLikeOriginal(false);
                        C2OriginalOrderChainV352.SubmitBornExitV433LikeOriginal(p, exit.ToArray(), exit.Count, true, true);
                        visit.Phase = 3;
                        break;
                    case 3:
                        if (p.IsBusyWithBornExitOrMoveLikeOriginal()) break;
                        Vector2 away = exit[exit.Count-1] + (exit[exit.Count-1] - exit[0]).normalized * (60 + (id & 7) * 12) * 16;
                        MoveV445(p, away);
                        visit.Phase = 4;
                        break;
                    case 4:
                        if (p.IsBusyWithBornExitOrMoveLikeOriginal()) break;
                        visit.Phase = 0;
                        visit.Until = Time.time + 1 + (id & 3);
                        break;
                }
            }
        }

        internal void LoadUpgradesV445()
        {
            UpgradesV445.Clear();
            try
            {
                XElement xml = null;
                if (!string.IsNullOrWhiteSpace(_data.UpgradesXmlV445)) xml = XElement.Parse(_data.UpgradesXmlV445);
                else
                {
                    string path = Path.Combine(@"C:\GSC Game World\Cossacks II\Data", (_data.SourceFile ?? "").Replace('\\', Path.DirectorySeparatorChar));
                    if (File.Exists(path)) xml = XElement.Parse(File.ReadAllText(path, Encoding.GetEncoding(1251)).Replace("<>", "<Settlement>").Replace("</>", "</Settlement>"));
                }
                if (xml == null) return;
                foreach (var e in xml.Descendants("SetlUpgrade"))
                {
                    if ((string)e.Element("EffType") != "PEASANT+" || (string)e.Element("PriType") != "PRICE") continue;
                    string res = new[] { "Wood", "Gold", "Stone", "Food", "Iron", "Coal" }[Mathf.Clamp(_data.ResourceType,0,5)];
                    var u = new SettlementUpgradeV445 {
                        IconFile = (string)e.Element("IconFile") ?? "Interf3\\Upg", Icon = XmlIntV445(e,"Icon"),
                        X = XmlIntV445(e,"X"), Y = XmlIntV445(e,"Y"), Hint = (string)e.Element("Hint") ?? "",
                        ExtraWorkers = XmlIntV445(e.Element("Effect"),res),
                        // SetlUpgrade::Run multiplies Duration by 8; GetGlobalTime is milliseconds.
                        Duration = Mathf.Max(.008f, XmlIntV445(e,"Duration") * .008f),
                        Done = (string)e.Element("isComplete") == "true"
                    };
                    foreach (var price in e.Element("Price")?.Elements() ?? Enumerable.Empty<XElement>())
                    {
                        int resource = C2ResourceStorageV445.ResourceId(price.Name.LocalName);
                        if (resource >= 0 && int.TryParse(price.Value,out int amount)) u.Price[resource] = Mathf.Max(0,amount);
                    }
                    if (u.ExtraWorkers > 0) UpgradesV445.Add(u);
                }
            }
            catch (Exception e) { Debug.LogWarning("[C2:VILLAGE ECONOMY] Upgrade data: " + e.Message); }
        }
        private static int XmlIntV445(XElement e, string key) => int.TryParse((string)e?.Element(key),out int n) ? n : 0;
        internal bool CanUpgradeV447(int index,int nation) =>
            C2EditorRuntimeStateV333LikeOriginal.CanControlNationLikeOriginal(nation)&&CanUpgradeForNationV451(index,nation);
        internal bool CanUpgradeForNationV451(int index,int nation) => (uint)nation<7&&nation==_owner&&!_attackedV445&&
            index>=0&&index<UpgradesV445.Count&&!UpgradesV445[index].Running&&!UpgradesV445[index].Done&&
            C2NationResourceEconomyV348LikeOriginal.CanAffordLikeOriginal(nation,UpgradesV445[index].Price,out _);
        internal bool BuyUpgradeV445(int index, int nation)
        {
            if (!CanUpgradeV447(index,nation)) return false;
            return BuyUpgradeForNationV451(index,nation);
        }
        internal bool BuyUpgradeForNationV451(int index,int nation)
        {
            if(!CanUpgradeForNationV451(index,nation))return false;
            var u = UpgradesV445[index];
            if (u.Running || u.Done || !C2NationResourceEconomyV348LikeOriginal.ApplyCostLikeOriginal(nation,u.Price,"village_upgrade",out _)) return false;
            u.Running = true; u.Started = Time.time;
            return true;
        }
        private void TickUpgradesV445()
        {
            foreach (var u in UpgradesV445)
                if (u.Running && Time.time - u.Started >= u.Duration)
                { u.Running = false; u.Done = true; _data.PeasantMax += u.ExtraWorkers; C2GameplayHudV1.ForceRefreshLikeOriginal(); }
        }
        internal static C2SettlementDipVillageV336LikeOriginal ForBuildingV445(C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            if (building == null) return null;
            C2SettlementDipVillageV336LikeOriginal best = null; float distance = float.MaxValue;
            foreach (var v in FindObjectsByType<C2SettlementDipVillageV336LikeOriginal>(FindObjectsSortMode.None))
            {
                if (v._mode != building.OwnerMode || !v._buildings.Contains(building)) continue;
                float d = (new Vector2(building.RealX/16f,building.RealY/16f)-new Vector2(v._data.CenterX,v._data.CenterY)).sqrMagnitude;
                if (d < distance) { distance = d; best = v; }
            }
            return best;
        }
    }
}
