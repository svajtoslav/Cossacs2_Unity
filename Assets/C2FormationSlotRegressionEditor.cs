#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cossacks2Bridge.UnityAdapters.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit batch entry points only. These checks exercise the production
// template builder and obstacle optimizer, not a copied implementation.
public static class C2FormationSlotRegressionEditor
{
    const string Data = "C:/GSC Game World/Cossacks II/Data";
    static bool baseline;
    static MethodInfo buildSlots;
    static MethodInfo optimizeSlots;

    public static void Run() { Begin(false); }
    public static void RunBaseline() { Begin(true); }

    static void Begin(bool recordBaseline)
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Batch only; preserves the user's saved scenes.");
        baseline = recordBaseline;
        C2RegressionSceneScopeEditor.Begin();
        EditorApplication.delayCall += Execute;
    }

    static void Execute()
    {
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var runtime = typeof(C2BattleTerrainMode).Assembly.GetType(
                "Cossacks2Bridge.UnityAdapters.Maps.C2FormationRuntimeV167LikeOriginal", true);
            buildSlots = runtime.GetMethod("BuildTemplateSlotsDirectedV352LikeOriginal",
                BindingFlags.Static | BindingFlags.NonPublic);
            optimizeSlots = runtime.GetMethod("OptimizeFormationSlotsForMotionFieldV347LikeOriginal",
                BindingFlags.Static | BindingFlags.NonPublic);
            Require(buildSlots != null && optimizeSlots != null, "Production methods missing.");

            C2FormationCreateCatalogV165LikeOriginal.ForceReloadLikeOriginal();
            var square = Template("#SQUARE120");
            Require(C2FormationCreateCatalogV165LikeOriginal.AuditLikeOriginal
                    .Replace('\\', '/').Contains(Data + "/orders.lst"),
                "The test must use the installed Cossacks II orders.lst.");
            Require(square.UnitCount == 120, "The shipped square must contain 120 soldiers.");

            if (baseline)
            {
                var slots = Build(square, 224, 100, 65536, 65536, 0, false);
                var original = new List<Vector2>(slots);
                Optimize(slots, 65536, 65536);
                int changed = Changed(original, slots);
                Require(changed > 0, "Baseline no longer reproduces the empty-field displacement.");
                Debug.Log("[C2 FORMATION SLOT BASELINE] reproduced template=#SQUARE120 direction=224" +
                    " soldiers=120 displaced=" + changed +
                    " obstacleCount=0; production C# optimizer executed.");
            }
            else
            {
                CheckFreeTemplates();
                CheckCommandGeometry();
                CheckVacantFormationPlaces();
                Debug.Log(C2FormationSymmetryRegressionEditor.CheckLoadedCatalog());
                CheckDistinctNearbyPlacesAndDuplicates();
                CheckRealObstacle();
                Debug.Log("[C2 FORMATION SLOT REGRESSION] PASS; geometry/obstacle checks only;" +
                    " movement, preview, combat and FPS are separate acceptance gates.");
            }
            C2RegressionSceneScopeEditor.Finish(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            C2RegressionSceneScopeEditor.Finish(1);
        }
    }

    static C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal
        Template(string id)
    {
        Require(C2FormationCreateCatalogV165LikeOriginal.TryGetOrderTemplateLikeOriginal(id, out var template),
            "Missing original template " + id);
        return template;
    }

    static List<Vector2> Build(
        C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal template,
        byte direction, int spacing, float cx, float cy, int commands, bool depleted)
    {
        var option = new C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal
            { OrderId = template.OrderId, OrderTemplate = template, UnitCount = template.UnitCount };
        int count = template.UnitCount + commands - (depleted ? 7 : 0);
        return (List<Vector2>)buildSlots.Invoke(null,
            new object[] { option, count, commands, null, cx, cy, spacing, direction });
    }

    static void Optimize(List<Vector2> slots, float cx, float cy)
    {
        optimizeSlots.Invoke(null, new object[] { slots, null, cx, cy });
    }

    static int Changed(List<Vector2> before, List<Vector2> after)
    {
        Require(before.Count == after.Count, "Optimizer changed the member count.");
        int changed = 0;
        for (int i = 0; i < before.Count; i++)
            if (!before[i].Equals(after[i])) changed++;
        return changed;
    }

    static void CheckFreeTemplates()
    {
        int cases = 0, places = 0;
        foreach (string id in new[] { "#SQUARE120", "#LINE120COS", "#KARE120COS",
            "#PRUS45COS", "#TRI1COS45", "#SHERCOS" })
        {
            var template = Template(id);
            int templateCases = 0;
            foreach (int spacing in new[] { 50, 100, 200 })
            foreach (Vector2 phase in new[] { new Vector2(0,0), new Vector2(64,192),
                new Vector2(128,64), new Vector2(192,128) })
            foreach (bool depleted in new[] { false, true })
            for (int direction = 0; direction < 256; direction++)
            {
                float cx = 65536 + phase.x, cy = 65536 + phase.y;
                var slots = Build(template, (byte)direction, spacing, cx, cy, 0, depleted);
                var before = new List<Vector2>(slots);
                Require(new HashSet<Vector2>(slots).Count == slots.Count,
                    "Template builder produced actual duplicate coordinates: " + id);
                Optimize(slots, cx, cy);
                Require(Changed(before, slots) == 0,
                    $"Free-field places changed: {id}, dir={direction}, spacing={spacing}, phase={phase}.");
                places += slots.Count; cases++; templateCases++;
            }
            Debug.Log($"[C2 FORMATION SLOT MATRIX] template={id} cases={templateCases} displaced=0");
        }

        // Command members occupy their separate C places; include them as well.
        var commandTemplate = Template("#SQUARE120");
        for (int direction = 0; direction < 256; direction++)
        {
            var slots = Build(commandTemplate, (byte)direction, 100, 65536, 65536, 3, false);
            var before = new List<Vector2>(slots);
            Require(slots.Count == 123 && new HashSet<Vector2>(slots).Count == 123,
                "Command and soldier places must be distinct.");
            Optimize(slots, 65536, 65536);
            Require(Changed(before, slots) == 0, "Command places displaced on free field.");
            places += slots.Count; cases++;
        }
        Debug.Log($"[C2 FORMATION SLOT MATRIX] PASS cases={cases} places={places}" +
            " directions=256 infantry/cavalry spacing=50/100/200 depleted=yes commands=yes");
    }

    static void CheckDistinctNearbyPlacesAndDuplicates()
    {
        // Distinct points may share one 16-pixel motion cell. A subpixel
        // difference also matters because destinations are stored in Real units.
        var distinct = new List<Vector2> {
            new Vector2(65536,65536), new Vector2(65537,65536),
            new Vector2(65536,65537), new Vector2(65600,65600)
        };
        var before = new List<Vector2>(distinct);
        Optimize(distinct, 65536, 65536);
        Require(Changed(before, distinct) == 0, "Distinct Real-coordinate destinations merged.");

        var duplicates = new List<Vector2> {
            new Vector2(65536,65536), new Vector2(65536,65536), new Vector2(65536,65536)
        };
        Optimize(duplicates, 65536, 65536);
        Require(new HashSet<Vector2>(duplicates).Count == duplicates.Count,
            "Exact duplicate destinations were no longer resolved.");
        Debug.Log("[C2 FORMATION SLOT IDENTITY] PASS distinct subpixel places retained; duplicates resolved");
    }

    static void CheckCommandGeometry()
    {
        Type runtime = typeof(C2BattleTerrainMode).Assembly.GetType(
            "Cossacks2Bridge.UnityAdapters.Maps.C2FormationRuntimeV167LikeOriginal", true);
        Type groupType = runtime.GetNestedType("RuntimeFormationV172LikeOriginal", BindingFlags.NonPublic);
        object group = Activator.CreateInstance(groupType, true);
        groupType.GetField("SoldierMemberId").SetValue(group, "AusGrn");
        groupType.GetField("Direction").SetValue(group, (byte)17);
        MethodInfo preview = runtime.GetMethod("BuildFormationSoldierPreviewAtV359LikeOriginal",
            BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo order = runtime.GetMethod("BuildFormationOrderSlotsAtV359LikeOriginal",
            BindingFlags.NonPublic | BindingFlags.Static);
        Require(preview != null && order != null, "Shared destination methods missing.");
        int cases = 0, places = 0;
        foreach (string id in new[] { "#SQUARE120", "#LINE120COS", "#KARE120COS",
            "#PRUS45COS", "#TRI1COS45", "#SHERCOS" })
        {
            var template = Template(id);
            var option = new C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal
                { OrderId = id, OrderTemplate = template, UnitCount = template.UnitCount };
            foreach (int commandCount in new[] { 0, Math.Min(3, template.CommandCount) })
            foreach (bool depleted in new[] { false, true })
            foreach (int mdScale in new[] { 75, 100, 150 })
            {
                int soldierCount = template.UnitCount - (depleted ? 7 : 0);
                var units = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
                for (int i = 0; i < commandCount + soldierCount; i++)
                {
                    var unit = new C2NeutralPeasantUnitInfoV2LikeOriginal {
                        SourceMonsterId = i < commandCount ? "AusOf" : "AusGrn",
                        RealX = 65536 + i, RealY = 65536 - i
                    };
                    var md = new C2UnitOriginalRuntimeAndRendererV1.MdModel {
                        FormationDistanceScale = i < commandCount ? 999 : mdScale
                    };
                    var link = new C2UnitOriginalRuntimeLinkLikeOriginal {
                        Runtime = new C2UnitOriginalRuntime { Md = md }
                    };
                    unit.BindRuntimeLinkV364LikeOriginal(link);
                    units.Add(unit);
                }
                var identity = units.ToArray();
                foreach (int spacing in new[] { 50, 100, 200 })
                {
                    groupType.GetField("SpacingPercent").SetValue(group, spacing);
                    for (int direction = 0; direction < 256; direction++)
                    {
                        // Fractional centres exercise the same final rounding as an order.
                        float cx = 70000.25f, cy = 67000.75f;
                        var args = new object[] { group, units, option, cx, cy, (byte)direction };
                        var marks = (List<Vector2>)preview.Invoke(null, args);
                        var destinations = (List<Vector2>)order.Invoke(null, args);
                        Require(marks.Count == soldierCount && destinations.Count == units.Count,
                            "Wrong soldier/command count in preview: " + id);
                        for (int i = 0; i < soldierCount; i++)
                        {
                            Vector2 expected = ReferenceSoldierPlace(template.SoldierPoints[i],
                                direction, cx, cy, mdScale, spacing);
                            Require((int)marks[i].x % 16 == 0 && (int)marks[i].y % 16 == 0,
                                "Destination must use original integer pixels.");
                            Require(marks[i].Equals(expected), "Preview differs from C2 PORD formula: " + id);
                            Require(marks[i].Equals(destinations[commandCount + i]),
                                "Preview/order destination differs: " + id);
                        }
                        for (int i = 0; i < units.Count; i++)
                            Require(ReferenceEquals(identity[i], units[i]), "Preview reordered live members.");
                        Require((byte)groupType.GetField("Direction").GetValue(group) == 17,
                            "Preview changed brigade direction.");
                        cases++; places += soldierCount;
                    }
                }
            }
        }
        CheckCommandTargetTransform(runtime);
        Debug.Log($"[C2 FORMATION COMMAND GEOMETRY] PASS cases={cases} soldierPlaces={places}" +
            " directions=256 MD=75/100/150 spacing=50/100/200 commandScaleIgnored=yes depleted=yes");
    }

    static void CheckVacantFormationPlaces()
    {
        Type runtime = typeof(C2BattleTerrainMode).Assembly.GetType(
            "Cossacks2Bridge.UnityAdapters.Maps.C2FormationRuntimeV167LikeOriginal", true);
        Type groupType = runtime.GetNestedType("RuntimeFormationV172LikeOriginal", BindingFlags.NonPublic);
        object group = Activator.CreateInstance(groupType, true);
        groupType.GetField("SoldierMemberId").SetValue(group, "AusGrn");
        groupType.GetField("CommandSlotCount").SetValue(group, 3);
        var preview = runtime.GetMethod("BuildFormationSoldierPreviewAtV359LikeOriginal", BindingFlags.NonPublic | BindingFlags.Static);
        var order = runtime.GetMethod("BuildFormationOrderSlotsAtV359LikeOriginal", BindingFlags.NonPublic | BindingFlags.Static);
        var swap = runtime.GetMethod("ApplyFormationTurnSwapV360LikeOriginal", BindingFlags.NonPublic | BindingFlags.Static);
        var template = Template("#SQUARE120");
        var option = new C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal {
            OrderId = template.OrderId, OrderTemplate = template, UnitCount = template.UnitCount
        };
        var units = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
        for (int i = 0; i < template.UnitCount + 3; i++)
        {
            if (i == 0 || i == 3 || i == 19) { units.Add(null); continue; }
            var unit = new C2NeutralPeasantUnitInfoV2LikeOriginal {
                SourceMonsterId = i < 3 ? "AusOf" : "AusGrn", RealX = 65536, RealY = 65536
            };
            unit.BindRuntimeLinkV364LikeOriginal(new C2UnitOriginalRuntimeLinkLikeOriginal {
                Runtime = new C2UnitOriginalRuntime { Md = new C2UnitOriginalRuntimeAndRendererV1.MdModel {
                    FormationDistanceScale = i < 3 ? 999 : 75
                } }
            });
            units.Add(unit);
        }
        var before = units.ToArray();
        for (int d = 0; d < 256; d++)
        {
            var args = new object[] { group, units, option, 70000.25f, 67000.75f, (byte)d };
            var marks = (List<Vector2>)preview.Invoke(null, args);
            var destinations = (List<Vector2>)order.Invoke(null, args);
            Require(marks.Count == 120 && destinations.Count == 123, "Vacant places were compacted.");
            for (int i = 0; i < 120; i++)
            {
                var expected = ReferenceSoldierPlace(template.SoldierPoints[i], d, 70000.25f, 67000.75f, 75, 100);
                Require(marks[i].Equals(expected) && destinations[i + 3].Equals(expected),
                    "Dead command/first soldier shifted order places or MD spacing.");
            }
        }
        // Exercise the actual field-move adapter. Deliberately give the group a
        // stale stored direction; the physical positions must choose the swap.
        groupType.GetField("Direction").SetValue(group, (byte)7);
        var physicalMethod = runtime.GetMethod("GetFormationPhysicalDirectionV360LikeOriginal", BindingFlags.NonPublic | BindingFlags.Static);
        byte physical = (byte)physicalMethod.Invoke(null, new object[] { template, units, 3 });
        var moveSwap = runtime.GetMethod("ApplyFormationSymmetricMoveV360LikeOriginal", BindingFlags.NonPublic | BindingFlags.Static);
        bool applied = (bool)moveSwap.Invoke(null, new object[] { group, units, template, unchecked((byte)(physical + 128)) });
        Require(applied, "Field movement did not use original inverse.");
        for (int i = 0; i < 3; i++) Require(ReferenceEquals(units[i], before[i]), "Command place swapped with a soldier.");
        for (int i = 0; i < 120; i++) Require(ReferenceEquals(units[i + 3], before[template.SymInv[i] + 3]),
            "Turn adapter lost soldier/vacancy identity.");
        swap.Invoke(null, new object[] { units, 3, template.SymInv });
        for (int i = 0; i < before.Length; i++) Require(ReferenceEquals(units[i], before[i]), "Two inversions did not restore membership.");
        Debug.Log("[C2 FORMATION VACANT PLACES] PASS directions=256 deadCommand=yes deadFirstSoldier=yes fieldMove=yes inverseRoundTrip=yes");
    }

    static Vector2 ReferenceSoldierPlace(
        C2FormationCreateCatalogV165LikeOriginal.C2FormationPointV165LikeOriginal point,
        int direction, float cx, float cy, int mdScale, int spacing)
    {
        // Independent transcription of COSSACKS2/Groups.cpp CreateSimpleOrdPos,
        // using the shipped direction tables, not the runtime slot builder.
        int dx = C2OriginalMovementMathV352.TCos[direction];
        int dy = C2OriginalMovementMathV352.TSin[direction];
        int length = (Math.Max(Math.Abs(dx), Math.Abs(dy)) + Math.Abs(dx) + Math.Abs(dy)) >> 1;
        int vx = dx * 1080 / length, vy = dy * 1080 / length;
        int scale = mdScale * spacing / 100;
        int x = (int)Math.Round(cx, MidpointRounding.ToEven) -
            (((point.X * vy - point.Y * vx) * scale / 800) * 270 >> 8);
        int y = (int)Math.Round(cy, MidpointRounding.ToEven) +
            (((point.X * vx + point.Y * vy) * scale / 800) * 270 >> 8);
        // Brigade.cpp stores pixel positions, and Groups.cpp draws px/py >> 4.
        return new Vector2((x >> 4) << 4, (y >> 4) << 4);
    }

    static void CheckCommandTargetTransform(Type runtime)
    {
        var method = runtime.GetMethod("ComputeFormationCommandTargetV359LikeOriginal",
            BindingFlags.NonPublic | BindingFlags.Static);
        var expected = new[] {
            new Vector2(1100, 2200), new Vector2(800, 2100),
            new Vector2(900, 1800), new Vector2(1200, 1900)
        };
        for (int i = 0; i < 4; i++)
        {
            var args = new object[] { 500f, 800f, 400f, 600f, 1000f, 2000f,
                (byte)240, i * 64, 0f, 0f, (byte)0 };
            method.Invoke(null, args);
            Require(new Vector2((float)args[8], (float)args[9]).Equals(expected[i]),
                "Selected brigade offset rotated around the wrong centre.");
            Require((byte)args[10] == (byte)((240 + i * 64) & 255),
                "Selected brigade facing failed to wrap.");
        }
        Debug.Log("[C2 FORMATION COMMAND TARGET] PASS selection offsets and facing wrap");
    }

    static void CheckRealObstacle()
    {
        var go = new GameObject("Formation regression LOCKPOINTS");
        try
        {
            var info = go.AddComponent<C2BuildingRuntimeInfoV247LikeOriginal>();
            info.ZoneQuads.Add(new C2BuildingRuntimeZoneQuadV247LikeOriginal(
                "LOCKPOINTS", Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, 256, 256));
            InvalidateBlockedCells();
            var target = new Vector2(256 * 256 + 128, 256 * 256 + 128);
            var clear = target + new Vector2(4096, 4096);
            Require(C2BuildingRuntimeInfoV247LikeOriginal.IsBlockedForUnitRealV247LikeOriginal(
                target.x, target.y, 1), "Fixture must activate the real building motion field.");
            var slots = new List<Vector2> { target, clear };
            Optimize(slots, target.x, target.y);
            Require(!slots[0].Equals(target), "Blocked building place was retained.");
            Require(!C2BuildingRuntimeInfoV247LikeOriginal.IsBlockedForUnitRealV247LikeOriginal(
                slots[0].x, slots[0].y, 1), "Replacement place is still blocked.");
            Require(slots[1].Equals(clear), "Unrelated free place moved.");
            Debug.Log("[C2 FORMATION SLOT OBSTACLE] PASS real LOCKPOINTS respected; clear place retained");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            InvalidateBlockedCells();
        }
    }

    static void InvalidateBlockedCells()
    {
        typeof(C2BuildingRuntimeInfoV247LikeOriginal).GetField("s_blockedCellsCacheUntilV247",
            BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, -1f);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
