using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2GameplayHudV1
    {
        private struct C2BuildingHudStateV113LikeOriginal
        {
            public bool IsConstructionProxy;
            public bool Ready;
            public int Stage;
            public int StageMax;
            public int Life;
            public int LifeMax;
            public int Places;
            public int Population;
            public int PopulationMax;
            public string UnitId;
            public string MdName;
            public string Audit;
        }

        private C2BuildingHudStateV113LikeOriginal ResolveBuildingHudStateV113LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 info)
        {
            C2BuildingHudStateV113LikeOriginal st = new C2BuildingHudStateV113LikeOriginal();
            st.Ready = true;
            st.Stage = 1;
            st.StageMax = 1;
            st.LifeMax = Mathf.Max(1, info.LifeMax);
            st.Life = st.LifeMax;
            st.Places = Mathf.Max(0, info.UnitAbsorber);
            st.Population = 0;
            st.PopulationMax = st.Places;
            st.UnitId = building != null ? (building.SourceMonsterId ?? string.Empty) : string.Empty;
            st.MdName = C2OriginalProduceCatalogV13.ResolveMdForSelectedBuildingLikeOriginal(building);
            st.Audit = "ready_saved_or_finished";

            C2RuntimeConstructionSiteProxyLikeOriginal proxy = building != null
                ? building.GetComponentInParent<C2RuntimeConstructionSiteProxyLikeOriginal>()
                : null;

            if (proxy != null)
            {
                st.IsConstructionProxy = true;
                st.Ready = proxy.Ready;
                st.Stage = Mathf.Clamp(proxy.Stage, 0, Mathf.Max(1, proxy.BuildStages));
                st.StageMax = Mathf.Max(1, proxy.BuildStages);
                st.UnitId = !string.IsNullOrEmpty(proxy.UnitId) ? proxy.UnitId : st.UnitId;
                st.MdName = !string.IsNullOrEmpty(proxy.MdName) ? proxy.MdName : st.MdName;

                if (!st.Ready)
                {
                    float t = Mathf.Clamp01(st.Stage / (float)Mathf.Max(1, st.StageMax));
                    st.Life = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1.0f, st.LifeMax, t)), 1, st.LifeMax);
                    st.Places = 0;
                    st.Population = 0;
                    st.PopulationMax = 0;
                    st.Audit = "construction_not_ready_hide_produce_upgrade";
                }
                else
                {
                    st.Stage = st.StageMax;
                    st.Life = st.LifeMax;
                    st.Audit = "construction_ready_show_produce_upgrade";
                }
            }

            return st;
        }

        private void AttachBuildingConstructionProgressUpdaterV136LikeOriginal(
            Image fill,
            Text label,
            C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            if (building == null) return;
            C2RuntimeConstructionSiteProxyLikeOriginal proxy = building.GetComponentInParent<C2RuntimeConstructionSiteProxyLikeOriginal>();
            if (proxy == null) return;

            GameObject target = fill != null ? fill.gameObject : (label != null ? label.gameObject : null);
            if (target == null) return;

            C2BuildingConstructionProgressUiUpdaterV136LikeOriginal updater =
                target.AddComponent<C2BuildingConstructionProgressUiUpdaterV136LikeOriginal>();
            updater.Configure(fill, label, proxy);
        }

        private sealed class C2BuildingConstructionProgressUiUpdaterV136LikeOriginal : MonoBehaviour
        {
            private Image _fill;
            private Text _label;
            private C2RuntimeConstructionSiteProxyLikeOriginal _proxy;

            public void Configure(Image fill, Text label, C2RuntimeConstructionSiteProxyLikeOriginal proxy)
            {
                _fill = fill;
                _label = label;
                _proxy = proxy;
                LateUpdate();
            }

            private void LateUpdate()
            {
                if (_proxy == null)
                {
                    Destroy(this);
                    return;
                }

                int max = Mathf.Max(1, _proxy.BuildStages);
                int stage = _proxy.Ready ? max : Mathf.Clamp(_proxy.Stage, 0, max);
                float amount = Mathf.Clamp01(stage / (float)max);

                if (_fill != null)
                    _fill.fillAmount = amount;
                if (_label != null)
                    _label.text = stage.ToString(CultureInfo.InvariantCulture) + "/" + max.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static string BuildBuildingHudStateKeyV114LikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building)
        {
            if (building == null) return "building=<null>";
            C2RuntimeConstructionSiteProxyLikeOriginal proxy = building.GetComponentInParent<C2RuntimeConstructionSiteProxyLikeOriginal>();
            string baseKey = proxy == null ? "prebuilt" : BuildConstructionProxyHudStateKeyV115LikeOriginal(proxy);

            // V133: queue count/progress are updated in-place by tiny UI components.
            // Do not rebuild the whole HUD when a unit finishes training.
            return baseKey;
        }

        private static string BuildConstructionProxyHudStateKeyV115LikeOriginal(C2RuntimeConstructionSiteProxyLikeOriginal proxy)
        {
            if (proxy == null) return "runtime=<null>";

            // V134: do not include current construction Stage in the HUD rebuild key.
            // V133 still rebuilt the whole building panel on every stage tick:
            // stage 0/50, 1/50, 2/50... which caused UI/G16 churn during construction.
            // The full panel must rebuild only when the building changes identity or crosses ready/not-ready.
            return "runtime md=" + (proxy.MdName ?? string.Empty) +
                   " unit=" + (proxy.UnitId ?? string.Empty) +
                   " stages=" + Mathf.Max(1, proxy.BuildStages).ToString(CultureInfo.InvariantCulture) +
                   " ready=" + proxy.Ready +
                   " dead=" + proxy.Dead +
                   " notSelectable=" + proxy.NotSelectable;
        }

        private void RebuildBuildingLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, int selectedCount)
        {
            EnsureCanvas();
            ReloadOriginalDataForModLikeOriginal();
            ClearSpawned();

            _spriteAudit.Length = 0;
            _spriteAuditOrder = 0;
            HideTooltip();

            C2OriginalProduceCatalogV13.C2MdIconInfoV13 info = C2OriginalProduceCatalogV13.LoadMdInfoForSelectedBuilding(building);
            C2BuildingHudStateV113LikeOriginal state = ResolveBuildingHudStateV113LikeOriginal(building, info);
            if (!string.IsNullOrWhiteSpace(state.MdName))
            {
                C2OriginalProduceCatalogV13.C2MdIconInfoV13 stateInfo = C2OriginalProduceCatalogV13.LoadMdIcon(state.MdName);
                if (!string.IsNullOrWhiteSpace(stateInfo.Path))
                {
                    info = stateInfo;
                    state = ResolveBuildingHudStateV113LikeOriginal(building, info);
                }
            }

            BuildSelectedBuildingLeftCardV114LikeOriginal(building, selectedCount, info, state);

            if (state.Ready && selectedCount == 1 && !C2BuildingPlacementPreviewV27.C2BuildPlacementActiveLikeOriginal)
            {
                BuildOriginalBuildingProducePanelLikeOriginal(building, state, selectedCount);
                BuildOriginalBuildingUpgradePanelLikeOriginal(building, state, selectedCount);
            }
            else
            {
                _lastProduceAudit = "hidden_by_original_filter ready=" + state.Ready +
                                    " selectedCount=" + selectedCount.ToString(CultureInfo.InvariantCulture) +
                                    " buildMode=" + C2BuildingPlacementPreviewV27.C2BuildPlacementActiveLikeOriginal +
                                    " state='" + state.Audit + "'";
            }

            EnsureTooltipLayer();
            DumpBuildingSpriteAuditLikeOriginal(building, selectedCount);
        }

        private void BuildSelectedBuildingLeftCardV114LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            int selectedCount,
            C2OriginalProduceCatalogV13.C2MdIconInfoV13 info,
            C2BuildingHudStateV113LikeOriginal state)
        {
            if (TryRenderSelectedPointXmlBuildingLeftCardV125LikeOriginal(building, selectedCount, info, state))
                return;

            const int baseX = -2;
            const int baseY = 459;
            const int rootX = baseX + 21;
            const int rootY = baseY + 43;

            // Building actions from original VUI_Actions.cpp:
            // va_SP_CenDown switches the XML base sprite 20 to 21 for buildings, removing the unit morale bottom.
            // va_SP_NameColor/va_SP_NameCircle keep their XML base sprites 27/23 for buildings.
            const int topSingleY = baseY + 6;
            const int topManyY = baseY - 1;
            const int bottomY = baseY + 280;

            string portraitFile = info.BigIconFile;
            int portraitSprite = info.BigIconSprite;
            string portraitSource = string.IsNullOrEmpty(portraitFile) ? "BIGICON_MISSING_NO_FALLBACK" : "BIGICON";

            string titleSource;
            string title = ResolveBuildingTitleLikeOriginal(info, building, state.MdName, out titleSource);

            // Back/outer frame first.
            AddG16ImageOverpaintV140LikeOriginal("sp_building_portrait_box_original", "Interf3\\cropped", 19, baseX + 0, baseY + 36, 183, 244, 255, false, 64, false, false);
            AddG16ImageOverpaintV140LikeOriginal("sp_building_name_color_original", "Interf3\\cropped", 27, baseX + 0, baseY + 15, 179, 21, 255, false, 64, false, false);
            AddG16ImageOverpaintV140LikeOriginal("sp_building_only_inner_frame_original", "Interf3\\cropped", 33, rootX, rootY, 139, 245, 255, false, 84, false, false);

            // Building content.
            if (!string.IsNullOrEmpty(portraitFile))
                AddG16ImageOverpaintV140LikeOriginal("sp_building_bigicon_" + portraitSource, portraitFile, portraitSprite, rootX + 4, rootY + 4, 131, 150, 255, false, 170, false, false);
            AddG16ImageOverpaintV140LikeOriginal("sp_building_info_plate_original", "Interf3\\cropped", 34, rootX, rootY + 159, 139, 50, 255, false, 72, false, false);

            if (!state.Ready)
            {
                AddG16ImageOverpaintV140LikeOriginal("sp_building_stage_line_back_original", "Interf3\\BuildProgress", 0, rootX + 8, rootY + 183, 123, 16, 255, false, 64, false, false);
                float stageFill = Mathf.Clamp01(state.Stage / (float)Mathf.Max(1, state.StageMax));
                Image fill = AddG16Image("sp_building_stage_line_fill_original", "Interf3\\BuildProgress", 1, rootX + 8, rootY + 183, 123, 16, 255, false, false, false);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                fill.fillAmount = stageFill;
                AttachBuildingConstructionProgressUpdaterV136LikeOriginal(fill, null, building);
            }

            // Top/bottom decorative overlays.
            AddG16ImageOverpaintV140LikeOriginal("sp_building_portrait_bottom_original", "Interf3\\cropped", 21, baseX + 0, bottomY, 183, 26, 255, false, 64, false, false);

            AddG16ImageOverpaintV140LikeOriginal("sp_building_name_circle_original", "Interf3\\cropped", 23, baseX + 0, baseY + 13, 181, 23, 255, false, 64, false, false);

            if (selectedCount <= 1)
                AddG16ImageOverpaintV140LikeOriginal("sp_building_center_top_one_original", "Interf3\\cropped", 32, baseX + 13, topSingleY, 153, 16, 255, false, 64, false, false);
            else
            {
                AddG16ImageOverpaintV140LikeOriginal("sp_building_center_top_many_original", "Interf3\\cropped", 31, baseX + 29, topManyY, 123, 21, 255, false, 64, false, false);
                AddCrispLabelV140LikeOriginal("sp_building_selected_count", selectedCount.ToString(CultureInfo.InvariantCulture), baseX + 83, topManyY + 5, 17, 10, 9, TextAnchor.MiddleCenter, Color.white);
            }

            AddCrispLabelV140LikeOriginal("sp_building_title", title, baseX + 53, baseY + 21, 75, 11, 11, TextAnchor.MiddleCenter, OriginalHudTitleTextColorV141LikeOriginal());

            if (state.Ready)
            {
                AddCrispLabelV140LikeOriginal("sp_building_life_label", "\u0416\u0438\u0437\u043d\u0438:", rootX + 9, rootY + 168, 54, 13, 10, TextAnchor.MiddleLeft, Color.black);
                AddCrispLabelV140LikeOriginal("sp_building_life_value", state.Life.ToString(CultureInfo.InvariantCulture) + "/" + state.LifeMax.ToString(CultureInfo.InvariantCulture), rootX + 79, rootY + 169, 55, 11, 10, TextAnchor.MiddleRight, new Color(0.65f, 0.0f, 0.0f, 1.0f));
                AddCrispLabelV140LikeOriginal("sp_building_places_label", "\u0416\u0438\u043b\u044b\u0435 \u043c\u0435\u0441\u0442\u0430:", rootX + 9, rootY + 188, 87, 11, 10, TextAnchor.MiddleLeft, Color.black);
                AddCrispLabelV140LikeOriginal("sp_building_places_value", state.Places.ToString(CultureInfo.InvariantCulture), rootX + 111, rootY + 189, 22, 11, 10, TextAnchor.MiddleRight, new Color(0.65f, 0.0f, 0.0f, 1.0f));
                AddCrispLabelV140LikeOriginal("sp_building_population_label", "\u041d\u0430\u0441\u0435\u043b\u0435\u043d\u0438\u0435:", rootX + 34, rootY + 214, 71, 11, 10, TextAnchor.MiddleCenter, Color.black);
                AddCrispLabelV140LikeOriginal("sp_building_population_value", state.Population.ToString(CultureInfo.InvariantCulture) + "/" + state.PopulationMax.ToString(CultureInfo.InvariantCulture), rootX + 42, rootY + 229, 55, 11, 9, TextAnchor.MiddleCenter, new Color(0.65f, 0.0f, 0.0f, 1.0f));
            }
            else
            {
                AddCrispLabelV140LikeOriginal("sp_building_stage_label", "\u041f\u0440\u043e\u0433\u0440\u0435\u0441\u0441:", rootX + 9, rootY + 168, 56, 11, 10, TextAnchor.MiddleLeft, Color.black);
                Text stageValue = AddCrispLabelV140LikeOriginal("sp_building_stage_value", state.Stage.ToString(CultureInfo.InvariantCulture) + "/" + state.StageMax.ToString(CultureInfo.InvariantCulture), rootX + 101, rootY + 169, 33, 11, 10, TextAnchor.MiddleRight, new Color(0.65f, 0.0f, 0.0f, 1.0f));
                AttachBuildingConstructionProgressUpdaterV136LikeOriginal(null, stageValue, building);
            }
        }

        private string ResolveBuildingTitleLikeOriginal(C2OriginalProduceCatalogV13.C2MdIconInfoV13 info, C2SettlementBuildingSelectableV1LikeOriginal building, string resolvedMdName, out string source)
        {
            string mdListName = C2OriginalProduceCatalogV13.ResolveMdDisplayNameV141LikeOriginal(resolvedMdName);
            if (string.IsNullOrEmpty(mdListName) && info.Path != null)
                mdListName = C2OriginalProduceCatalogV13.ResolveMdDisplayNameV141LikeOriginal(info.Path);
            if (!string.IsNullOrEmpty(mdListName))
            {
                source = "TEXT_MDLIST_V141";
                return mdListName;
            }

            if (!string.IsNullOrWhiteSpace(info.MessageKey))
            {
                string resolved = C2OriginalProduceCatalogV13.ResolveUiTextLikeOriginal(info.MessageKey);
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    source = "MD_MESSAGE_LITERAL";
                    return resolved.Replace('_', ' ');
                }
                source = "MD_MESSAGE_RAW";
                return info.MessageKey.Replace('_', ' ');
            }

            if (!string.IsNullOrWhiteSpace(info.NameKey))
            {
                source = "MD_NAME";
                return info.NameKey.Replace('_', ' ');
            }

            source = "BUILDING_ID";
            return building != null ? C2OriginalProduceCatalogV13.StripNationSuffixPublicLikeOriginal(building.SourceMonsterId) : "Building";
        }

        private void BuildOriginalBuildingProducePanelLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, C2BuildingHudStateV113LikeOriginal state, int selectedCount)
        {
            if (!state.Ready || selectedCount != 1 || C2BuildingPlacementPreviewV27.C2BuildPlacementActiveLikeOriginal)
            {
                _lastProduceAudit = "hidden_by_original_SetProduce_filter ready=" + state.Ready +
                                    " selectedCount=" + selectedCount.ToString(CultureInfo.InvariantCulture) +
                                    " buildMode=" + C2BuildingPlacementPreviewV27.C2BuildPlacementActiveLikeOriginal;
                return;
            }

            string audit;
            List<C2OriginalProduceItemV13> items = C2OriginalProduceCatalogV13.BuildForSelectedBuilding(building, out audit);
            _lastProduceAudit = audit;

            if (items == null || items.Count == 0)
                return;

            // V127: warm produced-unit visual/motion banks after the menu opens, one unit type per frame.
            // Without this, the first finished unit of each type builds G16/G17 frame banks at spawn time,
            // which is the freeze seen when the unit appears.
            C2BuildingProductionCardsRuntimeV114.SchedulePrewarmForBuildingMenuV127LikeOriginal(building, items);
            for (int i = 0; i < items.Count; i++)
            {
                C2OriginalProduceItemV13 item = items[i];

                // V125: V124 accidentally shifted the whole building mini-card block by -1/-4.
                // That made the cards/icons sit too far left and too high compared to the original frame.
                // Use the same base coordinates as the normal unit produce panel; keep this building-only note
                // so the peasant/build-preview cards remain untouched.
                const int BuildingProduceOffsetX_V125 = 0;
                const int BuildingProduceOffsetY_V125 = 0;
                int x = OriginalUnitProduceBaseX + item.GridX * OriginalUnitProduceStepX + BuildingProduceOffsetX_V125;
                int y = OriginalUnitProduceBaseY + (item.GridY - 1) * OriginalUnitProduceStepY + BuildingProduceOffsetY_V125;

                // Same clone source as original and same code path as BuildOriginalProducePanelLikeOriginal(unit).
                AddG16ImageOverpaintV140LikeOriginal("produce_cell_back_" + i.ToString(CultureInfo.InvariantCulture), "Interf3\\FormInterface", item.RootSpriteId, x, y, OriginalUnitProduceWidth, OriginalUnitProduceHeight, 255, false, 56);

                // V130 BUILDINGS ONLY:
                // Unit mini portraits used by building produce cards are native 58x118 in the original resources,
                // while UnitProduce.GPPicture.Dialogs.xml declares the child slot as 56x118.
                // Drawing them through Unity Image.preserveAspect in a 56x118 rect shrinks/centers them and makes
                // the soldier look shifted inside the FormInterface frame. The original engine effectively draws
                // the native mini sprite at the card origin and lets the card frame hide the extra edge.
                // Keep peasant/building cards untouched; apply this only to unit mini icons in building menus.
                bool isBuildingUnitMiniIconV132 = !item.Building
                    && !string.IsNullOrEmpty(item.IconFileId)
                    && item.IconFileId.IndexOf("Units_", StringComparison.OrdinalIgnoreCase) >= 0;

                int iconX_V132 = x + OriginalUnitProduceIconX;
                int iconY_V132 = y + OriginalUnitProduceIconY;
                int iconW_V132 = OriginalUnitProduceIconW;
                int iconH_V132 = OriginalUnitProduceIconH;
                bool preserveIconAspect_V132 = true;

                if (isBuildingUnitMiniIconV132)
                {
                    iconX_V132 = x - 1;
                    // V132: V131 moved the right edge left and increased the gap; move the right edge 2 px to the opposite side.
                    iconY_V132 = y - 1;
                    iconW_V132 = 60;
                    iconH_V132 = 118;
                    preserveIconAspect_V132 = false;
                }

                AddG16ImageOverpaintV140LikeOriginal("produce_icon_" + i.ToString(CultureInfo.InvariantCulture), item.IconFileId, item.IconSpriteId,
                            iconX_V132, iconY_V132, iconW_V132, iconH_V132,
                            item.Enabled ? 255 : 128, false, item.Enabled ? 120 : 48, true, preserveIconAspect_V132);

                if (!item.Enabled)
                    AddSolid("produce_disabled_" + i.ToString(CultureInfo.InvariantCulture), new Color(0f, 0f, 0f, 0.48f), x, y, 57, 123, false);

                DrawBuildingProduceRuntimeOverlaysV124LikeOriginal(building, item, i, x, y);

                AddClickArea("produce_click_" + i.ToString(CultureInfo.InvariantCulture), x, y, OriginalUnitProduceWidth, OriginalUnitProduceHeight, item);
            }
        }

        private void DrawBuildingProduceRuntimeOverlaysV124LikeOriginal(
            C2SettlementBuildingSelectableV1LikeOriginal building,
            C2OriginalProduceItemV13 item,
            int slotIndex,
            int x,
            int y)
        {
            if (building == null || item == null || item.Building)
                return;

            C2BuildingProduceCardStateV114LikeOriginal cardState =
                C2BuildingProductionCardsRuntimeV114.GetCardStateLikeOriginal(building, item.UnitId);

            if (cardState.Count <= 0 && !cardState.Infinite)
                return;

            // V128:
            // Do NOT draw the full FormInterface sprite 23 over the whole card.
            // In our UI decoder that full sprite exposes its order plate at the top of the 123px texture,
            // so drawing it full-size produced the wrong "green cap" above some unit cards.
            // Original XML still places va_Unit_P_Amount at x=20 y=108; keep one automatic path for every card:
            // take the visible plate slice from sprite 23 and place it explicitly at the original bottom position.
            Image orderPlateV133 = AddG16ImageTopSliceV117LikeOriginal("building_produce_order_plate_" + slotIndex.ToString(CultureInfo.InvariantCulture),
                        "Interf3\\FormInterface",
                        23,
                        x,
                        y + 103,
                        57,
                        20,
                        255,
                        false);

            Text amountTextV133 = AddCrispLabelV140LikeOriginal("building_produce_amount_" + slotIndex.ToString(CultureInfo.InvariantCulture),
                         cardState.Count.ToString(CultureInfo.InvariantCulture),
                         x + 20,
                         y + 108,
                         21,
                         11,
                         9,
                         TextAnchor.MiddleCenter,
                         Color.white);
            Image orderPlateSecondPassV143A = FindHudImageV143ALikeOriginal(
                "building_produce_order_plate_" + slotIndex.ToString(CultureInfo.InvariantCulture) + "_v140a_doublepass");
            C2BuildingProduceAmountPlateUiV133LikeOriginal amountUiV133 = orderPlateV133.gameObject.AddComponent<C2BuildingProduceAmountPlateUiV133LikeOriginal>();
            amountUiV133.InitLikeOriginal(building, item.UnitId, orderPlateV133, orderPlateSecondPassV143A, amountTextV133);

            if (cardState.Infinite)
            {
                AddG16Image("building_produce_infinite_" + slotIndex.ToString(CultureInfo.InvariantCulture),
                            "Interf3\\FormInterface",
                            32,
                            x + 21,
                            y + 109,
                            20,
                            12,
                            255,
                            false);
            }

            // Original va_UnitProdStage: green 2px line x=60 y=7 h=109, growing bottom-up.
            // V125: do not rebuild the whole HUD every progress tick. Create one tiny UI line and let it
            // update its RectTransform in-place from the building production queue.
            {
                const int fullH = 109;
                Image progressImg = AddSolid("building_produce_progress_" + slotIndex.ToString(CultureInfo.InvariantCulture),
                         new Color(0.0f, 1.0f, 0.0f, 1.0f),
                         x + 60,
                         y + 7 + fullH,
                         2,
                         1,
                         false);
                Image progressSecondPassV143A = FindHudImageV143ALikeOriginal(
                    "building_produce_progress_" + slotIndex.ToString(CultureInfo.InvariantCulture) + "_v140a_doublepass");
                C2BuildingProduceProgressBarUiV125LikeOriginal bar =
                    progressImg.gameObject.AddComponent<C2BuildingProduceProgressBarUiV125LikeOriginal>();
                bar.InitLikeOriginal(building, item.UnitId, x + 60, y + 7, 2, fullH, progressSecondPassV143A);
            }
        }

        private void BuildOriginalBuildingUpgradePanelLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, C2BuildingHudStateV113LikeOriginal state, int selectedCount)
        {
            if (!state.Ready || selectedCount != 1)
            {
                return;
            }

            string audit;
            List<C2OriginalBuildingUpgradeItemV29> items = C2OriginalProduceCatalogV13.BuildUpgradesForSelectedBuildingLikeOriginal(building, out audit);
            if (items == null || items.Count == 0)
            {
                return;
            }
            for (int i = 0; i < items.Count; i++)
            {
                C2OriginalBuildingUpgradeItemV29 item = items[i];
                int x = OriginalBuildingUpgradeBaseX + item.GridX * OriginalBuildingUpgradeStep;
                int y = OriginalBuildingUpgradeBaseY + item.GridY * OriginalBuildingUpgradeStep;
                AddG16Image("building_upgrade_box_" + i.ToString(CultureInfo.InvariantCulture), "Interf3\\FormInterface", 18, x, y, OriginalBuildingUpgradeBox, OriginalBuildingUpgradeBox, 255, false);
                AddG16Image("building_upgrade_icon_" + i.ToString(CultureInfo.InvariantCulture), item.IconFileId, item.IconSpriteId, x + 5, y + 5, OriginalBuildingUpgradeIcon, OriginalBuildingUpgradeIcon, 255, false, false);
            }
        }


        private void DumpBuildingSpriteAuditLikeOriginal(C2SettlementBuildingSelectableV1LikeOriginal building, int selectedCount)
        {
            // V133: removed verbose building sprite audit logging.
        }
    }
}
