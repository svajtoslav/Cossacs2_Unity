using System;
using System.Globalization;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const bool C2WallDambaFiftyChainTestV92EnabledLikeOriginal = false;
        private const int C2WallDambaFiftyChainTestV92SpriteIndexLikeOriginal = 60;
        private const int C2WallDambaFiftyChainTestV92CountLikeOriginal = 50;
        private const float C2WallDambaFiftyChainTestV92HeightBodiesLikeOriginal = 4.0f;
        private const string C2WallDambaFiftyChainTestV92ContractLikeOriginal = "V92_50_separate_W60_C2M_objects_manual_pair_delta_rigid_no_mesh_deform";

        private GameObject _c2WallDambaFiftyChainTestRootV92LikeOriginal;

        private void BuildWallDambaFiftyChainTestV92LikeOriginal()
        {
            if (!C2WallDambaFiftyChainTestV92EnabledLikeOriginal || _map == null || _terrainRoot == null)
                return;

            if (_c2WallDambaFiftyChainTestRootV92LikeOriginal != null)
                SafeDestroy(_c2WallDambaFiftyChainTestRootV92LikeOriginal);

            WallSpriteCatalogV1LikeOriginal catalog = LoadWallSpriteCatalogV1LikeOriginal();
            if (catalog == null ||
                !catalog.ByIndex.TryGetValue(C2WallDambaFiftyChainTestV92SpriteIndexLikeOriginal, out WallSpriteDescV1LikeOriginal desc) ||
                desc == null ||
                string.IsNullOrWhiteSpace(desc.ModelPath))
            {
                Debug.LogWarning("[C2:DAMBA 50 CHAIN V92] W60 catalog/model missing; test chain not spawned.");
                return;
            }

            WallC2MParsedMeshV23LikeOriginal c2m = TryLoadWallC2MVisualMeshV23LikeOriginal(desc.ModelPath, out string loadAudit);
            if (c2m == null)
            {
                Debug.LogWarning("[C2:DAMBA 50 CHAIN V92] C2M load failed: " + loadAudit);
                return;
            }

            OriginalTerrainKernelConfig kernel = CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            Vector3 stepWorld = new Vector3(
                C2WallObjectsV72DambaW60PairDeltaPixelsLikeOriginal.x * (kernel.BackingStepXWorld / 32.0f),
                0.0f,
                C2WallObjectsV72DambaW60PairDeltaPixelsLikeOriginal.y * (kernel.BackingStepZWorld * WorldZSign / 32.0f));

            Bounds bounds = BuildWallDambaCalibratorLocalBoundsV1LikeOriginal(c2m, kernel.HeightScale);
            float bodyHeight = Mathf.Max(8.0f, bounds.size.y);
            Vector3 origin = _terrainBounds.center;
            origin.y = _terrainBounds.max.y + bodyHeight * C2WallDambaFiftyChainTestV92HeightBodiesLikeOriginal;
            origin -= stepWorld * ((C2WallDambaFiftyChainTestV92CountLikeOriginal - 1) * 0.5f);

            _c2WallDambaFiftyChainTestRootV92LikeOriginal = new GameObject("C2_DAMBA_50_POINT_CHAIN_TEST_V92");
            _c2WallDambaFiftyChainTestRootV92LikeOriginal.transform.SetParent(transform, false);

            for (int i = 0; i < C2WallDambaFiftyChainTestV92CountLikeOriginal; i++)
            {
                GameObject go = new GameObject("V92_W60_" + i.ToString("000", CultureInfo.InvariantCulture));
                go.transform.SetParent(_c2WallDambaFiftyChainTestRootV92LikeOriginal.transform, false);
                go.transform.position = origin + stepWorld * i;
                AttachWallDambaCalibratorMeshV1LikeOriginal(go, desc, c2m, "V92_" + i.ToString("000", CultureInfo.InvariantCulture));
            }

            Debug.Log("[C2:DAMBA 50 CHAIN V92] spawned count=" + C2WallDambaFiftyChainTestV92CountLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " sprite=" + desc.Name + "#" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                      " model=" + desc.ModelPath +
                      " deltaPixels=(" + C2WallObjectsV72DambaW60PairDeltaPixelsLikeOriginal.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                      C2WallObjectsV72DambaW60PairDeltaPixelsLikeOriginal.y.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                      " stepWorld=(" + stepWorld.x.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                      stepWorld.y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                      stepWorld.z.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                      " contract=" + C2WallDambaFiftyChainTestV92ContractLikeOriginal);
        }
    }
}
