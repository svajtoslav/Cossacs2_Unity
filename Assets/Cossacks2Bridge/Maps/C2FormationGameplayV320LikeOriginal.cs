using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2GameplayHudV1
    {
        private int BuildOriginalFormationMiniComV320LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record,
            int activeGroupCount,
            int selPointCount,
            int baseX,
            int baseY,
            int renderedWeaponCards)
        {
            if (unit == null || record == null || record.Options == null || record.Options.Count == 0)
                return 0;
            if (!C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(unit))
                return 0;

            int stackShiftX = Mathf.Max(0, baseX - 182);
            int x0 = 248 + stackShiftX + Mathf.Max(0, renderedWeaponCards - 1) * 70;
            const int rootY = -47;
            int rendered = 0;

            string currentShape = C2FormationRuntimeV167LikeOriginal.CurrentShapeOfUnitV172LikeOriginal(unit);
            bool canFill;
            bool[] shotLines;
            C2FormationRuntimeV167LikeOriginal.TryGetMiniComStateV324LikeOriginal(
                unit, out canFill, out shotLines);
            HashSet<string> shapes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal> hotkeyOptions =
                new List<C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal>(3);
            // Original MiniCom takes the three common formations of the current
            // ord_groups.lst family. Infantry and cavalry use different families.
            bool cavalryFamily = false;
            bool cavalryFamily2 = false;
            for (int i = 0; i < record.Options.Count; i++)
            {
                string candidateShape = record.Options[i] != null ? record.Options[i].Shape : string.Empty;
                if (string.Equals(candidateShape, "SHER", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(candidateShape, "PRUS", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(candidateShape, "TRI", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(candidateShape, "SHER2", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(candidateShape, "PRUS2", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(candidateShape, "TRI2", StringComparison.OrdinalIgnoreCase))
                {
                    cavalryFamily = true;
                    if (candidateShape.EndsWith("2", StringComparison.OrdinalIgnoreCase))
                        cavalryFamily2 = true;
                }
            }
            string[] originalShapeOrder = cavalryFamily2
                ? new[] { "PRUS2", "SHER2", "TRI2" }
                : cavalryFamily
                    ? new[] { "PRUS", "SHER", "TRI" }
                    : new[] { "LINE", "SQUARE", "KARE" };
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal familySource =
                record.Options.Count > 0 ? record.Options[0] : null;
            int formRow = 0;
            for (int orderIndex = 0; orderIndex < originalShapeOrder.Length; orderIndex++)
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option = null;
                for (int i = 0; i < record.Options.Count; i++)
                {
                    C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal candidate = record.Options[i];
                    if (candidate != null &&
                        string.Equals(candidate.Shape, originalShapeOrder[orderIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        option = candidate;
                        break;
                    }
                }
                if (option == null && cavalryFamily)
                {
                    C2FormationCreateCatalogV165LikeOriginal.TryBuildSiblingFormationOptionLikeOriginal(
                        familySource,
                        originalShapeOrder[orderIndex],
                        out option);
                }
                if (option == null || !shapes.Add(option.Shape))
                    continue;

                bool active = string.Equals(currentShape, option.Shape, StringComparison.OrdinalIgnoreCase);
                AddFormationMiniComButtonV320LikeOriginal(
                    "formation_v320_shape_" + formRow.ToString(CultureInfo.InvariantCulture),
                    unit,
                    option,
                    C2FormationMiniComActionV320LikeOriginal.ChangeShape,
                    100,
                    option.Shape,
                    FormationShapeHintV320LikeOriginal(option.Shape),
                    Mathf.Max(0, option.IconSprite),
                    x0,
                    rootY + 640 + 39 * formRow,
                    true,
                    !active);
                hotkeyOptions.Add(option);
                formRow++;
                rendered++;
            }

            AddFormationMiniComButtonV320LikeOriginal(
                "formation_v324_stop", unit, null, C2FormationMiniComActionV320LikeOriginal.Stop,
                0, string.Empty, "Остановить отряд", 11,
                x0, rootY + 586, true, false);
            AddFormationMiniComButtonV320LikeOriginal(
                "formation_v324_fill", unit, null, C2FormationMiniComActionV320LikeOriginal.Fill,
                0, string.Empty, "Пополнить отряд", 6,
                x0 + 39, rootY + 586, canFill, !canFill);

            // These are rifle line-fire switches from SETATTSTATE_Pro, not
            // generic brigade controls.  Cavalry PRUS/SHER/TRI families and
            // melee-only units do not get them in the original MiniCom.
            bool threeShotLinesV405A =
                C2FormationRuntimeV167LikeOriginal.HasThreeShotLinesV405ALikeOriginal(unit);
            if (!cavalryFamily && renderedWeaponCards > 1 && threeShotLinesV405A)
            {
            int[] shotSprites = { 2, 1, 0 };
            string[] shotHints =
            {
                "Разрешить или запретить огонь первой линии",
                "Разрешить или запретить огонь второй линии",
                "Разрешить или запретить огонь третьей линии"
            };
            for (int line = 0; line < 3; line++)
            {
                bool lineActive = shotLines != null && line < shotLines.Length && shotLines[line];
                AddFormationMiniComButtonV320LikeOriginal(
                    "formation_v324_shot_line_" + line.ToString(CultureInfo.InvariantCulture),
                    unit, null, C2FormationMiniComActionV320LikeOriginal.LineShot,
                    line, string.Empty, shotHints[line], shotSprites[line],
                    x0 + 39, rootY + 640 + 39 * line, true, !lineActive);
                rendered++;
            }
            }
            AddFormationMiniComButtonV320LikeOriginal(
                "formation_v324_disband", unit, null, C2FormationMiniComActionV320LikeOriginal.Disband,
                0, string.Empty, "Распустить отряд", 7,
                x0 + 38, rootY + 772, true, false);
            rendered += 3;

            if (_canvas != null)
            {
                C2FormationMiniComHotkeysV320LikeOriginal hotkeys =
                    _canvas.GetComponent<C2FormationMiniComHotkeysV320LikeOriginal>();
                if (hotkeys == null)
                    hotkeys = _canvas.gameObject.AddComponent<C2FormationMiniComHotkeysV320LikeOriginal>();
                hotkeys.Bind(this, unit, hotkeyOptions);
            }

            return rendered;
        }

        private void AddFormationMiniComButtonV320LikeOriginal(
            string name,
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            C2FormationMiniComActionV320LikeOriginal action,
            int spacingPercent,
            string label,
            string hint,
            int sprite,
            int x,
            int y,
            bool active)
        {
            if (string.Equals(name, "formation_v320_fill", StringComparison.Ordinal) ||
                string.Equals(name, "formation_v320_disband", StringComparison.Ordinal))
                return;

            AddFormationMiniComButtonV320LikeOriginal(
                name, unit, option, action, spacingPercent, label, hint, sprite,
                x, y, true, !active);
        }

        private void AddFormationMiniComButtonV320LikeOriginal(
            string name,
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            C2FormationMiniComActionV320LikeOriginal action,
            int spacingPercent,
            string label,
            string hint,
            int sprite,
            int x,
            int y,
            bool enabled,
            bool disabledOverlay)
        {
            Image image = AddG16Image(
                name, "Interf3\\f_icons", Mathf.Max(0, sprite),
                x, y, 36, 36, enabled ? 255 : 128, enabled, false, false);
            if (image == null)
                return;

            C2HudFormationMiniComRelayV320LikeOriginal relay =
                image.gameObject.AddComponent<C2HudFormationMiniComRelayV320LikeOriginal>();
            relay.Owner = this;
            relay.Unit = unit;
            relay.Option = option;
            relay.Action = action;
            relay.SpacingPercent = spacingPercent;
            relay.Enabled = enabled;

            if (disabledOverlay)
                AddG16Image(name + "_state16", "Interf3\\f_icons", 16, x + 1, y + 1, 34, 34, 255, false, false, false);

            C2OriginalProduceItemV13 tooltipItem = new C2OriginalProduceItemV13();
            tooltipItem.DisplayText = hint ?? label ?? string.Empty;
            tooltipItem.Source = "MiniCom_original_formations_v320";
            C2HudTooltipRelayV13I tooltip = image.gameObject.AddComponent<C2HudTooltipRelayV13I>();
            tooltip.Owner = this;
            tooltip.Item = tooltipItem;
        }

        private static int FormationShapeSpriteV320LikeOriginal(string shape)
        {
            // Data/Nres.dat [ORDERICONS] is the authoritative OrderDesc.IconID table.
            if (string.Equals(shape, "LINE", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(shape, "SQUARE", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(shape, "KARE", StringComparison.OrdinalIgnoreCase)) return 23;
            if (string.Equals(shape, "PRUS2", StringComparison.OrdinalIgnoreCase)) return 20;
            if (string.Equals(shape, "SHER2", StringComparison.OrdinalIgnoreCase)) return 21;
            if (string.Equals(shape, "TRI2", StringComparison.OrdinalIgnoreCase)) return 22;
            return 0;
        }

        private static string FormationShapeHintV320LikeOriginal(string shape)
        {
            if (string.Equals(shape, "LINE", StringComparison.OrdinalIgnoreCase))
                return "Линия: боевой строй и линейная стрельба";
            if (string.Equals(shape, "SQUARE", StringComparison.OrdinalIgnoreCase))
                return "Колонна: марш и движение по дорогам";
            if (string.Equals(shape, "KARE", StringComparison.OrdinalIgnoreCase))
                return "Каре: защита от кавалерии, глобальное движение запрещено";
            if (string.Equals(shape, "SHER", StringComparison.OrdinalIgnoreCase))
                return "Кавалерийский маршевый строй";
            if (string.Equals(shape, "PRUS", StringComparison.OrdinalIgnoreCase))
                return "Развёрнутый кавалерийский строй";
            if (string.Equals(shape, "TRI", StringComparison.OrdinalIgnoreCase))
                return "Кавалерийский клин";
            return shape ?? string.Empty;
        }

        internal void ExecuteFormationMiniComActionV320LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            C2FormationMiniComActionV320LikeOriginal action,
            int spacingPercent)
        {
            string audit;
            bool ok;
            switch (action)
            {
                case C2FormationMiniComActionV320LikeOriginal.Stop:
                    ok = C2FormationRuntimeV167LikeOriginal.TryStopFormationV320LikeOriginal(unit, out audit);
                    break;
                case C2FormationMiniComActionV320LikeOriginal.ChangeShape:
                    ok = C2FormationRuntimeV167LikeOriginal.TryChangeFormationV320LikeOriginal(unit, option, out audit);
                    break;
                case C2FormationMiniComActionV320LikeOriginal.Fill:
                    ok = C2FormationRuntimeV167LikeOriginal.TryFillFormationV320LikeOriginal(unit, out audit);
                    break;
                case C2FormationMiniComActionV320LikeOriginal.Disband:
                    ok = C2FormationRuntimeV167LikeOriginal.TryDisbandFormationV320LikeOriginal(unit, out audit);
                    break;
                case C2FormationMiniComActionV320LikeOriginal.Spacing:
                    ok = C2FormationRuntimeV167LikeOriginal.TrySetFormationSpacingV320LikeOriginal(unit, spacingPercent, out audit);
                    break;
                case C2FormationMiniComActionV320LikeOriginal.LineShot:
                    ok = C2FormationRuntimeV167LikeOriginal.TryToggleShotLineV324LikeOriginal(
                        unit, spacingPercent, out audit);
                    break;
                default:
                    ok = false;
                    audit = "unknown_action";
                    break;
            }

            C2BuildingProductionCardsRuntimeV114.SuppressMapSelectionFromHudClickV126LikeOriginal();
            _lastSelectedCount = -999999;
            _lastUnitSelPointStateKeyV137LikeOriginal = string.Empty;
            _nextRefresh = 0.0f;
            Debug.Log("[C2:FORMATION MINICOM V320] action=" + action.ToString() +
                      " ok=" + ok.ToString() + " " + (audit ?? string.Empty));
        }
    }

    internal enum C2FormationMiniComActionV320LikeOriginal
    {
        Stop,
        ChangeShape,
        Fill,
        Disband,
        Spacing,
        LineShot
    }

    internal sealed class C2HudFormationMiniComRelayV320LikeOriginal : MonoBehaviour, IPointerClickHandler
    {
        public C2GameplayHudV1 Owner;
        public C2NeutralPeasantUnitInfoV2LikeOriginal Unit;
        public C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal Option;
        public C2FormationMiniComActionV320LikeOriginal Action;
        public int SpacingPercent = 100;
        public bool Enabled = true;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Enabled || Owner == null || eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;
            Owner.ExecuteFormationMiniComActionV320LikeOriginal(Unit, Option, Action, SpacingPercent);
        }
    }

    internal sealed class C2FormationMiniComHotkeysV320LikeOriginal : MonoBehaviour
    {
        private C2GameplayHudV1 _owner;
        private C2NeutralPeasantUnitInfoV2LikeOriginal _unit;
        private readonly List<C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal> _options =
            new List<C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal>(3);

        public void Bind(
            C2GameplayHudV1 owner,
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            IList<C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal> options)
        {
            _owner = owner;
            _unit = unit;
            _options.Clear();
            for (int i = 0; options != null && i < options.Count && i < 3; i++)
                if (options[i] != null) _options.Add(options[i]);
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (_owner == null || _unit == null ||
                !C2FormationRuntimeV167LikeOriginal.IsUnitInRuntimeFormationV168LikeOriginal(_unit))
                return;

            if (Input.GetKeyDown(KeyCode.F))
                _owner.ExecuteFormationMiniComActionV320LikeOriginal(
                    _unit, null, C2FormationMiniComActionV320LikeOriginal.Fill, 0);
            else if (Input.GetKeyDown(KeyCode.Escape))
                _owner.ExecuteFormationMiniComActionV320LikeOriginal(
                    _unit, null, C2FormationMiniComActionV320LikeOriginal.Stop, 0);
            else if (Input.GetKeyDown(KeyCode.W))
                _owner.ExecuteFormationMiniComActionV320LikeOriginal(
                    _unit, null, C2FormationMiniComActionV320LikeOriginal.LineShot, 0);
            else if (Input.GetKeyDown(KeyCode.S))
                _owner.ExecuteFormationMiniComActionV320LikeOriginal(
                    _unit, null, C2FormationMiniComActionV320LikeOriginal.LineShot, 1);
            else if (Input.GetKeyDown(KeyCode.X))
                _owner.ExecuteFormationMiniComActionV320LikeOriginal(
                    _unit, null, C2FormationMiniComActionV320LikeOriginal.LineShot, 2);
            else if (Input.GetKeyDown(KeyCode.E) && _options.Count > 0)
                _owner.ExecuteFormationMiniComActionV320LikeOriginal(
                    _unit, _options[0], C2FormationMiniComActionV320LikeOriginal.ChangeShape, 100);
            else if (Input.GetKeyDown(KeyCode.D) && _options.Count > 1)
                _owner.ExecuteFormationMiniComActionV320LikeOriginal(
                    _unit, _options[1], C2FormationMiniComActionV320LikeOriginal.ChangeShape, 100);
            else if (Input.GetKeyDown(KeyCode.C) && _options.Count > 2)
                _owner.ExecuteFormationMiniComActionV320LikeOriginal(
                    _unit, _options[2], C2FormationMiniComActionV320LikeOriginal.ChangeShape, 100);
#endif
        }
    }

    internal static partial class C2FormationRuntimeV167LikeOriginal
    {
        public static bool TryGetFormationRecordV320LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal selected,
            out C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record)
        {
            record = null;
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(selected, out group) || group == null)
                return false;
            record = ResolveRecordForGroupV320LikeOriginal(group);
            return record != null && record.Options != null && record.Options.Count > 0;
        }

        public static bool TryStopFormationV320LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal selected,
            out string audit)
        {
            audit = "not_runtime_formation";
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(selected, out group) || group == null)
                return false;

            CancelBrigadeGoOnRoadV385ALikeOriginal(group.GroupId, "formation_stop", false);
            int stopped = 0;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true))
                    continue;
                unit.StopMoveAndFaceDirectionLikeOriginal(group.Direction);
                stopped++;
            }

            audit = "ok group=" + group.GroupId.ToString(CultureInfo.InvariantCulture) +
                    " stopped=" + stopped.ToString(CultureInfo.InvariantCulture);
            return stopped > 0;
        }

        public static bool TryChangeFormationV320LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal selected,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            out string audit)
        {
            audit = "not_runtime_formation";
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(selected, out group) || group == null)
                return false;
            if (option == null)
            {
                audit = "no_formation_option";
                return false;
            }

            return ApplyFormationOptionV320LikeOriginal(group, option, group.SpacingPercent, "manual_reformation", out audit);
        }

        public static bool TrySetFormationSpacingV320LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal selected,
            int spacingPercent,
            out string audit)
        {
            audit = "not_runtime_formation";
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(selected, out group) || group == null)
                return false;

            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record =
                ResolveRecordForGroupV320LikeOriginal(group);
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option =
                FindFormationOptionV320LikeOriginal(record, group.Shape);
            if (option == null)
            {
                audit = "no_current_shape_option shape='" + (group.Shape ?? string.Empty) + "'";
                return false;
            }

            spacingPercent = Mathf.Clamp(spacingPercent, 50, 200);
            group.SpacingPercent = spacingPercent;
            return ApplyFormationOptionV320LikeOriginal(group, option, spacingPercent, "formation_spacing", out audit);
        }

        public static bool TryDisbandFormationV320LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal selected,
            out string audit)
        {
            audit = "not_runtime_formation";
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(selected, out group) || group == null)
                return false;

            int groupId = group.GroupId;
            CancelBrigadeGoOnRoadV385ALikeOriginal(groupId, "formation_disband", false);
            ForgetBrigadeStandGroundV403LikeOriginal(group);
            int released = 0;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (unit == null)
                    continue;
                RemoveUnitMembershipV172LikeOriginal(unit);
                if (IsUsableFormationUnitV172LikeOriginal(unit, true))
                    unit.StopMoveAndFaceDirectionLikeOriginal(group.Direction);
                released++;
            }
            _groupsByIdV172LikeOriginal.Remove(groupId);

            audit = "ok group=" + groupId.ToString(CultureInfo.InvariantCulture) +
                    " released=" + released.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        public static bool TryFillFormationV320LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal selected,
            out string audit)
        {
            audit = "not_runtime_formation";
            RuntimeFormationV172LikeOriginal group;
            if (!TryGetRuntimeGroupByUnitV172LikeOriginal(selected, out group) || group == null)
                return false;

            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record =
                ResolveRecordForGroupV320LikeOriginal(group);
            if (record == null)
            {
                audit = "no_formation_record";
                return false;
            }

            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option =
                FindFormationOptionV320LikeOriginal(record, group.Shape);
            if (option == null)
                option = record.Options.Count > 0 ? record.Options[0] : null;
            if (option == null)
            {
                audit = "no_formation_option";
                return false;
            }

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> alive =
                GetAliveGroupUnitsV320LikeOriginal(group);
            float centerX;
            float centerY;
            ComputeActualUnitCenterV320LikeOriginal(alive, out centerX, out centerY);

            bool creatorIsAlsoSoldier =
                string.Equals(record.OfficerId ?? string.Empty, record.UnitId ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrEmpty(record.DrummerId) &&
                string.IsNullOrEmpty(record.FlagId);

            C2NeutralPeasantUnitInfoV2LikeOriginal officer = null;
            C2NeutralPeasantUnitInfoV2LikeOriginal drummer = null;
            C2NeutralPeasantUnitInfoV2LikeOriginal flag = null;
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> soldiers =
                new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(Mathf.Max(1, option.UnitCount));

            for (int i = 0; i < alive.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = alive[i];
                if (!creatorIsAlsoSoldier && officer == null && MatchesFormationMemberV172LikeOriginal(unit, record.OfficerId))
                    officer = unit;
                else if (drummer == null && MatchesFormationMemberV172LikeOriginal(unit, record.DrummerId))
                    drummer = unit;
                else if (flag == null && MatchesFormationMemberV172LikeOriginal(unit, record.FlagId))
                    flag = unit;
                else
                    soldiers.Add(unit);
            }

            const float radiusReal = 800.0f * 16.0f;
            const float radius2 = radiusReal * radiusReal;
            C2NeutralPeasantUnitInfoV2LikeOriginal[] all =
                C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> free =
                new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            for (int i = 0; all != null && i < all.Length; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = all[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, false) || unit.Nation != group.Nation)
                    continue;
                float ux = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
                float uy = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
                float dx = ux - centerX;
                float dy = uy - centerY;
                if (dx * dx + dy * dy <= radius2)
                    free.Add(unit);
            }
            free.Sort(delegate(C2NeutralPeasantUnitInfoV2LikeOriginal a, C2NeutralPeasantUnitInfoV2LikeOriginal b)
            {
                return DistanceToCenterSquaredV320LikeOriginal(a, centerX, centerY)
                    .CompareTo(DistanceToCenterSquaredV320LikeOriginal(b, centerX, centerY));
            });

            int added = 0;
            for (int i = 0; i < free.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = free[i];
                if (!creatorIsAlsoSoldier && officer == null && MatchesFormationMemberV172LikeOriginal(unit, record.OfficerId))
                {
                    officer = unit;
                    added++;
                    continue;
                }
                if (drummer == null && MatchesFormationMemberV172LikeOriginal(unit, record.DrummerId))
                {
                    drummer = unit;
                    added++;
                    continue;
                }
                if (flag == null && MatchesFormationMemberV172LikeOriginal(unit, record.FlagId))
                {
                    flag = unit;
                    added++;
                    continue;
                }
                if (soldiers.Count < option.UnitCount && MatchesFormationMemberV172LikeOriginal(unit, record.UnitId))
                {
                    soldiers.Add(unit);
                    added++;
                }
            }

            if (added == 0)
            {
                audit = "no_free_members_in_radius radius=800 soldiers=" +
                        soldiers.Count.ToString(CultureInfo.InvariantCulture) + "/" +
                        option.UnitCount.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            List<C2NeutralPeasantUnitInfoV2LikeOriginal> ordered =
                new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(option.UnitCount + 3);
            if (!creatorIsAlsoSoldier) AddUniqueUnitV172LikeOriginal(ordered, officer);
            AddUniqueUnitV172LikeOriginal(ordered, drummer);
            AddUniqueUnitV172LikeOriginal(ordered, flag);
            int commandCount = ordered.Count;
            for (int i = 0; i < soldiers.Count && i < option.UnitCount; i++)
                AddUniqueUnitV172LikeOriginal(ordered, soldiers[i]);

            bool applied = ApplyFormationOptionV320LikeOriginal(
                group,
                option,
                group.SpacingPercent,
                "fill_formation",
                ordered,
                commandCount,
                centerX,
                centerY,
                out audit);
            audit += " added=" + added.ToString(CultureInfo.InvariantCulture);
            return applied;
        }

        private static bool ApplyFormationOptionV320LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            int spacingPercent,
            string source,
            out string audit)
        {
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> units = GetAliveGroupUnitsV320LikeOriginal(group);
            float centerX;
            float centerY;
            ComputeActualUnitCenterV320LikeOriginal(units, out centerX, out centerY);
            int commandCount = ResolveCommandPrefixCountV320LikeOriginal(group, units);
            return ApplyFormationOptionV320LikeOriginal(
                group, option, spacingPercent, source, units, commandCount, centerX, centerY, out audit);
        }

        private static bool ApplyFormationOptionV320LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            int spacingPercent,
            string source,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            int commandCount,
            float centerX,
            float centerY,
            out string audit)
        {
            audit = "invalid_formation";
            if (group == null || option == null || units == null || units.Count == 0)
                return false;

            CancelBrigadeGoOnRoadV385ALikeOriginal(
                group.GroupId, source ?? "formation_rebuild", false);

            List<Vector2> slots = BuildTemplateSlotsDirectedV352LikeOriginal(
                option, units.Count, commandCount, units,
                centerX, centerY, spacingPercent, group.Direction);
            ReorderSoldiersForNearestSlotsV172LikeOriginal(units, slots, commandCount);

            int groupId = group.GroupId;
            int nation = group.Nation;
            string soldierMemberId = !string.IsNullOrEmpty(group.SoldierMemberId)
                ? group.SoldierMemberId
                : string.Empty;
            byte direction = group.Direction;
            RegisterFormationInternalV172LikeOriginal(
                units, slots, option.Shape, soldierMemberId, groupId);
            RuntimeFormationV172LikeOriginal updated;
            if (_groupsByIdV172LikeOriginal.TryGetValue(groupId, out updated) && updated != null)
            {
                updated.Nation = nation;
                updated.Direction = direction;
                updated.SpacingPercent = Mathf.Clamp(spacingPercent, 50, 200);
            }

            int issued = 0;
            bool kareOrder =
                string.Equals(option.Shape, "KARE", StringComparison.OrdinalIgnoreCase);
            for (int i = 0; i < units.Count && i < slots.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = units[i];
                if (!IsUsableFormationUnitV172LikeOriginal(unit, true))
                    continue;
                C2BattleTerrainMode.C2BuildRuntimeCancelWorkerOrderForUnitLikeOriginal(
                    unit, source ?? "formation_rebuild");

                // V403D / exact CII ordering rule:
                // ORDUSAGE==2 (KARE) is NOT rotated outward while the formation is
                // merely created or reformed. Multi.cpp::MakeStandGround performs
                // that radial rotation only when BrigDelay reaches zero and the
                // brigade enters full InStandGround. Likewise orders.lst '@'
                // positions do not enter NewState=5 here; NewMon.cpp does that only
                // after BR->InStandGround becomes true.
                unit.SetFormationAssemblyDestinationRealLikeOriginal(
                    slots[i].x,
                    slots[i].y,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    direction);
                issued++;
            }

            RuntimeFormationV172LikeOriginal standGroundUpdatedV403LikeOriginal;
            if (_groupsByIdV172LikeOriginal.TryGetValue(groupId, out standGroundUpdatedV403LikeOriginal) &&
                standGroundUpdatedV403LikeOriginal != null)
            {
                AfterReformationV403LikeOriginal(
                    standGroundUpdatedV403LikeOriginal, source ?? "Multi.cpp::MakeReformation");
            }

            audit = "ok group=" + groupId.ToString(CultureInfo.InvariantCulture) +
                    " shape='" + (option.Shape ?? string.Empty) + "'" +
                    " spacing=" + spacingPercent.ToString(CultureInfo.InvariantCulture) +
                    " kareOrder=" + kareOrder.ToString() +
                    " units=" + issued.ToString(CultureInfo.InvariantCulture) +
                    " source=" + (source ?? string.Empty);
            return issued > 0;
        }

        private static List<int> BuildTemplateSlotOptionsV327LikeOriginal(
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            int count,
            int commandUnitCount)
        {
            List<int> options = new List<int>(Mathf.Max(1, count));
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal tpl =
                option != null ? option.OrderTemplate : null;
            if (tpl != null)
            {
                int actualCommandSlots = Mathf.Min(
                    Mathf.Max(0, commandUnitCount),
                    tpl.CommandPoints.Count);
                for (int i = 0; i < actualCommandSlots && options.Count < count; i++)
                    options.Add(0);
                for (int i = 0; i < tpl.SoldierPoints.Count && options.Count < count; i++)
                    options.Add(tpl.SoldierPoints[i].Option);
            }
            while (options.Count < count)
                options.Add(0);
            return options;
        }

        private static void ScaleAndRotateSlotsV320LikeOriginal(
            List<Vector2> slots,
            float centerX,
            float centerY,
            int spacingPercent,
            byte direction)
        {
            if (slots == null)
                return;
            int spacing = Mathf.Clamp(spacingPercent, 50, 200);
            int cos = C2OriginalMovementMathV352.TCos[direction];
            int sin = C2OriginalMovementMathV352.TSin[direction];
            for (int i = 0; i < slots.Count; i++)
            {
                float dx = (slots[i].x - centerX) * spacing / 100.0f;
                float dy = (slots[i].y - centerY) * spacing / 100.0f;
                slots[i] = new Vector2(
                    centerX + (dx * cos - dy * sin) / 256.0f,
                    centerY + (dx * sin + dy * cos) / 256.0f);
            }
        }

        private static int OScaleV352LikeOriginal(int value)
        {
            // Brigade.cpp: int OScale(int x){ return (x*FORMDIST)>>8; }, FORMDIST=270 in COSSACKS2.
            return (value * 270) >> 8;
        }

        internal static List<Vector2> BuildEditorFormationSlotsV375LikeOriginal(
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            int commandCount, int formationDistanceScale, int centerX, int centerY)
        {
            if (option == null || option.OrderTemplate == null || option.UnitCount <= 0 ||
                commandCount < 0)
                return null;
            // The editor creates the same indexed layout that the first dir=0 order
            // will address. Command members form an explicit prefix, including cavalry.
            return BuildTemplateSlotsWithMdScaleV375LikeOriginal(option,
                option.UnitCount + commandCount, commandCount, null,
                centerX, centerY, 100, 0, formationDistanceScale);
        }

        private static List<Vector2> BuildTemplateSlotsDirectedV352LikeOriginal(
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            int count, int commandUnitCount, IList<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            float centerRealX, float centerRealY, int spacingPercent, byte direction)
        {
            return BuildTemplateSlotsWithMdScaleV375LikeOriginal(option, count, commandUnitCount,
                units, centerRealX, centerRealY, spacingPercent, direction, 0);
        }

        private static List<Vector2> BuildTemplateSlotsWithMdScaleV375LikeOriginal(
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option,
            int count,
            int commandUnitCount,
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            float centerRealX,
            float centerRealY,
            int spacingPercent,
            byte direction,
            int formationDistanceScaleOverride)
        {
            List<Vector2> slots = new List<Vector2>(Mathf.Max(1, count));
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOrderTemplateV165LikeOriginal tpl =
                option != null ? option.OrderTemplate : null;
            if (tpl == null)
                return BuildFallbackSlotsV172LikeOriginal(count, centerRealX, centerRealY);

            int cx = Mathf.RoundToInt(centerRealX);
            int cy = Mathf.RoundToInt(centerRealY);

            // Brigade.cpp::FindCommandPlace.  Command points are centered in Nature.cpp
            // together with the soldier lines; each command member takes the nearest unused
            // C point to its current RealX/RealY.
            // FindCommandPlace leaves the requested centre unchanged when no C
            // point exists. Keep the command prefix even in LINE60MENTCOS (NCom=0).
            int actualCommandSlots = Mathf.Min(Mathf.Max(0, commandUnitCount), count);
            bool[] commandUsed = new bool[tpl.CommandPoints.Count];
            int sinNamedLikeSource = C2OriginalMovementMathV352.TCos[direction];
            int cosNamedLikeSource = C2OriginalMovementMathV352.TSin[direction];
            for (int ci = 0; ci < actualCommandSlots; ci++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = units != null && ci < units.Count ? units[ci] : null;
                int ux = unit != null ? Mathf.RoundToInt(unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX) : cx;
                int uy = unit != null ? Mathf.RoundToInt(unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY) : cy;
                int best = -1;
                int bestR = 100000000;
                int bestX = cx;
                int bestY = cy;
                for (int pi = 0; pi < tpl.CommandPoints.Count; pi++)
                {
                    if (commandUsed[pi]) continue;
                    C2FormationCreateCatalogV165LikeOriginal.C2FormationPointV165LikeOriginal pt = tpl.CommandPoints[pi];
                    int xc = pt.X;
                    int yc = pt.Y;
                    int xci = cx + OScaleV352LikeOriginal(
                        OScaleV352LikeOriginal(-xc * cosNamedLikeSource + yc * sinNamedLikeSource)) / 2;
                    int yci = cy + OScaleV352LikeOriginal(
                        OScaleV352LikeOriginal(yc * cosNamedLikeSource + xc * sinNamedLikeSource)) / 2;
                    int rr = C2OriginalMovementMathV352.Norma(ux - xci, uy - yci);
                    if (rr < bestR)
                    {
                        bestR = rr;
                        best = pi;
                        bestX = xci;
                        bestY = yci;
                    }
                }
                if (best >= 0) commandUsed[best] = true;
                slots.Add(new Vector2((bestX >> 4) << 4, (bestY >> 4) << 4));
            }

            // Groups.cpp::PositionOrder::CreateSimpleOrdPos.
            int dx = C2OriginalMovementMathV352.TCos[direction];
            int dy = C2OriginalMovementMathV352.TSin[direction];
            int maxR = 270 << 2;
            int nr = C2OriginalMovementMathV352.Norma(dx, dy);
            if (nr <= 0) nr = 1;
            int vx = (dx * maxR) / nr;
            int vy = (dy * maxR) / nr;

            int formationDistanceScale = formationDistanceScaleOverride > 0 ? formationDistanceScaleOverride : 100;
            int firstSoldier = Mathf.Clamp(actualCommandSlots, 0, units != null ? units.Count : 0);
            for (int ui = firstSoldier; formationDistanceScaleOverride <= 0 && units != null && ui < units.Count; ui++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = units[ui];
                if (unit == null) continue;
                C2UnitOriginalRuntimeLinkLikeOriginal link = unit.RuntimeLinkCachedLikeOriginal;
                if (link != null && link.Runtime != null && link.Runtime.Md != null)
                    formationDistanceScale = Mathf.Max(1, link.Runtime.Md.FormationDistanceScale);
                break;
            }
            // Brigade::ScaleFactor is 0 for normal spacing.  The Unity runtime stores
            // the user's 50/100/200 formation-scale choice as SpacingPercent; 100 is
            // therefore the source no-op ScaleFactor case.
            int bScale = (formationDistanceScale * Mathf.Clamp(spacingPercent, 50, 200)) / 100;

            for (int i = 0; i < tpl.SoldierPoints.Count && slots.Count < count; i++)
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationPointV165LikeOriginal pt = tpl.SoldierPoints[i];
                // Nature.cpp centers sx and YShift.  Parser invariant:
                // pt.X == sx, pt.Y == (2*iy - YShift).
                int termX = ((pt.X * vy - pt.Y * vx) * bScale) / 800;
                int termY = ((pt.X * vx + pt.Y * vy) * bScale) / 800;
                int px = cx - OScaleV352LikeOriginal(termX);
                int py = cy + OScaleV352LikeOriginal(termY);
                // Brigade::CreateSimpleOrderedPositions stores PORD.px/py >> 4;
                // ShowPositions draws the same integer-pixel destinations. The
                // Unity bridge carries them in Real units, so restore the x16 scale.
                slots.Add(new Vector2((px >> 4) << 4, (py >> 4) << 4));
            }

            if (slots.Count < count)
            {
                List<Vector2> fallback = BuildFallbackSlotsV172LikeOriginal(count, centerRealX, centerRealY);
                for (int i = slots.Count; i < count; i++)
                    slots.Add(fallback[Mathf.Min(i, fallback.Count - 1)]);
            }
            return slots;
        }

        private static C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal
            ResolveRecordForGroupV320LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            if (group == null)
                return null;
            List<C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal> records =
                C2FormationCreateCatalogV165LikeOriginal.BuildAllRecordsSnapshotLikeOriginal();
            for (int i = 0; records != null && i < records.Count; i++)
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record = records[i];
                if (record != null &&
                    string.Equals(record.UnitId ?? string.Empty, group.SoldierMemberId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    return record;
            }
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record;
                if (C2FormationCreateCatalogV165LikeOriginal.TryResolveForSelectedUnit(group.Units[i], out record))
                    return record;
            }
            return null;
        }

        private static C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal
            FindFormationOptionV320LikeOriginal(
                C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record,
                string shape)
        {
            if (record == null || record.Options == null)
                return null;
            for (int i = 0; i < record.Options.Count; i++)
            {
                C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option = record.Options[i];
                if (option != null && string.Equals(option.Shape, shape, StringComparison.OrdinalIgnoreCase))
                    return option;
            }
            return null;
        }

        private static List<C2NeutralPeasantUnitInfoV2LikeOriginal>
            GetAliveGroupUnitsV320LikeOriginal(RuntimeFormationV172LikeOriginal group)
        {
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> result =
                new List<C2NeutralPeasantUnitInfoV2LikeOriginal>();
            if (group == null)
                return result;
            for (int i = 0; i < group.Units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = group.Units[i];
                if (IsUsableFormationUnitV172LikeOriginal(unit, true))
                    result.Add(unit);
            }
            return result;
        }

        private static void ComputeActualUnitCenterV320LikeOriginal(
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> units,
            out float centerX,
            out float centerY)
        {
            centerX = 0.0f;
            centerY = 0.0f;
            int count = 0;
            for (int i = 0; units != null && i < units.Count; i++)
            {
                C2NeutralPeasantUnitInfoV2LikeOriginal unit = units[i];
                if (unit == null)
                    continue;
                centerX += unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
                centerY += unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
                count++;
            }
            if (count > 0)
            {
                centerX /= count;
                centerY /= count;
            }
        }

        private static int ResolveCommandPrefixCountV320LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            IList<C2NeutralPeasantUnitInfoV2LikeOriginal> units)
        {
            if (group == null || units == null)
                return 0;
            int count = 0;
            for (int i = 0; i < units.Count && i < 3; i++)
            {
                string member = C2FormationCreateCatalogV165LikeOriginal.ResolveMemberIdForSelectedUnitLikeOriginal(units[i]);
                if (string.Equals(member, group.SoldierMemberId, StringComparison.OrdinalIgnoreCase))
                    break;
                count++;
            }
            return count;
        }

        private static float DistanceToCenterSquaredV320LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            float centerX,
            float centerY)
        {
            if (unit == null)
                return float.MaxValue;
            float x = unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX;
            float y = unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY;
            float dx = x - centerX;
            float dy = y - centerY;
            return dx * dx + dy * dy;
        }

        private static byte DirectionFromDeltaV320LikeOriginal(
            float dx,
            float dy,
            byte fallback)
        {
            if (dx * dx + dy * dy < 1.0f)
                return fallback;
            return C2OriginalMovementMathV352.GetDir(
                Mathf.RoundToInt(dx), Mathf.RoundToInt(dy));
        }

        private static void RotateSlotsFromDirectionV320LikeOriginal(
            List<Vector2> slots,
            float centerX,
            float centerY,
            byte oldDirection,
            byte newDirection)
        {
            if (slots == null || oldDirection == newDirection)
                return;
            byte delta = (byte)(newDirection - oldDirection);
            int cos = C2OriginalMovementMathV352.TCos[delta];
            int sin = C2OriginalMovementMathV352.TSin[delta];
            for (int i = 0; i < slots.Count; i++)
            {
                float dx = slots[i].x - centerX;
                float dy = slots[i].y - centerY;
                slots[i] = new Vector2(
                    centerX + (dx * cos - dy * sin) / 256.0f,
                    centerY + (dx * sin + dy * cos) / 256.0f);
            }
        }

        private static bool TryBuildMarchFormationSlotsV320LikeOriginal(
            RuntimeFormationV172LikeOriginal group,
            List<C2NeutralPeasantUnitInfoV2LikeOriginal> groupUnits,
            float centerX,
            float centerY,
            byte direction,
            out List<Vector2> slots,
            out string shape)
        {
            slots = null;
            shape = group != null ? group.Shape : string.Empty;
            if (group == null || groupUnits == null || groupUnits.Count == 0)
                return false;

            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record =
                ResolveRecordForGroupV320LikeOriginal(group);
            string currentShape = group.Shape ?? string.Empty;
            bool cavalrySecondFamily =
                currentShape.StartsWith("SHER2", StringComparison.OrdinalIgnoreCase) ||
                currentShape.StartsWith("PRUS2", StringComparison.OrdinalIgnoreCase) ||
                currentShape.StartsWith("TRI2", StringComparison.OrdinalIgnoreCase);
            bool cavalryFamily =
                cavalrySecondFamily ||
                currentShape.StartsWith("SHER", StringComparison.OrdinalIgnoreCase) ||
                currentShape.StartsWith("PRUS", StringComparison.OrdinalIgnoreCase) ||
                currentShape.StartsWith("TRI", StringComparison.OrdinalIgnoreCase);

            // ord_groups.lst has independent march families. Infantry enters
            // SQUARE, cavalry keeps its SHER/SHER2 marching template. Forcing
            // SQUARE here made mounted brigades receive infantry geometry.
            string marchShape = cavalrySecondFamily
                ? "SHER2"
                : cavalryFamily
                    ? "SHER"
                    : "SQUARE";
            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal march =
                FindFormationOptionV320LikeOriginal(record, marchShape);
            if (march == null && cavalryFamily && record != null && record.Options.Count > 0)
            {
                C2FormationCreateCatalogV165LikeOriginal.TryBuildSiblingFormationOptionLikeOriginal(
                    record.Options[0],
                    marchShape,
                    out march);
            }
            if (march == null)
                return false;

            int commandCount = ResolveCommandPrefixCountV320LikeOriginal(group, groupUnits);
            slots = BuildTemplateSlotsDirectedV352LikeOriginal(
                march,
                groupUnits.Count,
                commandCount,
                groupUnits,
                centerX,
                centerY,
                group.SpacingPercent,
                direction);
            ReorderSoldiersForNearestSlotsV172LikeOriginal(groupUnits, slots, commandCount);
            shape = march.Shape;
            return slots != null && slots.Count == groupUnits.Count;
        }

        private static void IssueFormationRoadPathV320LikeOriginal(
            C2NeutralPeasantUnitInfoV2LikeOriginal unit,
            Vector2[] centerPath,
            float slotOffsetX,
            float slotOffsetY,
            byte finalDirection,
            string source)
        {
            if (unit == null || centerPath == null || centerPath.Length == 0)
                return;

            Vector2[] unitPath = new Vector2[centerPath.Length + 1];
            unitPath[0] = new Vector2(
                unit.RealXFloat != 0.0f ? unit.RealXFloat : unit.RealX,
                unit.RealYFloat != 0.0f ? unit.RealYFloat : unit.RealY);
            for (int i = 0; i < centerPath.Length; i++)
            {
                Vector2 previous = i > 0
                    ? centerPath[i - 1]
                    : centerPath[i];
                Vector2 next = i + 1 < centerPath.Length
                    ? centerPath[i + 1]
                    : centerPath[i];
                byte tangentDirection = DirectionFromDeltaV320LikeOriginal(
                    next.x - previous.x,
                    next.y - previous.y,
                    finalDirection);
                int delta = (tangentDirection - finalDirection) & 255;
                float angle = delta * Mathf.PI * 2.0f / 256.0f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                float rotatedOffsetX = slotOffsetX * cos - slotOffsetY * sin;
                float rotatedOffsetY = slotOffsetX * sin + slotOffsetY * cos;
                unitPath[i + 1] = new Vector2(
                    centerPath[i].x + rotatedOffsetX,
                    centerPath[i].y + rotatedOffsetY);
            }

            C2UnitOriginalRuntimeLinkLikeOriginal link =
                unit.RuntimeLinkCachedLikeOriginal;
            if (link != null)
            {
                link.SetMovePathRealLikeOriginal(
                    unitPath,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    true,
                    finalDirection,
                    false,
                    source ?? "formation_road_move");
            }
            else
            {
                Vector2 last = unitPath[unitPath.Length - 1];
                unit.SetMoveDestinationRealLikeOriginal(
                    last.x,
                    last.y,
                    C2BattleTerrainMode.C2NeutralPeasantUnitsV2MoveSpeedOriginalPixelsPerSecondLikeOriginal,
                    true,
                    finalDirection);
            }
        }
    }

    public sealed partial class C2BattleTerrainMode
    {
        public static bool C2FormationTryBuildRoadPathRealV320LikeOriginal(
            float startRealX,
            float startRealY,
            float destinationRealX,
            float destinationRealY,
            out Vector2[] path,
            out string audit)
        {
            path = null;
            audit = "no_road_network";
            C2BattleTerrainMode mode = UnityEngine.Object.FindObjectOfType<C2BattleTerrainMode>();
            if (mode == null || mode._map == null || !mode._map.HasRoadNet ||
                mode._map.RoadKnots == null || mode._map.RoadKnots.Length == 0)
                return false;

            ParsedRoadNetKnotLikeOriginal[] knots = mode._map.RoadKnots;
            int start = FindNearestRoadKnotV320LikeOriginal(knots, startRealX / 16.0f, startRealY / 16.0f);
            int finish = FindNearestRoadKnotV320LikeOriginal(knots, destinationRealX / 16.0f, destinationRealY / 16.0f);
            if (start < 0 || finish < 0)
            {
                audit = "no_visible_road_endpoint";
                return false;
            }

            float[] distance = new float[knots.Length];
            int[] previous = new int[knots.Length];
            bool[] visited = new bool[knots.Length];
            for (int i = 0; i < knots.Length; i++)
            {
                distance[i] = float.MaxValue;
                previous[i] = -1;
            }
            distance[start] = 0.0f;

            for (int pass = 0; pass < knots.Length; pass++)
            {
                int current = -1;
                float best = float.MaxValue;
                for (int i = 0; i < knots.Length; i++)
                {
                    if (!visited[i] && distance[i] < best)
                    {
                        best = distance[i];
                        current = i;
                    }
                }
                if (current < 0)
                    break;
                if (current == finish)
                    break;
                visited[current] = true;

                ParsedRoadNetKnotLikeOriginal knot = knots[current];
                for (int linkIndex = 0; linkIndex < knot.NLinks && linkIndex < C2RoadMaxLinksLikeOriginal; linkIndex++)
                {
                    int next = knot.Links[linkIndex];
                    if (next < 0 || next >= knots.Length || visited[next])
                        continue;
                    float dx = knots[next].X - knot.X;
                    float dy = knots[next].Y - knot.Y;
                    float candidate = distance[current] + Mathf.Sqrt(dx * dx + dy * dy);
                    if (candidate < distance[next])
                    {
                        distance[next] = candidate;
                        previous[next] = current;
                    }
                }
            }

            if (start != finish && previous[finish] < 0)
            {
                audit = "road_nodes_disconnected";
                return false;
            }

            List<int> reversed = new List<int>();
            int cursor = finish;
            reversed.Add(cursor);
            while (cursor != start)
            {
                cursor = previous[cursor];
                if (cursor < 0)
                {
                    audit = "road_reconstruction_failed";
                    return false;
                }
                reversed.Add(cursor);
            }
            reversed.Reverse();

            List<Vector2> result = new List<Vector2>(Mathf.Max(8, reversed.Count * 8));
            result.Add(new Vector2(startRealX, startRealY));
            if (reversed.Count == 1)
            {
                ParsedRoadNetKnotLikeOriginal only = knots[reversed[0]];
                result.Add(new Vector2(RoadXOnRoadV352LikeOriginal(knots, reversed[0]) * 16.0f,
                                       RoadYOnRoadV352LikeOriginal(knots, reversed[0]) * 16.0f));
            }
            else
            {
                for (int i = 1; i < reversed.Count; i++)
                    AppendRoadEdgeWaypointsV352LikeOriginal(knots, reversed[i - 1], reversed[i], result);
            }
            result.Add(new Vector2(destinationRealX, destinationRealY));
            path = result.ToArray();
            audit = "ok knots=" + reversed.Count.ToString(CultureInfo.InvariantCulture) +
                    " start=" + start.ToString(CultureInfo.InvariantCulture) +
                    " finish=" + finish.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static int RoadXOnRoadV352LikeOriginal(ParsedRoadNetKnotLikeOriginal[] knots, int index)
        {
            if (knots == null || index < 0 || index >= knots.Length) return 0;
            ParsedRoadNetKnotLikeOriginal k = knots[index];
            if (k.NLinks == 2 && k.Links != null && k.Links.Length >= 2 &&
                k.Links[0] < knots.Length && k.Links[1] < knots.Length)
                return (k.X * 6 + knots[k.Links[0]].X + knots[k.Links[1]].X) / 8;
            return k.X;
        }

        private static int RoadYOnRoadV352LikeOriginal(ParsedRoadNetKnotLikeOriginal[] knots, int index)
        {
            if (knots == null || index < 0 || index >= knots.Length) return 0;
            ParsedRoadNetKnotLikeOriginal k = knots[index];
            if (k.NLinks == 2 && k.Links != null && k.Links.Length >= 2 &&
                k.Links[0] < knots.Length && k.Links[1] < knots.Length)
                return (k.Y * 6 + knots[k.Links[0]].Y + knots[k.Links[1]].Y) / 8;
            return k.Y;
        }

        private static void AppendRoadPointV352LikeOriginal(List<Vector2> points, int x, int y)
        {
            if (points == null) return;
            Vector2 p = new Vector2(x * 16.0f, y * 16.0f);
            if (points.Count > 0 && Vector2.SqrMagnitude(points[points.Count - 1] - p) < 1.0f) return;
            points.Add(p);
        }

        private static void Calk2PV352LikeOriginal(int x1, int y1, int x2, int y2, int n, int p, out int nx, out int ny)
        {
            if (n <= 0) { nx = x2; ny = y2; return; }
            nx = (x1 * (n - p) + x2 * p) / n;
            ny = (y1 * (n - p) + y2 * p) / n;
        }

        private static void Calk3PV352LikeOriginal(int x1, int y1, int x2, int y2, int x3, int y3, int n, int p, out int nx, out int ny)
        {
            if (n <= 0) { nx = x3; ny = y3; return; }
            nx = (x1 * (n - p) + x3 * p) / n + ((4 * x2 - 2 * (x1 + x3)) * p * (n - p)) / (n * n);
            ny = (y1 * (n - p) + y3 * p) / n + ((4 * y2 - 2 * (y1 + y3)) * p * (n - p)) / (n * n);
        }

        private static void AppendRoadEdgeWaypointsV352LikeOriginal(
            ParsedRoadNetKnotLikeOriginal[] knots, int startK, int endK, List<Vector2> output)
        {
            // Factures3D.cpp::OneNetWayPointToPoint::FillWay, Step=26.
            if (knots == null || startK < 0 || endK < 0 || startK >= knots.Length || endK >= knots.Length) return;
            ParsedRoadNetKnotLikeOriginal st = knots[startK];
            ParsedRoadNetKnotLikeOriginal en = knots[endK];
            int sxr = RoadXOnRoadV352LikeOriginal(knots, startK);
            int syr = RoadYOnRoadV352LikeOriginal(knots, startK);
            int exr = RoadXOnRoadV352LikeOriginal(knots, endK);
            int eyr = RoadYOnRoadV352LikeOriginal(knots, endK);
            int mpx = (st.X + en.X) / 2;
            int mpy = (st.Y + en.Y) / 2;
            int dst1 = C2OriginalMovementMathV352.Norma(sxr - mpx, syr - mpy);
            int dst2 = C2OriginalMovementMathV352.Norma(exr - mpx, eyr - mpy);
            const int step = 26;
            int xx, yy;

            if (st.NLinks == 2 && st.Links != null && st.Links.Length >= 2)
            {
                int other = st.Links[0] == endK ? st.Links[1] : st.Links[0];
                if (other >= 0 && other < knots.Length)
                {
                    int mpprx = (st.X + knots[other].X) / 2;
                    int mppry = (st.Y + knots[other].Y) / 2;
                    int dst1pr = Math.Max(1, C2OriginalMovementMathV352.Norma(mpx - mpprx, mpy - mppry));
                    int npp = dst1pr / step;
                    if (npp > 0)
                    {
                        int sp = (npp * dst1) / dst1pr;
                        for (int pp = sp; pp < npp; pp++)
                        {
                            Calk3PV352LikeOriginal(mpprx, mppry, sxr, syr, mpx, mpy, npp, pp, out xx, out yy);
                            AppendRoadPointV352LikeOriginal(output, xx, yy);
                        }
                    }
                }
            }
            else
            {
                int npp = dst1 / step;
                for (int pp = 0; pp < npp; pp++)
                {
                    Calk2PV352LikeOriginal(sxr, syr, mpx, mpy, npp, pp, out xx, out yy);
                    AppendRoadPointV352LikeOriginal(output, xx, yy);
                }
            }

            AppendRoadPointV352LikeOriginal(output, mpx, mpy);

            if (en.NLinks == 2 && en.Links != null && en.Links.Length >= 2)
            {
                int other = en.Links[0] == startK ? en.Links[1] : en.Links[0];
                if (other >= 0 && other < knots.Length)
                {
                    int mpprx = (en.X + knots[other].X) / 2;
                    int mppry = (en.Y + knots[other].Y) / 2;
                    int dst2pr = Math.Max(1, C2OriginalMovementMathV352.Norma(mpx - mpprx, mpy - mppry));
                    int npp = dst2pr / step;
                    if (npp > 0)
                    {
                        int sp = (npp * dst2) / dst2pr;
                        for (int pp = 1; pp < sp + 1; pp++)
                        {
                            Calk3PV352LikeOriginal(mpx, mpy, exr, eyr, mpprx, mppry, npp, pp, out xx, out yy);
                            AppendRoadPointV352LikeOriginal(output, xx, yy);
                        }
                    }
                }
            }
            else
            {
                int npp = dst2 / step;
                for (int pp = 1; pp < npp + 1; pp++)
                {
                    Calk2PV352LikeOriginal(mpx, mpy, exr, eyr, npp, pp, out xx, out yy);
                    AppendRoadPointV352LikeOriginal(output, xx, yy);
                }
            }
        }

        private static int FindNearestRoadKnotV320LikeOriginal(
            ParsedRoadNetKnotLikeOriginal[] knots,
            float x,
            float y)
        {
            int bestIndex = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; knots != null && i < knots.Length; i++)
            {
                if (knots[i].Hidden != 0)
                    continue;
                float dx = knots[i].X - x;
                float dy = knots[i].Y - y;
                float distance = dx * dx + dy * dy;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }
    }
}
