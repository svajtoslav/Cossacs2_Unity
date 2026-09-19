// C2NationCityRuntimeV384ALikeOriginal.cs
// Port of the authoritative City/Nation counters used by Cossacks II UI.
// Original references:
//   COSSACKS2/Megapolis.cpp   City::EnumUnits, ResourcePeasants refresh
//   COSSACKS2/GroupPurpose.cpp GetCurrentUnits/GetMaxUnits
//   DipServer/DIP_SimpleBuilding.cpp vdf_GetAmountOfPeasant/vdf_GetAmountOfSettlements
// This runtime owns NGidot/NFarms/ResourcePeasants. HUD code must only read it.

using System;
using System.Globalization;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static class C2NationCityRuntimeV384ALikeOriginal
    {
        internal const int NationCount = 8;
        internal const int ResourceCount = 6;

        private static readonly int[] NGidot = new int[NationCount];
        private static readonly int[] NFarms = new int[NationCount];
        private static readonly int[,] ResourcePeasants = new int[NationCount, ResourceCount];
        private static readonly int[,] SettlementPeasants = new int[NationCount, ResourceCount];
        private static readonly int[,] RegularTakeResPeasants = new int[NationCount, ResourceCount];
        private static readonly int[] ProductiveSettlements = new int[NationCount];

        private static C2NationCityRuntimeDriverV384ALikeOriginal _driver;
        private static int _revision;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubsystemLikeOriginal()
        {
            ClearStateLikeOriginal();
            _driver = null;
            _revision = 0;
        }

        internal static void BeginMapLikeOriginal()
        {
            ClearStateLikeOriginal();
            EnsureDriverLikeOriginal();
            RefreshNowLikeOriginal("map_begin");
        }

        internal static int GetCurrentUnitsLikeOriginal(int nation)
        {
            if ((uint)nation >= NationCount) return 0;
            return NGidot[nation];
        }

        internal static int GetMaxUnitsLikeOriginal(int nation)
        {
            if ((uint)nation >= NationCount) return 0;
            return NFarms[nation];
        }

        internal static int GetResourcePeasantsLikeOriginal(int nation, int resource)
        {
            if ((uint)nation >= NationCount || (uint)resource >= ResourceCount) return 0;
            return ResourcePeasants[nation, resource];
        }

        internal static int RevisionLikeOriginal { get { return _revision; } }

        internal static void RefreshNowLikeOriginal(string reason)
        {
            int[] nextNGidot = new int[NationCount];
            int[] nextNFarms = new int[NationCount];
            int[,] nextResourcePeasants = new int[NationCount, ResourceCount];
            int[,] nextSettlementPeasants = new int[NationCount, ResourceCount];
            int[,] nextRegularPeasants = new int[NationCount, ResourceCount];
            int[] nextSettlements = new int[NationCount];

            // DIP_SimpleBuilding.cpp::vdf_GetAmountOfSettlements: one productive
            // settlement contributes once (it produces exactly one resource in the
            // current Unity settlement record).
            C2SettlementDipVillageV336LikeOriginal[] villages =
                UnityEngine.Object.FindObjectsByType<C2SettlementDipVillageV336LikeOriginal>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; villages != null && i < villages.Length; i++)
            {
                C2SettlementDipVillageV336LikeOriginal village = villages[i];
                if (village == null) continue;
                int owner;
                int resource;
                int maxWorkers;
                int produceTicks;
                if (!village.C2TryGetNationCitySettlementStateV384ALikeOriginal(
                        out owner, out resource, out maxWorkers, out produceTicks)) continue;
                if ((uint)owner >= 7 || (uint)resource >= ResourceCount || maxWorkers <= 0 || produceTicks <= 0) continue;
                nextSettlements[owner]++;
            }

            // Megapolis.cpp::City::EnumUnits scans the central Group[] table.
            // C2GetActiveUnitsSnapshotV359LikeOriginal is the Unity port's central
            // OneObject registry; this is authoritative simulation state, not HUD scanning.
            C2NeutralPeasantUnitInfoV2LikeOriginal[] units =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; units != null && i < units.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = units[i];
                if (unit == null || unit.IsDeadLikeOriginal) continue;

                C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                    C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);

                // Megapolis.cpp:
                // if(!(NM->Building||NM->NoFarm||(OB->Sdoxlo&&!OB->Hidden))) Nat->NGidot++;
                // Settlement DIP units keep NNUM==7, so they do not consume the
                // owning nation's living places merely because CombatNation maps to owner.
                int visualNation = unit.Nation;
                if ((uint)visualNation < 7 && !md.Building && !md.NoFarm)
                    nextNGidot[visualNation]++;

                C2GameplayUnitTaskV1 task = unit.GetComponent<C2GameplayUnitTaskV1>();
                if (task == null) continue;
                int workerNation;
                int resourceId;
                bool settlementWorker;
                if (!task.C2TryGetResourcePeasantAssignmentV384ALikeOriginal(
                        out workerNation, out resourceId, out settlementWorker)) continue;
                if ((uint)workerNation >= 7 || (uint)resourceId >= ResourceCount) continue;

                if (settlementWorker)
                {
                    // vdf_GetAmountOfPeasant -> DIP_SimpleBuilding::setlGetWorkers.
                    nextSettlementPeasants[workerNation, resourceId]++;
                }
                else if (md.Peasant)
                {
                    // Megapolis.cpp walks OB->LocalOrder and counts TakeResLink.
                    // _takeResourceV222 is the current port's persistent TakeRes order state.
                    nextRegularPeasants[workerNation, resourceId]++;
                }
            }

            // Megapolis.cpp::City::EnumUnits rebuilds NFarms from ready buildings.
            C2SettlementBuildingSelectableV1LikeOriginal[] buildings =
                UnityEngine.Object.FindObjectsByType<C2SettlementBuildingSelectableV1LikeOriginal>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; buildings != null && i < buildings.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal building = buildings[i];
                if (building == null || !building.ReadyLikeOriginal || building.LifeLikeOriginal <= 0) continue;
                int nation = building.Nation;
                if ((uint)nation >= 7) continue;

                C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                    C2OriginalProduceCatalogV13.LoadMdInfoForSelectedBuilding(building);
                if (md.FarmCapacity > 0)
                    nextNFarms[nation] += md.FarmCapacity;

                int newNFarm;
                int farmsPerSettlement;
                string unitId = !string.IsNullOrWhiteSpace(building.SourceMonsterId)
                    ? building.SourceMonsterId
                    : building.KindName;
                if (C2OriginalProduceCatalogV13.TryGetSettlementFarmRuleV384ALikeOriginal(
                        unitId, out newNFarm, out farmsPerSettlement))
                {
                    nextNFarms[nation] += Mathf.Max(0, newNFarm);
                    if (farmsPerSettlement > 0)
                        nextNFarms[nation] += farmsPerSettlement * nextSettlements[nation];
                }
            }

            bool changed = false;
            for (int nation = 0; nation < NationCount; nation++)
            {
                if (NGidot[nation] != nextNGidot[nation] ||
                    NFarms[nation] != nextNFarms[nation] ||
                    ProductiveSettlements[nation] != nextSettlements[nation]) changed = true;
                NGidot[nation] = nextNGidot[nation];
                NFarms[nation] = Mathf.Max(0, nextNFarms[nation]);
                ProductiveSettlements[nation] = nextSettlements[nation];
                for (int r = 0; r < ResourceCount; r++)
                {
                    int rp = nextSettlementPeasants[nation, r] + nextRegularPeasants[nation, r];
                    if (ResourcePeasants[nation, r] != rp ||
                        SettlementPeasants[nation, r] != nextSettlementPeasants[nation, r] ||
                        RegularTakeResPeasants[nation, r] != nextRegularPeasants[nation, r]) changed = true;
                    ResourcePeasants[nation, r] = rp;
                    SettlementPeasants[nation, r] = nextSettlementPeasants[nation, r];
                    RegularTakeResPeasants[nation, r] = nextRegularPeasants[nation, r];
                }
            }
            if (changed) _revision++;
        }

        internal static string BuildAuditLikeOriginal(int nation)
        {
            if ((uint)nation >= 7) return "nation=invalid";
            return "nation=" + nation.ToString(CultureInfo.InvariantCulture) +
                   " NGidot=" + NGidot[nation].ToString(CultureInfo.InvariantCulture) +
                   " NFarms=" + NFarms[nation].ToString(CultureInfo.InvariantCulture) +
                   " ResourcePeasants=[" + JoinResourcesLikeOriginal(ResourcePeasants, nation) + "]" +
                   " settlement=[" + JoinResourcesLikeOriginal(SettlementPeasants, nation) + "]" +
                   " takeRes=[" + JoinResourcesLikeOriginal(RegularTakeResPeasants, nation) + "]" +
                   " settlements=" + ProductiveSettlements[nation].ToString(CultureInfo.InvariantCulture) +
                   " producerInside=0_not_ported" +
                   " revision=" + _revision.ToString(CultureInfo.InvariantCulture);
        }

        private static string JoinResourcesLikeOriginal(int[,] values, int nation)
        {
            return values[nation, 0].ToString(CultureInfo.InvariantCulture) + "," +
                   values[nation, 1].ToString(CultureInfo.InvariantCulture) + "," +
                   values[nation, 2].ToString(CultureInfo.InvariantCulture) + "," +
                   values[nation, 3].ToString(CultureInfo.InvariantCulture) + "," +
                   values[nation, 4].ToString(CultureInfo.InvariantCulture) + "," +
                   values[nation, 5].ToString(CultureInfo.InvariantCulture);
        }

        private static void ClearStateLikeOriginal()
        {
            Array.Clear(NGidot, 0, NGidot.Length);
            Array.Clear(NFarms, 0, NFarms.Length);
            Array.Clear(ResourcePeasants, 0, ResourcePeasants.Length);
            Array.Clear(SettlementPeasants, 0, SettlementPeasants.Length);
            Array.Clear(RegularTakeResPeasants, 0, RegularTakeResPeasants.Length);
            Array.Clear(ProductiveSettlements, 0, ProductiveSettlements.Length);
        }

        private static void EnsureDriverLikeOriginal()
        {
            if (_driver != null) return;
            GameObject go = new GameObject("C2_NationCityRuntime_V384A");
            _driver = go.AddComponent<C2NationCityRuntimeDriverV384ALikeOriginal>();
        }
    }

    internal sealed class C2NationCityRuntimeDriverV384ALikeOriginal : MonoBehaviour
    {
        private float _nextRefresh;
        private float _nextAudit;
        private string _lastAudit = string.Empty;

        private void Update()
        {
            float now = Time.unscaledTime;
            if (now >= _nextRefresh)
            {
                _nextRefresh = now + 0.20f;
                C2NationCityRuntimeV384ALikeOriginal.RefreshNowLikeOriginal("tick");
            }

            if (now >= _nextAudit)
            {
                _nextAudit = now + 2.0f;
                int nation = C2EditorRuntimeStateV333LikeOriginal.ControlledNation;
                if ((uint)nation < 7)
                {
                    string audit = C2NationCityRuntimeV384ALikeOriginal.BuildAuditLikeOriginal(nation);
                    if (!string.Equals(audit, _lastAudit, StringComparison.Ordinal))
                    {
                        _lastAudit = audit;
                        Debug.Log("[C2:NATION CITY V384A] " + audit);
                    }
                }
            }
        }
    }
}
