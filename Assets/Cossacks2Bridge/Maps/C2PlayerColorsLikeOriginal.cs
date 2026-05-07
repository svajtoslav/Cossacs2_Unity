using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public static class C2PlayerColorsLikeOriginal
    {
        // ColorID order follows the current battle-room flag order:
        // 0 red, 1 blue, 2 green, 3 yellow/orange, 4 violet, 5 cyan/turquoise, 6 white, 7 black.
        public static readonly Color32[] NatColors =
        {
            new Color32(0xA5, 0x00, 0x00, 0xFF), // 0 red
            new Color32(0x00, 0x3C, 0xC6, 0xFF), // 1 blue
            new Color32(0x4C, 0x9A, 0x5F, 0xFF), // 2 green
            new Color32(0xF7, 0x86, 0x10, 0xFF), // 3 yellow/orange
            new Color32(0x9C, 0x49, 0xB5, 0xFF), // 4 violet
            new Color32(0x48, 0xBF, 0xC3, 0xFF), // 5 cyan/turquoise
            new Color32(0xE7, 0xE3, 0xE7, 0xFF), // 6 white
            new Color32(0x29, 0x28, 0x39, 0xFF), // 7 black/dark
        };

        private static readonly int[] PlayerColorId = { 0, 1, 2, 3, 4, 5, 6, 7 };

        public static int MaxPlayers => PlayerColorId.Length;

        public static int ClampColorId(int colorId)
        {
            if (NatColors == null || NatColors.Length == 0) return 0;
            int m = NatColors.Length;
            colorId %= m;
            if (colorId < 0) colorId += m;
            return colorId;
        }

        public static int ClampPlayerIndex(int playerIndex)
        {
            if (PlayerColorId == null || PlayerColorId.Length == 0) return 0;
            if (playerIndex < 0) return 0;
            if (playerIndex >= PlayerColorId.Length) return playerIndex % PlayerColorId.Length;
            return playerIndex;
        }

        public static int GetPlayerColorId(int playerIndex)
        {
            int p = ClampPlayerIndex(playerIndex);
            return ClampColorId(PlayerColorId[p]);
        }

        public static void SetPlayerColorId(int playerIndex, int colorId)
        {
            int p = ClampPlayerIndex(playerIndex);
            int c = ClampColorId(colorId);
            PlayerColorId[p] = c;
        }

        public static Color32 GetNatColorByColorId(int colorId)
        {
            return NatColors[ClampColorId(colorId)];
        }

        public static Color32 GetNatColorByPlayer(int playerIndex)
        {
            return GetNatColorByColorId(GetPlayerColorId(playerIndex));
        }

        public static string CacheSuffixForPlayer(int playerIndex)
        {
            int colorId = GetPlayerColorId(playerIndex);
            Color32 c = GetNatColorByColorId(colorId);
            return "nat=" + colorId.ToString() + "_" + c.r.ToString() + "_" + c.g.ToString() + "_" + c.b.ToString();
        }
    }
}
