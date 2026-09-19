// C2BuildingDestructionRuntimeV260.cs
// V266: disabled broken V260-V265 destruction renderer.
// The active building death path is C2BuildingDeleteRuntimeLikeOriginal.cs copied/adapted from the old working project.
// This file remains only because current building creation calls C2BuildingDestructionV260AttachLikeOriginal.

using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private static void C2BuildingDestructionV260AttachLikeOriginal(
            GameObject parent,
            C2Building3InuRecordLikeOriginal record,
            C2BuildingMdInfoLikeOriginal md)
        {
            // Intentionally no-op. Do not attach renderer/DeathLie/FX components here.
        }

        public void C2BuildingDestructionV260AttachConstructionSiteLikeOriginal(
            GameObject constructionSiteRoot,
            C2RuntimeConstructionSiteProxyLikeOriginal constructionSite)
        {
            // V266B compile-compat no-op.
            // Construction sites are handled by C2BuildingDeleteRuntimeLikeOriginal.
            // Do not attach V260-V265 renderer/destruction components here.
        }


        public void C2BuildingDestructionV260AttachConstructionSiteLikeOriginal(
            GameObject constructionSiteRoot,
            C2RuntimeConstructionSitePseudo3DV245LikeOriginal constructionSite)
        {
            // V266C compile-compat no-op.
            // C2BuildingConstructionPseudo3DV245 passes the real runtime construction site type.
            // Keep V260-V265 destruction disabled; old-project delete/death pipeline is active in C2BuildingDeleteRuntimeLikeOriginal.
        }

    }
}
