using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2GameplayHudV1
    {
        private static DialogNode _selPointMainDeskXmlV125LikeOriginal;
        private static string _selPointMainDeskAuditV125LikeOriginal = "not_loaded";

        private sealed class C2SelPointXmlContextV125LikeOriginal
        {
            public bool IsBuilding;
            public int BaseX;
            public int BaseY;
            public int SelectedCount;
            public C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
            public C2SettlementBuildingSelectableV1LikeOriginal Building;
            public C2OriginalProduceCatalogV13.C2MdIconInfoV13 Icon;
            public C2BuildingHudStateV113LikeOriginal BuildingState;
            public string Title;
            public string TitleSource;
        }

        private bool TryRenderSelectedPointXmlUnitLeftCardV125LikeOriginal(C2NeutralPeasantUnitInfoV2LikeOriginal unit, int selectedCount, int baseOffsetX = 0)
        {
            DialogNode desk = LoadSelPointMainDeskXmlV125LikeOriginal();
            if (desk == null) return false;

            C2OriginalProduceCatalogV13.C2MdIconInfoV13 icon = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedUnit(unit);
            string titleSource;
            string title = ResolveUnitTitleLikeOriginal(icon, unit, out titleSource);

            C2SelPointXmlContextV125LikeOriginal ctx = new C2SelPointXmlContextV125LikeOriginal
            {
                IsBuilding = false,
                BaseX = baseOffsetX,
                BaseY = 0,
                SelectedCount = selectedCount,
                Unit = unit,
                Icon = icon,
                Title = title,
                TitleSource = titleSource
            };

            RenderSelPointXmlNodeV125LikeOriginal(desk, ctx.BaseX, ctx.BaseY, true, ctx, 0);

            // V136: verbose XML unit-card audit removed; it was editor-log churn during selection.
            return true;
        }

        private bool TryRenderSelectedPointXmlBuildingLeftCardV125LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            int selectedCount,
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 info,
            C2BuildingHudStateV113LikeOriginal state)
        {
            DialogNode desk = LoadSelPointMainDeskXmlV125LikeOriginal();
            if (desk == null) return false;

            string titleSource;
            string title = ResolveBuildingTitleLikeOriginal(info, building, state.MdName, out titleSource);

            C2SelPointXmlContextV125LikeOriginal ctx = new C2SelPointXmlContextV125LikeOriginal
            {
                IsBuilding = true,
                BaseX = 0,
                BaseY = 0,
                SelectedCount = selectedCount,
                Building = building,
                Icon = info,
                BuildingState = state,
                Title = title,
                TitleSource = titleSource
            };

            RenderSelPointXmlNodeV125LikeOriginal(desk, ctx.BaseX, ctx.BaseY, true, ctx, 0);

            // V136: verbose XML building-card audit removed; it was editor-log churn during construction/ready transitions.
            return true;
        }

        private static DialogNode LoadSelPointMainDeskXmlV125LikeOriginal()
        {
            if (_selPointMainDeskXmlV125LikeOriginal != null) return _selPointMainDeskXmlV125LikeOriginal;

            string path = FindSelPointDialogsXmlV125LikeOriginal();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                _selPointMainDeskAuditV125LikeOriginal = "missing";
                return null;
            }

            try
            {
                string text;
                try { text = File.ReadAllText(path, Encoding.GetEncoding(1251)); }
                catch { text = File.ReadAllText(path, Encoding.Default); }

                DialogNode root = DialogNode.Parse(text);
                _selPointMainDeskXmlV125LikeOriginal = FindSelPointMainDeskV125LikeOriginal(root);
                _selPointMainDeskAuditV125LikeOriginal = _selPointMainDeskXmlV125LikeOriginal != null
                    ? path
                    : "main_desk_not_found path='" + path + "'";
                return _selPointMainDeskXmlV125LikeOriginal;
            }
            catch (Exception ex)
            {
                _selPointMainDeskAuditV125LikeOriginal = "parse_failed path='" + path + "' err=" + ex.GetType().Name + ": " + ex.Message;
                return null;
            }
        }

        private static string FindSelPointDialogsXmlV125LikeOriginal()
        {
            string[] roots = C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal();
            for (int i = 0; i < roots.Length; i++)
            {
                string root = roots[i];
                if (string.IsNullOrWhiteSpace(root)) continue;
                string p = Path.Combine(root, "Dialogs", "v", "SelPoint.DialogsDesk.Dialogs.xml");
                if (File.Exists(p)) return p;
            }

            string streaming = Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data", "Dialogs", "v", "SelPoint.DialogsDesk.Dialogs.xml");
            return File.Exists(streaming) ? streaming : string.Empty;
        }

        private static DialogNode FindSelPointMainDeskV125LikeOriginal(DialogNode node)
        {
            if (node == null) return null;
            if (string.Equals(node.Name, "DialogsDesk", StringComparison.OrdinalIgnoreCase) &&
                ContainsActionV125LikeOriginal(node, "va_SP_BranchColor") &&
                ContainsActionV125LikeOriginal(node, "va_SP_BuildingOnly"))
                return node;

            for (int i = 0; i < node.Children.Count; i++)
            {
                DialogNode found = FindSelPointMainDeskV125LikeOriginal(node.Children[i]);
                if (found != null) return found;
            }
            return null;
        }

        private static bool ContainsActionV125LikeOriginal(DialogNode node, string action)
        {
            if (node == null) return false;
            if (NodeHasActionV125LikeOriginal(node, action)) return true;
            for (int i = 0; i < node.Children.Count; i++)
                if (ContainsActionV125LikeOriginal(node.Children[i], action)) return true;
            return false;
        }

        private void RenderSelPointXmlNodeV125LikeOriginal(
            DialogNode node,
            int ox,
            int oy,
            bool inheritedVisible,
            C2SelPointXmlContextV125LikeOriginal ctx,
            int depth)
        {
            if (node == null || depth > 16 || ctx == null) return;

            int x = ox + node.Int("x", 0);
            int y = oy + node.Int("y", 0);
            int w = node.Int("Width", 0);
            int h = node.Int("Height", 0);

            bool visible = inheritedVisible && !string.Equals(node.TextOf("Visible"), "false", StringComparison.OrdinalIgnoreCase);
            ApplySelPointXmlActionsV125LikeOriginal(node, ctx, ref visible, ref x, ref y, ref w, ref h, out string fileId, out int spriteId, out string text, out bool skipChildren);
            if (!visible) return;

            bool isPicture = string.Equals(node.Name, "GPPicture", StringComparison.OrdinalIgnoreCase);
            bool isGpText = string.Equals(node.Name, "GP_TextButton", StringComparison.OrdinalIgnoreCase);
            bool isText = string.Equals(node.Name, "TextButton", StringComparison.OrdinalIgnoreCase);
            bool isCanvas = string.Equals(node.Name, "Canvas", StringComparison.OrdinalIgnoreCase);

            if (isCanvas && NodeHasActionV125LikeOriginal(node, "va_SP_MoraleLine"))
            {
                AddOriginalMoraleLineLikeOriginal(x, y, Mathf.Max(1, w), Mathf.Max(1, h), ResolveMoraleCurrentLikeOriginal(ctx.Unit), ResolveMoraleMaxLikeOriginal(ctx.Unit));
                return;
            }
            if (isCanvas && NodeHasActionV125LikeOriginal(node, "va_SP_LifeLine"))
            {
                int lifeV404, maxLifeV404;
                if (C2FormationRuntimeV167LikeOriginal.TryGetFormationAverageLifeV404LikeOriginal(
                        ctx.Unit, out lifeV404, out maxLifeV404))
                    AddOriginalLifeLineLikeOriginal(x, y, Mathf.Max(1, w), Mathf.Max(1, h), lifeV404, maxLifeV404);
                return;
            }
            if (isCanvas && NodeHasActionV125LikeOriginal(node, "va_SP_TiredLine"))
            {
                AddOriginalTiredLineLikeOriginal(
                    x, y, Mathf.Max(3, w), Mathf.Max(1, h),
                    C2CombatRuntimeV334LikeOriginal.GetFormationTiringRemainingLikeOriginal(ctx.Unit),
                    ctx.Unit);
                return;
            }

            if (isPicture && NodeHasActionV125LikeOriginal(node, "va_SP_B_StageLine"))
            {
                AddG16ImageOverpaintV140LikeOriginal("xml_sp_building_stage_back", fileId, spriteId, x, y, w, h, 255, false, 64, false, false);
                float fillAmount = ctx.BuildingState.Stage / (float)Mathf.Max(1, ctx.BuildingState.StageMax);
                Image fill = AddG16Image("xml_sp_building_stage_fill", "Interf3\\BuildProgress", 1, x, y, w, h, 255, false, false, false);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                fill.fillAmount = Mathf.Clamp01(fillAmount);
                AttachBuildingConstructionProgressUpdaterV136LikeOriginal(fill, null, ctx.Building);
                return;
            }

            if ((isPicture || isGpText) && !string.IsNullOrWhiteSpace(fileId) && w > 0 && h > 0)
            {
                int overpaintAlphaV140 = 64;
                if (NodeHasActionV125LikeOriginal(node, "va_UnitBigPortret") || NodeHasActionV125LikeOriginal(node, "va_SP_Bld_BigPortret"))
                    overpaintAlphaV140 = 170;
                else if (NodeHasActionV125LikeOriginal(node, "va_SP_BranchColor") || NodeHasActionV125LikeOriginal(node, "va_SP_BranchSprite"))
                    overpaintAlphaV140 = 96;
                else if (NodeHasActionV125LikeOriginal(node, "va_SP_BuildingOnly"))
                    overpaintAlphaV140 = 72;

                Image xmlImageV402B = AddG16ImageOverpaintV140LikeOriginal("xml_sp_" + San(fileId) + "_" + spriteId.ToString(CultureInfo.InvariantCulture), fileId, spriteId, x, y, w, h, 255, false, overpaintAlphaV140, false, false);
                if (xmlImageV402B != null && NodeHasActionV125LikeOriginal(node, "cva_SP_KillsGuardian"))
                    xmlImageV402B.gameObject.AddComponent<C2GuardianKillsAnimatorV402BLikeOriginal>().Configure(xmlImageV402B, fileId);
            }

            if ((isText || isGpText) && !string.IsNullOrEmpty(text) && w > 0 && h > 0)
            {
                string resolvedText = ResolveXmlMessageV125LikeOriginal(text);
                Color textColor = ResolveXmlTextColorV125LikeOriginal(node);
                if (NodeHasActionV125LikeOriginal(node, "va_SP_UnitNameSide"))
                    textColor = OriginalHudTitleTextColorV141LikeOriginal();
                Text label = AddCrispLabelV140LikeOriginal("xml_sp_text_" + San(resolvedText), resolvedText, x, y, w, h, ResolveXmlFontSizeV125LikeOriginal(node), ResolveXmlTextAnchorV125LikeOriginal(node), textColor);
                if (ctx.IsBuilding && NodeHasActionV125LikeOriginal(node, "va_SP_B_Stage"))
                    AttachBuildingConstructionProgressUpdaterV136LikeOriginal(null, label, ctx.Building);
                if (ctx.IsBuilding && NodeHasActionV125LikeOriginal(node, "va_SP_B_Life"))
                    AttachBuildingLifeUpdaterV267LikeOriginal(label, ctx.Building, ctx.BuildingState.LifeMax);
            }

            if (skipChildren) return;
            for (int i = 0; i < node.Children.Count; i++)
            {
                DialogNode child = node.Children[i];
                if (IsXmlMetaNodeV125LikeOriginal(child)) continue;
                RenderSelPointXmlNodeV125LikeOriginal(child, x, y, visible, ctx, depth + 1);
            }
        }

        private static int _guardianKillsThresholdV402BLikeOriginal = int.MinValue;
        private static int _guardianFrameCountV402BLikeOriginal = -1;

        private static int ResolveGuardianKillsThresholdV402BLikeOriginal()
        {
            if (_guardianKillsThresholdV402BLikeOriginal != int.MinValue) return _guardianKillsThresholdV402BLikeOriginal;
            int value = 300;
            string[] roots = C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal();
            for (int i = 0; i < roots.Length; i++)
            {
                string path = Path.Combine(roots[i], "Dialogs", "Interface", "InterfaceSystem.xml");
                if (!File.Exists(path)) continue;
                try
                {
                    string xml = File.ReadAllText(path);
                    System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
                        xml, @"<NKillsForGuardian>\s*(-?\d+)\s*</NKillsForGuardian>",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    int parsed;
                    if (m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                    {
                        value = parsed;
                        break;
                    }
                }
                catch { }
            }
            _guardianKillsThresholdV402BLikeOriginal = value;
            return value;
        }

        private static int ResolveGuardianFrameCountV402BLikeOriginal(string fileId)
        {
            if (_guardianFrameCountV402BLikeOriginal > 0) return _guardianFrameCountV402BLikeOriginal;
            int count = 0;
            string[] roots = C2OriginalProduceCatalogV13.OriginalDataRootsForSiblingLoadersLikeOriginal();
            for (int i = 0; i < roots.Length && count <= 0; i++)
            {
                try
                {
                    var gps = new TemnyLessViewer.C2GpSystem();
                    string error;
                    int gp = gps.PreLoadGPImage(fileId, roots[i], out error);
                    if (gp > 0) count = gps.GetFrameCount(gp);
                }
                catch { }
            }
            _guardianFrameCountV402BLikeOriginal = Mathf.Max(1, count);
            return _guardianFrameCountV402BLikeOriginal;
        }

        private sealed class C2GuardianKillsAnimatorV402BLikeOriginal : MonoBehaviour
        {
            private Image _image;
            private string _fileId = "Interf3\\zirochka";
            private int _frameCount = 1;
            private int _lastFrame = -1;
            private static float _origin = -1.0f;

            public void Configure(Image image, string fileId)
            {
                _image = image;
                if (!string.IsNullOrWhiteSpace(fileId)) _fileId = fileId;
                _frameCount = ResolveGuardianFrameCountV402BLikeOriginal(_fileId);
                if (_origin < 0.0f) _origin = Time.realtimeSinceStartup;
                UpdateFrameV402BLikeOriginal(true);
            }

            private void Update() { UpdateFrameV402BLikeOriginal(false); }

            private void UpdateFrameV402BLikeOriginal(bool force)
            {
                if (_image == null) { Destroy(this); return; }
                int frame = _frameCount > 0
                    ? ((int)((Time.realtimeSinceStartup - _origin) * 1000.0f) / 40) % _frameCount
                    : 0;
                if (!force && frame == _lastFrame) return;
                _lastFrame = frame;
                _image.sprite = C2GameplayOriginalSpriteCacheV1.LoadSprite(_fileId, frame, "guardian_v402b");
            }
        }

        private void ApplySelPointXmlActionsV125LikeOriginal(
            DialogNode node,
            C2SelPointXmlContextV125LikeOriginal ctx,
            ref bool visible,
            ref int x,
            ref int y,
            ref int w,
            ref int h,
            out string fileId,
            out int spriteId,
            out string text,
            out bool skipChildren)
        {
            fileId = node.TextOf("FileID");
            spriteId = node.Int("SpriteID", node.Int("Sprite", 0));
            text = node.TextOf("Message");
            skipChildren = false;

            if (NodeHasAnySideActionV125LikeOriginal(node))
            {
                visible = false;
                return;
            }

            if (NodeHasUnsupportedSelectedPointOverlayV125LikeOriginal(node))
            {
                visible = false;
                return;
            }

            if (NodeHasActionV125LikeOriginal(node, "va_SP_BuildingOnly"))
                visible = ctx.IsBuilding;
            if (NodeHasActionV125LikeOriginal(node, "va_SP_ConstructMode"))
                visible = ctx.IsBuilding && !ctx.BuildingState.Ready;
            if (NodeHasActionV125LikeOriginal(node, "va_SP_ConstructedMode"))
                visible = ctx.IsBuilding && ctx.BuildingState.Ready;
            if (NodeHasActionV125LikeOriginal(node, "va_SP_B_Stage") || NodeHasActionV125LikeOriginal(node, "va_SP_B_StageLine"))
                visible = ctx.IsBuilding && !ctx.BuildingState.Ready;
            if (NodeHasActionV125LikeOriginal(node, "va_SP_B_Life") || NodeHasActionV125LikeOriginal(node, "va_SP_B_Places"))
                visible = ctx.IsBuilding && ctx.BuildingState.Ready;
            if (NodeHasActionV125LikeOriginal(node, "va_SP_CenUp_One"))
                visible = ctx.SelectedCount <= 1 &&
                          !C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(ctx.Unit);
            if (NodeHasActionV125LikeOriginal(node, "va_SP_CenUp_Mul"))
                visible = ctx.SelectedCount > 1 ||
                          C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(ctx.Unit);
            if (NodeHasActionV125LikeOriginal(node, "va_SP_Morale") || NodeHasActionV125LikeOriginal(node, "va_SP_MoraleLine"))
                visible = !ctx.IsBuilding;
            if (NodeHasActionV125LikeOriginal(node, "va_SP_LifeLine") ||
                NodeHasActionV125LikeOriginal(node, "va_SP_TiredLine"))
                visible = !ctx.IsBuilding && ctx.Unit != null &&
                          C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(ctx.Unit);
            if (NodeHasActionV125LikeOriginal(node, "va_UnitBigPortret"))
                visible = !ctx.IsBuilding && !string.IsNullOrEmpty(ctx.Icon.BigIconFile);
            if (NodeHasActionV125LikeOriginal(node, "va_SP_Bld_BigPortret"))
                visible = ctx.IsBuilding && !string.IsNullOrEmpty(ctx.Icon.BigIconFile);
            if (NodeHasActionV125LikeOriginal(node, "va_SP_KillsAward"))
            {
                int rank = 0;
                if (!ctx.IsBuilding && ctx.Unit != null &&
                    C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(ctx.Unit))
                {
                    int exp, raw, speed, status;
                    float av;
                    if (C2FormationRuntimeV167LikeOriginal.TryGetBrigadeExperienceV402LikeOriginal(ctx.Unit, out exp, out raw, out speed, out av, out status))
                    {
                        if (exp != 0)
                        {
                            if (exp > 20) rank++;
                            if (exp > 60) rank++;
                            if (exp > 120) rank++;
                            if (exp > 300) rank++;
                            if (exp > 600) rank++;
                        }
                    }
                }
                int awardId = node.Int("ID", 0);
                visible = rank >= awardId && awardId > 0;
            }
            if (NodeHasActionV125LikeOriginal(node, "cva_SP_KillsGuardian"))
            {
                int exp = 0, raw = 0, speed = 100, status = 0;
                float av = 0.0f;
                bool brigade = !ctx.IsBuilding && ctx.Unit != null &&
                    C2FormationRuntimeV167LikeOriginal.TryGetBrigadeExperienceV402LikeOriginal(ctx.Unit, out exp, out raw, out speed, out av, out status);
                visible = brigade && exp != 0 && exp >= ResolveGuardianKillsThresholdV402BLikeOriginal();
            }

            if (!visible) return;

            if (NodeHasActionV125LikeOriginal(node, "va_SP_BranchColor"))
                spriteId = ResolveBranchColorSpriteLikeOriginal(ctx.Icon);
            if (NodeHasActionV125LikeOriginal(node, "va_SP_BranchSprite"))
            {
                visible = ctx.Icon.HasPortBranch;
                spriteId = ResolveBranchSpriteLikeOriginal(ctx.Icon);
            }
            if (NodeHasActionV125LikeOriginal(node, "va_UnitBigPortret") || NodeHasActionV125LikeOriginal(node, "va_SP_Bld_BigPortret"))
            {
                fileId = ctx.Icon.BigIconFile;
                spriteId = ctx.Icon.BigIconSprite;
            }
            if (NodeHasActionV125LikeOriginal(node, "va_SP_NatFlag"))
                spriteId = ResolveNationFlagSpriteLikeOriginal(ctx.Unit);
            if (NodeHasActionV125LikeOriginal(node, "va_SP_CenDown") && ctx.IsBuilding)
                spriteId++;
            if (NodeHasActionV125LikeOriginal(node, "va_SP_NameColor"))
                spriteId = ctx.IsBuilding ? spriteId : ResolveNameColorSpriteLikeOriginal(ctx.Icon);
            if (NodeHasActionV125LikeOriginal(node, "va_SP_NameCircle"))
                spriteId = ctx.IsBuilding ? spriteId : ResolveNameCircleSpriteLikeOriginal(ctx.Icon);

            if (NodeHasActionV125LikeOriginal(node, "va_SP_UnitNameSide"))
                text = ctx.Title;
            if (NodeHasActionV125LikeOriginal(node, "va_SP_Amount"))
            {
                int groupId;
                int live;
                int total;
                string shape;
                byte direction;
                text = C2FormationRuntimeV167LikeOriginal.TryGetFormationSummaryV321LikeOriginal(
                        ctx.Unit, out groupId, out live, out total, out shape, out direction)
                    ? live.ToString(CultureInfo.InvariantCulture) + "/" +
                      total.ToString(CultureInfo.InvariantCulture)
                    : ctx.SelectedCount.ToString(CultureInfo.InvariantCulture);
            }
            if (NodeHasActionV125LikeOriginal(node, "va_SP_Kills"))
            {
                if (!ctx.IsBuilding && ctx.Unit != null)
                {
                    int exp, raw, speed, status;
                    float av;
                    if (C2FormationRuntimeV167LikeOriginal.TryGetBrigadeExperienceV402LikeOriginal(ctx.Unit, out exp, out raw, out speed, out av, out status))
                        text = exp.ToString(CultureInfo.InvariantCulture);
                    else
                        text = C2FormationRuntimeV167LikeOriginal.GetUnitKillsV402LikeOriginal(ctx.Unit).ToString(CultureInfo.InvariantCulture);
                }
                else text = "0";
            }
            if (NodeHasActionV125LikeOriginal(node, "va_SP_Protect") && !ctx.IsBuilding && ctx.Unit != null)
            {
                int extraShield = C2FormationRuntimeV167LikeOriginal.GetBrigadeExperienceShieldBonusV402LikeOriginal(ctx.Unit);
                if (extraShield != 0) text = "+" + extraShield.ToString(CultureInfo.InvariantCulture);
            }
            if (NodeHasActionV125LikeOriginal(node, "va_SP_Morale"))
                text = ResolveMoraleTextLikeOriginal(ctx.Unit);
            if (NodeHasActionV125LikeOriginal(node, "va_SP_B_Life"))
                text = ctx.BuildingState.Life.ToString(CultureInfo.InvariantCulture) + "/" + ctx.BuildingState.LifeMax.ToString(CultureInfo.InvariantCulture);
            if (NodeHasActionV125LikeOriginal(node, "va_SP_B_Places"))
                text = ctx.BuildingState.Places.ToString(CultureInfo.InvariantCulture);
            if (NodeHasActionV125LikeOriginal(node, "va_SP_B_Population"))
                text = ctx.BuildingState.Population.ToString(CultureInfo.InvariantCulture) + "/" + ctx.BuildingState.PopulationMax.ToString(CultureInfo.InvariantCulture);
            if (NodeHasActionV125LikeOriginal(node, "va_SP_B_Stage"))
                text = ctx.BuildingState.Stage.ToString(CultureInfo.InvariantCulture) + "/" + ctx.BuildingState.StageMax.ToString(CultureInfo.InvariantCulture);
        }

        private static bool NodeHasAnySideActionV125LikeOriginal(DialogNode node)
        {
            return NodeHasActionV125LikeOriginal(node, "va_SP_UnitSprSide") ||
                   NodeHasActionV125LikeOriginal(node, "va_SP_BldOnly_Side") ||
                   NodeHasActionV125LikeOriginal(node, "va_SP_NameCircleSide") ||
                   NodeHasActionV125LikeOriginal(node, "va_SP_UnitNameSide_UNUSED_SIDE") ||
                   NodeHasActionV125LikeOriginal(node, "va_SP_MoraleSide") ||
                   NodeHasActionV125LikeOriginal(node, "va_SP_MoraleSideBlink") ||
                   NodeHasActionV125LikeOriginal(node, "va_SP_MoraleSideText");
        }

        private static bool NodeHasUnsupportedSelectedPointOverlayV125LikeOriginal(DialogNode node)
        {
            // These are optional selected-point overlays in the same original XML desk, not the
            // normal left portrait. The original action code enables them only for matching object
            // types; drawing them unconditionally overlays #Goods/owner/info panels on unit cards.
            return NodeHasActionV125LikeOriginal(node, "cvi_Act_Oboz") ||
                   NodeHasActionV125LikeOriginal(node, "cvi_AcademyDesk") ||
                   NodeHasActionV125LikeOriginal(node, "cva_U_Info_Switch");
        }

        private static bool NodeHasActionV125LikeOriginal(DialogNode node, string action)
        {
            if (node == null || string.IsNullOrEmpty(action)) return false;
            for (int i = 0; i < node.Children.Count; i++)
            {
                DialogNode c = node.Children[i];
                if (!string.Equals(c.Name, "v_Actions", StringComparison.OrdinalIgnoreCase)) continue;
                for (int j = 0; j < c.Children.Count; j++)
                {
                    if (string.Equals(c.Children[j].Name, action, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private static bool IsXmlMetaNodeV125LikeOriginal(DialogNode node)
        {
            if (node == null) return true;
            string n = node.Name ?? string.Empty;
            return string.Equals(n, "v_Actions", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(n, "Aligning", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(n, "Position&Width", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(n, "ColorParams", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(n, "Transform", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveXmlMessageV125LikeOriginal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string t = text.Trim();
            if (!t.StartsWith("#", StringComparison.Ordinal)) return t;

            string localized = C2OriginalProduceCatalogV13.ResolveUiTextLikeOriginal(t);
            if (!string.IsNullOrWhiteSpace(localized) && !string.Equals(localized, t, StringComparison.OrdinalIgnoreCase))
                return localized;

            if (string.Equals(t, "#Life", StringComparison.OrdinalIgnoreCase)) return "\u0416\u0438\u0437\u043d\u0438:";
            if (string.Equals(t, "#LivingPlaces", StringComparison.OrdinalIgnoreCase)) return "\u0416\u0438\u043b\u044b\u0435 \u043c\u0435\u0441\u0442\u0430:";
            if (string.Equals(t, "#Population#", StringComparison.OrdinalIgnoreCase)) return "\u041d\u0430\u0441\u0435\u043b\u0435\u043d\u0438\u0435:";
            if (string.Equals(t, "#Progress", StringComparison.OrdinalIgnoreCase)) return "\u041f\u0440\u043e\u0433\u0440\u0435\u0441\u0441:";
            return t;
        }

        private static int ResolveXmlFontSizeV125LikeOriginal(DialogNode node)
        {
            string f = (node.TextOf("ActiveFont") + " " + node.TextOf("PassiveFont")).Trim();
            if (f.IndexOf("SmallWhiteFont1", StringComparison.OrdinalIgnoreCase) >= 0) return 9;
            if (f.IndexOf("SmallRedFont1", StringComparison.OrdinalIgnoreCase) >= 0) return 10;
            if (f.IndexOf("SmallBlackFont1", StringComparison.OrdinalIgnoreCase) >= 0) return 10;
            if (f.IndexOf("SpecialYellowFont", StringComparison.OrdinalIgnoreCase) >= 0) return 11;
            return 10;
        }

        private static TextAnchor ResolveXmlTextAnchorV125LikeOriginal(DialogNode node)
        {
            string a = node.TextOf("Align");
            if (string.Equals(a, "Right", StringComparison.OrdinalIgnoreCase)) return TextAnchor.MiddleRight;
            if (string.Equals(a, "Left", StringComparison.OrdinalIgnoreCase)) return TextAnchor.MiddleLeft;
            return TextAnchor.MiddleCenter;
        }

        private static Color ResolveXmlTextColorV125LikeOriginal(DialogNode node)
        {
            string f = (node.TextOf("ActiveFont") + " " + node.TextOf("PassiveFont")).Trim();
            if (f.IndexOf("SmallRedFont1", StringComparison.OrdinalIgnoreCase) >= 0)
                return new Color(0.65f, 0.0f, 0.0f, 1.0f);
            if (f.IndexOf("SmallBlackFont1", StringComparison.OrdinalIgnoreCase) >= 0)
                return Color.black;
            if (f.IndexOf("SpecialYellowFont", StringComparison.OrdinalIgnoreCase) >= 0)
                return Color.white;
            return Color.white;
        }
    }
}
