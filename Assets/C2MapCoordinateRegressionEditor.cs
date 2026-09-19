#if UNITY_EDITOR
using System;
using Cossacks2Bridge.UnityAdapters.Maps;

// Pure coordinate checks also run against Assembly-CSharp outside the editor.
// Flat-ground fixtures intentionally check straightness and absolute positions:
// a round-trip alone would accept the old matching pair of parity errors.
public static class C2MapCoordinateRegressionEditor
{
    [UnityEditor.MenuItem("Tools/Cossacks II/Diagnostics/Check map coordinates")]
    public static void Run() { UnityEngine.Debug.Log(CheckCoordinates()); }

    public static string CheckCoordinates()
    {
        int cases = 0;
        foreach (float step in new[] { 32f, 16f, 8f })
        foreach (float sign in new[] { -1f, 1f })
        foreach (float center in new[] { 0f, 4096f })
        {
            float scale = step / 32f;
            // COSSACKS2/3DGraph.cpp::GetTriY: row*32 minus 16 for odd columns.
            for (int col = 0; col < 8; col++)
            for (int row = 0; row < 8; row++)
            {
                float originalY = row * 32f - ((col & 1) != 0 ? 16f : 0f);
                C2OriginalWorldCoordinatesV371LikeOriginal.ToWorld(col * 32f, originalY,
                    step, step, center, center, sign, out float x, out float z);
                Near(x, col * step - center, "Terrain vertex X");
                Near(z, (row * step + ((col & 1) == 0 ? step / 2 : 0) - center) * sign,
                    "Terrain vertex half-cell applied twice");
                cases++;
            }
            // 120 marks in a horizontal row must stay horizontal across columns.
            for (int i = 0; i < 120; i++)
            {
                float px = 17.75f * i + 0.25f, py = 1234.5f;
                C2OriginalWorldCoordinatesV371LikeOriginal.ToWorld(px, py, step, step,
                    center, center, sign, out float x, out float z);
                Near(x, px * scale - center, "Absolute ground X");
                Near(z, ((py + 16f) * scale - center) * sign, "Flat row became staggered");
                Require(C2OriginalWorldCoordinatesV371LikeOriginal.ToOriginal(x, z,
                    step, step, center, center, sign, out float rx, out float ry), "Inverse failed");
                Near(rx, px, "Inverse X"); Near(ry, py, "Inverse Y");
                cases++;
            }
            // The old function jumped at every 32-pixel boundary. Check both sides.
            for (int col = -4; col <= 12; col++)
            {
                float left = col * 32f - 0.125f, right = col * 32f + 0.125f;
                C2OriginalWorldCoordinatesV371LikeOriginal.ToWorld(left, 256, step, step,
                    center, center, sign, out float x0, out float z0);
                C2OriginalWorldCoordinatesV371LikeOriginal.ToWorld(right, 256, step, step,
                    center, center, sign, out float x1, out float z1);
                Near(x1 - x0, 0.25f * scale, "Boundary X discontinuity");
                Near(z1, z0, "Boundary Z discontinuity"); cases++;
            }
        }
        Require(!C2OriginalWorldCoordinatesV371LikeOriginal.ToOriginal(0, 0, 0, 32, 0, 0, -1,
            out float invalidX, out float invalidY) && invalidX == 0 && invalidY == 0,
            "Degenerate mapping accepted");
        return "[C2 MAP COORDINATES V371] PASS cases=" + cases +
            " flatRows=yes boundaryContinuity=yes terrainVertices=yes zoomScales=3 signs=2";
    }

    static void Near(float a, float b, string reason)
    {
        if (Math.Abs(a - b) > 0.001f) throw new InvalidOperationException(reason + ": " + a + " != " + b);
    }
    static void Require(bool ok, string reason) { if (!ok) throw new InvalidOperationException(reason); }
}
#endif
