using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // Cossacks II settlement path: map ZON2/2NOZ -> ActiveGroup/SettlementInfo -> DIP_SimpleBuilding.
    // Ownership, display, civilians, defence and the visible settlement caravan are active here.
    public sealed partial class C2BattleTerrainMode
    {
        private sealed partial class ParsedMap
        {
            public readonly List<C2SettlementMapRecordV336LikeOriginal> SettlementsV336LikeOriginal =
                new List<C2SettlementMapRecordV336LikeOriginal>();
        }

        internal sealed class C2SettlementMapRecordV336LikeOriginal
        {
            public string GroupName = string.Empty;
            public string SecondGroupName = string.Empty;
            public string SourceFile = string.Empty;
            public int CenterX;
            public int CenterY;
            public int StartOwner = 7;
            public int ResourceType = -1; // engine order: wood, gold, stone, food, iron, coal
            public string CopId = string.Empty;
            public int CopBuildTicks;
            public int CopMax;
            public string PeasantId = string.Empty;
            public int PeasantBuildTicks;
            public int PeasantMax;
            public int PeasantProduceTicks;
            public int ResourceMax;
            public string CaravanId = string.Empty;
            public int CaravanCapacity;
        }

        private static void ParseSettlementGroupsV336LikeOriginal(BinaryReader br, ParsedMap map, int payloadLen)
        {
            if (br == null || map == null || payloadLen < 8) return;
            long start = br.BaseStream.Position;
            try
            {
                int groupsLength = br.ReadInt32();
                if (groupsLength <= 0 || groupsLength > payloadLen - 4) return;
                byte[] groupsBytes = br.ReadBytes(groupsLength);
                string source = DecodeMapXmlV336LikeOriginal(groupsBytes);
                if (string.IsNullOrWhiteSpace(source)) return;

                MatchCollection blocks = Regex.Matches(source, @"<ActiveGroup>[\s\S]*?</ActiveGroup>", RegexOptions.IgnoreCase);
                for (int i = 0; i < blocks.Count; i++)
                {
                    XElement group;
                    try { group = XElement.Parse(blocks[i].Value, LoadOptions.None); }
                    catch { continue; }
                    XElement settlement = group.Descendants().FirstOrDefault(e => e.Name.LocalName == "SettlementInfo");
                    if (settlement == null) continue;

                    XElement firstUnit = group.Descendants().FirstOrDefault(e => e.Parent != null && e.Parent.Name.LocalName == "Units");
                    if (firstUnit == null) continue;
                    string[] coordinates = (firstUnit.Value ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    int x, y;
                    if (coordinates.Length < 2 ||
                        !int.TryParse(coordinates[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out x) ||
                        !int.TryParse(coordinates[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out y)) continue;

                    var record = new C2SettlementMapRecordV336LikeOriginal();
                    record.GroupName = ElementValueV336LikeOriginal(group, "Name");
                    record.SecondGroupName = ElementValueV336LikeOriginal(settlement, "SecondGroupName");
                    record.SourceFile = ElementValueV336LikeOriginal(settlement, "SourceFile");
                    record.CenterX = x;
                    record.CenterY = y;
                    record.StartOwner = ReadIntElementV336LikeOriginal(settlement, "StartOwner", 7);
                    record.CopId = ElementValueV336LikeOriginal(settlement, "CopID");
                    record.CopBuildTicks = ReadIntElementV336LikeOriginal(settlement, "CopBuidTime", 200);
                    record.CopMax = ReadIntElementV336LikeOriginal(settlement, "CopMaxAmount", 0);
                    record.PeasantId = ElementValueV336LikeOriginal(settlement, "PeonID");
                    record.PeasantBuildTicks = ReadIntElementV336LikeOriginal(settlement, "PeonBuidTime", 500);
                    record.CaravanId = ElementValueV336LikeOriginal(settlement, "ObozID");

                    XElement peon = settlement.Elements().FirstOrDefault(e => e.Name.LocalName == "PeonMaxAmount");
                    string[] resourceNames = { "Wood", "Food", "Stone", "Gold", "Iron", "Coal" };
                    int[] engineResourceIds = { 0, 3, 2, 1, 4, 5 };
                    for (int r = 0; r < resourceNames.Length; r++)
                    {
                        int amount = peon != null ? ReadIntElementV336LikeOriginal(peon, resourceNames[r], 0) : 0;
                        if (amount > 0)
                        {
                            record.ResourceType = engineResourceIds[r];
                            record.PeasantMax = amount;
                            XElement produce = settlement.Elements().FirstOrDefault(e => e.Name.LocalName == "PeonTimeOnProduce");
                            record.PeasantProduceTicks = produce != null
                                ? ReadIntElementV336LikeOriginal(produce, resourceNames[r], 0) : 0;
                            XElement maxResource = settlement.Elements().FirstOrDefault(e => e.Name.LocalName == "MaxResAmount");
                            record.ResourceMax = maxResource != null
                                ? ReadIntElementV336LikeOriginal(maxResource, resourceNames[r], 0) : 0;
                            XElement caravan = settlement.Elements().FirstOrDefault(e => e.Name.LocalName == "ObozResAmount");
                            record.CaravanCapacity = caravan != null
                                ? ReadIntElementV336LikeOriginal(caravan, resourceNames[r], 0) : 0;
                            break;
                        }
                    }
                    if (record.ResourceType < 0)
                        record.ResourceType = ResourceFromSourceV336LikeOriginal(record.SourceFile);
                    map.SettlementsV336LikeOriginal.Add(record);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[C2:SETTLEMENT V336] ZON2 parse: " + e.GetType().Name + ":" + e.Message);
            }
            finally
            {
                br.BaseStream.Position = start + payloadLen;
            }
        }

        private static string DecodeMapXmlV336LikeOriginal(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;
            int n = bytes.Length;
            while (n > 0 && bytes[n - 1] == 0) n--;
            try { return Encoding.GetEncoding(1251).GetString(bytes, 0, n); }
            catch { return Encoding.UTF8.GetString(bytes, 0, n); }
        }

        private static string ElementValueV336LikeOriginal(XElement parent, string name)
        {
            if (parent == null) return string.Empty;
            XElement e = parent.Elements().FirstOrDefault(v => v.Name.LocalName == name);
            return e != null ? (e.Value ?? string.Empty).Trim() : string.Empty;
        }

        private static int ReadIntElementV336LikeOriginal(XElement parent, string name, int fallback)
        {
            int value;
            return int.TryParse(ElementValueV336LikeOriginal(parent, name), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value) ? value : fallback;
        }

        private static int ResourceFromSourceV336LikeOriginal(string source)
        {
            string s = (source ?? string.Empty).ToLowerInvariant();
            if (s.Contains("food")) return 3;
            if (s.Contains("gold")) return 1;
            if (s.Contains("iron")) return 4;
            if (s.Contains("coal")) return 5;
            return 0;
        }

        private void InstallSettlementRuntimeV336LikeOriginal()
        {
            if (_map == null || _map.SettlementsV336LikeOriginal.Count == 0) return;
            GameObject root = new GameObject("C2_DIP_Settlements_V336");
            root.transform.SetParent(transform, false);
            C2SettlementDipManagerV336LikeOriginal manager = root.AddComponent<C2SettlementDipManagerV336LikeOriginal>();
            manager.ConfigureLikeOriginal(this, _map.SettlementsV336LikeOriginal);
            Debug.Log("[C2:SETTLEMENT V336] installed exact ZON2 settlements=" +
                      _map.SettlementsV336LikeOriginal.Count.ToString(CultureInfo.InvariantCulture));
        }

        internal Vector3 SettlementMapPointToWorldV336LikeOriginal(float x, float y)
        {
            return WallOriginalXYToWorldV1LikeOriginal(x, y, 0.0f);
        }

        internal Vector3 SettlementMapPointToWorldV349LikeOriginal(float x, float y, float extraZ)
        {
            return WallOriginalXYToWorldV1LikeOriginal(x, y, extraZ);
        }

        internal Vector3 SettlementSkewXYVectorToWorldV350LikeOriginal(float x, float y)
        {
            // akField.cpp bends the already SkewPt-transformed top vertices in
            // their X/Y plane.  This is a vector conversion, so it must not
            // sample terrain height or inherit the backing mesh's odd-column
            // offset (both would add a false jump to the wind displacement).
            OriginalTerrainKernelConfig kernel = _hasLastBuiltTerrainKernel
                ? _lastBuiltTerrainKernel
                : CreateOriginalTerrainKernelConfigLikeOriginal(_map);
            return new Vector3(
                x * kernel.BackingStepXWorld / 32.0f,
                0.0f,
                y * kernel.BackingStepZWorld * WorldZSign / 32.0f);
        }
    }

    /// <summary>
    /// Runtime counterpart of UnitsMD/Field.md -> EXSPRITES FG0 and
    /// walls.rsr #PSHENICA -> Models/field.c2m/FieldPatch.  It deliberately
    /// uses a shared 3D mesh/material: 169 field cells must not decode 169
    /// textures or allocate a material per cell.
    /// </summary>
    public sealed class C2SettlementFieldPatchV342LikeOriginal : MonoBehaviour
    {
        // complex.rsr is advanced by TimeReq::Handle.  The active CII 1.1
        // simulation starts with FrmDec=2 and a normal 80 ms game step.
        private const float OriginalSimulationStepSecondsV349 = 0.080f;
        private const int OriginalFrmDecV349 = 2;
        private const int Fg24StageV349 = 92;
        private const int Fw0StageV349 = 93;
        private const int Fw5StageV349 = 98;
        private const int Fw6StageV349 = 99;
        private const int Fw11StageV349 = 104;
        private static readonly byte[] LateGrowthZ0V349 = { 180, 190, 200, 210, 220, 230, 240, 255 };
        private static readonly byte[] CutGrowthZ0V349 = { 255, 255, 255, 255, 255, 220, 180, 160, 110, 90, 90, 90 };
        private static Material s_fieldMaterialV342;
        private static Texture2D s_fieldTextureV342;
        private C2BattleTerrainMode _mode;
        private C2SettlementDipVillageV336LikeOriginal _village;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _reservedBy;
        private float _reservationExpiresV342;
        private int _stageV349;
        private int _timePassedV349;
        private float _simulationAccumulatorV349;

        public Vector2 OriginalRealPositionV342LikeOriginal { get; private set; }

        internal float GrowRatioV349LikeOriginal
        {
            get
            {
                // complex.lst passes ObjCharacter::Z0/255 to DrawFPatch.
                if (_stageV349 <= 84)
                {
                    // FG0..FG16D.  Preserve the literal FG10D typo/value (100)
                    // from CII 1.1 instead of smoothing the source data.
                    if (_stageV349 == 54) return 100.0f / 255.0f;
                    return (2.0f + _stageV349 * 2.0f) / 255.0f;
                }
                if (_stageV349 <= Fg24StageV349)
                    return LateGrowthZ0V349[_stageV349 - 85] / 255.0f;
                return CutGrowthZ0V349[Mathf.Clamp(
                    _stageV349 - Fw0StageV349, 0, CutGrowthZ0V349.Length - 1)] / 255.0f;
            }
        }

        internal bool HasFoodSourceV349LikeOriginal
        {
            get { return _stageV349 == Fg24StageV349 || (_stageV349 >= Fw0StageV349 && _stageV349 <= Fw5StageV349); }
        }

        internal void ConfigureV342LikeOriginal(C2BattleTerrainMode mode,
            C2SettlementDipVillageV336LikeOriginal village, float originalX, float originalY,
            int ix, int iy)
        {
            _mode = mode;
            _village = village;
            OriginalRealPositionV342LikeOriginal = new Vector2(originalX * 16.0f, originalY * 16.0f);
            transform.position = mode.SettlementMapPointToWorldV336LikeOriginal(originalX, originalY) + Vector3.up * 0.015f;
            EnsureSharedFieldAssetsV342LikeOriginal();

            Vector3 x1 = mode.SettlementMapPointToWorldV336LikeOriginal(originalX + 44.0f, originalY);
            Vector3 y1 = mode.SettlementMapPointToWorldV336LikeOriginal(originalX, originalY + 44.0f);
            float sx = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(x1.x, x1.z));
            float sz = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(y1.x, y1.z));
            transform.localScale = new Vector3(Mathf.Max(0.25f, sx * 1.42f), 1.0f, Mathf.Max(0.25f, sz * 1.42f));
            transform.rotation = Quaternion.Euler(0.0f, 45.0f, 0.0f);
            _stageV349 = 0; // UnitsMD/Field.md creates FG0, not a ripe field.
            _timePassedV349 = 0;
            _simulationAccumulatorV349 = 0.0f;
            if (_village != null) _village.MarkFieldBatchDirtyV349LikeOriginal();
        }

        public bool TryReserveV342LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal peasant)
        {
            if (!HasFoodSourceV349LikeOriginal || peasant == null) return false;
            if (_reservedBy != null && !_reservedBy.IsDeadLikeOriginal &&
                Time.realtimeSinceStartup < _reservationExpiresV342 && _reservedBy != peasant)
                return false;
            _reservedBy = peasant;
            _reservationExpiresV342 = Time.realtimeSinceStartup + 75.0f;
            return true;
        }

        public void ReleaseReservationV342LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal peasant)
        {
            if (_reservedBy == peasant || peasant == null) _reservedBy = null;
        }

        public bool CanContinueWorkV349LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal peasant)
        {
            return HasFoodSourceV349LikeOriginal && (_reservedBy == null || _reservedBy == peasant);
        }

        public bool PerformWorkV349LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal peasant, out int gathered, out bool exhausted)
        {
            gathered = 0;
            exhausted = false;
            if (!CanContinueWorkV349LikeOriginal(peasant)) return false;

            // OneSprite::PerformWork increments WorkOver once.  For fields the
            // caller passes int(FoodEff/100)*10; with WorkAmount=5 this advances
            // exactly one WORKTRANSFORM and returns the old stage ResPerWork=10.
            gathered = 10;
            if (_stageV349 == Fg24StageV349) _stageV349 = Fw0StageV349;
            else _stageV349++;
            _timePassedV349 = 0;
            exhausted = _stageV349 >= Fw6StageV349;
            if (exhausted) _reservedBy = null;
            if (_village != null) _village.MarkFieldBatchDirtyV349LikeOriginal();
            return true;
        }

        internal bool TickTimeV349LikeOriginal(float deltaSeconds)
        {
            if (_reservedBy != null && (_reservedBy.IsDeadLikeOriginal || Time.realtimeSinceStartup >= _reservationExpiresV342))
                _reservedBy = null;

            bool changed = false;
            _simulationAccumulatorV349 += Mathf.Max(0.0f, deltaSeconds);
            while (_simulationAccumulatorV349 >= OriginalSimulationStepSecondsV349)
            {
                _simulationAccumulatorV349 -= OriginalSimulationStepSecondsV349;
                int timeAmount = TimeAmountForStageV349LikeOriginal();
                if (timeAmount <= 0) continue;
                _timePassedV349 += OriginalFrmDecV349;
                if (_timePassedV349 < timeAmount) continue;
                AdvanceTimedStageV349LikeOriginal();
                _timePassedV349 = 0;
                changed = true;
            }
            return changed;
        }

        private int TimeAmountForStageV349LikeOriginal()
        {
            if (_stageV349 >= 0 && _stageV349 < Fg24StageV349) return 10;
            if (_stageV349 == Fg24StageV349) return 20;
            if (_stageV349 >= Fw6StageV349 && _stageV349 < Fw11StageV349) return 10;
            if (_stageV349 == Fw11StageV349) return 30;
            return 0; // FW0..FW5 only advance through WORKTRANSFORM.
        }

        private void AdvanceTimedStageV349LikeOriginal()
        {
            if (_stageV349 < Fg24StageV349) _stageV349++;
            else if (_stageV349 == Fg24StageV349) _stageV349 = Fw0StageV349;
            else if (_stageV349 < Fw11StageV349) _stageV349++;
            else _stageV349 = 0;
        }

        internal static Material SharedFieldMaterialV349LikeOriginal
        {
            get
            {
                EnsureSharedFieldAssetsV342LikeOriginal();
                return s_fieldMaterialV342;
            }
        }

        private static void EnsureSharedFieldAssetsV342LikeOriginal()
        {
            if (s_fieldMaterialV342 != null) return;

            var fs = new Cossacks2Bridge.Core.CoreFileSystem(@"C:\GSC Game World\Cossacks II\Data");
            string resolved;
            s_fieldTextureV342 = C2OriginalTextureService.TryLoadTexture(fs, @"textures\pole1.tga",
                "C2_FIELD_pole1_V342", C2OriginalTexturePolicy.UnfilteredPictureLikeOriginal, out resolved);
            // Shaders/DeviceStates/field.xml: alpha blend + alpha test 0x10,
            // ZEnable/ZWrite enabled, no culling, linear filtering and wrapped UVs.
            Shader shader = Shader.Find("Cossacks2Bridge/C2FieldLikeOriginal");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Transparent Cutout");
            s_fieldMaterialV342 = new Material(shader) { name = "C2_FIELD_FieldPatch_pole1_V342" };
            if (s_fieldTextureV342 != null)
            {
                s_fieldTextureV342.filterMode = FilterMode.Bilinear;
                s_fieldTextureV342.wrapMode = TextureWrapMode.Repeat;
            }
            if (s_fieldMaterialV342.HasProperty("_BaseMap")) s_fieldMaterialV342.SetTexture("_BaseMap", s_fieldTextureV342);
            if (s_fieldMaterialV342.HasProperty("_MainTex")) s_fieldMaterialV342.SetTexture("_MainTex", s_fieldTextureV342);
            if (s_fieldMaterialV342.HasProperty("_Surface")) s_fieldMaterialV342.SetFloat("_Surface", 1.0f);
            if (s_fieldMaterialV342.HasProperty("_AlphaClip")) s_fieldMaterialV342.SetFloat("_AlphaClip", 1.0f);
            if (s_fieldMaterialV342.HasProperty("_Cutoff")) s_fieldMaterialV342.SetFloat("_Cutoff", 16.0f / 255.0f);
            if (s_fieldMaterialV342.HasProperty("_ZWrite")) s_fieldMaterialV342.SetFloat("_ZWrite", 1.0f);
            if (s_fieldMaterialV342.HasProperty("_ZTest")) s_fieldMaterialV342.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            if (s_fieldMaterialV342.HasProperty("_Cull")) s_fieldMaterialV342.SetFloat("_Cull", (float)CullMode.Off);
            if (s_fieldMaterialV342.HasProperty("_SrcBlend")) s_fieldMaterialV342.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (s_fieldMaterialV342.HasProperty("_DstBlend")) s_fieldMaterialV342.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            s_fieldMaterialV342.SetOverrideTag("RenderType", "TransparentCutout");
            s_fieldMaterialV342.EnableKeyword("_ALPHATEST_ON");
            // DrawSpriteTrees -> DrawFPatch -> g_FieldModel.Draw is one
            // depth-writing field batch before DrawUnits.
            s_fieldMaterialV342.renderQueue = 3000;
        }
    }

    public sealed class C2SettlementDipManagerV336LikeOriginal : MonoBehaviour
    {
        private C2BattleTerrainMode _mode;
        private readonly List<C2SettlementDipVillageV336LikeOriginal> _villages =
            new List<C2SettlementDipVillageV336LikeOriginal>();

        internal void ConfigureLikeOriginal(C2BattleTerrainMode mode,
            IList<C2BattleTerrainMode.C2SettlementMapRecordV336LikeOriginal> records)
        {
            _mode = mode;
            for (int i = 0; records != null && i < records.Count; i++)
            {
                GameObject go = new GameObject("C2_Settlement_" + i.ToString(CultureInfo.InvariantCulture) + "_" + records[i].GroupName);
                go.transform.SetParent(transform, false);
                C2SettlementDipVillageV336LikeOriginal village = go.AddComponent<C2SettlementDipVillageV336LikeOriginal>();
                village.ConfigureLikeOriginal(_mode, records[i], i);
                _villages.Add(village);
            }
        }
    }

    public sealed class C2SettlementDipVillageV336LikeOriginal : MonoBehaviour
    {
        private const float BigRadius = 900.0f;
        private const float VeryBigRadius = 1300.0f;
        // ActiveScenary.cpp stores animation/order progress in 8-bit stages;
        // DIP_SimpleBuilding queues initial villagers with ProduceUnitTicks(...,20).
        // At the original 25 Hz simulation step this is 0.8 seconds per order.
        private const float InitialPopulationOrderSecondsLikeOriginal = 20.0f / 25.0f;
        private C2BattleTerrainMode _mode;
        private C2BattleTerrainMode.C2SettlementMapRecordV336LikeOriginal _data;
        private int _index;
        private int _owner;
        private Camera _camera;
        private sealed class SettlementGpFrameV344LikeOriginal
        {
            internal Texture2D Texture;
            internal int OriginX;
            internal int OriginY;
        }
        private SettlementGpFrameV344LikeOriginal[,] _animatedSettlementFramesV344LikeOriginal;
        private SettlementGpFrameV344LikeOriginal[] _resourceFramesV344LikeOriginal;
        private int _animatedSettlementFrameCountV344LikeOriginal;
        private C2UnitOriginalRuntimeAndRendererV1 _fogRuntimeV344LikeOriginal;
        private Rect _lastIconRect;
        private bool _hover;
        private float _nextScan;
        private bool _populationSpawned;
        private bool _populationInitialized;
        private int _defendersToProduce;
        private int _peasantsToProduce;
        private int _caravansToProduce;
        private C2OriginalProduceItemV13 _defenderItem;
        private C2OriginalProduceItemV13 _peasantItem;
        private C2OriginalProduceItemV13 _caravanItem;
        private readonly HashSet<int> _militiaFormationGroups = new HashSet<int>();
        private readonly Dictionary<int, float> _nextDefenderPatrolAt = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _nextMilitiaPolkPatrolAt = new Dictionary<int, float>();
        private float _nextMilitiaFormationAttempt;
        private int _currentProduceFailures;
        private float _storedResource;
        private float _lastProduceRealtime;
        private bool _caravanInTransit;
        private int _caravanOwner = -1;
        private int _caravanShipmentAmount;
        private float _iconCanvasWidth = 42.0f;
        private float _iconCanvasHeight = 42.0f;
        private static readonly Dictionary<long, SettlementGpFrameV344LikeOriginal> SharedAnimatedSettlementFramesV344LikeOriginal =
            new Dictionary<long, SettlementGpFrameV344LikeOriginal>();
        private static readonly Dictionary<int, SettlementGpFrameV344LikeOriginal> SharedSettlementResourceFramesV344LikeOriginal =
            new Dictionary<int, SettlementGpFrameV344LikeOriginal>();
        private static global::TemnyLessViewer.C2GpSystem SharedSettlementIconGpsV343LikeOriginal;
        private static int SharedAnimatedSettlementGpIdV344LikeOriginal = -1;
        private static int SharedSettlementResourceGpIdV344LikeOriginal = -1;
        private static C2NeutralPeasantUnitInfoV2LikeOriginal[] _cachedMapUnits =
            new C2NeutralPeasantUnitInfoV2LikeOriginal[0];
        private static float _nextMapUnitsSnapshotAt;
        // ProduceUnitTicks(...,20) belongs to each producing building/order.
        // Cossacks II has no global cross-village spawn throttle here.
        private float _nextPopulationProduceAt;
        private readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> _peasants = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
        private readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> _defenders = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
        private readonly List<C2NeutralPeasantUnitInfoV2LikeOriginal> _caravans = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
        private readonly List<C2SettlementBuildingSelectableV1LikeOriginal> _buildings = new List<C2SettlementBuildingSelectableV1LikeOriginal>();
        private LineRenderer _bigLine;
        private LineRenderer _veryBigLine;
        private MeshRenderer _bigFill;
        private readonly Dictionary<int, float> _nextPeasantWorkAt = new Dictionary<int, float>();
        private readonly List<C2SettlementFieldPatchV342LikeOriginal> _fieldPatchesV342 =
            new List<C2SettlementFieldPatchV342LikeOriginal>(169);
        private int _nextFieldPatchCursorV342;
        private Mesh _fieldBatchMeshV349;
        private MeshRenderer _fieldBatchRendererV349;
        private bool _fieldBatchDirtyV349;
        private readonly List<Vector3> _fieldBatchVerticesV349 = new List<Vector3>(169 * 12 * 4);
        private readonly List<Vector2> _fieldBatchUvV349 = new List<Vector2>(169 * 12 * 4);
        private readonly List<Vector4> _fieldBatchWindV350 = new List<Vector4>(169 * 12 * 4);
        private readonly List<Vector2> _fieldBatchWindRandomV350 = new List<Vector2>(169 * 12 * 4);
        private readonly List<Color32> _fieldBatchColorsV349 = new List<Color32>(169 * 12 * 4);
        private readonly List<int> _fieldBatchTrianglesV349 = new List<int>(169 * 12 * 6);
        private static readonly float[] OriginalFieldRandomTableV349LikeOriginal =
            BuildOriginalFieldRandomTableV349LikeOriginal();
        private static int s_fieldWindShaderFrameV350 = -1;
        private static C2SettlementDipVillageV336LikeOriginal _selectedSettlement;
        private readonly List<Sprite> _upgradeIcons = new List<Sprite>();

        internal bool C2TryGetNationCitySettlementStateV384ALikeOriginal(
            out int owner, out int resourceType, out int maxWorkers, out int produceTicks)
        {
            owner = _owner;
            resourceType = _data != null ? _data.ResourceType : -1;
            maxWorkers = _data != null ? _data.PeasantMax : 0;
            produceTicks = _data != null ? _data.PeasantProduceTicks : 0;
            return _data != null;
        }

        internal void ConfigureLikeOriginal(C2BattleTerrainMode mode,
            C2BattleTerrainMode.C2SettlementMapRecordV336LikeOriginal data, int index)
        {
            _mode = mode;
            _data = data;
            _index = index;
            _owner = Mathf.Clamp(data.StartOwner, 0, 7);
            C2OriginalGameAiDataV340LikeOriginal.EnsureLoadedLikeOriginal();
            _camera = mode != null ? mode.GetActiveBattleCameraLikeOriginal() : Camera.main;
            transform.position = mode != null ? mode.SettlementMapPointToWorldV336LikeOriginal(data.CenterX, data.CenterY) : Vector3.zero;
            LoadIconsLikeOriginal();
            LoadUpgradeIconsLikeOriginal();
            Color32 ownerColor = C2PlayerColorsLikeOriginal.GetNatColorByPlayer(_owner);
            Debug.Log("[C2:SETTLEMENT ICON OWNER V345] village=" + index.ToString(CultureInfo.InvariantCulture) +
                      " group='" + (data.GroupName ?? string.Empty) + "' startOwner=" + _owner.ToString(CultureInfo.InvariantCulture) +
                      " colorId=" + C2PlayerColorsLikeOriginal.GetPlayerColorId(_owner).ToString(CultureInfo.InvariantCulture) +
                      " rgb=" + ownerColor.r.ToString(CultureInfo.InvariantCulture) + "," +
                      ownerColor.g.ToString(CultureInfo.InvariantCulture) + "," +
                      ownerColor.b.ToString(CultureInfo.InvariantCulture));
            SetZoneVisibleLikeOriginal(false);
            Invoke(nameof(InitializePopulationLikeOriginal), 0.35f);
            _nextPopulationProduceAt = Time.realtimeSinceStartup + 0.35f + InitialPopulationOrderSecondsLikeOriginal;
            _nextScan = Time.realtimeSinceStartup + 0.75f + index * 0.13f;
        }

        private void LoadIconsLikeOriginal()
        {
            string error;
            if (SharedSettlementIconGpsV343LikeOriginal == null)
            {
                SharedSettlementIconGpsV343LikeOriginal = new global::TemnyLessViewer.C2GpSystem
                {
                    MaxCachedFrames = 256,
                    MaxCachedBytes = 64L * 1024L * 1024L
                };
            }

            const string originalDataRoot = @"C:\GSC Game World\Cossacks II\Data";
            if (SharedAnimatedSettlementGpIdV344LikeOriginal < 0)
                SharedAnimatedSettlementGpIdV344LikeOriginal = SharedSettlementIconGpsV343LikeOriginal.PreLoadGPImage(
                    "Interf3\\vilage_icon", originalDataRoot, out error);
            if (SharedSettlementResourceGpIdV344LikeOriginal < 0)
                SharedSettlementResourceGpIdV344LikeOriginal = SharedSettlementIconGpsV343LikeOriginal.PreLoadGPImage(
                    "Interf3\\respanel", originalDataRoot, out error);

            // cva_VI_Setl::SetFrameState: ((GetTickCount()-t)/80)%GPNFrames.
            _animatedSettlementFrameCountV344LikeOriginal = SharedAnimatedSettlementGpIdV344LikeOriginal > 0
                ? SharedSettlementIconGpsV343LikeOriginal.GetFrameCount(SharedAnimatedSettlementGpIdV344LikeOriginal)
                : 0;
            if (_animatedSettlementFrameCountV344LikeOriginal <= 0)
                _animatedSettlementFrameCountV344LikeOriginal = 1;
            _animatedSettlementFramesV344LikeOriginal =
                new SettlementGpFrameV344LikeOriginal[8, _animatedSettlementFrameCountV344LikeOriginal];
            for (int nation = 0; nation < 8; nation++)
            {
                for (int frame = 0; frame < _animatedSettlementFrameCountV344LikeOriginal; frame++)
                {
                    SettlementGpFrameV344LikeOriginal rendered =
                        GetSharedAnimatedSettlementFrameV344LikeOriginal(nation, frame, out error);
                    _animatedSettlementFramesV344LikeOriginal[nation, frame] = rendered;
                    if (rendered != null && rendered.Texture != null)
                    {
                        _iconCanvasWidth = Mathf.Max(_iconCanvasWidth, rendered.Texture.width);
                        _iconCanvasHeight = Mathf.Max(_iconCanvasHeight, rendered.Texture.height);
                    }
                }
            }

            // EngineSettings.xml Resource.Sprite_0..5 = 2..7.
            _resourceFramesV344LikeOriginal = new SettlementGpFrameV344LikeOriginal[6];
            for (int resource = 0; resource < 6; resource++)
            {
                SettlementGpFrameV344LikeOriginal rendered =
                    GetSharedSettlementResourceFrameV344LikeOriginal(resource + 2, out error);
                _resourceFramesV344LikeOriginal[resource] = rendered;
            }
        }

        private static SettlementGpFrameV344LikeOriginal GetSharedAnimatedSettlementFrameV344LikeOriginal(
            int nation, int sprite, out string error)
        {
            error = string.Empty;
            long key = ((long)(nation & 255) << 32) | (uint)sprite;
            SettlementGpFrameV344LikeOriginal cached;
            if (SharedAnimatedSettlementFramesV344LikeOriginal.TryGetValue(key, out cached) && cached != null)
                return cached;
            global::TemnyLessViewer.C2RenderedFrame frame;
            Color32 nationColor = C2PlayerColorsLikeOriginal.GetNatColorByPlayer(nation);
            if (SharedAnimatedSettlementGpIdV344LikeOriginal < 0 ||
                !SharedSettlementIconGpsV343LikeOriginal.GetRenderedFrameNationColor(
                    SharedAnimatedSettlementGpIdV344LikeOriginal, sprite,
                    nationColor.r, nationColor.g, nationColor.b, out frame, out error) ||
                frame == null)
                return null;
            SettlementGpFrameV344LikeOriginal result = MakeSettlementGpFrameV344LikeOriginal(
                frame, "settlement_vilage_icon_n" + nation.ToString(CultureInfo.InvariantCulture) +
                       "_f" + sprite.ToString(CultureInfo.InvariantCulture));
            if (result != null) SharedAnimatedSettlementFramesV344LikeOriginal[key] = result;
            return result;
        }

        private static SettlementGpFrameV344LikeOriginal GetSharedSettlementResourceFrameV344LikeOriginal(
            int sprite, out string error)
        {
            error = string.Empty;
            SettlementGpFrameV344LikeOriginal cached;
            if (SharedSettlementResourceFramesV344LikeOriginal.TryGetValue(sprite, out cached) && cached != null)
                return cached;
            global::TemnyLessViewer.C2RenderedFrame frame;
            if (SharedSettlementResourceGpIdV344LikeOriginal < 0 ||
                !SharedSettlementIconGpsV343LikeOriginal.GetRenderedFrame(
                    SharedSettlementResourceGpIdV344LikeOriginal, sprite, out frame, out error) || frame == null)
                return null;
            SettlementGpFrameV344LikeOriginal result = MakeSettlementGpFrameV344LikeOriginal(
                frame, "settlement_resource_" + sprite.ToString(CultureInfo.InvariantCulture));
            if (result != null) SharedSettlementResourceFramesV344LikeOriginal[sprite] = result;
            return result;
        }

        private static SettlementGpFrameV344LikeOriginal MakeSettlementGpFrameV344LikeOriginal(
            global::TemnyLessViewer.C2RenderedFrame frame, string name)
        {
            Texture2D texture = MakeTextureV336LikeOriginal(frame, name);
            return texture == null ? null : new SettlementGpFrameV344LikeOriginal
            {
                Texture = texture,
                OriginX = frame.OriginX,
                OriginY = frame.OriginY
            };
        }

        private static Texture2D MakeTextureV336LikeOriginal(global::TemnyLessViewer.C2RenderedFrame frame, string name)
        {
            if (frame == null || frame.Width <= 0 || frame.Height <= 0 || frame.Rgba == null) return null;
            byte[] rgba = (byte[])frame.Rgba.Clone();
            int row = frame.Width * 4;
            byte[] tmp = new byte[row];
            for (int y = 0; y < frame.Height / 2; y++)
            {
                int a = y * row, b = (frame.Height - 1 - y) * row;
                Buffer.BlockCopy(rgba, a, tmp, 0, row);
                Buffer.BlockCopy(rgba, b, rgba, a, row);
                Buffer.BlockCopy(tmp, 0, rgba, b, row);
            }
            Texture2D tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false, false);
            tex.name = name;
            tex.LoadRawTextureData(rgba);
            tex.Apply(false, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        private void LoadUpgradeIconsLikeOriginal()
        {
            _upgradeIcons.Clear();
            string relative = (_data.SourceFile ?? string.Empty).Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            string path = Path.Combine(@"C:\GSC Game World\Cossacks II\Data", relative);
            if (!File.Exists(path)) return;
            try
            {
                XDocument doc = XDocument.Load(path, LoadOptions.None);
                foreach (XElement upgrade in doc.Descendants().Where(e => e.Name.LocalName == "SetlUpgrade"))
                {
                    string file = ElementValueLocalLikeOriginal(upgrade, "IconFile");
                    int icon;
                    if (!int.TryParse(ElementValueLocalLikeOriginal(upgrade, "Icon"), out icon)) icon = 0;
                    _upgradeIcons.Add(C2GameplayOriginalSpriteCacheV1.LoadSprite(
                        string.IsNullOrWhiteSpace(file) ? "Interf3\\Upg" : file,
                        icon, "settlement_upgrade_v341_" + _upgradeIcons.Count));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[C2:SETTLEMENT UPGRADE V341] " + e.Message);
            }
        }

        private static string ElementValueLocalLikeOriginal(XElement parent, string name)
        {
            XElement element = parent != null
                ? parent.Elements().FirstOrDefault(e => e.Name.LocalName == name) : null;
            return element != null ? (element.Value ?? string.Empty).Trim() : string.Empty;
        }

        private void InitializePopulationLikeOriginal()
        {
            if (_populationInitialized) return;
            FindVillageBuildingsLikeOriginal();
            EnsureFoodFieldV342LikeOriginal();
            _populationInitialized = true;
            _defendersToProduce = ResolvePopulationItemLikeOriginal(
                _data.CopId, Mathf.Clamp(_data.CopMax, 0, 50), out _defenderItem);
            // SettlementInfo::Load queues the full PeonMaxAmount through the
            // producer (DIP_SimpleBuilding.cpp, ProduceUnitTicks(...,20)).
            _peasantsToProduce = ResolvePopulationItemLikeOriginal(
                _data.PeasantId, Mathf.Clamp(_data.PeasantMax, 0, 50), out _peasantItem);
            // Original DipCaravan::Run explicitly rejects Owner==7.  ObozID is
            // only a type definition here; it is not an initial population item.
            ResolvePopulationItemLikeOriginal(_data.CaravanId, 1, out _caravanItem);
            _caravansToProduce = 0;
            // DIP_SimpleBuilding::Load: Resource[i] starts with one full caravan load,
            // while SetUp initializes LastProduce to the current simulation time.
            _storedResource = Mathf.Max(0, _data.CaravanCapacity);
            _lastProduceRealtime = Time.realtimeSinceStartup;
            _caravanShipmentAmount = 0;
            _populationSpawned = _defendersToProduce + _peasantsToProduce + _caravansToProduce == 0;
            Debug.Log("[C2:SETTLEMENT POPULATION QUEUE V338] index=" + _index.ToString(CultureInfo.InvariantCulture) +
                      " cop=" + _defendersToProduce.ToString(CultureInfo.InvariantCulture) +
                      " peon=" + _peasantsToProduce.ToString(CultureInfo.InvariantCulture) +
                      " caravan=" + _caravansToProduce.ToString(CultureInfo.InvariantCulture) +
                      " producerBuildings=" + _buildings.Count.ToString(CultureInfo.InvariantCulture));
        }

        private int ResolvePopulationItemLikeOriginal(
            string unitId, int requested, out C2OriginalProduceItemV13 item)
        {
            item = null;
            if (requested <= 0 || string.IsNullOrWhiteSpace(unitId)) return 0;
            if (C2OriginalProduceCatalogV13.TryBuildEditorItemForUnitIdV333LikeOriginal(
                    unitId, out item) && item != null)
                return requested;
            Debug.LogWarning("[C2:SETTLEMENT SPAWN V338] unresolved unitId='" + unitId +
                             "' village=" + _index.ToString(CultureInfo.InvariantCulture));
            return 0;
        }

        private void TickPopulationProductionLikeOriginal()
        {
            float now = Time.realtimeSinceStartup;
            if (!_populationInitialized || _populationSpawned || now < _nextPopulationProduceAt)
                return;
            if (_buildings.Count == 0) FindVillageBuildingsLikeOriginal();
            if (_buildings.Count == 0) return;

            bool produced = false;
            if (_defendersToProduce > 0)
            {
                produced = ProduceOneFromVillageBuildingLikeOriginal(
                    _defenderItem, _defenders, _data.CopBuildTicks, _defenders.Count);
                if (produced) { _defendersToProduce--; _currentProduceFailures = 0; }
            }
            else if (_peasantsToProduce > 0)
            {
                produced = ProduceOneFromVillageBuildingLikeOriginal(
                    _peasantItem, _peasants, _data.PeasantBuildTicks, _peasants.Count);
                if (produced) { _peasantsToProduce--; _currentProduceFailures = 0; }
            }

            if (!produced) _currentProduceFailures++;
            // A temporarily unavailable/blocked producer must never erase the
            // population queue. Original ProduceUnitTicks keeps the request
            // pending until its producer can release the unit.

            _nextPopulationProduceAt = Time.realtimeSinceStartup +
                (produced ? InitialPopulationOrderSecondsLikeOriginal : 1.0f / 25.0f);
            if (_defendersToProduce + _peasantsToProduce + _caravansToProduce == 0)
            {
                _populationSpawned = true;
                Debug.Log("[C2:SETTLEMENT POPULATION READY V338] index=" + _index.ToString(CultureInfo.InvariantCulture) +
                          " cop=" + _defenders.Count.ToString(CultureInfo.InvariantCulture) +
                          " peon=" + _peasants.Count.ToString(CultureInfo.InvariantCulture) +
                          " caravan=" + _caravans.Count.ToString(CultureInfo.InvariantCulture));
            }
        }

        private bool ProduceOneFromVillageBuildingLikeOriginal(
            C2OriginalProduceItemV13 item,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> target,
            int configuredBuilding,
            int sequence)
        {
            if (item == null || _buildings.Count == 0) return false;
            // ProduceGroup is rooted at the central producer; rotating through
            // every house scattered births and made unrelated doors contend.
            string audit = "no_producer_with_exact_BORNPOINTS";
            for (int producerIndex = 0; producerIndex < _buildings.Count; producerIndex++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal producer = _buildings[producerIndex];
                if (!HasProductionBornPathLikeOriginal(producer)) continue;
                C2NeutralPeasantUnitInfoV2LikeOriginal spawned;
                bool ok = C2UnitOriginalRuntimeAndRendererV1.TrySpawnProducedUnitFromBuildingLikeOriginal(
                    _mode, producer, item, 7, out spawned, out audit);
                if (!ok || spawned == null) continue;
                spawned.Nation = 7;
                spawned.SettlementAllegianceNationLikeOriginal = _owner;
                spawned.SettlementAiControlledLikeOriginal = true;
                spawned.ControllableByPlayer = false;
                target.Add(spawned);
                return true;
            }
            Debug.LogWarning("[C2:SETTLEMENT PRODUCE V338] village=" + _index.ToString(CultureInfo.InvariantCulture) +
                             " unit='" + (item.UnitId ?? string.Empty) + "' audit=" + audit);
            return false;
        }

        private static bool HasProductionBornPathLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            if (building == null) return false;
            C2BuildingRuntimeInfoV247LikeOriginal info = building.GetComponent<C2BuildingRuntimeInfoV247LikeOriginal>();
            if (info == null) info = building.GetComponentInParent<C2BuildingRuntimeInfoV247LikeOriginal>();
            if (info == null) return false;
            IList<Vector2> path = C2BuildingRuntimeInfoV247LikeOriginal
                .C2BuildingRuntimeV309GetProductionBornExitPathLikeOriginal(info);
            return path != null && path.Count > 1;
        }

        private void TickCaravanEconomyLikeOriginal()
        {
            if (_populationSpawned == false || _owner == 7 || _caravanInTransit ||
                _caravanItem == null || _data.CaravanCapacity <= 0 || _data.PeasantProduceTicks <= 0) return;

            int livePeasants = _peasants.Count(p => p != null && !p.IsDeadLikeOriginal);
            float now = Time.realtimeSinceStartup;

            // DIP_SimpleBuilding::Process resets LastProduce whenever no workers are
            // available, preventing dead time from being converted into resources
            // when peasants later return.
            if (livePeasants <= 0)
            {
                _lastProduceRealtime = now;
                return;
            }

            C2SettlementBuildingSelectableV1LikeOriginal storage;
            if (!TryFindNearestResourceStorageLikeOriginal(out storage)) return;

            // Original timing: p0 = GetAnimTime() - LastProduce;
            // t = Produce[i] * 256. At normal 25 Hz simulation this is
            // PeonTimeOnProduce * 0.04 seconds. Preserve the remainder so a
            // slow frame can complete multiple production periods exactly once.
            float periodSeconds = Mathf.Max(0.04f, _data.PeasantProduceTicks * 0.04f);
            float elapsed = Mathf.Max(0.0f, now - _lastProduceRealtime);
            int completedPeriods = Mathf.FloorToInt(elapsed / periodSeconds);
            if (completedPeriods <= 0) return;

            _lastProduceRealtime += completedPeriods * periodSeconds;
            long produced = (long)completedPeriods * livePeasants;
            _storedResource = Mathf.Min(float.MaxValue, _storedResource + produced);

            // DIP_SimpleBuilding dispatches the largest exact multiple of
            // CaravanCapacity that has accumulated, rather than only one load.
            int available = Mathf.FloorToInt(Mathf.Max(0.0f, _storedResource));
            int shipment = _data.CaravanCapacity * (available / _data.CaravanCapacity);
            if (shipment <= 0)
            {
                if (_data.ResourceMax > 0 && _storedResource > _data.ResourceMax)
                    _storedResource = _data.ResourceMax;
                return;
            }

            if (_buildings.Count == 0) FindVillageBuildingsLikeOriginal();
            if (_buildings.Count == 0) return;
            Vector3 start = ResolveBuildingServicePointLikeOriginal(_buildings[0], true);
            Vector3 destination = ResolveBuildingServicePointLikeOriginal(storage, false);
            GameObject go = new GameObject("C2_Settlement_Caravan_" + _index.ToString(CultureInfo.InvariantCulture));
            C2SettlementCaravanProxyV339LikeOriginal proxy = go.AddComponent<C2SettlementCaravanProxyV339LikeOriginal>();

            _storedResource -= shipment;
            _caravanInTransit = true;
            _caravanOwner = _owner;
            _caravanShipmentAmount = shipment;
            int shipmentOwner = _caravanOwner;
            int shipmentResource = _data.ResourceType;
            int shipmentAmount = shipment;

            proxy.ConfigureDeliveryLikeOriginal(
                start, destination, shipmentOwner,
                () =>
                {
                    // DipCaravan::Process State 3: unload into the owning nation's
                    // global resource ledger only after reaching DestStorage.
                    C2NationResourceEconomyV348LikeOriginal.AddResourceLikeOriginal(
                        shipmentOwner, shipmentResource, shipmentAmount,
                        "settlement_caravan_v361_village_" + _index.ToString(CultureInfo.InvariantCulture));
                    // The cargo is now unloaded. If ownership changes while the
                    // empty caravan is returning, SetOwner must not restore it.
                    _caravanShipmentAmount = 0;
                    Debug.Log("[C2:SETTLEMENT CARAVAN DELIVERY V361] village=" +
                              _index.ToString(CultureInfo.InvariantCulture) +
                              " owner=" + shipmentOwner.ToString(CultureInfo.InvariantCulture) +
                              " resource=" + shipmentResource.ToString(CultureInfo.InvariantCulture) +
                              " amount=" + shipmentAmount.ToString(CultureInfo.InvariantCulture) +
                              " nationTotal=" + C2NationResourceEconomyV348LikeOriginal
                                  .GetResourceLikeOriginal(shipmentOwner, shipmentResource)
                                  .ToString(CultureInfo.InvariantCulture));
                },
                () =>
                {
                    _caravanInTransit = false;
                    _caravanOwner = -1;
                    _caravanShipmentAmount = 0;
                });

            // ResourceMax clamp is performed after caravan creation in the
            // original Process() implementation.
            if (_data.ResourceMax > 0 && _storedResource > _data.ResourceMax)
                _storedResource = _data.ResourceMax;

            Debug.Log("[C2:SETTLEMENT CARAVAN V361] village=" + _index.ToString(CultureInfo.InvariantCulture) +
                      " owner=" + _owner.ToString(CultureInfo.InvariantCulture) +
                      " resource=" + _data.ResourceType.ToString(CultureInfo.InvariantCulture) +
                      " amount=" + shipment.ToString(CultureInfo.InvariantCulture) +
                      " stored=" + Mathf.FloorToInt(_storedResource).ToString(CultureInfo.InvariantCulture) +
                      " max=" + _data.ResourceMax.ToString(CultureInfo.InvariantCulture) +
                      " storage='" + (storage.SourceMonsterId ?? storage.KindName ?? string.Empty) + "'");
        }

        private bool TryFindNearestResourceStorageLikeOriginal(
            out C2SettlementBuildingSelectableV1LikeOriginal storage)
        {
            storage = null;
            float best = float.MaxValue;
            C2SettlementBuildingSelectableV1LikeOriginal[] all =
                FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            for (int i = 0; i < all.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = all[i];
                if (b == null || !b.isActiveAndEnabled || b.Nation != _owner || !AcceptsResourceLikeOriginal(b, _data.ResourceType))
                    continue;
                float dx = b.RealX / 16.0f - _data.CenterX;
                float dy = b.RealY / 16.0f - _data.CenterY;
                float d = dx * dx + dy * dy;
                if (d < best) { best = d; storage = b; }
            }
            return storage != null;
        }

        private Vector3 ResolveBuildingServicePointLikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building, bool born)
        {
            if (building == null) return transform.position;
            C2BuildingRuntimeInfoV247LikeOriginal info =
                building.GetComponent<C2BuildingRuntimeInfoV247LikeOriginal>();
            if (info == null) info = building.GetComponentInParent<C2BuildingRuntimeInfoV247LikeOriginal>();
            if (info != null)
            {
                // V379 FOUNDATION-A: settlement gameplay must use original MD
                // gameplay service points. Visual-projected paths belong only to
                // renderer/debug presentation and must never drive movement.
                IList<Vector2> path = born
                    ? info.BornExitPathReal
                    : info.ConcentratorPathReal;
                if (path != null && path.Count > 0)
                {
                    Vector2 p = path[path.Count - 1];
                    return _mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(
                        Mathf.RoundToInt(p.x) >> 4, Mathf.RoundToInt(p.y) >> 4);
                }
            }
            return building.transform.position;
        }

        private static bool AcceptsResourceLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, int resourceType)
        {
            string id = !string.IsNullOrWhiteSpace(building.KindName) ? building.KindName : building.SourceMonsterId;
            id = C2OriginalProduceCatalogV13.StripNationSuffixPublicLikeOriginal(id ?? string.Empty);
            string path = Path.Combine(@"C:\GSC Game World\Cossacks II\Data\UnitsMD", id + ".md");
            if (!File.Exists(path)) return false;
            string wanted = new[] { "WOOD", "GOLD", "STONE", "FOOD", "IRON", "COAL" }
                [Mathf.Clamp(resourceType, 0, 5)];
            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.GetEncoding(1251));
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = (lines[i] ?? string.Empty).Trim();
                    if (line.StartsWith("RESOURCEBASE", StringComparison.OrdinalIgnoreCase))
                        return Regex.IsMatch(line, @"(^|\s)" + Regex.Escape(wanted) + @"(\s|$)", RegexOptions.IgnoreCase);
                }
            }
            catch { }
            return false;
        }

        private void FindVillageBuildingsLikeOriginal()
        {
            _buildings.Clear();
            C2SettlementBuildingSelectableV1LikeOriginal[] all = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            for (int i = 0; i < all.Length; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = all[i];
                if (b == null) continue;
                float dx = b.RealX / 16.0f - _data.CenterX;
                float dy = b.RealY / 16.0f - _data.CenterY;
                if (dx * dx + dy * dy <= BigRadius * BigRadius) _buildings.Add(b);
            }
            _buildings.Sort((a, b) =>
            {
                float adx = a.RealX / 16.0f - _data.CenterX;
                float ady = a.RealY / 16.0f - _data.CenterY;
                float bdx = b.RealX / 16.0f - _data.CenterX;
                float bdy = b.RealY / 16.0f - _data.CenterY;
                return (adx * adx + ady * ady).CompareTo(bdx * bdx + bdy * bdy);
            });
        }

        private void Update()
        {
            TickFieldsV349LikeOriginal(Time.deltaTime);
            TickPopulationProductionLikeOriginal();
            TickCaravanEconomyLikeOriginal();
            _camera = _mode != null ? _mode.GetActiveBattleCameraLikeOriginal() : (_camera != null ? _camera : Camera.main);
            Vector2 mouse;
            bool leftPressed;
#if ENABLE_INPUT_SYSTEM
            mouse = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            leftPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            mouse = Input.mousePosition;
            leftPressed = Input.GetMouseButtonDown(0);
#endif
            mouse.y = Screen.height - mouse.y;
            bool hover = _lastIconRect.Contains(mouse);
            if (hover != _hover)
            {
                _hover = hover;
                SetZoneVisibleLikeOriginal(_hover);
            }
            if (_hover && leftPressed) SelectMainBuildingLikeOriginal();
            if (Time.realtimeSinceStartup >= _nextScan)
            {
                // The old 0.40s scan per village multiplied FindObjectsOfType
                // across the whole map. Stagger villages and scan less often.
                _nextScan = Time.realtimeSinceStartup + 1.25f;
                TickCaptureAndDefenceLikeOriginal();
            }
        }

        private void TickCaptureAndDefenceLikeOriginal()
        {
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all = SharedMapUnitsSnapshotLikeOriginal();
            var formationByNation = new Dictionary<int, HashSet<int>>();
            var ownerFormationGroups = new HashSet<int>();
            int ownerUnits = 0;
            var settlementPopulationIds = new HashSet<int>();
            for (int i = 0; i < _defenders.Count; i++)
                if (_defenders[i] != null) settlementPopulationIds.Add(_defenders[i].GetInstanceID());
            for (int i = 0; i < _peasants.Count; i++)
                if (_peasants[i] != null) settlementPopulationIds.Add(_peasants[i].GetInstanceID());
            for (int i = 0; i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = all[i];
                if (u == null || u.IsDeadLikeOriginal) continue;
                float dx = (u.RealXFloat != 0 ? u.RealXFloat : u.RealX) / 16.0f - _data.CenterX;
                float dy = (u.RealYFloat != 0 ? u.RealYFloat : u.RealY) / 16.0f - _data.CenterY;
                if (dx * dx + dy * dy > BigRadius * BigRadius) continue;
                int nation = u.CombatNationLikeOriginal;
                if (nation == _owner)
                {
                    // DIP_SimpleBuilding keeps Defenders/PeasGrp apart from
                    // NOwner (external troops belonging to the settlement
                    // owner). Counting villagers here made NOwner permanently
                    // non-zero and disabled ProcessDeffenders patrol forever.
                    if (!settlementPopulationIds.Contains(u.GetInstanceID()))
                        ownerUnits++;
                    int ownerGroupId;
                    if (C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(u, out ownerGroupId) &&
                        !settlementPopulationIds.Contains(u.GetInstanceID()) &&
                        !_militiaFormationGroups.Contains(ownerGroupId))
                        ownerFormationGroups.Add(ownerGroupId);
                }
                if (nation == _owner || nation >= 7) continue;
                int groupId;
                if (!C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(u, out groupId)) continue;
                HashSet<int> groups;
                if (!formationByNation.TryGetValue(nation, out groups))
                    formationByNation[nation] = groups = new HashSet<int>();
                groups.Add(groupId);
            }

            int pretenders = 0, aggressor = -1, mostBrigades = 0;
            foreach (KeyValuePair<int, HashSet<int>> pair in formationByNation)
            {
                if (pair.Value.Count <= 0) continue;
                pretenders++;
                if (pair.Value.Count > mostBrigades) { mostBrigades = pair.Value.Count; aggressor = pair.Key; }
            }
            int liveDefenders = _defenders.Count(u => u != null && !u.IsDeadLikeOriginal);
            if (liveDefenders + ownerUnits < 10 && pretenders == 1 && aggressor >= 0 && mostBrigades > 0)
                SetOwnerLikeOriginal(aggressor);

            bool attacked = pretenders > 0;
            for (int i = 0; i < _peasants.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal p = _peasants[i];
                if (p == null || p.IsDeadLikeOriginal) continue;
                // Original SGP_ComeIntoBuilding removes villagers from the field until danger leaves.
                p.NotSelectable = attacked;
                p.SetActiveLikeOriginal(!attacked);
            }
            MaintainMilitiaPolkLikeOriginal(ownerFormationGroups.Count > 0);
            if (attacked) CommandDefendersLikeOriginal(all);
            // DIP_SimpleBuilding::ProcessDeffenders runs while ProduceUnitTicks
            // is still filling Defenders. Waiting for the complete population
            // queue left every newly born cop standing on the same producer exit.
            else if (ownerUnits == 0) PatrolLooseDefendersLikeOriginal();
            // DIP_SimpleBuilding::Process handles every non-empty PeasGrp on
            // every pass. Already released peasants must start working while
            // the remaining militia/peasants are still in the produce queue.
            if (!attacked && _peasants.Count > 0) WorkPeasantsLikeOriginal();
        }

        private void MaintainMilitiaPolkLikeOriginal(bool regularOwnerBrigadeNearby)
        {
            // DIP_SimpleBuilding disbands its local Polk while an owner's regular
            // brigade is inside BigZone, then recreates it from >=20 loose cops.
            if (regularOwnerBrigadeNearby && _militiaFormationGroups.Count > 0)
            {
                for (int i = 0; i < _defenders.Count; i++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal unit = _defenders[i];
                    int existingMilitiaGroupId;
                    if (unit == null || !C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(unit, out existingMilitiaGroupId) ||
                        !_militiaFormationGroups.Contains(existingMilitiaGroupId))
                        continue;
                    string audit;
                    C2FormationRuntimeV167LikeOriginal.TryDisbandFormationV320LikeOriginal(unit, out audit);
                    Debug.Log("[C2:SETTLEMENT MILITIA POLK V340] disband village=" + _index.ToString(CultureInfo.InvariantCulture) +
                              " " + audit + " reason=owner_brigade_in_big_zone");
                }
                _militiaFormationGroups.Clear();
                _nextMilitiaPolkPatrolAt.Clear();
                return;
            }
            if (regularOwnerBrigadeNearby)
                return;
            PatrolMilitiaPolksLikeOriginal();
            if (Time.realtimeSinceStartup < _nextMilitiaFormationAttempt)
                return;
            _nextMilitiaFormationAttempt = Time.realtimeSinceStartup + UnityEngine.Random.Range(3.0f, 6.0f);

            var loose = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            for (int i = 0; i < _defenders.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = _defenders[i];
                if (unit != null && !unit.IsDeadLikeOriginal &&
                    !C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(unit))
                    loose.Add(unit);
            }
            if (loose.Count < 20) return;

            // Exact original call:
            // CreateFormationFromGroup(7,&Defenders,OU.x,OU.y,GetRND(256)).
            // OU is the settlement main object, not the producer BORNPOINT exit.
            float px = _data.CenterX;
            float py = _data.CenterY;
            byte dir = (byte)UnityEngine.Random.Range(0, 256);
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> formed;
            int groupId;
            string createAudit;
            if (!C2FormationRuntimeV167LikeOriginal.TryCreateMilitiaFormationFromGroupV340LikeOriginal(
                    loose, px * 16.0f, py * 16.0f, dir, out formed, out groupId, out createAudit))
                return;
            _militiaFormationGroups.Add(groupId);
            _nextMilitiaPolkPatrolAt[groupId] = 0.0f;
            Debug.Log("[C2:SETTLEMENT MILITIA POLK V340] create village=" + _index.ToString(CultureInfo.InvariantCulture) +
                      " " + createAudit + " move=wait_until_formation_assembly_finishes");
        }

        private void PatrolMilitiaPolksLikeOriginal()
        {
            float now = Time.realtimeSinceStartup;
            var liveGroupIds = new HashSet<int>();
            var membersByGroup = new Dictionary<int, List<C2NeutralPeasantUnitInfoV2LikeOriginal>>();
            for (int i = 0; i < _defenders.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = _defenders[i];
                int groupId;
                if (unit == null || unit.IsDeadLikeOriginal ||
                    !C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(unit, out groupId) ||
                    !_militiaFormationGroups.Contains(groupId))
                    continue;
                List<C2NeutralPeasantUnitInfoV2LikeOriginal> members;
                if (!membersByGroup.TryGetValue(groupId, out members))
                    membersByGroup[groupId] = members = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
                members.Add(unit);
                liveGroupIds.Add(groupId);
            }

            var stale = new List<int>();
            foreach (int groupId in _militiaFormationGroups)
            {
                if (!liveGroupIds.Contains(groupId)) { stale.Add(groupId); continue; }
                float due;
                if (_nextMilitiaPolkPatrolAt.TryGetValue(groupId, out due) && now < due) continue;
                List<C2NeutralPeasantUnitInfoV2LikeOriginal> members = membersByGroup[groupId];
                bool busy = false;
                for (int i = 0; i < members.Count; i++)
                    if (members[i] != null && members[i].IsBusyWithBornExitOrMoveLikeOriginal()) { busy = true; break; }
                if (busy) continue;
                IssueMilitiaPolkPatrolLikeOriginal(groupId, members, false);
            }
            for (int i = 0; i < stale.Count; i++)
            {
                _militiaFormationGroups.Remove(stale[i]);
                _nextMilitiaPolkPatrolAt.Remove(stale[i]);
            }
        }

        private string IssueMilitiaPolkPatrolLikeOriginal(
            int groupId,
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> members,
            bool forceWhileAssembling)
        {
            // DIP_SimpleBuilding::ProcessDeffenders calls MovePolkTo around the
            // village with OU.x/y - 256 + GetRND(512). Never issue the formation
            // back to its producer exit: that was the source of the mill pile-up.
            float targetX = (_data.CenterX + UnityEngine.Random.Range(-256.0f, 256.0f)) * 16.0f;
            float targetY = (_data.CenterY + UnityEngine.Random.Range(-256.0f, 256.0f)) * 16.0f;
            float freeX, freeY;
            if (C2BattleTerrainMode.C2BuildingMotionFieldV1TryFindNearestFreeRealLikeOriginal(
                    targetX, targetY, out freeX, out freeY, 32))
            {
                targetX = freeX;
                targetY = freeY;
            }
            byte dir = (byte)UnityEngine.Random.Range(0, 256);
            int issued;
            string audit;
            bool ok = C2FormationRuntimeV167LikeOriginal.TryIssueMoveV167LikeOriginal(
                members, targetX, targetY, true, dir,
                forceWhileAssembling ? "settlement_DefPolk_initial_MovePolkTo" : "settlement_DefPolk_patrol_MovePolkTo",
                out issued, out audit);
            _nextMilitiaPolkPatrolAt[groupId] = Time.realtimeSinceStartup + UnityEngine.Random.Range(12.0f, 24.0f);
            return (ok ? "ok " : "failed ") + audit +
                   " patrolPix=(" + Mathf.RoundToInt(targetX / 16.0f).ToString(CultureInfo.InvariantCulture) + "," +
                   Mathf.RoundToInt(targetY / 16.0f).ToString(CultureInfo.InvariantCulture) + ")";
        }

        private Vector2 ResolvePopulationExitRealLikeOriginal()
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal building = _buildings[i];
                if (!HasProductionBornPathLikeOriginal(building)) continue;
                C2BuildingRuntimeInfoV247LikeOriginal info = building.GetComponent<C2BuildingRuntimeInfoV247LikeOriginal>();
                if (info == null) info = building.GetComponentInParent<C2BuildingRuntimeInfoV247LikeOriginal>();
                IList<Vector2> path = C2BuildingRuntimeInfoV247LikeOriginal
                    .C2BuildingRuntimeV309GetProductionBornExitPathLikeOriginal(info);
                Vector2 last = path[path.Count - 1];
                Vector2 before = path[Mathf.Max(0, path.Count - 2)];
                Vector2 direction = (last - before).normalized;
                return last + direction * 220.0f * 16.0f;
            }
            return new Vector2(_data.CenterX * 16.0f, _data.CenterY * 16.0f);
        }

        private void WorkPeasantsLikeOriginal()
        {
            float now = Time.realtimeSinceStartup;
            Vector2 centre = new Vector2(_data.CenterX * 16.0f, _data.CenterY * 16.0f);
            Vector2 workGate = ResolvePopulationExitRealLikeOriginal();
            Vector2 outward = (workGate - centre).normalized;
            if (outward.sqrMagnitude < 0.5f) outward = Vector2.right;
            Vector2 side = new Vector2(-outward.y, outward.x);
            for (int i = 0; i < _peasants.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal peasant = _peasants[i];
                if (peasant == null || peasant.IsDeadLikeOriginal || peasant.IsBusyWithBornExitOrMoveLikeOriginal()) continue;
                float due;
                if (_nextPeasantWorkAt.TryGetValue(peasant.GetInstanceID(), out due) && now < due) continue;
                // DIP_SimpleBuilding.cpp:1372 calls SGP_TakeResourcesZone.
                // The previous bridge only moved a peasant once and left him
                // idle.  Feed the existing resource task state machine instead:
                // work -> carry -> enter producer -> leave -> work again.
                C2SettlementFieldPatchV342LikeOriginal fieldPatch = null;
                if (_data.ResourceType == 3)
                    fieldPatch = FindRipeUnreservedFieldPatchV342LikeOriginal(peasant);

                int resourceOriginalX;
                int resourceOriginalY;
                int workRadius;
                string resourceName;
                string resourceAudit;
                Vector2 resourceReal;
                if (fieldPatch != null)
                {
                    resourceReal = fieldPatch.OriginalRealPositionV342LikeOriginal;
                    resourceOriginalX = Mathf.RoundToInt(resourceReal.x) >> 4;
                    resourceOriginalY = Mathf.RoundToInt(resourceReal.y) >> 4;
                    workRadius = 112;
                    resourceName = "FIELD";
                    resourceAudit = "settlement_field_patch";
                }
                else
                {
                    int workOriginalX;
                    int workOriginalY;
                    if (_mode == null || !_mode.C2OriginalResourceMapV1TryFindResourceWorkTargetLikeOriginal(
                            (byte)Mathf.Clamp(_data.ResourceType, 0, 5),
                            Mathf.RoundToInt(_data.CenterX),
                            Mathf.RoundToInt(_data.CenterY),
                            Mathf.RoundToInt(VeryBigRadius),
                            peasant,
                            out resourceOriginalX,
                            out resourceOriginalY,
                            out workOriginalX,
                            out workOriginalY,
                            out workRadius,
                            out resourceName,
                            out resourceAudit))
                    {
                        _nextPeasantWorkAt[peasant.GetInstanceID()] = now + 1.0f;
                        continue;
                    }
                    resourceReal = new Vector2(resourceOriginalX << 4, resourceOriginalY << 4);
                }

                C2SettlementBuildingSelectableV1LikeOriginal producer = null;
                for (int b = 0; b < _buildings.Count; b++)
                {
                    if (HasProductionBornPathLikeOriginal(_buildings[b]))
                    {
                        producer = _buildings[b];
                        break;
                    }
                }

                C2BuildingRuntimeInfoV247LikeOriginal info = producer != null
                    ? producer.GetComponent<C2BuildingRuntimeInfoV247LikeOriginal>() : null;
                if (info == null && producer != null)
                    info = producer.GetComponentInParent<C2BuildingRuntimeInfoV247LikeOriginal>();

                Vector2 storeReal = workGate;
                Vector2 depositReal = centre;
                Vector2[] concentratorPath = null;
                Vector2[] bornPath = null;
                if (info != null)
                {
                    IList<Vector2> cp = info.ConcentratorPathReal;
                    IList<Vector2> bp = info.BornExitPathReal;
                    if (cp != null && cp.Count > 0)
                    {
                        concentratorPath = cp.ToArray();
                        storeReal = cp[0];
                        depositReal = cp[cp.Count - 1];
                    }
                    if (bp != null && bp.Count > 0)
                    {
                        bornPath = bp.ToArray();
                        if (concentratorPath == null)
                        {
                            storeReal = bp[bp.Count - 1];
                            depositReal = bp[0];
                        }
                    }
                }

                C2GameplayUnitTaskV1 task = peasant.GetComponent<C2GameplayUnitTaskV1>();
                GameObject unitProxy = task == null ? peasant.EnsureUnityProxyLikeOriginal() : null;
                if (task == null && unitProxy != null) task = unitProxy.AddComponent<C2GameplayUnitTaskV1>();
                if (task == null) continue;
                C2GameplayTargetKindV1 kind = _data.ResourceType == 3
                    ? C2GameplayTargetKindV1.Field
                    : (_data.ResourceType == 2 ? C2GameplayTargetKindV1.Stone :
                       (_data.ResourceType == 0 ? C2GameplayTargetKindV1.Tree : C2GameplayTargetKindV1.Unknown));
                Vector3 resourceWorld = _mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(
                    resourceOriginalX, resourceOriginalY);
                Vector3 storeWorld = producer != null ? producer.transform.position : resourceWorld;
                task.BeginTakeResourceV222LikeOriginal(
                    peasant, kind, (byte)Mathf.Clamp(_data.ResourceType, 0, 5),
                    resourceOriginalX, resourceOriginalY,
                    workRadius, resourceWorld, producer != null,
                    Mathf.RoundToInt(storeReal.x), Mathf.RoundToInt(storeReal.y),
                    Mathf.RoundToInt(depositReal.x), Mathf.RoundToInt(depositReal.y),
                    storeWorld, concentratorPath, bornPath);
                task.BindSettlementFieldV342LikeOriginal(fieldPatch, this);
                _nextPeasantWorkAt[peasant.GetInstanceID()] = now + 120.0f;
            }
        }

        private void EnsureFoodFieldV342LikeOriginal()
        {
            if (_data == null || _data.ResourceType != 3 || _mode == null || _fieldPatchesV342.Count > 0)
                return;

            C2SettlementBuildingSelectableV1LikeOriginal mill = null;
            for (int i = 0; i < _buildings.Count; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = _buildings[i];
                string id = ((b != null ? b.SourceMonsterId : string.Empty) + " " +
                             (b != null ? b.KindName : string.Empty)).ToUpperInvariant();
                if (id.Contains("MEL") || id.Contains("MILL")) { mill = b; break; }
            }

            // NewMon.cpp::CreateFields uses Radius1=6, MotionDist=44 from
            // UnitsMD/Field.md: a 13x13 isometric lattice around the mill.
            float centerX = mill != null ? mill.RealX / 16.0f : _data.CenterX;
            float centerY = mill != null ? mill.RealY / 16.0f : _data.CenterY;
            const int radius = 6;
            const float motionDist = 44.0f;
            for (int ix = -radius; ix <= radius; ix++)
            for (int iy = -radius; iy <= radius; iy++)
            {
                float x = centerX + (ix + iy) * motionDist;
                float y = centerY + (ix - iy) * motionDist;
                float freeX, freeY;
                bool free = C2BattleTerrainMode.C2BuildingMotionFieldV1TryFindNearestFreeRealLikeOriginal(
                    x * 16.0f, y * 16.0f, out freeX, out freeY, 2);
                if (!free || Mathf.Abs(freeX / 16.0f - x) > 18.0f || Mathf.Abs(freeY / 16.0f - y) > 18.0f)
                    continue; // SpriteSuccess==false in CreateFields.

                GameObject go = new GameObject("C2_FIELD_FG0_" + _index.ToString(CultureInfo.InvariantCulture) +
                                               "_" + ix.ToString(CultureInfo.InvariantCulture) +
                                               "_" + iy.ToString(CultureInfo.InvariantCulture));
                go.transform.SetParent(transform, true);
                C2SettlementFieldPatchV342LikeOriginal patch = go.AddComponent<C2SettlementFieldPatchV342LikeOriginal>();
                patch.ConfigureV342LikeOriginal(_mode, this, x, y, ix, iy);
                _fieldPatchesV342.Add(patch);
            }
            EnsureFieldBatchRendererV349LikeOriginal();
            _fieldBatchDirtyV349 = true;
            RebuildFieldBatchV349LikeOriginal();
            Debug.Log("[C2:FIELD V342] village=" + _index.ToString(CultureInfo.InvariantCulture) +
                      " source=UnitsMD/Field.md EXSPRITES=FG0 radius1=6 motionDist=44 patches=" +
                      _fieldPatchesV342.Count.ToString(CultureInfo.InvariantCulture) +
                      " model=Models/field.c2m texture=textures/pole1.tga renderer=single_FieldModel_batch");
        }

        internal void MarkFieldBatchDirtyV349LikeOriginal()
        {
            _fieldBatchDirtyV349 = true;
        }

        private void TickFieldsV349LikeOriginal(float deltaSeconds)
        {
            // akField.inl::GetAnimationTime returns GetTickCount, independent
            // of game time scale. Environment.TickCount64 has the same
            // system-uptime millisecond clock and is uploaded only once/frame.
            if (s_fieldWindShaderFrameV350 != Time.frameCount)
            {
                s_fieldWindShaderFrameV350 = Time.frameCount;
                float tickMs = unchecked((uint)Environment.TickCount);
                Shader.SetGlobalFloat("_C2OriginalFieldTimeMs", tickMs);
            }

            bool changed = false;
            for (int i = 0; i < _fieldPatchesV342.Count; i++)
            {
                C2SettlementFieldPatchV342LikeOriginal patch = _fieldPatchesV342[i];
                if (patch != null && patch.TickTimeV349LikeOriginal(deltaSeconds)) changed = true;
            }
            if (changed) _fieldBatchDirtyV349 = true;
            if (_fieldBatchDirtyV349) RebuildFieldBatchV349LikeOriginal();
        }

        private void EnsureFieldBatchRendererV349LikeOriginal()
        {
            if (_fieldBatchRendererV349 != null) return;
            GameObject go = new GameObject("C2_FIELD_FieldModel_Batch_V349");
            go.transform.SetParent(transform, false);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            _fieldBatchRendererV349 = go.AddComponent<MeshRenderer>();
            _fieldBatchMeshV349 = new Mesh { name = "C2_FIELD_FieldModel_Batch_V349" };
            _fieldBatchMeshV349.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _fieldBatchMeshV349.MarkDynamic();
            filter.sharedMesh = _fieldBatchMeshV349;
            _fieldBatchRendererV349.sharedMaterial =
                C2SettlementFieldPatchV342LikeOriginal.SharedFieldMaterialV349LikeOriginal;
            // Original DrawSpriteTrees collects all FIELDPATH entries into one
            // depth-enabled g_FieldModel pass.  Keep the whole village in that
            // single pass instead of assigning one renderer/order per square.
            _fieldBatchRendererV349.sortingOrder = Mathf.Clamp(
                5998 + (_data != null ? _data.CenterY : 0), -30000, 30000);
        }

        private void RebuildFieldBatchV349LikeOriginal()
        {
            _fieldBatchDirtyV349 = false;
            EnsureFieldBatchRendererV349LikeOriginal();
            if (_fieldBatchMeshV349 == null) return;

            _fieldBatchVerticesV349.Clear();
            _fieldBatchUvV349.Clear();
            _fieldBatchWindV350.Clear();
            _fieldBatchWindRandomV350.Clear();
            _fieldBatchColorsV349.Clear();
            _fieldBatchTrianglesV349.Clear();
            float maxWindDisplacementLocalV350 = 0.0f;

            // Scape3D.cpp::DrawFPatch and akField.cpp::FieldModel::AddPatch.
            // Keep these source constants literal: the original uses a 64 px
            // patch, converts it to the four D=45 diamond corners, then lays
            // straw rails every 6 px with an alternating 2.5 px offset.
            const int patchDiagonalLikeOriginal = (64 * 14142) / 20000;
            const float rowStepLikeOriginal = 6.0f;
            const float rowStepPartLikeOriginal = 2.5f;
            const float textureURatioLikeOriginal = 1.0f / 180.0f;
            const float skewHeightLikeOriginal = 0.8660254037844386f;
            for (int p = 0; p < _fieldPatchesV342.Count; p++)
            {
                C2SettlementFieldPatchV342LikeOriginal patch = _fieldPatchesV342[p];
                if (patch == null) continue;
                float grow = Mathf.Clamp01(patch.GrowRatioV349LikeOriginal);
                // akField.inl::GetGrowColor truncates the float into DWORD.
                byte c = (byte)Mathf.Clamp((int)(10.0f + grow * 245.0f), 0, 255);
                Color32 growColor = new Color32(c, 255, c, c);

                int centerX = Mathf.RoundToInt(patch.OriginalRealPositionV342LikeOriginal.x / 16.0f);
                int centerY = Mathf.RoundToInt(patch.OriginalRealPositionV342LikeOriginal.y / 16.0f);
                int d = patchDiagonalLikeOriginal;

                int hLt = _mode.C2OriginalFogTerrainHeightV1LikeOriginal(centerX, centerY + d);
                int hRt = _mode.C2OriginalFogTerrainHeightV1LikeOriginal(centerX + d, centerY);
                int hLb = _mode.C2OriginalFogTerrainHeightV1LikeOriginal(centerX - d, centerY);
                int hRb = _mode.C2OriginalFogTerrainHeightV1LikeOriginal(centerX, centerY - d);

                Vector3 ltEngine = OriginalFieldSkewPointV349LikeOriginal(centerX, centerY + d, hLt);
                Vector3 rtEngine = OriginalFieldSkewPointV349LikeOriginal(centerX + d, centerY, hRt);
                Vector3 lbEngine = OriginalFieldSkewPointV349LikeOriginal(centerX - d, centerY, hLb);
                Vector3 rbEngine = OriginalFieldSkewPointV349LikeOriginal(centerX, centerY - d, hRb);
                Vector3 lRailEngine = ltEngine - lbEngine;
                Vector3 rRailEngine = rtEngine - rbEngine;
                float lLen = lRailEngine.magnitude;
                float rLen = rRailEngine.magnitude;
                int rows = Mathf.Max((int)(lLen / rowStepLikeOriginal), (int)(rLen / rowStepLikeOriginal));
                if (rows <= 0 || lLen <= 0.0001f || rLen <= 0.0001f) continue;

                Vector3 lRailDirection = lRailEngine / lLen;
                Vector3 rRailDirection = rRailEngine / rLen;
                float lStep = lLen / rows;
                float rStep = rLen / rows;
                float lStepPart = lStep * rowStepPartLikeOriginal / rowStepLikeOriginal;
                float rStepPart = rStep * rowStepPartLikeOriginal / rowStepLikeOriginal;
                float lCursor = 0.0f;
                float rCursor = 0.0f;

                Vector3 ltWorld = _mode.SettlementMapPointToWorldV349LikeOriginal(centerX, centerY + d, 0.0f);
                Vector3 rtWorld = _mode.SettlementMapPointToWorldV349LikeOriginal(centerX + d, centerY, 0.0f);
                Vector3 lbWorld = _mode.SettlementMapPointToWorldV349LikeOriginal(centerX - d, centerY, 0.0f);
                Vector3 rbWorld = _mode.SettlementMapPointToWorldV349LikeOriginal(centerX, centerY - d, 0.0f);

                for (int row = 0; row < rows; row++)
                {
                    int randomState = unchecked(
                        (row * row * row * row + 128189 - 101 * row) ^
                        (int)(Mathf.Abs(ltEngine.x) * Mathf.Abs(ltEngine.y) + 1317.0f));
                    float lShift = (row & 1) != 0 ? lCursor : lCursor + lStepPart;
                    float rShift = (row & 1) != 0 ? rCursor : rCursor + rStepPart;
                    lCursor += lStep;
                    rCursor += rStep;

                    Vector3 vclEngine = lbEngine + lRailDirection * lShift;
                    Vector3 vcrEngine = rbEngine + rRailDirection * rShift;
                    float ushift = OriginalFieldRandomValueV349LikeOriginal(ref randomState);
                    float u1 = ushift + vclEngine.x * textureURatioLikeOriginal;
                    float u2 = ushift + vcrEngine.x * textureURatioLikeOriginal;

                    Vector3 vlbWorld = Vector3.LerpUnclamped(lbWorld, ltWorld, lShift / lLen);
                    Vector3 vrbWorld = Vector3.LerpUnclamped(rbWorld, rtWorld, rShift / rLen);
                    float leftStrawHeight = 30.0f *
                        (1.0f - OriginalFieldRandomTableV349LikeOriginal[
                            ((int)Mathf.Abs(vclEngine.y - vclEngine.x)) % OriginalFieldRandomTableV349LikeOriginal.Length] * 0.1f) * grow;
                    float rightStrawHeight = 30.0f *
                        (1.0f - OriginalFieldRandomTableV349LikeOriginal[
                            ((int)Mathf.Abs(vcrEngine.y - vcrEngine.x)) % OriginalFieldRandomTableV349LikeOriginal.Length] * 0.1f) * grow;

                    // AddPatch adds straw height after SkewPt, therefore its
                    // post-skew Z amount corresponds to height/cos(pi/6) in
                    // the Unity terrain coordinate bridge.
                    Vector3 leftHeightWorld =
                        _mode.SettlementMapPointToWorldV349LikeOriginal(centerX, centerY, leftStrawHeight / skewHeightLikeOriginal) -
                        _mode.SettlementMapPointToWorldV349LikeOriginal(centerX, centerY, 0.0f);
                    Vector3 rightHeightWorld =
                        _mode.SettlementMapPointToWorldV349LikeOriginal(centerX, centerY, rightStrawHeight / skewHeightLikeOriginal) -
                        _mode.SettlementMapPointToWorldV349LikeOriginal(centerX, centerY, 0.0f);
                    Vector3 vltWorld = vlbWorld + leftHeightWorld;
                    Vector3 vrtWorld = vrbWorld + rightHeightWorld;

                    // akField.inl/.cpp exact wind inputs. The shader evaluates
                    // bendAmount=1.8*sin(t*0.0004+frnd)*frnd1 and applies
                    // bendDir*bendAmount^2*height^2*0.01 only to top vertices.
                    float leftFrnd = OriginalFieldRandomTableV349LikeOriginal[
                        ((int)Mathf.Abs(vclEngine.x + vclEngine.y)) % OriginalFieldRandomTableV349LikeOriginal.Length];
                    float leftFrnd1 = OriginalFieldRandomTableV349LikeOriginal[
                        ((int)Mathf.Abs(vclEngine.x)) % OriginalFieldRandomTableV349LikeOriginal.Length];
                    float rightFrnd = OriginalFieldRandomTableV349LikeOriginal[
                        ((int)Mathf.Abs(vcrEngine.x + vcrEngine.y)) % OriginalFieldRandomTableV349LikeOriginal.Length];
                    float rightFrnd1 = OriginalFieldRandomTableV349LikeOriginal[
                        ((int)Mathf.Abs(vcrEngine.x)) % OriginalFieldRandomTableV349LikeOriginal.Length];

                    Vector3 leftWindWorld = _mode.SettlementSkewXYVectorToWorldV350LikeOriginal(
                        0.5f - leftFrnd, 0.5f - leftFrnd1);
                    Vector3 rightWindWorld = _mode.SettlementSkewXYVectorToWorldV350LikeOriginal(
                        0.5f - rightFrnd, 0.5f - rightFrnd1);
                    Vector3 leftWindLocal = transform.InverseTransformVector(leftWindWorld) *
                                            (leftStrawHeight * leftStrawHeight * 0.01f);
                    Vector3 rightWindLocal = transform.InverseTransformVector(rightWindWorld) *
                                             (rightStrawHeight * rightStrawHeight * 0.01f);
                    maxWindDisplacementLocalV350 = Mathf.Max(
                        maxWindDisplacementLocalV350,
                        Mathf.Max(leftWindLocal.magnitude, rightWindLocal.magnitude) * 3.24f);

                    int v = _fieldBatchVerticesV349.Count;
                    _fieldBatchVerticesV349.Add(transform.InverseTransformPoint(vltWorld));
                    _fieldBatchVerticesV349.Add(transform.InverseTransformPoint(vrtWorld));
                    _fieldBatchVerticesV349.Add(transform.InverseTransformPoint(vlbWorld));
                    _fieldBatchVerticesV349.Add(transform.InverseTransformPoint(vrbWorld));
                    // Direct3D's V=0 is the top of pole1.tga. C2OriginalImageIO
                    // stores the decoded TGA bottom-up for Unity, so use 1-V.
                    _fieldBatchUvV349.Add(new Vector2(u1, 1.0f));
                    _fieldBatchUvV349.Add(new Vector2(u2, 1.0f));
                    _fieldBatchUvV349.Add(new Vector2(u1, 1.0f - 32.0f / 256.0f));
                    _fieldBatchUvV349.Add(new Vector2(u2, 1.0f - 32.0f / 256.0f));
                    _fieldBatchWindV350.Add(new Vector4(leftWindLocal.x, leftWindLocal.y, leftWindLocal.z, 1.0f));
                    _fieldBatchWindV350.Add(new Vector4(rightWindLocal.x, rightWindLocal.y, rightWindLocal.z, 1.0f));
                    _fieldBatchWindV350.Add(Vector4.zero);
                    _fieldBatchWindV350.Add(Vector4.zero);
                    _fieldBatchWindRandomV350.Add(new Vector2(leftFrnd, leftFrnd1));
                    _fieldBatchWindRandomV350.Add(new Vector2(rightFrnd, rightFrnd1));
                    _fieldBatchWindRandomV350.Add(Vector2.zero);
                    _fieldBatchWindRandomV350.Add(Vector2.zero);
                    _fieldBatchColorsV349.Add(growColor);
                    _fieldBatchColorsV349.Add(growColor);
                    _fieldBatchColorsV349.Add(growColor);
                    _fieldBatchColorsV349.Add(growColor);
                    _fieldBatchTrianglesV349.Add(v);
                    _fieldBatchTrianglesV349.Add(v + 1);
                    _fieldBatchTrianglesV349.Add(v + 2);
                    _fieldBatchTrianglesV349.Add(v + 2);
                    _fieldBatchTrianglesV349.Add(v + 1);
                    _fieldBatchTrianglesV349.Add(v + 3);
                }
            }

            _fieldBatchMeshV349.Clear(false);
            _fieldBatchMeshV349.SetVertices(_fieldBatchVerticesV349);
            _fieldBatchMeshV349.SetUVs(0, _fieldBatchUvV349);
            _fieldBatchMeshV349.SetUVs(1, _fieldBatchWindV350);
            _fieldBatchMeshV349.SetUVs(2, _fieldBatchWindRandomV350);
            _fieldBatchMeshV349.SetColors(_fieldBatchColorsV349);
            _fieldBatchMeshV349.SetTriangles(_fieldBatchTrianglesV349, 0, true);
            _fieldBatchMeshV349.RecalculateBounds();
            if (maxWindDisplacementLocalV350 > 0.0f)
            {
                Bounds bounds = _fieldBatchMeshV349.bounds;
                bounds.Expand(maxWindDisplacementLocalV350 * 2.0f);
                _fieldBatchMeshV349.bounds = bounds;
            }
            _fieldBatchRendererV349.enabled = _fieldBatchVerticesV349.Count > 0;
        }

        private static Vector3 OriginalFieldSkewPointV349LikeOriginal(float x, float y, float height)
        {
            return new Vector3(x, y - 0.5f * height, height * 0.8660254037844386f);
        }

        private static float[] BuildOriginalFieldRandomTableV349LikeOriginal()
        {
            // mRandom.cpp starts with s_LastRnd=1. InitGroundZbuffer calls
            // g_FieldModel.Init before field patches are drawn and fills all
            // 1024 entries with this exact MSVC LCG sequence.
            float[] result = new float[1024];
            int state = 1;
            for (int i = 0; i < result.Length; i++)
                result[i] = OriginalFieldRandomValueV349LikeOriginal(ref state);
            return result;
        }

        private static float OriginalFieldRandomValueV349LikeOriginal(ref int state)
        {
            state = unchecked(state * 214013 + 2531011);
            return ((state >> 16) & 0x7fff) / 32767.0f;
        }

        private C2SettlementFieldPatchV342LikeOriginal FindRipeUnreservedFieldPatchV342LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal peasant)
        {
            int count = _fieldPatchesV342.Count;
            for (int n = 0; n < count; n++)
            {
                int index = (_nextFieldPatchCursorV342 + n) % count;
                C2SettlementFieldPatchV342LikeOriginal patch = _fieldPatchesV342[index];
                if (patch == null || !patch.TryReserveV342LikeOriginal(peasant)) continue;
                _nextFieldPatchCursorV342 = (index + 1) % Mathf.Max(1, count);
                return patch;
            }
            return null;
        }

        internal bool QueueNextFieldAfterHarvestV342LikeOriginal(
            C2GameplayUnitTaskV1 task, C2NeutralPeasantUnitInfoV2LikeOriginal peasant)
        {
            if (task == null || peasant == null) return false;
            C2SettlementFieldPatchV342LikeOriginal next = FindRipeUnreservedFieldPatchV342LikeOriginal(peasant);
            if (next == null) return false;
            AssignExistingTaskToFieldV342LikeOriginal(task, peasant, next);
            return true;
        }

        private void AssignExistingTaskToFieldV342LikeOriginal(
            C2GameplayUnitTaskV1 task, C2NeutralPeasantUnitInfoV2LikeOriginal peasant,
            C2SettlementFieldPatchV342LikeOriginal patch)
        {
            if (task == null || peasant == null || patch == null) return;
            C2SettlementBuildingSelectableV1LikeOriginal producer = _buildings.FirstOrDefault(HasProductionBornPathLikeOriginal);
            C2BuildingRuntimeInfoV247LikeOriginal info = producer != null
                ? producer.GetComponentInParent<C2BuildingRuntimeInfoV247LikeOriginal>() : null;
            Vector2 real = patch.OriginalRealPositionV342LikeOriginal;
            Vector2 store = ResolvePopulationExitRealLikeOriginal();
            Vector2 deposit = new Vector2(_data.CenterX * 16.0f, _data.CenterY * 16.0f);
            Vector2[] cp = null, bp = null;
            if (info != null)
            {
                IList<Vector2> cpl = info.ConcentratorPathReal;
                IList<Vector2> bpl = info.BornExitPathReal;
                if (cpl != null && cpl.Count > 0) { cp = cpl.ToArray(); store = cpl[0]; deposit = cpl[cpl.Count - 1]; }
                if (bpl != null && bpl.Count > 0) bp = bpl.ToArray();
            }
            Vector3 world = _mode.C2NeutralPeasantUnitsV2OriginalPixelToWorldV15LikeOriginal(
                Mathf.RoundToInt(real.x) >> 4, Mathf.RoundToInt(real.y) >> 4);
            task.BeginTakeResourceV222LikeOriginal(peasant, C2GameplayTargetKindV1.Field, 3,
                Mathf.RoundToInt(real.x) >> 4, Mathf.RoundToInt(real.y) >> 4, 112, world,
                producer != null, Mathf.RoundToInt(store.x), Mathf.RoundToInt(store.y),
                Mathf.RoundToInt(deposit.x), Mathf.RoundToInt(deposit.y),
                producer != null ? producer.transform.position : world, cp, bp, true);
            task.BindSettlementFieldV342LikeOriginal(patch, this);
        }

        private void PatrolLooseDefendersLikeOriginal()
        {
            float now = Time.realtimeSinceStartup;
            float centerRealX = _data.CenterX * 16.0f;
            float centerRealY = _data.CenterY * 16.0f;
            float bigReal = BigRadius * 16.0f;
            for (int i = 0; i < _defenders.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = _defenders[i];
                if (unit == null || unit.IsDeadLikeOriginal ||
                    unit.IsBusyWithBornExitOrMoveLikeOriginal() ||
                    C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(unit))
                    continue;
                C2CombatRuntimeV334LikeOriginal combat = unit.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                if (combat != null && combat.enabled) continue;

                float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
                float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
                float dxHome = ux - centerRealX;
                float dyHome = uy - centerRealY;
                bool outside = dxHome * dxHome + dyHome * dyHome > bigReal * bigReal;
                float due;
                if (_nextDefenderPatrolAt.TryGetValue(unit.GetInstanceID(), out due) && now < due && !outside)
                    continue;

                float dx = UnityEngine.Random.Range(-256.0f, 256.0f);
                float dy = UnityEngine.Random.Range(-256.0f, 256.0f);
                float length = Mathf.Max(1.0f, Mathf.Sqrt(dx * dx + dy * dy));
                dx += dx * 200.0f / length;
                dy += dy * 200.0f / length;
                unit.SetMoveDestinationRealLikeOriginal(
                    (_data.CenterX + dx) * 16.0f,
                    (_data.CenterY + dy) * 16.0f,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    false, 0);
                // Original GetRND(15)==3 is evaluated only while idle. A
                // staggered 12..24 second gate reproduces that sparse patrol
                // without polling every defender every simulation tick.
                _nextDefenderPatrolAt[unit.GetInstanceID()] = now + (outside
                    ? UnityEngine.Random.Range(2.0f, 5.0f)
                    : UnityEngine.Random.Range(12.0f, 24.0f));
            }
        }

        private static C2NeutralPeasantUnitInfoV2LikeOriginal[] SharedMapUnitsSnapshotLikeOriginal()
        {
            float now = Time.realtimeSinceStartup;
            if (_cachedMapUnits == null || now >= _nextMapUnitsSnapshotAt)
            {
                _cachedMapUnits = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
                _nextMapUnitsSnapshotAt = now + 0.65f;
            }
            return _cachedMapUnits;
        }

        private void CommandDefendersLikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal[] all)
        {
            for (int i = 0; i < _defenders.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal defender = _defenders[i];
                if (defender == null || defender.IsDeadLikeOriginal) continue;
                C2CombatRuntimeV334LikeOriginal current = defender.GetComponent<C2CombatRuntimeV334LikeOriginal>();
                if (current != null && current.enabled) continue;
                C2NeutralPeasantUnitInfoV2LikeOriginal nearest = null;
                float best = float.MaxValue;
                for (int j = 0; all != null && j < all.Length; j++)
                {
                    C2NeutralPeasantUnitInfoV2LikeOriginal enemy = all[j];
                    if (enemy == null || enemy.IsDeadLikeOriginal || enemy.CombatNationLikeOriginal == _owner ||
                        enemy.CombatNationLikeOriginal >= 7) continue;
                    float ex = (enemy.RealXFloat != 0 ? enemy.RealXFloat : enemy.RealX) / 16.0f - _data.CenterX;
                    float ey = (enemy.RealYFloat != 0 ? enemy.RealYFloat : enemy.RealY) / 16.0f - _data.CenterY;
                    if (ex * ex + ey * ey > VeryBigRadius * VeryBigRadius) continue;
                    float dx = (enemy.RealXFloat != 0 ? enemy.RealXFloat : enemy.RealX) -
                               (defender.RealXFloat != 0 ? defender.RealXFloat : defender.RealX);
                    float dy = (enemy.RealYFloat != 0 ? enemy.RealYFloat : enemy.RealY) -
                               (defender.RealYFloat != 0 ? defender.RealYFloat : defender.RealY);
                    float d = dx * dx + dy * dy;
                    if (d < best) { best = d; nearest = enemy; }
                }
                if (nearest == null) continue;
                C2OriginalProduceCatalogV13.C2MdIconInfoV13 md =
                    C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(defender);
                // ProcessDeffenders sets RifleAttack when the settlement has
                // more than nine cops. This selects ATTACK1 only for units whose
                // C2 MD really defines a ranged weapon; grenades remain an
                // explicit BrigadeAI ThrowGrenade order and are never substituted.
                int weaponMode = _defenders.Count > 9 && md.Damage1 > 0 && md.AttackRadius1 > 0 ? 1 : 0;
                int militiaGroupId;
                if (C2FormationRuntimeV167LikeOriginal.TryGetFormationGroupIdV321LikeOriginal(defender, out militiaGroupId) &&
                    _militiaFormationGroups.Contains(militiaGroupId))
                {
                    float distanceOriginal = Mathf.Sqrt(best) / 16.0f;
                    int aiMode;
                    string aiAudit;
                    if (C2OriginalGameAiDataV340LikeOriginal.TryChoosePolkWeaponModeLikeOriginal(
                            defender, distanceOriginal, _defenders.Count, out aiMode, out aiAudit) &&
                        (aiMode == 0 && md.Damage0 > 0 ||
                         aiMode == 1 && md.Damage1 > 0 && md.AttackRadius1 > 0 ||
                         aiMode == 2 && md.Damage2 > 0 && md.AttackRadius2 > 0))
                        weaponMode = aiMode;
                }
                if (weaponMode == 2)
                    C2CombatRuntimeV334LikeOriginal.ArmGrenadeLikeOriginal(defender);
                else
                    C2CombatRuntimeV334LikeOriginal.SetCommandWeaponModeLikeOriginal(defender, weaponMode);
                GameObject unitProxy = current == null ? defender.EnsureUnityProxyLikeOriginal() : null;
                if (current == null && unitProxy != null) current = unitProxy.AddComponent<C2CombatRuntimeV334LikeOriginal>();
                if (current == null) continue;
                current.BeginAttackLikeOriginal(defender, nearest, null, nearest.transform.position);
            }
        }

        private void SetOwnerLikeOriginal(int nation)
        {
            if (nation < 0 || nation > 6 || nation == _owner) return;
            _owner = nation;
            Color32 ownerColor = C2PlayerColorsLikeOriginal.GetNatColorByPlayer(_owner);
            // Original capture calls setlFindNearStorages for the new owner.
            // An old owner's loaded caravan must not deliver into the new
            // owner's economy; cancel it and restore the shipment locally.
            if (_caravanInTransit && _caravanOwner != nation)
            {
                C2SettlementCaravanProxyV339LikeOriginal[] proxies =
                    FindObjectsOfType<C2SettlementCaravanProxyV339LikeOriginal>();
                for (int i = 0; i < proxies.Length; i++)
                    if (proxies[i] != null && proxies[i].name.EndsWith("_" + _index.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
                        Destroy(proxies[i].gameObject);
                // The shipment has not reached a valid owner storage yet, so
                // return the exact loaded amount to the village's local stock.
                _storedResource += Mathf.Max(0, _caravanShipmentAmount);
                _caravanInTransit = false;
                _caravanOwner = -1;
                _caravanShipmentAmount = 0;
            }
            // DIP_SimpleBuilding changes NMASK/allegiance on capture, not the
            // visual NNUM.  Village buildings and inhabitants therefore keep
            // their neutral visual nation; only the settlement badge shows the
            // current owner colour.
            for (int i = 0; i < _peasants.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _peasants[i];
                if (u == null || u.IsDeadLikeOriginal) continue;
                u.Nation = 7;
                u.SettlementAllegianceNationLikeOriginal = nation;
                u.SettlementAiControlledLikeOriginal = true;
                u.ControllableByPlayer = false;
            }
            for (int i = 0; i < _defenders.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal u = _defenders[i];
                if (u == null || u.IsDeadLikeOriginal) continue;
                u.Nation = 7;
                u.SettlementAllegianceNationLikeOriginal = nation;
                u.SettlementAiControlledLikeOriginal = true;
                u.ControllableByPlayer = false;
            }
            C2UnitOriginalRuntimeAndRendererV1.RefreshNationColorsV332LikeOriginal();
            Debug.Log("[C2:SETTLEMENT CAPTURE V336] index=" + _index.ToString(CultureInfo.InvariantCulture) +
                      " group='" + _data.GroupName + "' owner=" + nation.ToString(CultureInfo.InvariantCulture) +
                      " colorId=" + C2PlayerColorsLikeOriginal.GetPlayerColorId(_owner).ToString(CultureInfo.InvariantCulture) +
                      " rgb=" + ownerColor.r.ToString(CultureInfo.InvariantCulture) + "," +
                      ownerColor.g.ToString(CultureInfo.InvariantCulture) + "," +
                      ownerColor.b.ToString(CultureInfo.InvariantCulture));
        }

        private void SelectMainBuildingLikeOriginal()
        {
            if (_buildings.Count == 0) FindVillageBuildingsLikeOriginal();
            C2SettlementBuildingSelectableV1LikeOriginal nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < _buildings.Count; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal b = _buildings[i];
                if (b == null) continue;
                float dx = b.RealX / 16.0f - _data.CenterX;
                float dy = b.RealY / 16.0f - _data.CenterY;
                float d = dx * dx + dy * dy;
                if (d < best) { best = d; nearest = b; }
            }
            if (nearest == null) return;
            _selectedSettlement = this;
            C2SettlementBuildingSelectableV1LikeOriginal[] all = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            for (int i = 0; i < all.Length; i++) if (all[i] != null) all[i].SetSelected(all[i] == nearest);
            C2GameplayHudV1.ForceRefreshLikeOriginal();
        }

        private void OnGUI()
        {
            if (_camera == null || _data == null) return;
            if (_fogRuntimeV344LikeOriginal == null)
                _fogRuntimeV344LikeOriginal = FindObjectOfType<C2UnitOriginalRuntimeAndRendererV1>();
            // cext_VisualInterface.cpp only exposes a settlement icon when
            // GetObjectVisibilityInFog(center) succeeds.
            if (_fogRuntimeV344LikeOriginal != null &&
                !_fogRuntimeV344LikeOriginal.IsOriginalMapPointVisibleInFogLikeOriginal(_data.CenterX, _data.CenterY))
            {
                _lastIconRect = new Rect(-1000, -1000, 0, 0);
                return;
            }
            Vector3 world = ResolveIconAnchorWorldLikeOriginal();
            Vector3 screen = _camera.WorldToScreenPoint(world);
            if (screen.z <= 0.0f) { _lastIconRect = new Rect(-1000, -1000, 0, 0); return; }
            if (screen.x < -200.0f || screen.x > Screen.width + 200.0f ||
                screen.y < -200.0f || screen.y > Screen.height + 200.0f)
            {
                _lastIconRect = new Rect(-1000, -1000, 0, 0);
                return;
            }
            int frameIndex = _animatedSettlementFrameCountV344LikeOriginal > 0
                ? (int)((unchecked((uint)Environment.TickCount) / 80u) % (uint)_animatedSettlementFrameCountV344LikeOriginal)
                : 0;
            SettlementGpFrameV344LikeOriginal outer = _animatedSettlementFramesV344LikeOriginal != null
                ? _animatedSettlementFramesV344LikeOriginal[Mathf.Clamp(_owner, 0, 7), frameIndex]
                : null;
            SettlementGpFrameV344LikeOriginal resource = _resourceFramesV344LikeOriginal != null
                ? _resourceFramesV344LikeOriginal[Mathf.Clamp(_data.ResourceType, 0, 5)]
                : null;
            float anchorGuiY = Screen.height - screen.y;
            Rect outerRect = outer != null && outer.Texture != null
                ? new Rect(screen.x - outer.OriginX, anchorGuiY - outer.OriginY,
                    outer.Texture.width, outer.Texture.height)
                : new Rect(screen.x - _iconCanvasWidth * 0.5f, anchorGuiY - _iconCanvasHeight,
                    _iconCanvasWidth, _iconCanvasHeight);
            Rect resourceRect = resource != null && resource.Texture != null
                ? new Rect(screen.x + 3.0f - resource.OriginX, anchorGuiY + 8.0f - resource.OriginY,
                    resource.Texture.width, resource.Texture.height)
                : new Rect(screen.x + 3.0f, anchorGuiY + 8.0f, 0, 0);
            float minX = Mathf.Min(outerRect.xMin, resourceRect.xMin);
            float minY = Mathf.Min(outerRect.yMin, resourceRect.yMin);
            float maxX = Mathf.Max(outerRect.xMax, resourceRect.xMax);
            float maxY = Mathf.Max(outerRect.yMax, resourceRect.yMax);
            _lastIconRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            Color old = GUI.color;
            GUI.color = Color.white;
            if (outer != null && outer.Texture != null)
                GUI.DrawTexture(outerRect, outer.Texture, ScaleMode.ScaleToFit, true);
            if (resource != null && resource.Texture != null)
                GUI.DrawTexture(resourceRect, resource.Texture, ScaleMode.ScaleToFit, true);
            if (_hover) DrawSettlementHintLikeOriginal();
            if (false && _hover)
                GUI.Label(new Rect(_lastIconRect.x - 55, _lastIconRect.y - 22, 180, 22),
                    "Поселение — " + ResourceNameLikeOriginal(_data.ResourceType));
            GUI.color = old;
        }

        private void OnDestroy()
        {
            if (_selectedSettlement == this) _selectedSettlement = null;
        }

        private static void DrawSpriteRectLikeOriginal(Sprite sprite, Rect target)
        {
            if (sprite == null || sprite.texture == null) return;
            Rect tr = sprite.textureRect;
            Texture2D texture = sprite.texture;
            GUI.DrawTextureWithTexCoords(target, texture, new Rect(
                tr.x / texture.width, tr.y / texture.height,
                tr.width / texture.width, tr.height / texture.height), true);
        }

        private static void DrawSpriteOpaqueLikeOriginal(Sprite sprite, Rect uv, Rect target)
        {
            if (sprite == null || sprite.texture == null) return;
            GUI.DrawTextureWithTexCoords(target, sprite.texture, uv, true);
        }

        private static Rect CalculateOpaqueSpriteUvLikeOriginal(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return new Rect(0, 0, 1, 1);
            Texture2D texture = sprite.texture;
            Rect tr = sprite.textureRect;
            Color32[] pixels;
            try { pixels = texture.GetPixels32(); }
            catch { return new Rect(tr.x / texture.width, tr.y / texture.height,
                tr.width / texture.width, tr.height / texture.height); }
            int x0 = Mathf.RoundToInt(tr.x), y0 = Mathf.RoundToInt(tr.y);
            int x1 = Mathf.RoundToInt(tr.xMax) - 1, y1 = Mathf.RoundToInt(tr.yMax) - 1;
            int minX = x1, minY = y1, maxX = x0, maxY = y0;
            bool found = false;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                if (pixels[y * texture.width + x].a < 8) continue;
                found = true;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
            if (!found) return new Rect(tr.x / texture.width, tr.y / texture.height,
                tr.width / texture.width, tr.height / texture.height);
            return new Rect(minX / (float)texture.width, minY / (float)texture.height,
                (maxX - minX + 1) / (float)texture.width,
                (maxY - minY + 1) / (float)texture.height);
        }

        private void DrawSettlementHintLikeOriginal()
        {
            string resource = ResourceNameCleanLikeOriginal(_data.ResourceType);
            int amount = Mathf.FloorToInt(Mathf.Max(0.0f, _storedResource));
            int capacity = Mathf.Max(0, _data.CaravanCapacity);
            string caravan = _owner == 7
                ? "\u041e\u0431\u043e\u0437: \u043f\u043e\u0441\u043b\u0435 \u0437\u0430\u0445\u0432\u0430\u0442\u0430 \u0438 \u043f\u043e\u0434\u043a\u043b\u044e\u0447\u0435\u043d\u0438\u044f \u0441\u043a\u043b\u0430\u0434\u0430"
                : (_caravanInTransit ? "\u041e\u0431\u043e\u0437: \u0432 \u043f\u0443\u0442\u0438"
                   : "\u041e\u0431\u043e\u0437: \u043d\u0430\u043a\u0430\u043f\u043b\u0438\u0432\u0430\u0435\u0442 \u0440\u0435\u0441\u0443\u0440\u0441");
            string value = "\u041f\u043e\u0441\u0435\u043b\u0435\u043d\u0438\u0435 \u0434\u043e\u0441\u0442\u0430\u0432\u043b\u044f\u0435\u0442 \u0440\u0435\u0441\u0443\u0440\u0441\u044b \u0432\u043b\u0430\u0434\u0435\u043b\u044c\u0446\u0443.\n" +
                           caravan + ".\n" +
                           "\u0420\u0435\u0441\u0443\u0440\u0441: " + resource + ": " + amount + "/" + capacity;
            GUIStyle style = new GUIStyle(GUI.skin.box) {
                alignment = TextAnchor.UpperLeft, wordWrap = true, fontSize = 13,
                padding = new RectOffset(9, 9, 7, 7)
            };
            float width = 390.0f;
            float height = style.CalcHeight(new GUIContent(value), width);
            float x = Mathf.Clamp(_lastIconRect.center.x - width * 0.5f, 6.0f, Screen.width - width - 6.0f);
            float y = Mathf.Max(6.0f, _lastIconRect.y - height - 5.0f);
            GUI.Box(new Rect(x, y, width, height), value, style);
        }

        private static string ResourceNameCleanLikeOriginal(int resourceType)
        {
            string[] names = { "\u0434\u0435\u0440\u0435\u0432\u043e", "\u0437\u043e\u043b\u043e\u0442\u043e", "\u043a\u0430\u043c\u0435\u043d\u044c", "\u0435\u0434\u0430", "\u0436\u0435\u043b\u0435\u0437\u043e", "\u0443\u0433\u043e\u043b\u044c" };
            return names[Mathf.Clamp(resourceType, 0, names.Length - 1)];
        }

        private static string ResourceNameLikeOriginal(int resourceType)
        {
            string[] names = { "дерево", "золото", "камень", "еда", "железо", "уголь" };
            return names[Mathf.Clamp(resourceType, 0, names.Length - 1)];
        }

        private Vector3 ResolveIconAnchorWorldLikeOriginal()
        {
            if (_buildings.Count == 0) FindVillageBuildingsLikeOriginal();
            C2SettlementBuildingSelectableV1LikeOriginal nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < _buildings.Count; i++)
            {
                C2SettlementBuildingSelectableV1LikeOriginal building = _buildings[i];
                if (building == null) continue;
                float dx = building.RealX / 16.0f - _data.CenterX;
                float dy = building.RealY / 16.0f - _data.CenterY;
                float d = dx * dx + dy * dy;
                if (d < best) { best = d; nearest = building; }
            }
            if (nearest != null)
            {
                Renderer[] renderers = nearest.GetComponentsInChildren<Renderer>(true);
                bool has = false;
                Bounds bounds = default(Bounds);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null || !renderer.enabled) continue;
                    if (!has) { bounds = renderer.bounds; has = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                if (has)
                    return new Vector3(bounds.center.x, bounds.max.y + 0.18f, bounds.center.z);
                return nearest.transform.position + Vector3.up * 1.8f;
            }
            return _mode.SettlementMapPointToWorldV336LikeOriginal(_data.CenterX, _data.CenterY) + Vector3.up * 1.8f;
        }

        private static Color NationColorV336LikeOriginal(int nation)
        {
            Color32 c = C2PlayerColorsLikeOriginal.GetNatColorByPlayer(Mathf.Clamp(nation, 0, 7));
            return c;
        }

        private void BuildZoneLineLikeOriginal(ref LineRenderer line, float radius, Color color, string suffix)
        {
            const int segments = 192;
            GameObject go = new GameObject("C2_Settlement_" + suffix);
            go.transform.SetParent(transform, false);
            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = segments;
            line.startWidth = line.endWidth = suffix == "BigZone" ? 0.22f : 0.14f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = color;
            line.sortingOrder = 32700;
            line.numCornerVertices = 2;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2.0f / segments;
                line.SetPosition(i, _mode.SettlementMapPointToWorldV336LikeOriginal(
                    _data.CenterX + Mathf.Cos(a) * radius, _data.CenterY + Mathf.Sin(a) * radius) + Vector3.up * 0.30f);
            }
        }

        private void BuildZoneGradientFillLikeOriginal(ref MeshRenderer renderer)
        {
            const int segments = 192;
            const int radialSteps = 24;
            GameObject go = new GameObject("C2_Settlement_OriginalGradientZone");
            go.transform.SetParent(transform, false);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            renderer = go.AddComponent<MeshRenderer>();
            int columns = segments;
            var vertices = new Vector3[(radialSteps + 1) * columns];
            var colors = new Color32[vertices.Length];
            var triangles = new int[radialSteps * segments * 6];
            Color32 inner = new Color32(255, 32, 32, 47);
            Color32 outer = new Color32(255, 255, 255, 47);
            for (int r = 0; r <= radialSteps; r++)
            {
                float t = r / (float)radialSteps;
                float radius = VeryBigRadius * t;
                float blend = Mathf.InverseLerp(BigRadius, VeryBigRadius, radius);
                Color32 c = Color32.Lerp(inner, outer, blend);
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2.0f / segments;
                    Vector3 world = _mode.SettlementMapPointToWorldV336LikeOriginal(
                        _data.CenterX + Mathf.Cos(a) * radius,
                        _data.CenterY + Mathf.Sin(a) * radius);
                    int v = r * columns + i;
                    vertices[v] = world - transform.position + Vector3.up * 0.34f;
                    colors[v] = c;
                }
            }
            int ti = 0;
            for (int r = 0; r < radialSteps; r++)
            for (int i = 0; i < segments; i++)
            {
                int n = (i + 1) % segments;
                int a = r * columns + i;
                int b = r * columns + n;
                int c = (r + 1) * columns + i;
                int d = (r + 1) * columns + n;
                triangles[ti++] = a; triangles[ti++] = c; triangles[ti++] = d;
                triangles[ti++] = a; triangles[ti++] = d; triangles[ti++] = b;
            }
            Mesh mesh = new Mesh { name = "SettlementOriginalGradientZone" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.color = Color.white;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 32690;
        }

        private void BuildZoneFillLikeOriginal(ref MeshRenderer renderer, float radius, Color color, string suffix)
        {
            const int segments = 64;
            GameObject go = new GameObject("C2_Settlement_" + suffix);
            go.transform.SetParent(transform, false);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            renderer = go.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh { name = suffix + "_Mesh" };
            var vertices = new Vector3[segments + 1];
            var uv = new Vector2[segments + 1];
            var triangles = new int[segments * 3];
            Vector3 center = _mode.SettlementMapPointToWorldV336LikeOriginal(_data.CenterX, _data.CenterY);
            vertices[0] = center - transform.position + Vector3.up * 0.22f;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2.0f / segments;
                Vector3 world = _mode.SettlementMapPointToWorldV336LikeOriginal(
                    _data.CenterX + Mathf.Cos(a) * radius, _data.CenterY + Mathf.Sin(a) * radius);
                vertices[i + 1] = world - transform.position + Vector3.up * 0.22f;
                uv[i + 1] = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f);
                int t = i * 3;
                triangles[t] = 0;
                triangles[t + 1] = i + 1;
                triangles[t + 2] = ((i + 1) % segments) + 1;
            }
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
            Shader shader = Shader.Find("Sprites/Default");
            renderer.sharedMaterial = new Material(shader) { color = color };
            renderer.sortingOrder = 32690;
        }

        private void SetZoneVisibleLikeOriginal(bool value)
        {
            if (_bigLine != null) _bigLine.enabled = false;
            if (_veryBigLine != null) _veryBigLine.enabled = false;
            if (_bigFill != null) _bigFill.enabled = false;
        }
    }

    // HGu caravans are original Cossacks II COMPLEXOBJECT OBOZ records.  They
    // intentionally contain no USERLC animations, so the infantry renderer
    // cannot display them.  This proxy uses the native OBOZ GP and preserves
    // the important settlement behaviour: it appears at a village building
    // and rolls outward instead of being scattered across the map.
    public sealed class C2SettlementCaravanProxyV339LikeOriginal : MonoBehaviour
    {
        private Vector3 _start;
        private Vector3 _finish;
        private float _bornAt;
        private Texture2D[] _frames;
        private Camera _camera;
        private Action _onDelivered;
        private Action _onReturned;
        private bool _returning;
        private float _legStartedAt;

        internal void ConfigureLikeOriginal(Vector3 start, Vector3 finish, int nation)
        {
            ConfigureDeliveryLikeOriginal(start, finish, nation, null);
        }

        internal void ConfigureDeliveryLikeOriginal(Vector3 start, Vector3 finish, int nation, Action onReturned)
        {
            ConfigureDeliveryLikeOriginal(start, finish, nation, null, onReturned);
        }

        internal void ConfigureDeliveryLikeOriginal(
            Vector3 start, Vector3 finish, int nation, Action onDelivered, Action onReturned)
        {
            _start = start;
            _finish = finish;
            transform.position = start;
            _bornAt = Time.realtimeSinceStartup;
            _legStartedAt = _bornAt;
            _onDelivered = onDelivered;
            _onReturned = onReturned;
            _returning = false;
            _camera = Camera.main;
            var gps = new global::TemnyLessViewer.C2GpSystem();
            string error;
            int gp = gps.PreLoadGPImage("OBOZ", @"C:\GSC Game World\Cossacks II\Data", out error);
            int count = gp > 0 ? gps.GetFrameCount(gp) : 0;
            _frames = new Texture2D[Mathf.Max(1, count)];
            Color32 nc = nation < 7 ? C2PlayerColorsLikeOriginal.GetNatColorByPlayer(nation) : new Color32(220, 220, 220, 255);
            for (int i = 0; i < _frames.Length; i++)
            {
                global::TemnyLessViewer.C2RenderedFrame frame = null;
                bool ok = gp > 0 && gps.GetRenderedFrameNationColor(gp, i, nc.r, nc.g, nc.b, out frame, out error);
                if (!ok && gp > 0) ok = gps.GetRenderedFrame(gp, i, out frame, out error);
                if (ok && frame != null) _frames[i] = MakeTextureLikeOriginal(frame, "settlement_oboz_" + i);
            }
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.realtimeSinceStartup - _legStartedAt) / 4.5f);
            Vector3 from = _returning ? _finish : _start;
            Vector3 to = _returning ? _start : _finish;
            transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0.0f, 1.0f, t));
            if (t >= 1.0f)
            {
                if (!_returning)
                {
                    // Original State 3 unloads at DestStorage, sets ResAmount=0,
                    // then State 2 sends the empty caravan back to the village.
                    Action delivered = _onDelivered;
                    _onDelivered = null;
                    if (delivered != null) delivered();
                    _returning = true;
                    _legStartedAt = Time.realtimeSinceStartup;
                }
                else
                {
                    Action completed = _onReturned;
                    _onReturned = null;
                    if (completed != null) completed();
                    Destroy(gameObject);
                    return;
                }
            }
            if (_camera == null) _camera = Camera.main;
        }

        private void OnGUI()
        {
            if (_camera == null || _frames == null || _frames.Length == 0) return;
            Texture2D tex = _frames[((int)((Time.realtimeSinceStartup - _bornAt) * 10.0f)) % _frames.Length];
            if (tex == null) return;
            Vector3 p = _camera.WorldToScreenPoint(transform.position + Vector3.up * 0.25f);
            if (p.z <= 0.0f) return;
            GUI.DrawTexture(new Rect(p.x - tex.width * 0.5f, Screen.height - p.y - tex.height, tex.width, tex.height),
                tex, ScaleMode.ScaleToFit, true);
        }

        private static Texture2D MakeTextureLikeOriginal(global::TemnyLessViewer.C2RenderedFrame frame, string name)
        {
            byte[] rgba = (byte[])frame.Rgba.Clone();
            int row = frame.Width * 4;
            byte[] tmp = new byte[row];
            for (int y = 0; y < frame.Height / 2; y++)
            {
                int a = y * row, b = (frame.Height - 1 - y) * row;
                Buffer.BlockCopy(rgba, a, tmp, 0, row);
                Buffer.BlockCopy(rgba, b, rgba, a, row);
                Buffer.BlockCopy(tmp, 0, rgba, b, row);
            }
            Texture2D tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false, false);
            tex.name = name;
            tex.LoadRawTextureData(rgba);
            tex.Apply(false, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
    }
}
