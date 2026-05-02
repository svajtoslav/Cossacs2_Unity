using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode : MonoBehaviour
    {
        private const bool C2NatureObjectsV1EnabledLikeOriginal = true;
        private const bool C2NatureObjectsV1DrawDebugCardsLikeOriginal = false;
        private const int C2NatureObjectsV1RenderQueueLikeOriginal = 2995;
        private const bool C2NatureObjectsV2DrawRealSpritesLikeOriginal = true;
        private const string C2NatureObjectsV3TreeResolverContractLikeOriginal = "TREES_gp_uses_Trees_g2d_or_TreesAll_g2d_no_Trees_g16";
        private const string C2NatureObjectsV3StoneResolverContractLikeOriginal = "STONES_gp_uses_STONES_g16_no_g2d";
        private const int C2NatureObjectsV2RenderQueueLikeOriginal = 3660;
        private const bool C2NatureObjectsV12DrawTreeShadowPassLikeOriginal = true;
        private const int C2NatureObjectsV12TreeShadowRenderQueueLikeOriginal = 3650;
        private const int C2NatureObjectsV14TreeShadowSortingOrderLikeOriginal = 32760;
        private const int C2NatureObjectsV14NatureVisibleSortingOrderLikeOriginal = 32767;
        private const float C2NatureObjectsV16TreeAlphaRefLikeOriginal = 0x40 / 255.0f;
        private const float C2NatureObjectsV16AnimatedTreeAlphaRefLikeOriginal = 0x10 / 255.0f;
        private const float C2NatureObjectsV16TreeShadowAlphaRefLikeOriginal = 0x04 / 255.0f;
        private const float C2NatureObjectsV17TreeShadowDiffuseRLikeOriginal = 0.0f;
        private const float C2NatureObjectsV17TreeShadowDiffuseGLikeOriginal = 0.0f;
        private const float C2NatureObjectsV17TreeShadowDiffuseBLikeOriginal = 0.0f;
        private const float C2NatureObjectsV17TreeShadowDiffuseALikeOriginal = 0.80f;
        private const bool C2NatureObjectsV12TreeAmplitudeSwayLikeOriginal = true;
        private const float C2NatureObjectsV16TreeRollAnglePerAmplitudeLikeOriginal = 0.0025f / 30.0f;
        private const float C2NatureObjectsV16TreeSwayBaseTimeDivisorMsLikeOriginal = 450.0f;
        private const float C2NatureObjectsV2YOffsetWorldLikeOriginal = 0.035f;
        private const float C2NatureObjectsV2FieldYOffsetWorldLikeOriginal = 0.045f;
        private const bool C2NatureObjectsV23TreeDepthMicroSeparationLikeOriginal = true;
        private const int C2NatureObjectsV23TreeDepthMicroLayersLikeOriginal = 64;
        private const float C2NatureObjectsV23TreeDepthMicroStepPixelsLikeOriginal = 0.0625f;
        private const int C2NatureObjectsV1MaxAuditSamplesLikeOriginal = 32;
        private const int C2NatureObjectsV1InstancesPerMeshLikeOriginal = 12000;
        private const float C2NatureObjectsV1DebugAlphaLikeOriginal = 0.62f;
        private const float C2NatureObjectsV1DebugYOffsetWorldLikeOriginal = 0.06f;
        private const bool C2NatureObjectsV3_2DrawGeneratedFieldPatchesLikeOriginal = false;
        private const bool C2NatureObjectsV3_2DrawFallbackWhenTextureMissingLikeOriginal = false;
        private const string C2NatureObjectsV1ContractLikeOriginal = "V26_FROM_V17_TRE2_GA_TS_original_tree_render_state_roll_sway_billboard_TreesAll_shadow_fine_indexed_micro_depth_lanes_shadow_boost";
        private const string C2NatureObjectsV6PivotContractLikeOriginal = "V26_keep_V17_orientation_foot_clamp_alpha_zwrite_ztest_add_fine_indexed_micro_depth_lanes_max4px_no_thinning_shadow_boost";
        private const string C2NatureObjectsV12TreeShadowContractLikeOriginal = "V26_FROM_V17_TreesShadow_L_XML_same_fine_indexed_micro_depth_lane_as_tree_shadow_alpha_0_80";

        private bool _c2NatureObjectsV1BuiltLikeOriginal;
        private GameObject _c2NatureObjectsRootV1LikeOriginal;
        private readonly Dictionary<string, Texture2D> _c2NatureTextureCacheV2LikeOriginal = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Material> _c2NatureMaterialCacheV2LikeOriginal = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Texture2D, Vector2> _c2NatureAlphaRowsCacheV9LikeOriginal = new Dictionary<Texture2D, Vector2>();

        private IEnumerator Start()
        {
            if (!C2NatureObjectsV1EnabledLikeOriginal)
                yield break;

            for (int i = 0; i < 300; i++)
            {
                if (_terrainBuilt && _terrainRoot != null && _map != null && _bootstrap != null && _bootstrap.Fs != null)
                {
                    BuildNatureObjectsLayerV1LikeOriginal();
                    yield break;
                }

                yield return null;
            }

            Debug.LogWarning("[C2:NATURE V1] skipped: terrain/bootstrap not ready after wait. contract=" + C2NatureObjectsV1ContractLikeOriginal);
        }

        private void BuildNatureObjectsLayerV1LikeOriginal()
        {
            if (_c2NatureObjectsV1BuiltLikeOriginal)
                return;
            _c2NatureObjectsV1BuiltLikeOriginal = true;

            if (!C2NatureObjectsV1EnabledLikeOriginal || _terrainRoot == null || _map == null || _bootstrap == null || _bootstrap.Fs == null)
                return;

            try
            {
                if (_c2NatureObjectsRootV1LikeOriginal != null)
                    SafeDestroy(_c2NatureObjectsRootV1LikeOriginal);

                _c2NatureObjectsRootV1LikeOriginal = new GameObject("C2_NatureObjects_V1_" + _selectedId);
                _c2NatureObjectsRootV1LikeOriginal.transform.SetParent(_terrainRoot.transform, false);

                WallMapStateV1LikeOriginal state = TryLoadWallMapStateFromCurrentMapV1LikeOriginal();
                if (state == null || state.Tre2Objects == null || state.Tre2Objects.Count == 0)
                {
                    Debug.Log("[C2:NATURE V1] no TRE2 objects. contract=" + C2NatureObjectsV1ContractLikeOriginal);
                    return;
                }

                NatureSpriteCatalogV1LikeOriginal trees = LoadNatureSpriteCatalogV1LikeOriginal(
                    "GA", "trees", new[] { "Treelist.lst", "treelist.lst" }, new[] { "treelist.rsr", "Treelist.rsr" });
                NatureSpriteCatalogV1LikeOriginal stones = LoadNatureSpriteCatalogV1LikeOriginal(
                    "TS", "stones", new[] { "stonlist.LST", "stonlist.lst" }, new[] { "stonlist.rsr", "stonlist.RSR" });
                NatureSpriteCatalogV1LikeOriginal complex = LoadNatureSpriteCatalogV1LikeOriginal(
                    "OC", "complex", new[] { "complex.lst" }, new[] { "complex.rsr" });

                List<Tre2MapObjectV28LikeOriginal> ga = new List<Tre2MapObjectV28LikeOriginal>();
                List<Tre2MapObjectV28LikeOriginal> ts = new List<Tre2MapObjectV28LikeOriginal>();
                List<Tre2MapObjectV28LikeOriginal> oc = new List<Tre2MapObjectV28LikeOriginal>();
                int other = 0;

                for (int i = 0; i < state.Tre2Objects.Count; i++)
                {
                    Tre2MapObjectV28LikeOriginal o = state.Tre2Objects[i];
                    if (o == null) continue;
                    if (string.Equals(o.Sign, "GA", StringComparison.OrdinalIgnoreCase)) ga.Add(o);
                    else if (string.Equals(o.Sign, "TS", StringComparison.OrdinalIgnoreCase)) ts.Add(o);
                    else if (string.Equals(o.Sign, "OC", StringComparison.OrdinalIgnoreCase)) oc.Add(o);
                    else other++;
                }

                int missingGa = 0, missingTs = 0, missingOc = 0;
                int realGa = 0, realTs = 0, realOc = 0;
                int shadowGa = 0, missingShadowGa = 0;
                int fieldOc = 0;
                int fallbackGa = 0, fallbackTs = 0, fallbackOc = 0;

                if (C2NatureObjectsV2DrawRealSpritesLikeOriginal)
                {
                    BuildNatureTreeShadowPassV12LikeOriginal(ga, trees, ref missingShadowGa, ref shadowGa, ref _terrainBounds);
                    BuildNatureRealObjectSpritesV2LikeOriginal(ga, trees, NatureKindV1LikeOriginal.Tree, ref missingGa, ref realGa, ref fieldOc, ref fallbackGa, ref _terrainBounds);
                    BuildNatureRealObjectSpritesV2LikeOriginal(ts, stones, NatureKindV1LikeOriginal.Stone, ref missingTs, ref realTs, ref fieldOc, ref fallbackTs, ref _terrainBounds);
                    BuildNatureRealObjectSpritesV2LikeOriginal(oc, complex, NatureKindV1LikeOriginal.Complex, ref missingOc, ref realOc, ref fieldOc, ref fallbackOc, ref _terrainBounds);
                }
                else if (C2NatureObjectsV1DrawDebugCardsLikeOriginal)
                {
                    BuildNatureDebugObjectCardsV1LikeOriginal(ga, trees, NatureKindV1LikeOriginal.Tree, ref missingGa, ref _terrainBounds);
                    BuildNatureDebugObjectCardsV1LikeOriginal(ts, stones, NatureKindV1LikeOriginal.Stone, ref missingTs, ref _terrainBounds);
                    BuildNatureDebugObjectCardsV1LikeOriginal(oc, complex, NatureKindV1LikeOriginal.Complex, ref missingOc, ref _terrainBounds);
                }

                Debug.Log("[C2:NATURE V6] contract=" + C2NatureObjectsV1ContractLikeOriginal +
                          " map='" + _mapRelativePath + "'" +
                          " TRE2_total=" + state.Tre2Objects.Count.ToString(CultureInfo.InvariantCulture) +
                          " GA_trees=" + ga.Count.ToString(CultureInfo.InvariantCulture) +
                          " TS_stones=" + ts.Count.ToString(CultureInfo.InvariantCulture) +
                          " OC_complex=" + oc.Count.ToString(CultureInfo.InvariantCulture) +
                          " other=" + other.ToString(CultureInfo.InvariantCulture) +
                          " realSprites=" + C2NatureObjectsV2DrawRealSpritesLikeOriginal +
                          " debugCards=" + C2NatureObjectsV1DrawDebugCardsLikeOriginal +
                          " realGA=" + realGa.ToString(CultureInfo.InvariantCulture) +
                          " shadowGA=" + shadowGa.ToString(CultureInfo.InvariantCulture) +
                          " realTS=" + realTs.ToString(CultureInfo.InvariantCulture) +
                          " realOC=" + realOc.ToString(CultureInfo.InvariantCulture) +
                          " fieldOC=" + fieldOc.ToString(CultureInfo.InvariantCulture) +
                          " generatedFieldPatches=" + C2NatureObjectsV3_2DrawGeneratedFieldPatchesLikeOriginal +
                          " fallbackTextures=" + C2NatureObjectsV3_2DrawFallbackWhenTextureMissingLikeOriginal +
                          " fallbackGA=" + fallbackGa.ToString(CultureInfo.InvariantCulture) +
                          " fallbackTS=" + fallbackTs.ToString(CultureInfo.InvariantCulture) +
                          " fallbackOC=" + fallbackOc.ToString(CultureInfo.InvariantCulture) +
                          " missingGA=" + missingGa.ToString(CultureInfo.InvariantCulture) +
                          " missingShadowGA=" + missingShadowGa.ToString(CultureInfo.InvariantCulture) +
                          " missingTS=" + missingTs.ToString(CultureInfo.InvariantCulture) +
                          " missingOC=" + missingOc.ToString(CultureInfo.InvariantCulture));

                LogNatureObjectSamplesV1LikeOriginal("GA", ga, trees);
                LogNatureObjectSamplesV1LikeOriginal("TS", ts, stones);
                LogNatureObjectSamplesV1LikeOriginal("OC", oc, complex);
            }
            catch (Exception ex)
            {
                Debug.LogError("[C2:NATURE V1] failed:\n" + ex);
            }
        }

        private enum NatureKindV1LikeOriginal
        {
            Tree,
            Stone,
            Complex
        }

        private sealed class NatureSpriteCatalogV1LikeOriginal
        {
            public string Sign = string.Empty;
            public string Label = string.Empty;
            public string SourceListPath = string.Empty;
            public string SourceRsrPath = string.Empty;
            public string GpName = string.Empty;
            public int DeclaredCount;
            public readonly Dictionary<int, NatureSpriteDescV1LikeOriginal> ByIndex = new Dictionary<int, NatureSpriteDescV1LikeOriginal>();
            public readonly Dictionary<string, NatureSpriteDescV1LikeOriginal> ByName = new Dictionary<string, NatureSpriteDescV1LikeOriginal>(StringComparer.OrdinalIgnoreCase);
            public int RandomRules;
            public int AutoAnimateRules;
            public int AnimateRules;
            public int AmplitudeRules;
            public int FixHeightRules;
            public int GroundRules;
            public int ModelRules;
            public int AlignRules;
            public int AutobornRules;
            public int SourceRules;
            public int SoundRules;
        }

        private sealed class NatureSpriteDescV1LikeOriginal
        {
            public int Index;
            public int SpriteIndex;
            public string Name = string.Empty;
            public int CenterX;
            public int CenterY;
            public int Radius;
            public int NRandom;
            public int AutoAnimateFrames;
            public int Amplitude;
            public int FixHeight = -1000;
            public bool OnGround;
            public bool HasModel;
            public string ModelPath = string.Empty;
            public char AlignMode;
            public bool IsFieldPatch;
            public int FieldWidth = 64;
            public int FieldHeight = 64;
            public int FieldGrowStage;
            public int FieldYScale = 256;
            public int VaX1;
            public int VaY1;
            public int VaX2;
            public int VaY2;
            public readonly List<int> TimeAnimationSpriteIndices = new List<int>();
            public readonly List<string> AutobornNames = new List<string>();
            public readonly List<Vector2Int> AutobornOffsets = new List<Vector2Int>();
        }

        private NatureSpriteCatalogV1LikeOriginal LoadNatureSpriteCatalogV1LikeOriginal(string sign, string label, string[] listPaths, string[] rsrPaths)
        {
            NatureSpriteCatalogV1LikeOriginal catalog = new NatureSpriteCatalogV1LikeOriginal
            {
                Sign = sign ?? string.Empty,
                Label = label ?? string.Empty
            };

            if (TryReadGameTextV1LikeOriginal(listPaths, out string listPath, out string listText))
            {
                catalog.SourceListPath = listPath;
                ParseNatureLstV1LikeOriginal(catalog, listText);
            }
            else
            {
                Debug.LogWarning("[C2:NATURE CATALOG V1] missing LST for " + label + " paths=" + string.Join(",", listPaths ?? new string[0]));
            }

            if (TryReadGameTextV1LikeOriginal(rsrPaths, out string rsrPath, out string rsrText))
            {
                catalog.SourceRsrPath = rsrPath;
                ParseNatureRsrV1LikeOriginal(catalog, rsrText);
            }

            Debug.Log("[C2:NATURE CATALOG V1] label=" + catalog.Label +
                      " sign=" + catalog.Sign +
                      " gp='" + catalog.GpName + "'" +
                      " declared=" + catalog.DeclaredCount.ToString(CultureInfo.InvariantCulture) +
                      " parsed=" + catalog.ByIndex.Count.ToString(CultureInfo.InvariantCulture) +
                      " list='" + catalog.SourceListPath + "'" +
                      " rsr='" + catalog.SourceRsrPath + "'" +
                      " random=" + catalog.RandomRules.ToString(CultureInfo.InvariantCulture) +
                      " autoAnimate=" + catalog.AutoAnimateRules.ToString(CultureInfo.InvariantCulture) +
                      " animate=" + catalog.AnimateRules.ToString(CultureInfo.InvariantCulture) +
                      " amplitude=" + catalog.AmplitudeRules.ToString(CultureInfo.InvariantCulture) +
                      " fixH=" + catalog.FixHeightRules.ToString(CultureInfo.InvariantCulture) +
                      " ground=" + catalog.GroundRules.ToString(CultureInfo.InvariantCulture) +
                      " model=" + catalog.ModelRules.ToString(CultureInfo.InvariantCulture) +
                      " align=" + catalog.AlignRules.ToString(CultureInfo.InvariantCulture) +
                      " autoborn=" + catalog.AutobornRules.ToString(CultureInfo.InvariantCulture));

            return catalog;
        }

        private bool TryReadGameTextV1LikeOriginal(string[] candidatePaths, out string path, out string text)
        {
            path = string.Empty;
            text = string.Empty;
            if (_bootstrap == null || _bootstrap.Fs == null || candidatePaths == null)
                return false;

            for (int i = 0; i < candidatePaths.Length; i++)
            {
                string p = candidatePaths[i];
                if (string.IsNullOrWhiteSpace(p))
                    continue;

                if (!_bootstrap.Fs.Exists(p))
                    continue;

                byte[] bytes = _bootstrap.Fs.ReadAllBytes(p);
                if (bytes == null)
                    continue;

                path = p;
                text = Encoding.ASCII.GetString(bytes).Replace("\0", string.Empty);
                return true;
            }

            return false;
        }

        private static void ParseNatureLstV1LikeOriginal(NatureSpriteCatalogV1LikeOriginal catalog, string text)
        {
            if (catalog == null || string.IsNullOrEmpty(text))
                return;

            string[] lines = text.Replace("\r", "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            bool headerRead = false;
            int objIndex = 0;
            int gpIndex = 0;

            for (int li = 0; li < lines.Length; li++)
            {
                string line = StripNatureCommentV1LikeOriginal(lines[li]);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] p = SplitNatureTokensV1LikeOriginal(line);
                if (p.Length == 0)
                    continue;

                if (!headerRead)
                {
                    catalog.GpName = p[0];
                    if (p.Length > 1) TryParseIntV1LikeOriginal(p[1], out catalog.DeclaredCount);
                    headerRead = true;
                    continue;
                }

                if (p[0].StartsWith("#", StringComparison.Ordinal))
                {
                    if (p.Length >= 7 && TryParseIntV1LikeOriginal(p[1], out int n1) && TryParseIntV1LikeOriginal(p[2], out int n2))
                    {
                        string baseName = p[3];
                        TryParseIntV1LikeOriginal(p[4], out int cx);
                        TryParseIntV1LikeOriginal(p[5], out int cy);
                        TryParseIntV1LikeOriginal(p[6], out int rr);
                        int count = Mathf.Max(0, n2 - n1 + 1);
                        if (baseName.StartsWith("@", StringComparison.Ordinal))
                        {
                            for (int k = 0; k < count; k++)
                            {
                                AddNatureCatalogDescV1LikeOriginal(catalog, objIndex++, gpIndex + k, baseName + (n1 + k).ToString(CultureInfo.InvariantCulture) + "F", cx, cy, rr);
                            }
                            for (int k = 0; k < count; k++)
                            {
                                AddNatureCatalogDescV1LikeOriginal(catalog, objIndex++, gpIndex + k + 4096, baseName + (n1 + k).ToString(CultureInfo.InvariantCulture) + "B", -cx, cy, rr);
                            }
                            gpIndex += count;
                        }
                        else
                        {
                            for (int k = 0; k < count; k++)
                                AddNatureCatalogDescV1LikeOriginal(catalog, objIndex++, gpIndex++, baseName + (n1 + k).ToString(CultureInfo.InvariantCulture), cx, cy, rr);
                        }
                    }
                    continue;
                }

                if (p[0].StartsWith("@", StringComparison.Ordinal))
                {
                    if (p.Length >= 4)
                    {
                        TryParseIntV1LikeOriginal(p[1], out int cx);
                        TryParseIntV1LikeOriginal(p[2], out int cy);
                        TryParseIntV1LikeOriginal(p[3], out int rr);
                        AddNatureCatalogDescV1LikeOriginal(catalog, objIndex++, gpIndex, p[0] + "F", cx, cy, rr);
                        AddNatureCatalogDescV1LikeOriginal(catalog, objIndex++, gpIndex + 4096, p[0] + "B", -cx, cy, rr);
                        gpIndex++;
                    }
                    continue;
                }

                if (p.Length >= 4)
                {
                    TryParseIntV1LikeOriginal(p[1], out int cx);
                    TryParseIntV1LikeOriginal(p[2], out int cy);
                    TryParseIntV1LikeOriginal(p[3], out int rr);
                    NatureSpriteDescV1LikeOriginal desc = AddNatureCatalogDescV1LikeOriginal(catalog, objIndex++, gpIndex++, p[0], cx, cy, rr);
                    if (desc != null && p.Length >= 9 && string.Equals(p[4], "#FIELDPATH", StringComparison.OrdinalIgnoreCase))
                    {
                        desc.IsFieldPatch = true;
                        TryParseIntV1LikeOriginal(p[5], out desc.FieldWidth);
                        TryParseIntV1LikeOriginal(p[6], out desc.FieldHeight);
                        TryParseIntV1LikeOriginal(p[7], out desc.FieldGrowStage);
                        TryParseIntV1LikeOriginal(p[8], out desc.FieldYScale);
                        if (desc.FieldWidth <= 0) desc.FieldWidth = 64;
                        if (desc.FieldHeight <= 0) desc.FieldHeight = 64;
                    }
                }
            }
        }

        private static NatureSpriteDescV1LikeOriginal AddNatureCatalogDescV1LikeOriginal(NatureSpriteCatalogV1LikeOriginal catalog, int index, int spriteIndex, string name, int centerX, int centerY, int radius)
        {
            if (catalog == null)
                return null;

            NatureSpriteDescV1LikeOriginal desc = new NatureSpriteDescV1LikeOriginal
            {
                Index = index,
                SpriteIndex = spriteIndex,
                Name = name ?? string.Empty,
                CenterX = centerX,
                CenterY = centerY,
                Radius = radius,
                FixHeight = -1000
            };
            catalog.ByIndex[index] = desc;
            if (!string.IsNullOrWhiteSpace(desc.Name) && !catalog.ByName.ContainsKey(desc.Name))
                catalog.ByName.Add(desc.Name, desc);
            return desc;
        }

        private static void ParseNatureRsrV1LikeOriginal(NatureSpriteCatalogV1LikeOriginal catalog, string text)
        {
            if (catalog == null || string.IsNullOrEmpty(text))
                return;

            string section = string.Empty;
            string[] lines = text.Replace("\r", "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int li = 0; li < lines.Length; li++)
            {
                string line = StripNatureCommentV1LikeOriginal(lines[li]);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] p = SplitNatureTokensV1LikeOriginal(line);
                if (p.Length == 0)
                    continue;

                if (p[0].StartsWith("[", StringComparison.Ordinal))
                {
                    section = p[0].Trim('[', ']').Trim().ToUpperInvariant();
                    continue;
                }

                if (!catalog.ByName.TryGetValue(p[0], out NatureSpriteDescV1LikeOriginal desc))
                    continue;

                if (section == "RANDOM" && p.Length >= 2 && TryParseIntV1LikeOriginal(p[1], out int nrand))
                {
                    desc.NRandom = nrand + 1;
                    catalog.RandomRules++;
                }
                else if (section == "AUTOANIMATE" && p.Length >= 2 && TryParseIntV1LikeOriginal(p[1], out int frames))
                {
                    desc.AutoAnimateFrames = frames;
                    RegisterNatureAutoAnimateSequenceV12LikeOriginal(catalog, desc, frames);
                    catalog.AutoAnimateRules++;
                }
                else if (section == "ANIMATE")
                {
                    RegisterNatureExplicitAnimateSequenceV12LikeOriginal(catalog, desc, p);
                    catalog.AnimateRules++;
                }
                else if (section == "AMPLITUDE" && p.Length >= 2 && TryParseIntV1LikeOriginal(p[1], out int amp))
                {
                    desc.Amplitude = amp;
                    catalog.AmplitudeRules++;
                }
                else if (section == "FIXH" && p.Length >= 2 && TryParseIntV1LikeOriginal(p[1], out int fixh))
                {
                    desc.FixHeight = fixh;
                    catalog.FixHeightRules++;
                }
                else if (section == "GROUND")
                {
                    desc.OnGround = true;
                    catalog.GroundRules++;
                }
                else if (section == "MODEL" && p.Length >= 2)
                {
                    desc.HasModel = true;
                    desc.ModelPath = p[1];
                    catalog.ModelRules++;
                }
                else if (section == "ALIGNING" && p.Length >= 2)
                {
                    desc.AlignMode = p[1].Length > 0 ? p[1][0] : '\0';
                    if ((desc.AlignMode == 'V' || desc.AlignMode == 'S') && p.Length >= 6)
                    {
                        TryParseIntV1LikeOriginal(p[2], out desc.VaX1);
                        TryParseIntV1LikeOriginal(p[3], out desc.VaY1);
                        TryParseIntV1LikeOriginal(p[4], out desc.VaX2);
                        TryParseIntV1LikeOriginal(p[5], out desc.VaY2);
                    }
                    catalog.AlignRules++;
                }
                else if (section == "AUTOBORN" && p.Length >= 2 && TryParseIntV1LikeOriginal(p[1], out int childCount))
                {
                    // The original line can continue with child triples. We count it now; exact child spawn is stage V2.
                    if (p.Length >= 2 + childCount * 3)
                    {
                        for (int i = 0; i < childCount; i++)
                        {
                            int baseIdx = 2 + i * 3;
                            desc.AutobornNames.Add(p[baseIdx]);
                            TryParseIntV1LikeOriginal(p[baseIdx + 1], out int dx);
                            TryParseIntV1LikeOriginal(p[baseIdx + 2], out int dy);
                            desc.AutobornOffsets.Add(new Vector2Int(dx, dy));
                        }
                    }
                    catalog.AutobornRules++;
                }
                else if (section == "SOURCES" || section == "INTERNAL_SOURCE")
                {
                    catalog.SourceRules++;
                }
                else if (section == "SOUND")
                {
                    catalog.SoundRules++;
                }
            }
        }

        private static void RegisterNatureAutoAnimateSequenceV12LikeOriginal(NatureSpriteCatalogV1LikeOriginal catalog, NatureSpriteDescV1LikeOriginal desc, int frames)
        {
            if (catalog == null || desc == null || frames <= 1)
                return;

            List<NatureSpriteDescV1LikeOriginal> seq = new List<NatureSpriteDescV1LikeOriginal>();
            for (int i = 0; i < frames; i++)
            {
                if (catalog.ByIndex.TryGetValue(desc.Index + i, out NatureSpriteDescV1LikeOriginal frameDesc) && frameDesc != null)
                    seq.Add(frameDesc);
            }

            RegisterNatureRotatingAnimationSequenceV12LikeOriginal(seq);
        }

        private static void RegisterNatureExplicitAnimateSequenceV12LikeOriginal(NatureSpriteCatalogV1LikeOriginal catalog, NatureSpriteDescV1LikeOriginal desc, string[] tokens)
        {
            if (catalog == null || desc == null || tokens == null || tokens.Length < 2)
                return;

            if (!TryParseIntV1LikeOriginal(tokens[1], out int count) || count <= 0)
                return;

            List<NatureSpriteDescV1LikeOriginal> seq = new List<NatureSpriteDescV1LikeOriginal>();
            seq.Add(desc);
            for (int i = 0; i < count && 2 + i < tokens.Length; i++)
            {
                if (catalog.ByName.TryGetValue(tokens[2 + i], out NatureSpriteDescV1LikeOriginal frameDesc) && frameDesc != null)
                    seq.Add(frameDesc);
            }

            RegisterNatureRotatingAnimationSequenceV12LikeOriginal(seq);
        }

        private static void RegisterNatureRotatingAnimationSequenceV12LikeOriginal(List<NatureSpriteDescV1LikeOriginal> seq)
        {
            if (seq == null || seq.Count <= 1)
                return;

            for (int start = 0; start < seq.Count; start++)
            {
                NatureSpriteDescV1LikeOriginal desc = seq[start];
                if (desc == null)
                    continue;

                desc.TimeAnimationSpriteIndices.Clear();
                for (int i = 0; i < seq.Count; i++)
                {
                    NatureSpriteDescV1LikeOriginal frameDesc = seq[(start + i) % seq.Count];
                    if (frameDesc != null)
                        desc.TimeAnimationSpriteIndices.Add(frameDesc.SpriteIndex);
                }
            }
        }

        private void BuildNatureDebugObjectCardsV1LikeOriginal(
            List<Tre2MapObjectV28LikeOriginal> objects,
            NatureSpriteCatalogV1LikeOriginal catalog,
            NatureKindV1LikeOriginal kind,
            ref int missing,
            ref Bounds terrainBounds)
        {
            if (objects == null || objects.Count == 0 || catalog == null)
                return;

            int batchIndex = 0;
            for (int start = 0; start < objects.Count; start += C2NatureObjectsV1InstancesPerMeshLikeOriginal)
            {
                int end = Mathf.Min(objects.Count, start + C2NatureObjectsV1InstancesPerMeshLikeOriginal);
                Mesh mesh = BuildNatureDebugBatchMeshV1LikeOriginal(objects, start, end, catalog, kind, ref missing);
                if (mesh == null || mesh.vertexCount == 0)
                    continue;

                GameObject go = new GameObject("C2_Nature_" + catalog.Sign + "_V1_batch_" + batchIndex.ToString(CultureInfo.InvariantCulture));
                go.transform.SetParent(_c2NatureObjectsRootV1LikeOriginal.transform, false);

                MeshFilter mf = go.AddComponent<MeshFilter>();
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh;
                mr.sharedMaterial = CreateNatureDebugMaterialV1LikeOriginal(kind);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

                if (mesh.bounds.size.sqrMagnitude > 0.000001f)
                    terrainBounds.Encapsulate(mesh.bounds);

                batchIndex++;
            }
        }

        private Mesh BuildNatureDebugBatchMeshV1LikeOriginal(
            List<Tre2MapObjectV28LikeOriginal> objects,
            int start,
            int end,
            NatureSpriteCatalogV1LikeOriginal catalog,
            NatureKindV1LikeOriginal kind,
            ref int missing)
        {
            List<Vector3> verts = new List<Vector3>((end - start) * 4);
            List<Vector2> uv = new List<Vector2>((end - start) * 4);
            List<Color32> colors = new List<Color32>((end - start) * 4);
            List<int> tris = new List<int>((end - start) * 6);

            float pixelToWorld = Mathf.Max(0.001f, WallOriginalXYUnitToWorldScaleV8LikeOriginal());
            Color32 color = NatureDebugColorV1LikeOriginal(kind);

            for (int i = start; i < end; i++)
            {
                Tre2MapObjectV28LikeOriginal obj = objects[i];
                if (obj == null)
                    continue;

                if (!catalog.ByIndex.TryGetValue(obj.SpriteIndex, out NatureSpriteDescV1LikeOriginal desc))
                {
                    desc = null;
                    missing++;
                }

                int cx = desc != null ? Mathf.Abs(desc.CenterX) : 32;
                int cy = desc != null ? Mathf.Abs(desc.CenterY) : 48;
                int rr = desc != null ? Mathf.Max(1, desc.Radius) : 16;
                int fixH = desc != null ? desc.FixHeight : -1000;

                float wPx;
                float hPx;
                switch (kind)
                {
                    case NatureKindV1LikeOriginal.Stone:
                        wPx = Mathf.Max(12.0f, cx * 2.0f);
                        hPx = Mathf.Max(8.0f, cy);
                        break;
                    case NatureKindV1LikeOriginal.Complex:
                        wPx = Mathf.Max(32.0f, cx * 2.0f);
                        hPx = Mathf.Max(32.0f, cy * 2.0f);
                        break;
                    default:
                        wPx = Mathf.Max(24.0f, cx * 2.0f);
                        hPx = Mathf.Max(32.0f, cy);
                        break;
                }

                Vector3 baseWorld = WallOriginalXYToWorldV1LikeOriginal(obj.X, obj.Y, fixH > -1000 ? fixH : 0.0f);
                baseWorld.y += C2NatureObjectsV1DebugYOffsetWorldLikeOriginal;

                float w = wPx * pixelToWorld;
                float h = hPx * pixelToWorld;
                float pivotX = (desc != null ? desc.CenterX : Mathf.RoundToInt(wPx * 0.5f)) * pixelToWorld;
                float pivotY = (desc != null ? desc.CenterY : Mathf.RoundToInt(hPx)) * pixelToWorld;

                // Stage V1 debug placement: use exact M3D X/Y and original height sampler.
                // Real GP/G2D frame pivot will replace this synthetic card after decoder binding.
                Vector3 right = Vector3.right;
                Vector3 up = Vector3.up;
                Vector3 bl = baseWorld - right * pivotX;
                Vector3 br = bl + right * w;
                Vector3 tl = bl + up * h;
                Vector3 tr = br + up * h;

                int v0 = verts.Count;
                verts.Add(bl);
                verts.Add(br);
                verts.Add(tr);
                verts.Add(tl);
                uv.Add(new Vector2(0, 0));
                uv.Add(new Vector2(1, 0));
                uv.Add(new Vector2(1, 1));
                uv.Add(new Vector2(0, 1));

                Color32 c = color;
                if (desc != null && desc.Amplitude > 0 && kind == NatureKindV1LikeOriginal.Tree)
                    c = new Color32((byte)Mathf.Min(255, c.r + 30), c.g, c.b, c.a);
                colors.Add(c); colors.Add(c); colors.Add(c); colors.Add(c);
                tris.Add(v0 + 0); tris.Add(v0 + 2); tris.Add(v0 + 1);
                tris.Add(v0 + 0); tris.Add(v0 + 3); tris.Add(v0 + 2);
            }

            if (verts.Count == 0)
                return null;

            Mesh mesh = new Mesh { name = "C2_Nature_DebugCards_V1_" + catalog.Sign };
            if (verts.Count > 65000)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uv);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        private Material CreateNatureDebugMaterialV1LikeOriginal(NatureKindV1LikeOriginal kind)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            mat.name = "C2_Nature_Debug_" + kind.ToString() + "_V1";
            Color32 c32 = NatureDebugColorV1LikeOriginal(kind);
            Color c = new Color(c32.r / 255.0f, c32.g / 255.0f, c32.b / 255.0f, C2NatureObjectsV1DebugAlphaLikeOriginal);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.renderQueue = C2NatureObjectsV1RenderQueueLikeOriginal;
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            return mat;
        }

        private static Color32 NatureDebugColorV1LikeOriginal(NatureKindV1LikeOriginal kind)
        {
            switch (kind)
            {
                case NatureKindV1LikeOriginal.Stone: return new Color32(105, 105, 100, 165);
                case NatureKindV1LikeOriginal.Complex: return new Color32(155, 105, 55, 165);
                default: return new Color32(35, 95, 45, 165);
            }
        }


        private sealed class NatureMeshBatchV2LikeOriginal
        {
            public string Name = string.Empty;
            public Texture2D Texture;
            public Material Material;
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<Color32> Colors = new List<Color32>();
            public readonly List<int> Triangles = new List<int>();
            public readonly List<float> SwayAmounts = new List<float>();
            public readonly List<float> SwayPhases = new List<float>();
            public readonly List<Vector3> SwayPivots = new List<Vector3>();
            public bool HasSway;
            public int Count;
        }

        private void BuildNatureTreeShadowPassV12LikeOriginal(
            List<Tre2MapObjectV28LikeOriginal> objects,
            NatureSpriteCatalogV1LikeOriginal catalog,
            ref int missing,
            ref int shadowDrawn,
            ref Bounds terrainBounds)
        {
            if (!C2NatureObjectsV12DrawTreeShadowPassLikeOriginal || objects == null || objects.Count == 0 || catalog == null)
                return;

            NatureSpriteCatalogV1LikeOriginal shadowCatalog = new NatureSpriteCatalogV1LikeOriginal
            {
                Sign = "GA",
                Label = "treesShadow",
                GpName = "TreesAll"
            };

            Dictionary<string, NatureMeshBatchV2LikeOriginal> batches = new Dictionary<string, NatureMeshBatchV2LikeOriginal>(StringComparer.OrdinalIgnoreCase);
            int missingTextureLog = 0;
            string firstTextureSource = string.Empty;

            for (int i = 0; i < objects.Count; i++)
            {
                Tre2MapObjectV28LikeOriginal obj = objects[i];
                if (obj == null)
                    continue;

                if (!catalog.ByIndex.TryGetValue(obj.SpriteIndex, out NatureSpriteDescV1LikeOriginal desc) || desc == null)
                {
                    missing++;
                    desc = new NatureSpriteDescV1LikeOriginal
                    {
                        Index = obj.SpriteIndex,
                        SpriteIndex = obj.SpriteIndex,
                        Name = "MISSING_SHADOW_" + obj.SpriteIndex.ToString(CultureInfo.InvariantCulture),
                        CenterX = 32,
                        CenterY = 64,
                        Radius = 20
                    };
                }

                Texture2D sourceTex = TryLoadNatureSpriteTextureV2LikeOriginal(shadowCatalog, desc, NatureKindV1LikeOriginal.Tree, out string source);
                if (string.IsNullOrEmpty(firstTextureSource) && !string.IsNullOrEmpty(source))
                    firstTextureSource = source;

                Texture2D shadowTex = sourceTex;
                if (shadowTex == null)
                {
                    if (missingTextureLog < 12)
                    {
                        Debug.LogWarning("[C2:NATURE SHADOW V17] skip missing TreesAll shadow texture obj=" + desc.Name +
                                         " id=" + desc.Index.ToString(CultureInfo.InvariantCulture) +
                                         " frame=" + (desc.SpriteIndex & 4095).ToString(CultureInfo.InvariantCulture) +
                                         " source=" + (source ?? string.Empty));
                        missingTextureLog++;
                    }
                    continue;
                }

                Material mat = GetNatureTreeShadowMaterialV12LikeOriginal(shadowTex);
                string spriteKey = "SHADOW_BILLBOARD:TreesAll:" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) + ":" + shadowTex.GetInstanceID().ToString(CultureInfo.InvariantCulture);
                NatureMeshBatchV2LikeOriginal batch = GetNatureBatchV2LikeOriginal(batches, spriteKey, "C2_Nature_GA_ShadowBillboard_V17", shadowTex, mat);
                // V17 rollback: TreesAll shadow/halo is a tree-attached billboard layer.
                // Do NOT lay it onto terrain. Keep the same Unity-correct orientation as the visible tree.
                AppendNatureSpriteQuadV2LikeOriginal(batch, obj, desc, shadowTex, NatureKindV1LikeOriginal.Tree, i);
                shadowDrawn++;
            }

            int meshCount = 0;
            int totalVerts = 0;
            foreach (KeyValuePair<string, NatureMeshBatchV2LikeOriginal> kv in batches)
            {
                NatureMeshBatchV2LikeOriginal b = kv.Value;
                if (b == null || b.Vertices.Count == 0)
                    continue;

                Mesh mesh = new Mesh { name = b.Name + "_mesh_" + meshCount.ToString(CultureInfo.InvariantCulture) };
                if (b.Vertices.Count > 65000)
                    mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(b.Vertices);
                mesh.SetUVs(0, b.Uv);
                mesh.SetColors(b.Colors);
                mesh.SetTriangles(b.Triangles, 0, true);
                mesh.RecalculateBounds();

                GameObject go = new GameObject(b.Name + "_batch_" + meshCount.ToString(CultureInfo.InvariantCulture));
                go.transform.SetParent(_c2NatureObjectsRootV1LikeOriginal.transform, false);
                MeshFilter mf = go.AddComponent<MeshFilter>();
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh;
                mr.sharedMaterial = b.Material;
                ApplyNatureRendererOrderingV14LikeOriginal(mr, true, NatureKindV1LikeOriginal.Tree);
                AttachNatureTreeSwayAnimatorV12LikeOriginal(go, mesh, b);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                if (mesh.bounds.size.sqrMagnitude > 0.000001f)
                    terrainBounds.Encapsulate(mesh.bounds);
                totalVerts += b.Vertices.Count;
                meshCount++;
            }

            Debug.Log("[C2:NATURE SHADOW V17] objects=" + objects.Count.ToString(CultureInfo.InvariantCulture) +
                      " drawn=" + shadowDrawn.ToString(CultureInfo.InvariantCulture) +
                      " meshes=" + meshCount.ToString(CultureInfo.InvariantCulture) +
                      " verts=" + totalVerts.ToString(CultureInfo.InvariantCulture) +
                      " firstTexture='" + firstTextureSource + "'" +
                      " renderQueue=" + C2NatureObjectsV12TreeShadowRenderQueueLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " sortingOrder=" + C2NatureObjectsV14TreeShadowSortingOrderLikeOriginal.ToString(CultureInfo.InvariantCulture) +
                      " alphaRef=" + C2NatureObjectsV16TreeShadowAlphaRefLikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) +
                      " diffuseRGBA=(" + C2NatureObjectsV17TreeShadowDiffuseRLikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                      C2NatureObjectsV17TreeShadowDiffuseGLikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                      C2NatureObjectsV17TreeShadowDiffuseBLikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                      C2NatureObjectsV17TreeShadowDiffuseALikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) + ")" +
                      " contract=" + C2NatureObjectsV12TreeShadowContractLikeOriginal);
        }

        private void BuildNatureRealObjectSpritesV2LikeOriginal(
            List<Tre2MapObjectV28LikeOriginal> objects,
            NatureSpriteCatalogV1LikeOriginal catalog,
            NatureKindV1LikeOriginal kind,
            ref int missing,
            ref int realDrawn,
            ref int fieldDrawn,
            ref int fallbackDrawn,
            ref Bounds terrainBounds)
        {
            if (objects == null || objects.Count == 0 || catalog == null)
                return;

            Dictionary<string, NatureMeshBatchV2LikeOriginal> batches = new Dictionary<string, NatureMeshBatchV2LikeOriginal>(StringComparer.OrdinalIgnoreCase);
            int missingTextureLog = 0;
            string firstTextureSource = string.Empty;

            for (int i = 0; i < objects.Count; i++)
            {
                Tre2MapObjectV28LikeOriginal obj = objects[i];
                if (obj == null)
                    continue;

                if (!catalog.ByIndex.TryGetValue(obj.SpriteIndex, out NatureSpriteDescV1LikeOriginal desc) || desc == null)
                {
                    missing++;
                    desc = new NatureSpriteDescV1LikeOriginal
                    {
                        Index = obj.SpriteIndex,
                        SpriteIndex = obj.SpriteIndex,
                        Name = "MISSING_" + obj.SpriteIndex.ToString(CultureInfo.InvariantCulture),
                        CenterX = 32,
                        CenterY = 64,
                        Radius = 20
                    };
                }

                if (kind == NatureKindV1LikeOriginal.Complex && desc.IsFieldPatch)
                {
                    if (!C2NatureObjectsV3_2DrawGeneratedFieldPatchesLikeOriginal)
                    {
                        // V3.2: do not draw synthetic #FIELDPATH quads. They are not original sprites and
                        // produced huge pale sheets across the terrain. Real crop/field rendering must be
                        // implemented later from the original complex/G2D path, not generated here.
                        continue;
                    }

                    Texture2D fieldTex = GetNatureFieldPatchTextureV2LikeOriginal(desc);
                    Material fieldMat = GetNatureMaterialV2LikeOriginal(fieldTex, "FIELDPATH", NatureKindV1LikeOriginal.Complex, false);
                    string fieldKey = "FIELD:" + desc.FieldGrowStage.ToString(CultureInfo.InvariantCulture) + ":" + desc.FieldYScale.ToString(CultureInfo.InvariantCulture);
                    NatureMeshBatchV2LikeOriginal batch = GetNatureBatchV2LikeOriginal(batches, fieldKey, "C2_Nature_FIELD_V2", fieldTex, fieldMat);
                    AppendNatureFieldPatchV2LikeOriginal(batch, obj, desc);
                    fieldDrawn++;
                    continue;
                }

                Texture2D tex = TryLoadNatureSpriteTextureV2LikeOriginal(catalog, desc, kind, out string source);
                if (string.IsNullOrEmpty(firstTextureSource) && !string.IsNullOrEmpty(source))
                    firstTextureSource = source;

                bool fallback = false;
                if (tex == null)
                {
                    source = "missing_real_texture_no_fallback:" + (source ?? string.Empty);
                    if (missingTextureLog < 12)
                    {
                        Debug.LogWarning("[C2:NATURE TEX V5] skip missing real texture kind=" + kind + " catalog=" + catalog.GpName +
                                         " obj=" + desc.Name + " id=" + desc.Index.ToString(CultureInfo.InvariantCulture) +
                                         " gpFrame=" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                                         " source=" + source +
                                         " fallbackEnabled=" + C2NatureObjectsV3_2DrawFallbackWhenTextureMissingLikeOriginal);
                        missingTextureLog++;
                    }

                    if (!C2NatureObjectsV3_2DrawFallbackWhenTextureMissingLikeOriginal)
                        continue;

                    tex = GetNatureFallbackTextureV2LikeOriginal(kind);
                    fallback = true;
                    fallbackDrawn++;
                }
                else
                {
                    realDrawn++;
                }

                bool animatedTree = IsNatureAnimatedTreeV16LikeOriginal(desc, kind);
                Material mat = GetNatureMaterialV2LikeOriginal(tex, catalog.GpName, kind, true, animatedTree);
                string spriteKey = (fallback ? "FALLBACK:" : "SPRITE:") + catalog.GpName + ":" + desc.SpriteIndex.ToString(CultureInfo.InvariantCulture) + ":" + tex.GetInstanceID().ToString(CultureInfo.InvariantCulture) + (animatedTree ? ":animated_trees" : string.Empty);
                NatureMeshBatchV2LikeOriginal spriteBatch = GetNatureBatchV2LikeOriginal(batches, spriteKey, "C2_Nature_" + catalog.Sign + "_V2", tex, mat);
                AppendNatureSpriteQuadV2LikeOriginal(spriteBatch, obj, desc, tex, kind, i);
            }

            int meshCount = 0;
            int totalVerts = 0;
            foreach (KeyValuePair<string, NatureMeshBatchV2LikeOriginal> kv in batches)
            {
                NatureMeshBatchV2LikeOriginal b = kv.Value;
                if (b == null || b.Vertices.Count == 0)
                    continue;

                Mesh mesh = new Mesh { name = b.Name + "_mesh_" + meshCount.ToString(CultureInfo.InvariantCulture) };
                if (b.Vertices.Count > 65000)
                    mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(b.Vertices);
                mesh.SetUVs(0, b.Uv);
                mesh.SetColors(b.Colors);
                mesh.SetTriangles(b.Triangles, 0, true);
                mesh.RecalculateBounds();

                GameObject go = new GameObject(b.Name + "_batch_" + meshCount.ToString(CultureInfo.InvariantCulture));
                go.transform.SetParent(_c2NatureObjectsRootV1LikeOriginal.transform, false);
                MeshFilter mf = go.AddComponent<MeshFilter>();
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh;
                mr.sharedMaterial = b.Material;
                ApplyNatureRendererOrderingV14LikeOriginal(mr, false, kind);
                AttachNatureTreeSwayAnimatorV12LikeOriginal(go, mesh, b);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                if (mesh.bounds.size.sqrMagnitude > 0.000001f)
                    terrainBounds.Encapsulate(mesh.bounds);
                totalVerts += b.Vertices.Count;
                meshCount++;
            }

            Debug.Log("[C2:NATURE REAL V6] kind=" + kind +
                      " catalog=" + catalog.GpName +
                      " objects=" + objects.Count.ToString(CultureInfo.InvariantCulture) +
                      " meshes=" + meshCount.ToString(CultureInfo.InvariantCulture) +
                      " verts=" + totalVerts.ToString(CultureInfo.InvariantCulture) +
                      " firstTexture='" + firstTextureSource + "'" +
                      " pivotV9=" + C2NatureObjectsV6PivotContractLikeOriginal +
                      " sortingOrder=" + GetNatureRendererSortingOrderV14LikeOriginal(false, kind).ToString(CultureInfo.InvariantCulture) +
                      " treeAlphaRef=" + (kind == NatureKindV1LikeOriginal.Tree ? C2NatureObjectsV16TreeAlphaRefLikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) : "n/a") +
                      " animatedTreeAlphaRef=" + (kind == NatureKindV1LikeOriginal.Tree ? C2NatureObjectsV16AnimatedTreeAlphaRefLikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) : "n/a") +
                      " groundClampV14=Trees_g2d_visual_frame_visible_alpha_foot_clamp_renderQueue_3660" +
                      " treeShadowV14=" + C2NatureObjectsV12TreeShadowContractLikeOriginal + " renderOrderV14=roads_3600_3601_then_shadow_3650_then_visible_3660_sortingOrder_32767 microDepthV23=" + C2NatureObjectsV23TreeDepthMicroSeparationLikeOriginal.ToString(CultureInfo.InvariantCulture) + "/layers=" + C2NatureObjectsV23TreeDepthMicroLayersLikeOriginal.ToString(CultureInfo.InvariantCulture) + "/stepPx=" + C2NatureObjectsV23TreeDepthMicroStepPixelsLikeOriginal.ToString("0.###", CultureInfo.InvariantCulture) + " treeResolverV3=" + C2NatureObjectsV3TreeResolverContractLikeOriginal +
                      " stoneResolverV3=" + C2NatureObjectsV3StoneResolverContractLikeOriginal);
        }

        private static int GetNatureRendererSortingOrderV14LikeOriginal(bool shadow, NatureKindV1LikeOriginal kind)
        {
            return shadow ? C2NatureObjectsV14TreeShadowSortingOrderLikeOriginal : C2NatureObjectsV14NatureVisibleSortingOrderLikeOriginal;
        }

        private static void ApplyNatureRendererOrderingV14LikeOriginal(MeshRenderer renderer, bool shadow, NatureKindV1LikeOriginal kind)
        {
            if (renderer == null)
                return;

            renderer.sortingOrder = GetNatureRendererSortingOrderV14LikeOriginal(shadow, kind);
        }

        private static NatureMeshBatchV2LikeOriginal GetNatureBatchV2LikeOriginal(
            Dictionary<string, NatureMeshBatchV2LikeOriginal> batches,
            string key,
            string name,
            Texture2D tex,
            Material mat)
        {
            if (!batches.TryGetValue(key, out NatureMeshBatchV2LikeOriginal b) || b == null)
            {
                b = new NatureMeshBatchV2LikeOriginal
                {
                    Name = name,
                    Texture = tex,
                    Material = mat
                };
                batches[key] = b;
            }
            return b;
        }

        private static void AttachNatureTreeSwayAnimatorV12LikeOriginal(GameObject go, Mesh mesh, NatureMeshBatchV2LikeOriginal batch)
        {
            if (!C2NatureObjectsV12TreeAmplitudeSwayLikeOriginal || go == null || mesh == null || batch == null || !batch.HasSway)
                return;
            if (batch.SwayAmounts.Count != batch.Vertices.Count ||
                batch.SwayPhases.Count != batch.Vertices.Count ||
                batch.SwayPivots.Count != batch.Vertices.Count)
                return;

            NatureTreeSwayAnimatorV12LikeOriginal animator = go.AddComponent<NatureTreeSwayAnimatorV12LikeOriginal>();
            animator.Configure(mesh, batch.SwayAmounts.ToArray(), batch.SwayPhases.ToArray(), batch.SwayPivots.ToArray());
        }

        private static bool IsNatureAnimatedTreeV16LikeOriginal(NatureSpriteDescV1LikeOriginal desc, NatureKindV1LikeOriginal kind)
        {
            return kind == NatureKindV1LikeOriginal.Tree &&
                   desc != null &&
                   (desc.Amplitude > 0 || desc.AutoAnimateFrames > 1 ||
                    (desc.TimeAnimationSpriteIndices != null && desc.TimeAnimationSpriteIndices.Count > 0));
        }

        private static float GetNatureTreeSwayAmountWorldV12LikeOriginal(NatureSpriteDescV1LikeOriginal desc, NatureKindV1LikeOriginal kind, float pixelToWorld)
        {
            if (!C2NatureObjectsV12TreeAmplitudeSwayLikeOriginal || kind != NatureKindV1LikeOriginal.Tree || desc == null || desc.Amplitude <= 0)
                return 0.0f;

            // Original MiniMap4X.cpp: ang = 0.0025f * mod * Amplitude * cos(time / (450 + seed)) / 30.
            // The old Unity path used a large world-space top-vertex offset. V16 stores a tiny angle instead
            // and rotates the whole quad around its original foot/pivot, so the tree does not stretch.
            float amp = Mathf.Clamp(desc.Amplitude, 0, 256);
            return amp * C2NatureObjectsV16TreeRollAnglePerAmplitudeLikeOriginal;
        }

        private static float GetNatureTreeSwayPhaseV12LikeOriginal(Tre2MapObjectV28LikeOriginal obj)
        {
            if (obj == null)
                return 0.0f;

            float seed = obj.X * 0.013f + obj.Y * 0.021f + obj.SpriteIndex * 0.37f;
            return Mathf.Repeat(seed, Mathf.PI * 2.0f);
        }

        private static float GetNatureTreeDepthMicroOffsetWorldV23LikeOriginal(Tre2MapObjectV28LikeOriginal obj, NatureSpriteDescV1LikeOriginal desc, float pixelToWorld, int objectOrderForDepth = -1)
        {
            if (!C2NatureObjectsV23TreeDepthMicroSeparationLikeOriginal || obj == null || desc == null)
                return 0.0f;

            int layers = Mathf.Max(2, C2NatureObjectsV23TreeDepthMicroLayersLikeOriginal);
            float stepWorld = Mathf.Max(0.0005f, pixelToWorld * C2NatureObjectsV23TreeDepthMicroStepPixelsLikeOriginal);

            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ (uint)obj.X) * 16777619u;
                h = (h ^ (uint)obj.Y) * 16777619u;
                h = (h ^ (uint)obj.SpriteIndex) * 16777619u;
                h = (h ^ (uint)obj.NIndex) * 16777619u;
                h = (h ^ (uint)desc.Index) * 16777619u;

                // V24: add the TRE2 object order to the lane hash. V23 fixed most trees,
                // but rare dense rows can still contain several cards that land in the same
                // micro-lane. The same order is passed by both the visible pass and the
                // TreesAll shadow/halo pass, so the halo stays glued to its tree.
                uint order = objectOrderForDepth >= 0 ? (uint)objectOrderForDepth : 0u;
                h = (h ^ (order * 40503u)) * 16777619u;
                h = (h ^ (order >> 3)) * 16777619u;

                int layer = (int)(h % (uint)layers);
                float center = (layers - 1) * 0.5f;

                // Deterministic sub-lane along the sprite normal (Unity Z).
                // No texture/material/contrast/shadow changes; this only separates
                // almost-coplanar tree cards that fight inside very dense forests.
                return (layer - center) * stepWorld;
            }
        }

        private sealed class NatureTreeSwayAnimatorV12LikeOriginal : MonoBehaviour
        {
            private Mesh _mesh;
            private Vector3[] _baseVertices;
            private Vector3[] _workVertices;
            private float[] _amounts;
            private float[] _phases;
            private Vector3[] _pivots;
            private bool _ready;

            public void Configure(Mesh mesh, float[] amounts, float[] phases, Vector3[] pivots)
            {
                _mesh = mesh;
                _amounts = amounts;
                _phases = phases;
                _pivots = pivots;
                if (_mesh == null || _amounts == null || _phases == null || _pivots == null)
                    return;

                _baseVertices = _mesh.vertices;
                if (_baseVertices == null || _baseVertices.Length == 0 ||
                    _amounts.Length != _baseVertices.Length ||
                    _phases.Length != _baseVertices.Length ||
                    _pivots.Length != _baseVertices.Length)
                    return;

                _workVertices = new Vector3[_baseVertices.Length];
                float maxAmount = 0.0f;
                for (int i = 0; i < _amounts.Length; i++)
                    if (_amounts[i] > maxAmount)
                        maxAmount = _amounts[i];

                if (maxAmount <= 0.000001f)
                    return;

                _mesh.MarkDynamic();
                Bounds b = _mesh.bounds;
                b.Expand(Mathf.Max(0.05f, maxAmount * 64.0f));
                _mesh.bounds = b;
                _ready = true;
            }

            private void Update()
            {
                if (!_ready || _mesh == null || _baseVertices == null || _workVertices == null || _amounts == null || _phases == null || _pivots == null)
                    return;

                float timeMs = Time.time * 1000.0f;
                for (int i = 0; i < _baseVertices.Length; i++)
                {
                    Vector3 v = _baseVertices[i];
                    float amount = _amounts[i];
                    if (amount > 0.000001f)
                    {
                        float phase = _phases[i];
                        float divisor = C2NatureObjectsV16TreeSwayBaseTimeDivisorMsLikeOriginal + Mathf.Repeat(phase * 57.29578f, 100.0f);
                        float wave = Mathf.Cos(timeMs / Mathf.Max(1.0f, divisor) + phase);
                        float mod = 0.65f + 0.35f * Mathf.Sin(timeMs / 1700.0f + phase * 1.7f);
                        float a = amount * mod * wave;
                        float ca = Mathf.Cos(a);
                        float sa = Mathf.Sin(a);
                        Vector3 pivot = _pivots[i];
                        Vector3 d = _baseVertices[i] - pivot;
                        v.x = pivot.x + d.x * ca - d.y * sa;
                        v.y = pivot.y + d.x * sa + d.y * ca;
                        v.z = _baseVertices[i].z;
                    }
                    _workVertices[i] = v;
                }

                _mesh.vertices = _workVertices;
            }
        }

        private void AppendNatureSpriteQuadV2LikeOriginal(NatureMeshBatchV2LikeOriginal batch, Tre2MapObjectV28LikeOriginal obj, NatureSpriteDescV1LikeOriginal desc, Texture2D tex, NatureKindV1LikeOriginal kind, int objectOrderForDepth = -1)
        {
            if (batch == null || obj == null || desc == null || tex == null)
                return;

            float pixelToWorld = Mathf.Max(0.001f, WallOriginalXYUnitToWorldScaleV8LikeOriginal());
            int rawFrame = desc.SpriteIndex;
            bool flipU = rawFrame >= 4096;

            float wPx = Mathf.Max(2.0f, tex.width);
            float hPx = Mathf.Max(2.0f, tex.height);
            float pivotX = desc.CenterX;
            float pivotY = desc.CenterY;
            if (Mathf.Abs(pivotX) < 0.001f) pivotX = wPx * 0.5f;
            if (Mathf.Abs(pivotY) < 0.001f) pivotY = hPx;

            Vector3 baseWorld = WallOriginalXYToWorldV1LikeOriginal(obj.X, obj.Y, desc.FixHeight > -1000 ? desc.FixHeight : 0.0f);
            baseWorld.y += C2NatureObjectsV2YOffsetWorldLikeOriginal;

            float left = -pivotX * pixelToWorld;
            float right = (wPx - pivotX) * pixelToWorld;

            // V9: keep V7 visual orientation, but stop using CenterY as Unity world-height pivot.
            // In the original engine CenterY is a GP/screen billboard pivot used by AddWorldPoint +
            // GetRolledBillboardTransform. Mapping that value directly to Unity world Y made trees
            // and bushes float, sink or appear under the terrain. In this Unity approximation the card
            // is built upward from the terrain contact point; then the lowest visible alpha row is
            // clamped to terrain. X/Z and frame selection remain unchanged.
            float bottom = 0.0f;
            float top = hPx * pixelToWorld;
            bool flipV = (kind == NatureKindV1LikeOriginal.Tree);
            float visibleBottomT = GetNatureVisibleBottomTFromAlphaV9LikeOriginal(tex, flipV);
            float visibleBottomLocalY = Mathf.Lerp(bottom, top, Mathf.Clamp01(visibleBottomT));
            baseWorld.y -= visibleBottomLocalY;
            if (kind == NatureKindV1LikeOriginal.Tree)
                baseWorld.z += GetNatureTreeDepthMicroOffsetWorldV23LikeOriginal(obj, desc, pixelToWorld, objectOrderForDepth);

            Vector3 bl = baseWorld + Vector3.right * left + Vector3.up * bottom;
            Vector3 br = baseWorld + Vector3.right * right + Vector3.up * bottom;
            Vector3 tr = baseWorld + Vector3.right * right + Vector3.up * top;
            Vector3 tl = baseWorld + Vector3.right * left + Vector3.up * top;

            int v0 = batch.Vertices.Count;
            batch.Vertices.Add(bl);
            batch.Vertices.Add(br);
            batch.Vertices.Add(tr);
            batch.Vertices.Add(tl);

            AppendNatureQuadUvsV16LikeOriginal(batch, flipU, flipV);

            Color32 c = new Color32(255, 255, 255, 255);
            batch.Colors.Add(c); batch.Colors.Add(c); batch.Colors.Add(c); batch.Colors.Add(c);

            float sway = GetNatureTreeSwayAmountWorldV12LikeOriginal(desc, kind, pixelToWorld);
            float phase = GetNatureTreeSwayPhaseV12LikeOriginal(obj);
            float pivotLocalY = Mathf.Clamp((hPx - pivotY) * pixelToWorld, bottom - top, top);
            Vector3 rollPivot = baseWorld + Vector3.up * pivotLocalY;
            batch.SwayAmounts.Add(sway);
            batch.SwayAmounts.Add(sway);
            batch.SwayAmounts.Add(sway);
            batch.SwayAmounts.Add(sway);
            batch.SwayPhases.Add(phase);
            batch.SwayPhases.Add(phase);
            batch.SwayPhases.Add(phase);
            batch.SwayPhases.Add(phase);
            batch.SwayPivots.Add(rollPivot);
            batch.SwayPivots.Add(rollPivot);
            batch.SwayPivots.Add(rollPivot);
            batch.SwayPivots.Add(rollPivot);
            if (sway > 0.000001f)
                batch.HasSway = true;
            batch.Triangles.Add(v0 + 0); batch.Triangles.Add(v0 + 2); batch.Triangles.Add(v0 + 1);
            batch.Triangles.Add(v0 + 0); batch.Triangles.Add(v0 + 3); batch.Triangles.Add(v0 + 2);
            batch.Count++;
        }

        private static void AppendNatureQuadUvsV16LikeOriginal(NatureMeshBatchV2LikeOriginal batch, bool flipU, bool flipV)
        {
            if (batch == null)
                return;

            if (flipU)
            {
                if (flipV)
                {
                    batch.Uv.Add(new Vector2(1, 0));
                    batch.Uv.Add(new Vector2(0, 0));
                    batch.Uv.Add(new Vector2(0, 1));
                    batch.Uv.Add(new Vector2(1, 1));
                }
                else
                {
                    batch.Uv.Add(new Vector2(1, 1));
                    batch.Uv.Add(new Vector2(0, 1));
                    batch.Uv.Add(new Vector2(0, 0));
                    batch.Uv.Add(new Vector2(1, 0));
                }
            }
            else
            {
                if (flipV)
                {
                    batch.Uv.Add(new Vector2(0, 0));
                    batch.Uv.Add(new Vector2(1, 0));
                    batch.Uv.Add(new Vector2(1, 1));
                    batch.Uv.Add(new Vector2(0, 1));
                }
                else
                {
                    batch.Uv.Add(new Vector2(0, 1));
                    batch.Uv.Add(new Vector2(1, 1));
                    batch.Uv.Add(new Vector2(1, 0));
                    batch.Uv.Add(new Vector2(0, 0));
                }
            }
        }

        private float GetNatureVisibleBottomTFromAlphaV9LikeOriginal(Texture2D tex, bool flipV)
        {
            Vector2 rows = GetNatureVisibleAlphaRowsV9LikeOriginal(tex);
            float h = tex != null ? Mathf.Max(2.0f, tex.height) : 2.0f;
            float min01 = Mathf.Clamp01(rows.x / Mathf.Max(1.0f, h - 1.0f));
            float max01 = Mathf.Clamp01(rows.y / Mathf.Max(1.0f, h - 1.0f));

            // If bottom geometry samples texture V=0, visible bottom is min alpha row.
            // If bottom geometry samples texture V=1, visible bottom is mirrored from max alpha row.
            return flipV ? min01 : (1.0f - max01);
        }

        private Vector2 GetNatureVisibleAlphaRowsV9LikeOriginal(Texture2D tex)
        {
            if (tex == null)
                return new Vector2(0.0f, 1.0f);

            if (_c2NatureAlphaRowsCacheV9LikeOriginal.TryGetValue(tex, out Vector2 cached))
                return cached;

            int w = Mathf.Max(1, tex.width);
            int h = Mathf.Max(1, tex.height);
            int minY = h - 1;
            int maxY = 0;
            bool any = false;

            try
            {
                Color32[] px = tex.GetPixels32();
                int minPixelsInRow = Mathf.Max(2, w / 128);
                for (int y = 0; y < h; y++)
                {
                    int row = 0;
                    int ofs = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        if (px[ofs + x].a > 24)
                        {
                            row++;
                            if (row >= minPixelsInRow)
                                break;
                        }
                    }

                    if (row >= minPixelsInRow)
                    {
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                        any = true;
                    }
                }
            }
            catch
            {
                any = false;
            }

            Vector2 result = any ? new Vector2(minY, maxY) : new Vector2(0.0f, h - 1.0f);
            _c2NatureAlphaRowsCacheV9LikeOriginal[tex] = result;
            return result;
        }

        private void AppendNatureFieldPatchV2LikeOriginal(NatureMeshBatchV2LikeOriginal batch, Tre2MapObjectV28LikeOriginal obj, NatureSpriteDescV1LikeOriginal desc)
        {
            if (batch == null || obj == null || desc == null)
                return;

            float halfW = Mathf.Max(16.0f, desc.FieldWidth * 32.0f);
            float halfH = Mathf.Max(16.0f, desc.FieldHeight * 32.0f);
            float x0 = obj.X - halfW;
            float x1 = obj.X + halfW;
            float y0 = obj.Y - halfH;
            float y1 = obj.Y + halfH;

            Vector3 bl = WallOriginalXYToWorldV1LikeOriginal(x0, y0, 0.0f); bl.y += C2NatureObjectsV2FieldYOffsetWorldLikeOriginal;
            Vector3 br = WallOriginalXYToWorldV1LikeOriginal(x1, y0, 0.0f); br.y += C2NatureObjectsV2FieldYOffsetWorldLikeOriginal;
            Vector3 tr = WallOriginalXYToWorldV1LikeOriginal(x1, y1, 0.0f); tr.y += C2NatureObjectsV2FieldYOffsetWorldLikeOriginal;
            Vector3 tl = WallOriginalXYToWorldV1LikeOriginal(x0, y1, 0.0f); tl.y += C2NatureObjectsV2FieldYOffsetWorldLikeOriginal;

            int v0 = batch.Vertices.Count;
            batch.Vertices.Add(bl);
            batch.Vertices.Add(br);
            batch.Vertices.Add(tr);
            batch.Vertices.Add(tl);
            batch.Uv.Add(new Vector2(0, 0));
            batch.Uv.Add(new Vector2(1, 0));
            batch.Uv.Add(new Vector2(1, 1));
            batch.Uv.Add(new Vector2(0, 1));
            Color32 c = new Color32(255, 255, 255, 220);
            batch.Colors.Add(c); batch.Colors.Add(c); batch.Colors.Add(c); batch.Colors.Add(c);
            batch.SwayAmounts.Add(0.0f); batch.SwayAmounts.Add(0.0f); batch.SwayAmounts.Add(0.0f); batch.SwayAmounts.Add(0.0f);
            batch.SwayPhases.Add(0.0f); batch.SwayPhases.Add(0.0f); batch.SwayPhases.Add(0.0f); batch.SwayPhases.Add(0.0f);
            batch.Triangles.Add(v0 + 0); batch.Triangles.Add(v0 + 2); batch.Triangles.Add(v0 + 1);
            batch.Triangles.Add(v0 + 0); batch.Triangles.Add(v0 + 3); batch.Triangles.Add(v0 + 2);
            batch.Count++;
        }

        private static void ApplyNatureTextureSamplerV16LikeOriginal(Texture2D tex, NatureKindV1LikeOriginal kind)
        {
            if (tex == null)
                return;

            if (kind == NatureKindV1LikeOriginal.Tree)
            {
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Mirror;
            }
            else
            {
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
            }
        }

        private Texture2D TryLoadNatureSpriteTextureV2LikeOriginal(NatureSpriteCatalogV1LikeOriginal catalog, NatureSpriteDescV1LikeOriginal desc, NatureKindV1LikeOriginal kind, out string source)
        {
            source = string.Empty;
            if (catalog == null || desc == null)
                return null;

            int frame = desc.SpriteIndex & 4095;
            string gp = string.IsNullOrWhiteSpace(catalog.GpName) ? catalog.Label : catalog.GpName;
            string cacheKey = gp + ":" + frame.ToString(CultureInfo.InvariantCulture);
            if (_c2NatureTextureCacheV2LikeOriginal.TryGetValue(cacheKey, out Texture2D cached))
            {
                source = "cache:" + cacheKey;
                return cached;
            }

            List<string> paths = BuildNaturePackageCandidatePathsV3LikeOriginal(gp, kind);
            for (int i = 0; i < paths.Count; i++)
            {
                string abs = paths[i];
                if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                    continue;

                Texture2D tex = null;
                string s = string.Empty;
                string ext = Path.GetExtension(abs) ?? string.Empty;

                if (ext.Equals(".g2d", StringComparison.OrdinalIgnoreCase))
                {
                    tex = TryLoadG2DFrameViaMelinojaV3LikeOriginal(abs, frame, out s);
                    if (tex != null)
                    {
                        tex.name = "C2_Nature_G2D_" + gp + "_frame_" + frame.ToString("0000", CultureInfo.InvariantCulture);
                        ApplyNatureTextureSamplerV16LikeOriginal(tex, kind);
                        _c2NatureTextureCacheV2LikeOriginal[cacheKey] = tex;
                        source = "G2D:" + s;
                        return tex;
                    }
                }
                else if (ext.Equals(".g16", StringComparison.OrdinalIgnoreCase))
                {
                    tex = TryLoadG16FrameViaMelinojaV42LikeOriginal(abs, frame, out s);
                    if (tex != null)
                    {
                        tex.name = "C2_Nature_G16_" + gp + "_frame_" + frame.ToString("0000", CultureInfo.InvariantCulture);
                        ApplyNatureTextureSamplerV16LikeOriginal(tex, kind);
                        _c2NatureTextureCacheV2LikeOriginal[cacheKey] = tex;
                        source = "G16:" + s;
                        return tex;
                    }
                }

                source = s;
            }

            string resFrame = "frame_" + frame.ToString("0000", CultureInfo.InvariantCulture);
            string[] resPaths =
            {
                "Nature/" + gp + "_frames/" + resFrame,
                gp + "_frames/" + resFrame,
                "Cash/" + gp + "_frames/" + resFrame
            };
            for (int i = 0; i < resPaths.Length; i++)
            {
                Texture2D tex = Resources.Load<Texture2D>(resPaths[i]);
                if (tex != null)
                {
                    ApplyNatureTextureSamplerV16LikeOriginal(tex, kind);
                    _c2NatureTextureCacheV2LikeOriginal[cacheKey] = tex;
                    source = "Resources:" + resPaths[i];
                    return tex;
                }
            }

            if (string.IsNullOrWhiteSpace(source))
                source = "missing nature package gp=" + gp + " kind=" + kind + " frame=" + frame.ToString(CultureInfo.InvariantCulture) +
                         " resolver=" + C2NatureObjectsV3TreeResolverContractLikeOriginal + "/" + C2NatureObjectsV3StoneResolverContractLikeOriginal;
            return null;
        }

        private static readonly HashSet<string> C2NatureObjectsLoadedG2DV3LikeOriginal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static Texture2D TryLoadG2DFrameViaMelinojaV3LikeOriginal(string abs, int frameIndex, out string source)
        {
            source = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                {
                    source = "g2d_path_not_found:" + (abs ?? string.Empty);
                    return null;
                }

                Texture2D decodedFrameTex = TryLoadG2DFrameViaDecodeToFramesV4LikeOriginal(abs, frameIndex, out string decodedFrameAudit);
                if (decodedFrameTex != null)
                {
                    source = decodedFrameAudit;
                    return decodedFrameTex;
                }

                Type bridgeType = ResolveMelinojaBridgeTypeV2LikeOriginal();
                if (bridgeType == null)
                {
                    source = "G2D DecodeToFrames failed: " + decodedFrameAudit + "; Melinoja bridge type not found for old G2D API";
                    return null;
                }

                if (!C2NatureObjectsLoadedG2DV3LikeOriginal.Contains(abs))
                {
                    TryInvokeNatureG2DLoadV3LikeOriginal(bridgeType, abs, out string loadAudit);
                    C2NatureObjectsLoadedG2DV3LikeOriginal.Add(abs);
                    source = loadAudit;
                }

                Texture2D tex = TryInvokeNatureG2DFrameTextureV3LikeOriginal(bridgeType, abs, frameIndex, out string texAudit);
                if (tex != null)
                {
                    source = (string.IsNullOrWhiteSpace(source) ? string.Empty : source + " ") + texAudit;
                    return tex;
                }

                byte[] rgba = TryInvokeNatureG2DFrameRgbaV3LikeOriginal(bridgeType, abs, frameIndex, out int w, out int h, out string rgbaAudit);
                if (rgba != null && w > 0 && h > 0 && rgba.Length >= w * h * 4)
                {
                    tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                    tex.name = "C2_G2D_" + Path.GetFileNameWithoutExtension(abs) + "_frame_" + frameIndex.ToString(CultureInfo.InvariantCulture);
                    tex.LoadRawTextureData(rgba);
                    tex.Apply(false, false);
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    source = (string.IsNullOrWhiteSpace(source) ? string.Empty : source + " ") + rgbaAudit;
                    return tex;
                }

                source = (string.IsNullOrWhiteSpace(source) ? string.Empty : source + " ") + texAudit + " " + rgbaAudit;
                return null;
            }
            catch (Exception ex)
            {
                source = "MelinojaG2D failed: " + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }


        private static readonly Dictionary<string, string> C2NatureObjectsDecodedG2DDirsV4LikeOriginal = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> C2NatureObjectsLoggedG2DMethodsV4LikeOriginal = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static Texture2D TryLoadG2DFrameViaDecodeToFramesV4LikeOriginal(string abs, int frameIndex, out string source)
        {
            source = "G2DDecodeToFramesV5:none";
            try
            {
                if (string.IsNullOrWhiteSpace(abs) || !File.Exists(abs))
                {
                    source = "G2DDecodeToFramesV5 path_not_found:" + (abs ?? string.Empty);
                    return null;
                }

                string outDir = GetNatureG2DDecodeCacheDirV4LikeOriginal(abs);
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                bool decoded = false;
                string decodeAudit = string.Empty;
                string searchDirsJoined = string.Empty;

                if (!C2NatureObjectsDecodedG2DDirsV4LikeOriginal.TryGetValue(abs, out searchDirsJoined) || string.IsNullOrWhiteSpace(searchDirsJoined))
                {
                    decoded = TryInvokeAnyG2DDecodeToFramesV4LikeOriginal(abs, outDir, out decodeAudit);
                    List<string> dirs = BuildNatureG2DFrameSearchDirsV5LikeOriginal(abs, outDir, decodeAudit);
                    searchDirsJoined = JoinNatureDirsV5LikeOriginal(dirs);
                    C2NatureObjectsDecodedG2DDirsV4LikeOriginal[abs] = searchDirsJoined;
                    Debug.Log("[C2:NATURE G2D DIRS V5] file='" + abs + "' decoded=" + decoded + " dirs=" + searchDirsJoined + " audit=" + decodeAudit);
                }
                else
                {
                    decoded = true;
                    decodeAudit = "cached_search_dirs=" + searchDirsJoined;
                }

                string existing = FindNatureDecodedG2DFrameFileInDirsV5LikeOriginal(searchDirsJoined, frameIndex);
                if (string.IsNullOrWhiteSpace(existing))
                {
                    source = "G2DDecodeToFramesV5 no frame file frame=" + frameIndex.ToString(CultureInfo.InvariantCulture) +
                             " decoded=" + decoded + " dirs=" + searchDirsJoined + " audit=" + decodeAudit;
                    return null;
                }

                Texture2D tex = LoadNatureDecodedFrameTextureV4LikeOriginal(existing, out string loadAudit);
                if (tex == null)
                {
                    source = "G2DDecodeToFramesV5 frameFile=" + existing + " loadFailed=" + loadAudit;
                    return null;
                }

                tex.name = "C2_Nature_G2D_DecodedFrame_" + Path.GetFileNameWithoutExtension(abs) + "_" + frameIndex.ToString("0000", CultureInfo.InvariantCulture);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                source = "G2DDecodeToFramesV5 frameFile=" + existing + " decoded=" + decoded + " loaded=" + loadAudit;
                return tex;
            }
            catch (Exception ex)
            {
                source = "G2DDecodeToFramesV5 exception=" + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }

        private static string GetNatureG2DDecodeCacheDirV4LikeOriginal(string abs)
        {
            string root = Application.temporaryCachePath;
            if (string.IsNullOrWhiteSpace(root))
                root = Path.Combine(Application.dataPath, "..", "Library");
            string safe = MakeNatureSafeFileNameV4LikeOriginal(Path.GetFileNameWithoutExtension(abs));
            string hash = GetNatureStableHashHexV4LikeOriginal(abs ?? string.Empty);
            return Path.Combine(root, "C2NatureG2DFrames", safe + "_" + hash);
        }

        private static string MakeNatureSafeFileNameV4LikeOriginal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "g2d";
            char[] arr = s.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                char c = arr[i];
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-')) arr[i] = '_';
            }
            return new string(arr);
        }

        private static string GetNatureStableHashHexV4LikeOriginal(string s)
        {
            unchecked
            {
                uint h = 2166136261u;
                for (int i = 0; i < s.Length; i++)
                {
                    h ^= char.ToUpperInvariant(s[i]);
                    h *= 16777619u;
                }
                return h.ToString("X8", CultureInfo.InvariantCulture);
            }
        }

        private static string FindNatureDecodedG2DFrameFileV4LikeOriginal(string outDir, int frameIndex)
        {
            if (string.IsNullOrWhiteSpace(outDir) || !Directory.Exists(outDir))
                return string.Empty;

            string f4 = "frame_" + frameIndex.ToString("D4", CultureInfo.InvariantCulture);
            string f0 = "frame_" + frameIndex.ToString(CultureInfo.InvariantCulture);
            string n4 = frameIndex.ToString("D4", CultureInfo.InvariantCulture);
            string n0 = frameIndex.ToString(CultureInfo.InvariantCulture);
            string[] exact =
            {
                Path.Combine(outDir, f4 + ".tga"),
                Path.Combine(outDir, f4 + ".png"),
                Path.Combine(outDir, f0 + ".tga"),
                Path.Combine(outDir, f0 + ".png"),
                Path.Combine(outDir, n4 + ".tga"),
                Path.Combine(outDir, n4 + ".png"),
                Path.Combine(outDir, n0 + ".tga"),
                Path.Combine(outDir, n0 + ".png")
            };
            for (int i = 0; i < exact.Length; i++)
                if (File.Exists(exact[i])) return exact[i];

            try
            {
                string[] files = Directory.GetFiles(outDir, "*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(files[i]) ?? string.Empty;
                    string ext = Path.GetExtension(files[i]) ?? string.Empty;
                    if (!ext.Equals(".tga", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (name.Equals(f4, StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(f0, StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(n4, StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(n0, StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("_" + n4, StringComparison.OrdinalIgnoreCase) ||
                        name.EndsWith("_" + n0, StringComparison.OrdinalIgnoreCase) ||
                        name.IndexOf(f4, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf(f0, StringComparison.OrdinalIgnoreCase) >= 0)
                        return files[i];
                }
            }
            catch { }

            return string.Empty;
        }

        private static string FindNatureDecodedG2DFrameFileInDirsV5LikeOriginal(string joinedDirs, int frameIndex)
        {
            if (string.IsNullOrWhiteSpace(joinedDirs))
                return string.Empty;
            string[] dirs = joinedDirs.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < dirs.Length; i++)
            {
                string file = FindNatureDecodedG2DFrameFileV4LikeOriginal(dirs[i], frameIndex);
                if (!string.IsNullOrWhiteSpace(file))
                    return file;
            }
            return string.Empty;
        }

        private static List<string> BuildNatureG2DFrameSearchDirsV5LikeOriginal(string abs, string requestedOutDir, string decodeAudit)
        {
            var dirs = new List<string>();
            AddNatureDirV5LikeOriginal(dirs, requestedOutDir);

            string srcDir = string.Empty;
            string baseNoExt = string.Empty;
            try
            {
                srcDir = Path.GetDirectoryName(abs) ?? string.Empty;
                baseNoExt = Path.GetFileNameWithoutExtension(abs) ?? string.Empty;
            }
            catch { }

            AddNatureFrameDirVariantsV5LikeOriginal(dirs, srcDir, baseNoExt);
            AddNatureFrameDirVariantsV5LikeOriginal(dirs, Path.Combine(srcDir, "Cash"), baseNoExt);
            AddNatureFrameDirVariantsV5LikeOriginal(dirs, Path.Combine(srcDir, "..", "Cash"), baseNoExt);

            List<string> hints = ExtractNaturePathHintsV5LikeOriginal(decodeAudit);
            for (int i = 0; i < hints.Count; i++)
            {
                string h = hints[i];
                if (string.IsNullOrWhiteSpace(h)) continue;
                try
                {
                    if (File.Exists(h))
                    {
                        string hd = Path.GetDirectoryName(h) ?? string.Empty;
                        AddNatureDirV5LikeOriginal(dirs, hd);
                        AddNatureFrameDirVariantsV5LikeOriginal(dirs, hd, baseNoExt);
                        AddNatureFrameDirVariantsV5LikeOriginal(dirs, Path.GetDirectoryName(hd) ?? string.Empty, baseNoExt);
                    }
                    else
                    {
                        AddNatureDirV5LikeOriginal(dirs, h);
                        AddNatureFrameDirVariantsV5LikeOriginal(dirs, h, baseNoExt);
                    }
                }
                catch { }
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(srcDir) && Directory.Exists(srcDir))
                {
                    string[] sub = Directory.GetDirectories(srcDir, "*frames*", SearchOption.TopDirectoryOnly);
                    for (int i = 0; i < sub.Length; i++)
                    {
                        string n = Path.GetFileName(sub[i]) ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(baseNoExt) || n.IndexOf(baseNoExt, StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("tree", StringComparison.OrdinalIgnoreCase) >= 0)
                            AddNatureDirV5LikeOriginal(dirs, sub[i]);
                    }
                }
            }
            catch { }

            return dirs;
        }

        private static void AddNatureFrameDirVariantsV5LikeOriginal(List<string> dirs, string root, string baseNoExt)
        {
            if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(baseNoExt))
                return;
            AddNatureDirV5LikeOriginal(dirs, Path.Combine(root, baseNoExt + "_frames"));
            AddNatureDirV5LikeOriginal(dirs, Path.Combine(root, baseNoExt + "_Frames"));
            AddNatureDirV5LikeOriginal(dirs, Path.Combine(root, baseNoExt + "_FRAMES"));
            AddNatureDirV5LikeOriginal(dirs, Path.Combine(root, baseNoExt + ".g2d_frames"));
            AddNatureDirV5LikeOriginal(dirs, Path.Combine(root, baseNoExt + "_g2d_frames"));
            AddNatureDirV5LikeOriginal(dirs, Path.Combine(root, baseNoExt + "_G2D_frames"));
            AddNatureDirV5LikeOriginal(dirs, Path.Combine(root, baseNoExt + "Frames"));
            AddNatureDirV5LikeOriginal(dirs, Path.Combine(root, baseNoExt));
        }

        private static void AddNatureDirV5LikeOriginal(List<string> dirs, string dir)
        {
            if (dirs == null || string.IsNullOrWhiteSpace(dir))
                return;
            string full;
            try { full = Path.GetFullPath(dir); } catch { full = dir; }
            if (!Directory.Exists(full))
                return;
            for (int i = 0; i < dirs.Count; i++)
                if (string.Equals(dirs[i], full, StringComparison.OrdinalIgnoreCase))
                    return;
            dirs.Add(full);
        }

        private static string JoinNatureDirsV5LikeOriginal(List<string> dirs)
        {
            if (dirs == null || dirs.Count == 0)
                return string.Empty;
            return string.Join("|", dirs.ToArray());
        }

        private static List<string> ExtractNaturePathHintsV5LikeOriginal(string audit)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(audit))
                return result;
            string[] parts = audit.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i];
                int eq = p.IndexOf('=');
                if (eq >= 0) p = p.Substring(eq + 1);
                p = p.Trim().Trim('\'', '"', ';', ',');
                if (p.IndexOf(@":\", StringComparison.OrdinalIgnoreCase) >= 0 || p.StartsWith(@"\", StringComparison.Ordinal))
                    result.Add(p);
            }
            return result;
        }

        private static bool TryInvokeAnyG2DDecodeToFramesV4LikeOriginal(string abs, string outDir, out string audit)
        {
            audit = "DecodeG2DToLogAndFrames:not_found";
            try
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); } catch { continue; }
                    for (int ti = 0; ti < types.Length; ti++)
                    {
                        Type t = types[ti];
                        if (t == null) continue;
                        MethodInfo[] methods;
                        try { methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static); } catch { continue; }
                        for (int mi = 0; mi < methods.Length; mi++)
                        {
                            MethodInfo m = methods[mi];
                            if (m == null || !string.Equals(m.Name, "DecodeG2DToLogAndFrames", StringComparison.Ordinal))
                                continue;

                            string sig = DescribeNatureMethodV4LikeOriginal(t, m);
                            if (C2NatureObjectsLoggedG2DMethodsV4LikeOriginal.Add(sig))
                                Debug.Log("[C2:NATURE G2D API V5] found " + sig);

                            if (TryInvokeNatureDecodeMethodV4LikeOriginal(m, abs, outDir, out string methodAudit))
                            {
                                audit = sig + " -> " + methodAudit;
                                return true;
                            }

                            audit = sig + " failed: " + methodAudit;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                audit = "DecodeG2DToLogAndFrames exception=" + ex.GetType().Name + ":" + ex.Message;
            }
            return false;
        }

        private static string DescribeNatureMethodV4LikeOriginal(Type t, MethodInfo m)
        {
            try
            {
                ParameterInfo[] ps = m.GetParameters();
                string[] parts = new string[ps.Length];
                for (int i = 0; i < ps.Length; i++)
                    parts[i] = ps[i].ParameterType.Name + " " + ps[i].Name;
                return t.FullName + "." + m.Name + "(" + string.Join(",", parts) + ") -> " + m.ReturnType.Name;
            }
            catch { return (t != null ? t.FullName : "?") + "." + (m != null ? m.Name : "?"); }
        }

        private static bool TryInvokeNatureDecodeMethodV4LikeOriginal(MethodInfo m, string abs, string outDir, out string audit)
        {
            audit = "invoke:none";
            try
            {
                ParameterInfo[] ps = m.GetParameters();
                object[] args = new object[ps.Length];
                int stringInCount = 0;

                for (int i = 0; i < ps.Length; i++)
                {
                    Type pt = ps[i].ParameterType;
                    bool byRef = pt.IsByRef;
                    Type et = byRef ? pt.GetElementType() : pt;
                    string pn = (ps[i].Name ?? string.Empty).ToLowerInvariant();

                    if (byRef)
                    {
                        if (et == typeof(string)) args[i] = null;
                        else if (et == typeof(int)) args[i] = 0;
                        else if (et == typeof(bool)) args[i] = false;
                        else args[i] = null;
                        continue;
                    }

                    if (et == typeof(string))
                    {
                        if (pn.Contains("out") || pn.Contains("dir") || pn.Contains("folder") || pn.Contains("frame")) args[i] = outDir;
                        else if (stringInCount == 0) args[i] = abs;
                        else args[i] = outDir;
                        stringInCount++;
                    }
                    else if (et == typeof(bool)) args[i] = true;
                    else if (et == typeof(int)) args[i] = 0;
                    else args[i] = null;
                }

                object result = m.Invoke(null, args);
                bool ok = true;
                if (result is bool rb) ok = rb;
                else if (result is int ri) ok = ri >= 0;

                string outs = string.Empty;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (ps[i].ParameterType.IsByRef)
                        outs += " out" + i.ToString(CultureInfo.InvariantCulture) + "=" + (args[i] == null ? "null" : args[i].ToString());
                }

                audit = "ok=" + ok + " result=" + (result == null ? "null" : result.ToString()) + outs;
                return ok;
            }
            catch (Exception ex)
            {
                audit = "exception=" + ex.GetType().Name + ":" + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                return false;
            }
        }

        private static Texture2D LoadNatureDecodedFrameTextureV4LikeOriginal(string path, out string audit)
        {
            audit = string.Empty;
            try
            {
                string ext = Path.GetExtension(path) ?? string.Empty;
                if (ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                    if (tex.LoadImage(bytes, false))
                    {
                        audit = "png " + tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture);
                        return tex;
                    }
                    audit = "png LoadImage false";
                    return null;
                }

                if (ext.Equals(".tga", StringComparison.OrdinalIgnoreCase))
                    return LoadNatureTgaTextureV4LikeOriginal(path, out audit);

                audit = "unsupported ext=" + ext;
                return null;
            }
            catch (Exception ex)
            {
                audit = "load exception=" + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }

        private static Texture2D LoadNatureTgaTextureV4LikeOriginal(string path, out string audit)
        {
            audit = string.Empty;
            try
            {
                byte[] d = File.ReadAllBytes(path);
                if (d == null || d.Length < 18)
                {
                    audit = "tga too small";
                    return null;
                }

                int idLen = d[0];
                int colorMapType = d[1];
                int imageType = d[2];
                int w = d[12] | (d[13] << 8);
                int h = d[14] | (d[15] << 8);
                int bpp = d[16];
                int desc = d[17];
                if (w <= 0 || h <= 0 || colorMapType != 0 || (imageType != 2 && imageType != 3 && imageType != 10) || (bpp != 24 && bpp != 32 && bpp != 8))
                {
                    audit = "unsupported tga type=" + imageType + " bpp=" + bpp + " cmap=" + colorMapType + " size=" + w + "x" + h;
                    return null;
                }

                Color32[] pix = new Color32[w * h];
                int p = 18 + idLen;
                bool topOrigin = (desc & 0x20) != 0;

                Action<int, byte, byte, byte, byte> put = (idx, r, g, b, a) =>
                {
                    int x = idx % w;
                    int y = idx / w;
                    int dy = topOrigin ? (h - 1 - y) : y;
                    if (x >= 0 && x < w && dy >= 0 && dy < h)
                        pix[dy * w + x] = new Color32(r, g, b, a);
                };

                int outIdx = 0;
                if (imageType == 2 || imageType == 3)
                {
                    while (outIdx < pix.Length && p < d.Length)
                    {
                        byte b, g, r, a;
                        if (bpp == 8)
                        {
                            byte v = d[p++]; b = g = r = v; a = 255;
                        }
                        else
                        {
                            b = d[p++]; g = d[p++]; r = d[p++]; a = bpp == 32 && p < d.Length ? d[p++] : (byte)255;
                        }
                        put(outIdx++, r, g, b, a);
                    }
                }
                else if (imageType == 10)
                {
                    while (outIdx < pix.Length && p < d.Length)
                    {
                        int header = d[p++];
                        int count = (header & 0x7F) + 1;
                        bool rle = (header & 0x80) != 0;
                        if (rle)
                        {
                            byte b, g, r, a;
                            if (bpp == 8)
                            {
                                byte v = d[p++]; b = g = r = v; a = 255;
                            }
                            else
                            {
                                b = d[p++]; g = d[p++]; r = d[p++]; a = bpp == 32 && p < d.Length ? d[p++] : (byte)255;
                            }
                            for (int k = 0; k < count && outIdx < pix.Length; k++) put(outIdx++, r, g, b, a);
                        }
                        else
                        {
                            for (int k = 0; k < count && outIdx < pix.Length && p < d.Length; k++)
                            {
                                byte b, g, r, a;
                                if (bpp == 8)
                                {
                                    byte v = d[p++]; b = g = r = v; a = 255;
                                }
                                else
                                {
                                    b = d[p++]; g = d[p++]; r = d[p++]; a = bpp == 32 && p < d.Length ? d[p++] : (byte)255;
                                }
                                put(outIdx++, r, g, b, a);
                            }
                        }
                    }
                }

                Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
                tex.SetPixels32(pix);
                tex.Apply(false, false);
                audit = "tga " + w.ToString(CultureInfo.InvariantCulture) + "x" + h.ToString(CultureInfo.InvariantCulture) + " type=" + imageType + " bpp=" + bpp;
                return tex;
            }
            catch (Exception ex)
            {
                audit = "tga exception=" + ex.GetType().Name + ":" + ex.Message;
                return null;
            }
        }

        private static bool TryInvokeNatureG2DLoadV3LikeOriginal(Type bridgeType, string abs, out string audit)
        {
            audit = "G2DLoad:none";
            if (bridgeType == null)
                return false;

            string[] methodNames =
            {
                "LoadG2DToMemory",
                "LoadG2D",
                "LoadGP2DToMemory",
                "LoadSpritePackageToMemory"
            };

            for (int i = 0; i < methodNames.Length; i++)
            {
                MethodInfo mi = bridgeType.GetMethod(methodNames[i], BindingFlags.Public | BindingFlags.Static);
                if (mi == null)
                    continue;

                try
                {
                    ParameterInfo[] ps = mi.GetParameters();
                    object result;
                    if (ps.Length == 3)
                    {
                        object[] args = { abs, null, false };
                        result = mi.Invoke(null, args);
                        bool ok = !(result is bool b) || b;
                        audit = methodNames[i] + " ok=" + ok + " err=" + (args[1] as string ?? string.Empty);
                        return ok;
                    }
                    if (ps.Length == 2)
                    {
                        object[] args = { abs, null };
                        result = mi.Invoke(null, args);
                        bool ok = !(result is bool b) || b;
                        audit = methodNames[i] + " ok=" + ok + " err=" + (args[1] as string ?? string.Empty);
                        return ok;
                    }
                    if (ps.Length == 1)
                    {
                        object[] args = { abs };
                        result = mi.Invoke(null, args);
                        bool ok = !(result is bool b) || b;
                        audit = methodNames[i] + " ok=" + ok;
                        return ok;
                    }
                }
                catch (Exception ex)
                {
                    audit = methodNames[i] + " failed=" + ex.GetType().Name + ":" + ex.Message;
                }
            }

            return false;
        }

        private static Texture2D TryInvokeNatureG2DFrameTextureV3LikeOriginal(Type bridgeType, string abs, int frameIndex, out string audit)
        {
            audit = "G2DTexture:none";
            if (bridgeType == null)
                return null;

            string[] methodNames =
            {
                "TryGetG2DFrameTexture",
                "TryGetG2DFrameTexture2D",
                "GetG2DFrameTexture",
                "GetG2DFrameTexture2D"
            };

            for (int i = 0; i < methodNames.Length; i++)
            {
                MethodInfo mi = bridgeType.GetMethod(methodNames[i], BindingFlags.Public | BindingFlags.Static);
                if (mi == null)
                    continue;

                try
                {
                    ParameterInfo[] ps = mi.GetParameters();
                    object result;
                    if (ps.Length == 4)
                    {
                        object[] args = { abs, frameIndex, null, null };
                        result = mi.Invoke(null, args);
                        Texture2D tex = args[2] as Texture2D ?? result as Texture2D;
                        bool ok = result is bool b ? b : tex != null;
                        audit = methodNames[i] + " ok=" + ok + " err=" + (args[3] as string ?? string.Empty);
                        if (ok && tex != null) return tex;
                    }
                    else if (ps.Length == 3)
                    {
                        object[] args = { abs, frameIndex, null };
                        result = mi.Invoke(null, args);
                        Texture2D tex = args[2] as Texture2D ?? result as Texture2D;
                        bool ok = result is bool b ? b : tex != null;
                        audit = methodNames[i] + " ok=" + ok;
                        if (ok && tex != null) return tex;
                    }
                    else if (ps.Length == 2)
                    {
                        object[] args = { abs, frameIndex };
                        result = mi.Invoke(null, args);
                        Texture2D tex = result as Texture2D;
                        audit = methodNames[i] + " tex=" + (tex != null);
                        if (tex != null) return tex;
                    }
                }
                catch (Exception ex)
                {
                    audit = methodNames[i] + " failed=" + ex.GetType().Name + ":" + ex.Message;
                }
            }

            return null;
        }

        private static byte[] TryInvokeNatureG2DFrameRgbaV3LikeOriginal(Type bridgeType, string abs, int frameIndex, out int w, out int h, out string audit)
        {
            w = 0;
            h = 0;
            audit = "G2DRGBA:none";
            if (bridgeType == null)
                return null;

            string[] methodNames =
            {
                "TryGetG2DFrameRGBA",
                "TryGetG2DFrameRgba",
                "TryGetG2DFramePixelsRGBA",
                "TryGetSpritePackageFrameRGBA"
            };

            for (int i = 0; i < methodNames.Length; i++)
            {
                MethodInfo mi = bridgeType.GetMethod(methodNames[i], BindingFlags.Public | BindingFlags.Static);
                if (mi == null)
                    continue;

                try
                {
                    ParameterInfo[] ps = mi.GetParameters();
                    object result;
                    if (ps.Length == 6)
                    {
                        object[] args = { abs, frameIndex, 0, 0, null, null };
                        result = mi.Invoke(null, args);
                        bool ok = result is bool b && b;
                        w = args[2] is int iw ? iw : 0;
                        h = args[3] is int ih ? ih : 0;
                        byte[] rgba = args[4] as byte[];
                        audit = methodNames[i] + " ok=" + ok + " size=" + w + "x" + h + " err=" + (args[5] as string ?? string.Empty);
                        if (ok && rgba != null) return rgba;
                    }
                    else if (ps.Length == 5)
                    {
                        object[] args = { abs, frameIndex, 0, 0, null };
                        result = mi.Invoke(null, args);
                        bool ok = result is bool b && b;
                        w = args[2] is int iw ? iw : 0;
                        h = args[3] is int ih ? ih : 0;
                        byte[] rgba = args[4] as byte[];
                        audit = methodNames[i] + " ok=" + ok + " size=" + w + "x" + h;
                        if (ok && rgba != null) return rgba;
                    }
                }
                catch (Exception ex)
                {
                    audit = methodNames[i] + " failed=" + ex.GetType().Name + ":" + ex.Message;
                }
            }

            return null;
        }

        private static List<string> BuildNaturePackageCandidatePathsV3LikeOriginal(string gp, NatureKindV1LikeOriginal kind)
        {
            List<string> names = new List<string>();
            gp = string.IsNullOrWhiteSpace(gp) ? string.Empty : gp.Trim();

            if (kind == NatureKindV1LikeOriginal.Tree)
            {
                // Original chain is Treelist.lst -> gp='TREES' -> package resolver.
                // V11: TreesAll.g2d is not a replacement for the visible tree atlas in this path.
                // V10 proved it mostly renders the shadow/silhouette layer and hides the actual tree sprites.
                // Use the real visible tree package first. A separate shadow pass must be implemented later.
                // Never probe Trees.g16 here.
                AddNaturePackageCandidateNameV3LikeOriginal(names, gp + ".g2d");
                AddNaturePackageCandidateNameV3LikeOriginal(names, gp.ToUpperInvariant() + ".g2d");
                AddNaturePackageCandidateNameV3LikeOriginal(names, gp.ToLowerInvariant() + ".g2d");
                AddNaturePackageCandidateNameV3LikeOriginal(names, "Trees.g2d");
                AddNaturePackageCandidateNameV3LikeOriginal(names, "TREES.g2d");
                // Keep TreesAll only as a last-resort fallback so V11 does not blank the map if Trees.g2d is absent.
                AddNaturePackageCandidateNameV3LikeOriginal(names, "TreesAll.g2d");
                AddNaturePackageCandidateNameV3LikeOriginal(names, "TREESALL.g2d");
            }
            else if (kind == NatureKindV1LikeOriginal.Stone)
            {
                // User confirmed stones are G16.
                AddNaturePackageCandidateNameV3LikeOriginal(names, gp + ".g16");
                AddNaturePackageCandidateNameV3LikeOriginal(names, gp.ToUpperInvariant() + ".g16");
                AddNaturePackageCandidateNameV3LikeOriginal(names, gp.ToLowerInvariant() + ".g16");
                AddNaturePackageCandidateNameV3LikeOriginal(names, "STONES.g16");
                AddNaturePackageCandidateNameV3LikeOriginal(names, "Stones.g16");
            }
            else
            {
                AddNaturePackageCandidateNameV3LikeOriginal(names, gp + ".g16");
                AddNaturePackageCandidateNameV3LikeOriginal(names, gp + ".g2d");
                AddNaturePackageCandidateNameV3LikeOriginal(names, gp.ToUpperInvariant() + ".g16");
                AddNaturePackageCandidateNameV3LikeOriginal(names, gp.ToUpperInvariant() + ".g2d");
                AddNaturePackageCandidateNameV3LikeOriginal(names, gp.ToLowerInvariant() + ".g16");
                AddNaturePackageCandidateNameV3LikeOriginal(names, gp.ToLowerInvariant() + ".g2d");
                AddNaturePackageCandidateNameV3LikeOriginal(names, "COMPLEX.g16");
                AddNaturePackageCandidateNameV3LikeOriginal(names, "Complex.g16");
                AddNaturePackageCandidateNameV3LikeOriginal(names, "COMPLEX.g2d");
                AddNaturePackageCandidateNameV3LikeOriginal(names, "Complex.g2d");
            }

            string streaming = Application.streamingAssetsPath;
            string data = Application.dataPath;
            string[] roots =
            {
                Path.Combine(data, "Resources"),
                Path.Combine(data, "Resources", "Cash"),
                Path.Combine(data, "Resources", "Nature"),
                Path.Combine(streaming, "Cossacks2", "Data"),
                Path.Combine(streaming, "Cossacks2", "Data", "Cash"),
                Path.Combine(streaming, "Cossacks2", "Data1"),
                Path.Combine(streaming, "Cossacks2", "Data1", "Cash"),
                @"C:\GSC Game World\Cossacks II\Data",
                @"C:\GSC Game World\Cossacks II\Data\Cash",
                @"C:\GSC Game World\Cossacks II\Data1",
                @"C:\GSC Game World\Cossacks II\Data1\Cash"
            };

            List<string> result = new List<string>();
            for (int r = 0; r < roots.Length; r++)
            {
                for (int n = 0; n < names.Count; n++)
                    result.Add(Path.Combine(roots[r], names[n]));
            }
            return result;
        }

        private static void AddNaturePackageCandidateNameV3LikeOriginal(List<string> names, string name)
        {
            if (names == null || string.IsNullOrWhiteSpace(name))
                return;
            string a = name.Replace('/', '\\').Trim();
            for (int i = 0; i < names.Count; i++)
            {
                if (string.Equals(names[i], a, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            names.Add(a);
        }

        private Material GetNatureMaterialV2LikeOriginal(Texture2D tex, string gpName, NatureKindV1LikeOriginal kind, bool transparent, bool animatedTree = false)
        {
            string key = (tex != null ? tex.GetInstanceID().ToString(CultureInfo.InvariantCulture) : "null") + ":" + gpName + ":" + kind + ":" + transparent + ":" + animatedTree;
            if (_c2NatureMaterialCacheV2LikeOriginal.TryGetValue(key, out Material cached) && cached != null)
                return cached;

            Shader shader = kind == NatureKindV1LikeOriginal.Tree
                ? Shader.Find("Cossacks2Bridge/NatureTreeSpriteV14")
                : null;
            if (shader == null && kind == NatureKindV1LikeOriginal.Tree)
                shader = Shader.Find("Cossacks2Bridge/WallObjectSpriteV31ExactCutout");
            if (shader == null && kind == NatureKindV1LikeOriginal.Tree)
                shader = Shader.Find("Cossacks2Bridge/WallObjectSpriteV29");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            mat.name = "C2_Nature_" + gpName + "_" + kind + "_V2";
            if (tex != null)
            {
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }
            Color visibleDiffuse = (kind == NatureKindV1LikeOriginal.Tree && animatedTree)
                ? new Color(0.5f, 0.5f, 0.5f, 1.0f)
                : Color.white;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", visibleDiffuse);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", visibleDiffuse);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            mat.renderQueue = C2NatureObjectsV2RenderQueueLikeOriginal;
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", kind == NatureKindV1LikeOriginal.Tree ? 1 : 0);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            if (mat.HasProperty("_AlphaCutoff"))
            {
                float alphaRef = kind == NatureKindV1LikeOriginal.Tree
                    ? (animatedTree ? C2NatureObjectsV16AnimatedTreeAlphaRefLikeOriginal : C2NatureObjectsV16TreeAlphaRefLikeOriginal)
                    : 1.0f / 255.0f;
                mat.SetFloat("_AlphaCutoff", alphaRef);
            }
            if (mat.HasProperty("_OpaqueAfterClip")) mat.SetFloat("_OpaqueAfterClip", 0.0f);
            // animated_trees.xml uses ColorOp=Modulate2x. With the original 0x808080 diffuse,
            // 0.5 * texture * 2 returns the texture while preserving the original stage contract.
            if (mat.HasProperty("_ColorBoost")) mat.SetFloat("_ColorBoost", (kind == NatureKindV1LikeOriginal.Tree && animatedTree) ? 2.0f : 1.0f);
            mat.EnableKeyword("_ALPHABLEND_ON");
            _c2NatureMaterialCacheV2LikeOriginal[key] = mat;
            return mat;
        }

        private Material GetNatureTreeShadowMaterialV12LikeOriginal(Texture2D tex)
        {
            string key = (tex != null ? tex.GetInstanceID().ToString(CultureInfo.InvariantCulture) : "null") + ":TreesAllShadow:V17Billboard";
            if (_c2NatureMaterialCacheV2LikeOriginal.TryGetValue(key, out Material cached) && cached != null)
                return cached;

            Shader shader = Shader.Find("Cossacks2Bridge/NatureTreeShadowV17BillboardLikeOriginal");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
            mat.name = "C2_Nature_TreesAll_ShadowBillboard_V17";
            if (tex != null)
            {
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }
            // TreesShadow_L selects Diffuse RGB and takes alpha from Diffuse * Texture.
            // Use dark diffuse; the previous 0.5 gray looked like a pale broken mask in Unity.
            Color shadowDiffuse = new Color(C2NatureObjectsV17TreeShadowDiffuseRLikeOriginal,
                                            C2NatureObjectsV17TreeShadowDiffuseGLikeOriginal,
                                            C2NatureObjectsV17TreeShadowDiffuseBLikeOriginal,
                                            C2NatureObjectsV17TreeShadowDiffuseALikeOriginal);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", shadowDiffuse);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", shadowDiffuse);
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
            mat.renderQueue = C2NatureObjectsV12TreeShadowRenderQueueLikeOriginal;
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            if (mat.HasProperty("_AlphaCutoff")) mat.SetFloat("_AlphaCutoff", C2NatureObjectsV16TreeShadowAlphaRefLikeOriginal);
            mat.EnableKeyword("_ALPHABLEND_ON");
            _c2NatureMaterialCacheV2LikeOriginal[key] = mat;
            return mat;
        }

        private Texture2D GetNatureFallbackTextureV2LikeOriginal(NatureKindV1LikeOriginal kind)
        {
            string key = "__fallback__" + kind;
            if (_c2NatureTextureCacheV2LikeOriginal.TryGetValue(key, out Texture2D tex) && tex != null)
                return tex;
            tex = new Texture2D(16, 16, TextureFormat.RGBA32, false, true);
            tex.name = "C2_Nature_Fallback_" + kind;
            Color32 c = NatureDebugColorV1LikeOriginal(kind);
            Color32[] px = new Color32[16 * 16];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            tex.SetPixels32(px);
            tex.Apply(false, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            _c2NatureTextureCacheV2LikeOriginal[key] = tex;
            return tex;
        }

        private Texture2D GetNatureFieldPatchTextureV2LikeOriginal(NatureSpriteDescV1LikeOriginal desc)
        {
            int grow = desc != null ? desc.FieldGrowStage : 0;
            int scale = desc != null ? desc.FieldYScale : 256;
            string key = "__field__" + grow.ToString(CultureInfo.InvariantCulture) + "_" + scale.ToString(CultureInfo.InvariantCulture);
            if (_c2NatureTextureCacheV2LikeOriginal.TryGetValue(key, out Texture2D tex) && tex != null)
                return tex;

            tex = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            tex.name = "C2_Nature_FieldPatch_" + grow.ToString(CultureInfo.InvariantCulture) + "_" + scale.ToString(CultureInfo.InvariantCulture);
            Color32[] px = new Color32[64 * 64];
            float mature = Mathf.Clamp01(grow / 255.0f);
            byte r0 = (byte)Mathf.Lerp(70, 166, mature);
            byte g0 = (byte)Mathf.Lerp(110, 140, mature);
            byte b0 = (byte)Mathf.Lerp(38, 52, mature);
            byte a0 = (byte)Mathf.Clamp(scale, 90, 235);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    int stripe = ((x * 3 + y * 5) & 15);
                    int noise = ((x * 17 + y * 31 + grow * 7) & 15) - 7;
                    byte r = (byte)Mathf.Clamp(r0 + noise + (stripe < 4 ? 10 : 0), 0, 255);
                    byte g = (byte)Mathf.Clamp(g0 + noise + (stripe < 4 ? 8 : 0), 0, 255);
                    byte b = (byte)Mathf.Clamp(b0 + noise, 0, 255);
                    px[x + y * 64] = new Color32(r, g, b, a0);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            _c2NatureTextureCacheV2LikeOriginal[key] = tex;
            return tex;
        }

        private void LogNatureObjectSamplesV1LikeOriginal(string sign, List<Tre2MapObjectV28LikeOriginal> objects, NatureSpriteCatalogV1LikeOriginal catalog)
        {
            if (objects == null || objects.Count == 0)
                return;

            int n = Mathf.Min(C2NatureObjectsV1MaxAuditSamplesLikeOriginal, objects.Count);
            List<string> samples = new List<string>(n);
            for (int i = 0; i < n; i++)
            {
                Tre2MapObjectV28LikeOriginal o = objects[i];
                string name = "?";
                int cx = 0, cy = 0, rr = 0, amp = 0, rand = 0;
                if (catalog != null && catalog.ByIndex.TryGetValue(o.SpriteIndex, out NatureSpriteDescV1LikeOriginal d))
                {
                    name = d.Name;
                    cx = d.CenterX;
                    cy = d.CenterY;
                    rr = d.Radius;
                    amp = d.Amplitude;
                    rand = d.NRandom;
                }
                samples.Add("#" + i.ToString(CultureInfo.InvariantCulture) +
                            " id=" + o.SpriteIndex.ToString(CultureInfo.InvariantCulture) +
                            " name=" + name +
                            " xy=(" + o.X.ToString(CultureInfo.InvariantCulture) + "," + o.Y.ToString(CultureInfo.InvariantCulture) + ")" +
                            " c=(" + cx.ToString(CultureInfo.InvariantCulture) + "," + cy.ToString(CultureInfo.InvariantCulture) + ")" +
                            " r=" + rr.ToString(CultureInfo.InvariantCulture) +
                            " amp=" + amp.ToString(CultureInfo.InvariantCulture) +
                            " rand=" + rand.ToString(CultureInfo.InvariantCulture) +
                            " m4=" + o.HasMatrix);
            }

            Debug.Log("[C2:NATURE SAMPLES V1] sign=" + sign + " count=" + objects.Count.ToString(CultureInfo.InvariantCulture) + " " + string.Join(" | ", samples.ToArray()));
        }

        private static string StripNatureCommentV1LikeOriginal(string line)
        {
            if (string.IsNullOrEmpty(line)) return string.Empty;
            int slash = line.IndexOf("//", StringComparison.Ordinal);
            if (slash >= 0) line = line.Substring(0, slash);
            if (line.TrimStart().StartsWith("/", StringComparison.Ordinal)) return string.Empty;
            return line.Trim();
        }

        private static string[] SplitNatureTokensV1LikeOriginal(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return new string[0];
            return line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool TryParseIntV1LikeOriginal(string s, out int v)
        {
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
        }
    }
}
