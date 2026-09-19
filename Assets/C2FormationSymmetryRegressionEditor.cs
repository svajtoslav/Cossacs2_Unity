#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Cossacks2Bridge.UnityAdapters.Maps;
using Catalog = Cossacks2Bridge.UnityAdapters.Maps.C2FormationCreateCatalogV165LikeOriginal;

// Pure checks: can run against the compiled catalog without native Unity services.
public static class C2FormationSymmetryRegressionEditor
{
    public static string CheckLoadedCatalog()
    {
        int templates = 0, halves = 0, quarters = 0, entries = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string pendingId = null;
        int unfinished = 0;
        foreach (string raw in File.ReadAllLines("C:/GSC Game World/Cossacks II/Data/orders.lst", Encoding.GetEncoding(1251)))
        {
            string text = raw.Trim();
            if (text == "#EXIT") { if (pendingId != null) unfinished++; break; }
            if (!text.StartsWith("#", StringComparison.Ordinal)) continue;
            if (text != "#END")
            {
                pendingId = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)[0];
                continue;
            }
            // Original LoadOrders builds symmetry only at #END. The installed file
            // ends with an unfinished #KARE120PIK; do not invent its finalized data.
            string id = pendingId;
            pendingId = null;
            Require(id != null, "Order terminator without a header.");
            if (!seen.Add(id)) continue;
            Require(Catalog.TryGetOrderTemplateLikeOriginal(id, out var t), "Missing order " + id);
            templates++;
            if (t.Symmetry == "SYM2" || t.Symmetry == "SYM4")
            {
                CheckPermutation(t.SymInv, t.UnitCount, id + " SYM2");
                halves++; entries += t.SymInv.Length;
                for (int i = 0; i < t.UnitCount; i++)
                    Require(t.SymInv[t.SymInv[i]] == i, "Half-turn is not an involution: " + id);
            }
            else Require(t.SymInv == null && t.Sym4f == null && t.Sym4i == null,
                "Undeclared symmetry added: " + id);
            if (t.Symmetry == "SYM4")
            {
                CheckPermutation(t.Sym4f, t.UnitCount, id + " SYM4f");
                CheckPermutation(t.Sym4i, t.UnitCount, id + " SYM4i");
                quarters++; entries += t.Sym4f.Length + t.Sym4i.Length;
                for (int i = 0; i < t.UnitCount; i++)
                {
                    Require(t.Sym4i[t.Sym4f[i]] == i, "Quarter-turn maps do not undo each other: " + id);
                    Require(t.Sym4f[t.Sym4f[i]] == t.SymInv[i], "Two quarter turns differ from half turn: " + id);
                }
            }
        }
        CheckSparseColumnRanks();
        CheckQuarterTurnAndVacancies();
        CheckDirectionBoundaries();
        CheckPhysicalDirection();
        Require(unfinished == 1, "Unexpected unfinished order count: " + unfinished);
        Require(templates == 144, "Unexpected installed orders.lst coverage: " + templates);
        return "[C2 FORMATION SYMMETRY] PASS templates=" + templates + " halfTurnTables=" + halves +
            " quarterTurnPairs=" + quarters + " mapEntries=" + entries + " unfinishedHeaders=" + unfinished +
            " sparseColumns=yes vacancies=yes directionDeltas=256 physicalDirection=yes";
    }

    static void CheckPermutation(int[] map, int count, string label)
    {
        Require(map != null && map.Length == count, "Missing/wrong table: " + label);
        var seen = new HashSet<int>();
        foreach (int i in map)
            Require(i >= 0 && i < count && seen.Add(i), "Non-bijective table: " + label);
    }

    static Catalog.C2FormationOrderTemplateV165LikeOriginal Pattern(string symmetry, params int[] xy)
    {
        var t = new Catalog.C2FormationOrderTemplateV165LikeOriginal { Symmetry = symmetry };
        for (int i = 0; i < xy.Length; i += 2)
            t.SoldierPoints.Add(new Catalog.C2FormationPointV165LikeOriginal {
                X = xy[i], LineIndex = xy[i + 1]
            });
        t.UnitCount = t.SoldierPoints.Count;
        C2FormationSymmetryLikeOriginal.Build(t);
        return t;
    }

    static void CheckSparseColumnRanks()
    {
        // No point exists at (2,1): geometric rotation cannot produce the original
        // SYM2 mapping, but matching column ordinals can.
        var t = Pattern("SYM2", 0,0, 2,0, 0,1, 2,2);
        Equal(t.SymInv, new[] { 3,2,1,0 }, "Staggered column-rank inversion");
        var invalid = Pattern("SYM2", 0,0, 2,0, 0,1);
        Require(invalid.SymInv == null && invalid.SymmetryAudit == "invalid_SYM2",
            "Invalid declared inverse was silently accepted.");
    }

    static void CheckQuarterTurnAndVacancies()
    {
        var t = Pattern("SYM4", 0,0, 2,0, 0,1, 2,1);
        Equal(t.Sym4f, new[] { 2,0,3,1 }, "Quarter forward destination->source");
        Equal(t.Sym4i, new[] { 1,3,0,2 }, "Quarter reverse destination->source");
        var input = new[] { "a", null, "c" };
        var result = C2FormationSymmetryLikeOriginal.ApplySoldierSwap(input, t.SymInv);
        Require(result.Length == 4 && result[0] == null && result[1] == "c" &&
                result[2] == null && result[3] == "a", "Casualties compacted during inversion.");
        Require(input.Length == 3 && input[0] == "a" && input[1] == null && input[2] == "c",
            "Permutation modified its input.");
        bool rejected = false;
        try { C2FormationSymmetryLikeOriginal.ApplySoldierSwap(input, new[] { 0,0,2,3 }); }
        catch (ArgumentException) { rejected = true; }
        Require(rejected, "Invalid map accepted.");
    }

    static void CheckDirectionBoundaries()
    {
        var four = Pattern("SYM4", 0,0, 2,0, 0,1, 2,1);
        var two = Pattern("SYM2", 0,0, 2,0, 0,1, 2,1);
        var none = Pattern("NONE", 0,0, 2,0, 0,1, 2,1);
        for (int delta = -128; delta < 128; delta++)
        {
            const byte actual = 17, stored = 33;
            byte requested = unchecked((byte)(actual + delta));
            var map = C2FormationSymmetryLikeOriginal.SelectTurnSwap(four, stored, actual, requested, out byte next);
            int[] expected = Math.Abs(delta) < 32 ? null :
                delta >= 32 && delta < 96 ? four.Sym4f :
                delta <= -32 && delta > -96 ? four.Sym4i : four.SymInv;
            Require(ReferenceEquals(map, expected) && next == requested, "SYM4 turn boundary " + delta);
            map = C2FormationSymmetryLikeOriginal.SelectTurnSwap(two, stored, actual, requested, out next);
            expected = delta > 64 || delta < -64 ? two.SymInv : null;
            Require(ReferenceEquals(map, expected) && next == requested, "SYM2 turn boundary " + delta);
            map = C2FormationSymmetryLikeOriginal.SelectTurnSwap(none, stored, actual, requested, out next);
            byte expectedDirection = Math.Abs(delta) < 32 ? requested :
                unchecked((byte)(stored + (delta > 0 ? 32 : -32)));
            Require(map == null && next == expectedDirection, "Asymmetric turn boundary " + delta);
        }
    }

    static void CheckPhysicalDirection()
    {
        var t = Pattern("SYM4", -1,0, 1,0, -1,1, 1,1);
        t.FirstActualLine = 0; t.ActualLineCount = 2;
        int[] x = { -256,-256,256,256 }, y = { -256,256,-256,256 };
        // Independent square fixtures at four cardinal orientations. No stored
        // brigade direction is passed: movement positions must determine it.
        // Signed >>8 rounds negative vector components down in the C++ source.
        // Result vectors are (24,-1), (-1,24), (-26,-1), (-1,-26).
        byte[] expected = { 255, 65, 129, 191 };
        for (int quarter = 0; quarter < 4; quarter++)
        {
            var members = new C2FormationSymmetryLikeOriginal.MemberPosition[7];
            for (int i = 0; i < 3; i++) members[i] = new C2FormationSymmetryLikeOriginal.MemberPosition {
                Present = true, RealX = 65536, RealY = 65536
            };
            for (int i = 0; i < 4; i++)
            {
                int px = x[i], py = y[i];
                for (int k = 0; k < quarter; k++) { int oldX = px; px = -py; py = oldX; }
                members[i + 3] = new C2FormationSymmetryLikeOriginal.MemberPosition {
                    Present = true, RealX = 65536 + px, RealY = 65536 + py
                };
            }
            byte direction = C2FormationSymmetryLikeOriginal.DirectionByPositions(t, members, 3);
            Require(direction == expected[quarter], "Physical direction fixture: " + quarter + " actual=" + direction);
            var translated = (C2FormationSymmetryLikeOriginal.MemberPosition[])members.Clone();
            for (int i = 0; i < translated.Length; i++) { translated[i].RealX += 3200; translated[i].RealY += 6400; }
            Require(C2FormationSymmetryLikeOriginal.DirectionByPositions(t, translated, 3) == direction,
                "Translation changed formation direction.");
            // Missing positions must not contribute even if stale coordinates remain.
            members[3].Present = false;
            byte withVacancy = C2FormationSymmetryLikeOriginal.DirectionByPositions(t, members, 3);
            members[3].RealX = 123456; members[3].RealY = -54321;
            Require(C2FormationSymmetryLikeOriginal.DirectionByPositions(t, members, 3) == withVacancy,
                "Vacant member contributed to formation direction.");
        }
    }

    static void Equal(int[] a, int[] b, string label)
    {
        Require(a != null && a.Length == b.Length, label);
        for (int i = 0; i < a.Length; i++) Require(a[i] == b[i], label + " index=" + i);
    }
    static void Require(bool ok, string reason) { if (!ok) throw new InvalidOperationException(reason); }
}
#endif
