using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal static class C2SpriteDepthLayerLikeOriginal
    {
        internal const string LayerName = "C2_SpriteDepth";
        internal const int FallbackLayerIndex = 7;
        internal const bool UseSeparateSpriteDepthCamera = true;

        internal static int LayerIndex
        {
            get
            {
                int named = LayerMask.NameToLayer(LayerName);
                return named >= 0 ? named : FallbackLayerIndex;
            }
        }

        internal static int Mask => 1 << LayerIndex;

        internal static void SetLayerRecursive(GameObject go)
        {
            if (go == null)
                return;

            int layer = LayerIndex;
            SetLayerRecursive(go, layer);
        }

        internal static void SetLayerRecursive(GameObject go, int layer)
        {
            if (go == null)
                return;

            go.layer = layer;
            Transform tr = go.transform;
            for (int i = 0; i < tr.childCount; i++)
                SetLayerRecursive(tr.GetChild(i).gameObject, layer);
        }
    }

    /// <summary>
    /// Cossacks II draws FogOfWar after terrain, buildings and units, but before
    /// the map interface. Keeping the fog on its own camera/layer preserves that
    /// pass order instead of relying on transparent material sorting.
    /// </summary>
    internal static class C2FogOverlayLayerLikeOriginal
    {
        internal const string LayerName = "C2_FogOverlay";
        internal const int FallbackLayerIndex = 8;

        internal static int LayerIndex
        {
            get
            {
                int named = LayerMask.NameToLayer(LayerName);
                return named >= 0 ? named : FallbackLayerIndex;
            }
        }

        internal static int Mask => 1 << LayerIndex;

        internal static void SetLayerRecursive(GameObject go)
        {
            if (go == null) return;
            SetLayerRecursive(go, LayerIndex);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            Transform tr = go.transform;
            for (int i = 0; i < tr.childCount; i++)
                SetLayerRecursive(tr.GetChild(i).gameObject, layer);
        }
    }
}
