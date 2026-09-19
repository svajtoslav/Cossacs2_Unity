using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public static class C2EditorRuntimeStateV333LikeOriginal
    {
        public const float PaletteWidth = 356.0f;
        public static int ControlledNation;
        public static bool PointerOverPalette;
        public static bool CollapsedV377;
        public static C2BattleTerrainMode PaletteOwnerV377;

        // OnGUI coordinates (top-left), derived from the same rectangle as input.
        public static Rect PaletteRectV377LikeOriginal(float width, float height, Rect minimap, bool collapsed)
        {
            float top = 54.0f * height / 768.0f; // original ResPanel above the editor
            float panelWidth = Mathf.Min(PaletteWidth, width);
            float x = width - panelWidth;
            float available = Mathf.Max(32.0f, height - minimap.yMax - top - 8.0f);
            if (!collapsed && available < 360.0f && minimap.width > 0.0f && minimap.xMin >= panelWidth + 8.0f)
            {
                x = minimap.xMin - panelWidth - 8.0f;
                available = height - top;
            }
            return new Rect(x, top, panelWidth, collapsed ? 32.0f : Mathf.Min(height - top, available));
        }

        public static Rect CurrentPaletteRectV377LikeOriginal()
        {
            Rect minimap = PaletteOwnerV377 != null ? PaletteOwnerV377.C2MinimapScreenRectV377LikeOriginal() : new Rect();
            return PaletteRectV377LikeOriginal(Screen.width, Screen.height, minimap, CollapsedV377);
        }

        public static bool IsPointerOverPaletteLikeOriginal(Vector2 pointer)
        {
            return C2BattleTerrainMode.EditorTestModeLikeOriginal && PaletteOwnerV377 != null &&
                   CurrentPaletteRectV377LikeOriginal().Contains(new Vector2(pointer.x, Screen.height - pointer.y));
        }

        public static bool CanControlNationLikeOriginal(int nation)
        {
            // The active player always controls exactly one nation/color. The
            // editor may deliberately change ControlledNation, but closing or
            // never opening the editor must not grant orders to every nation.
            return nation == ControlledNation;
        }
    }

    public sealed partial class C2BattleTerrainMode
    {
        public bool C2EditorScreenToOriginalPixelV332LikeOriginal(Vector2 screen, out float ox, out float oy)
        {
            if (!_freeCameraMode)
                return TryStrictIsoScreenToOriginalPixelV283LikeOriginal(screen, out ox, out oy);

            ox = 0.0f;
            oy = 0.0f;
            if (_freeCamera == null) return false;
            Plane plane = new Plane(Vector3.up, new Vector3(0.0f, _terrainBounds.center.y, 0.0f));
            Ray ray = _freeCamera.ScreenPointToRay(screen);
            float enter;
            if (!plane.Raycast(ray, out enter) || enter < 0.0f) return false;
            return C2NoUnitWorldToOriginalPixelLikeOriginal(ray.GetPoint(enter), out ox, out oy);
        }
    }

    public sealed class C2EditorTestPaletteV332LikeOriginal : MonoBehaviour
    {
        private const float PanelWidth = C2EditorRuntimeStateV333LikeOriginal.PaletteWidth;
        private const float RepeatDelay = 0.10f;
        private C2BattleTerrainMode _mode;
        private List<C2EditorCountryV333LikeOriginal> _countries = new List<C2EditorCountryV333LikeOriginal>();
        private List<C2OriginalProduceItemV13> _all = new List<C2OriginalProduceItemV13>();
        private readonly Dictionary<string, Texture2D> _icons = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private Vector2 _scroll;
        private string _search = string.Empty;
        private int _tab;
        private int _ownerNation;
        private int _countryIndex;
        private float _nextRepeatAt;
        private C2OriginalProduceItemV13 _selected;
        private string _status = string.Empty;

        public static void InstallLikeOriginal(C2BattleTerrainMode mode)
        {
            if (mode == null) return;
            C2EditorTestPaletteV332LikeOriginal old = FindObjectOfType<C2EditorTestPaletteV332LikeOriginal>();
            if (old != null) Destroy(old.gameObject);
            GameObject go = new GameObject("C2_EditorTestPalette_V333");
            go.transform.SetParent(mode.transform, false);
            C2EditorTestPaletteV332LikeOriginal palette = go.AddComponent<C2EditorTestPaletteV332LikeOriginal>();
            palette._mode = mode;
            C2EditorRuntimeStateV333LikeOriginal.PaletteOwnerV377 = mode;
            C2EditorRuntimeStateV333LikeOriginal.CollapsedV377 = false;
            C2EditorRuntimeStateV333LikeOriginal.ControlledNation = 0;
            string audit;
            palette._countries = C2OriginalProduceCatalogV13.BuildEditorCountriesV333LikeOriginal(out audit);
            int austria = palette._countries.FindIndex(c => string.Equals(c.Id, "AUSTRIA", StringComparison.OrdinalIgnoreCase));
            palette._countryIndex = austria >= 0 ? austria : 0;
            palette.ReloadCountryCatalogLikeOriginal(audit);
        }

        private void ReloadCountryCatalogLikeOriginal(string prefix)
        {
            C2EditorCountryV333LikeOriginal country = CurrentCountry;
            string audit;
            _all = C2OriginalProduceCatalogV13.BuildEditorCatalogV333LikeOriginal(country, out audit);
            _scroll = Vector2.zero;
            _selected = null;
            _status = (prefix ?? string.Empty) + " " + audit;
            Debug.Log("[C2:EDITOR V333 COUNTRY] " + _status);
        }

        private C2EditorCountryV333LikeOriginal CurrentCountry
        {
            get
            {
                return _countries != null && _countries.Count > 0
                    ? _countries[Mathf.Clamp(_countryIndex, 0, _countries.Count - 1)]
                    : null;
            }
        }

        private void Update()
        {
            Vector2 pointer = ReadPointerPositionLikeOriginal();
            C2EditorRuntimeStateV333LikeOriginal.PointerOverPalette =
                C2EditorRuntimeStateV333LikeOriginal.IsPointerOverPaletteLikeOriginal(pointer);
            if (_mode == null || _selected == null || C2EditorRuntimeStateV333LikeOriginal.CollapsedV377) return;
            if (WasCancelPressedLikeOriginal())
            {
                _selected = null;
                _status = "Размещение отменено";
                return;
            }

            bool pressed;
            bool held;
            ReadPlaceButtonsLikeOriginal(out pressed, out held);
            if (!pressed && !held) return;
            if (C2EditorRuntimeStateV333LikeOriginal.PointerOverPalette ||
                _mode.C2MinimapScreenRectV377LikeOriginal().Contains(pointer)) return;

            bool control = IsLeftControlHeldLikeOriginal();
            if (control && !pressed) return;
            if (!pressed && Time.realtimeSinceStartup < _nextRepeatAt) return;
            _nextRepeatAt = Time.realtimeSinceStartup + RepeatDelay;

            float px;
            float py;
            if (!_mode.C2EditorScreenToOriginalPixelV332LikeOriginal(pointer, out px, out py))
            {
                _status = "Точка карты не определена";
                return;
            }

            int realX = Mathf.RoundToInt(px * 16.0f);
            int realY = Mathf.RoundToInt(py * 16.0f);
            if (control)
                SpawnFormationLikeOriginal(realX, realY);
            else
                SpawnSingleLikeOriginal(_selected, realX, realY, true);
        }

        private bool SpawnSingleLikeOriginal(C2OriginalProduceItemV13 item, int wantedX, int wantedY, bool report)
        {
            int realX;
            int realY;
            if (!TryFindFreeUnitPointLikeOriginal(wantedX, wantedY, out realX, out realY))
            {
                if (report) _status = "Рядом нет свободного места";
                return false;
            }

            C2NeutralPeasantUnitInfoV2LikeOriginal spawned;
            string audit;
            bool ok = C2UnitOriginalRuntimeAndRendererV1.TrySpawnEditorUnitAtRealV332LikeOriginal(
                _mode, item, _ownerNation, realX, realY, out spawned, out audit);
            if (report)
                _status = ok ? "Поставлен юнит: " + DisplayName(item) : "Ошибка юнита: " + audit;
            return ok;
        }

        private void SpawnFormationLikeOriginal(int centerX, int centerY)
        {
            C2FormationCreateCatalogV165LikeOriginal.C2FormationRecordV165LikeOriginal record;
            if (!C2FormationCreateCatalogV165LikeOriginal.TryResolveForUnitMemberIdLikeOriginal(
                    _selected.UnitId, _ownerNation, out record) || record == null || record.Options.Count == 0)
            {
                SpawnSingleLikeOriginal(_selected, centerX, centerY, true);
                _status += " (для этого типа нет строя в NDS)";
                return;
            }

            C2FormationCreateCatalogV165LikeOriginal.C2FormationOptionV165LikeOriginal option = null;
            for (int i = 0; i < record.Options.Count; i++)
            {
                var candidate = record.Options[i];
                if (candidate == null || candidate.OrderTemplate == null || candidate.UnitCount <= 0) continue;
                if (option == null || candidate.UnitCount > option.UnitCount) option = candidate;
            }
            if (option == null) { _status = "Нет доступного шаблона строя"; return; }

            var commandItems = new List<C2OriginalProduceItemV13>(3);
            foreach (string id in new[] { record.OfficerId, record.DrummerId, record.FlagId })
            {
                if (string.IsNullOrWhiteSpace(id) || string.Equals(id, record.UnitId, StringComparison.OrdinalIgnoreCase)) continue;
                C2OriginalProduceItemV13 commandItem;
                if (!C2OriginalProduceCatalogV13.TryBuildEditorItemForUnitIdV333LikeOriginal(id, out commandItem) || commandItem == null)
                { _status = "Не найден командир: " + id; return; }
                commandItems.Add(commandItem);
            }
            int distanceScale;
            string audit;
            if (!C2UnitOriginalRuntimeAndRendererV1.TryGetEditorFormationScaleV375LikeOriginal(_selected, out distanceScale, out audit))
            { _status = "Не загружен юнит: " + audit; return; }
            List<Vector2> slots = C2FormationRuntimeV167LikeOriginal.BuildEditorFormationSlotsV375LikeOriginal(
                option, commandItems.Count, distanceScale, centerX, centerY);
            if (slots == null) { _status = "Шаблон не содержит нужных мест командования"; return; }
            // Validate the whole footprint before creating anything. Moving individual
            // blocked places would silently deform the canonical formation at spawn.
            if (!CanPlaceFormationV375LikeOriginal(slots))
            { _status = "Для целого отряда нужно свободное место"; return; }

            var commandUnits = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(commandItems.Count);
            var soldiers = new List<C2NeutralPeasantUnitInfoV2LikeOriginal>(option.UnitCount);
            for (int i = 0; i < slots.Count; i++)
            {
                bool isCommand = i < commandItems.Count;
                var item = isCommand ? commandItems[i] : _selected;
                C2NeutralPeasantUnitInfoV2LikeOriginal spawned;
                if (!C2UnitOriginalRuntimeAndRendererV1.TrySpawnEditorUnitAtRealV332LikeOriginal(
                        _mode, item, _ownerNation, Mathf.RoundToInt(slots[i].x), Mathf.RoundToInt(slots[i].y), out spawned, out audit) || spawned == null)
                {
                    _status = "Размещение прервано: " + audit + ". Созданные юниты остаются без отряда.";
                    return;
                }
                if (isCommand) commandUnits.Add(spawned); else soldiers.Add(spawned);
            }
            int groupId;
            bool formed = C2FormationRuntimeV167LikeOriginal.TryRegisterPlacedFormationV346LikeOriginal(
                commandUnits, soldiers, option.Shape, record.UnitId, out groupId, out audit);
            _status = formed ? "Поставлен собранный отряд: " + option.UnitCount + " чел., группа " + groupId
                : "Не удалось собрать отряд: " + audit;
            Debug.Log("[C2:EDITOR FORMATION V375] order=" + option.OrderId + " commands=" + commandItems.Count +
                " soldiers=" + soldiers.Count + " mdScale=" + distanceScale + " canonicalSlots=" + formed + " " + audit);
        }

        private static bool CanPlaceFormationV375LikeOriginal(IList<Vector2> slots)
        {
            if (slots == null || slots.Count == 0) return false;
            const float clearance = 220.0f;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < slots.Count; i++)
            {
                Vector2 p = slots[i];
                if (C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(
                        Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), 1)) return false;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            var existing = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int u = 0; existing != null && u < existing.Length; u++)
            {
                var unit = existing[u];
                if (unit == null || !unit.isActiveAndEnabled || unit.IsDeadLikeOriginal) continue;
                float x = unit.RealXFloat != 0 ? unit.RealXFloat : unit.RealX;
                float y = unit.RealYFloat != 0 ? unit.RealYFloat : unit.RealY;
                if (x < minX - clearance || x > maxX + clearance || y < minY - clearance || y > maxY + clearance) continue;
                for (int i = 0; i < slots.Count; i++)
                {
                    float dx = x - slots[i].x, dy = y - slots[i].y;
                    if (dx * dx + dy * dy < clearance * clearance) return false;
                }
            }
            return true;
        }


        private static bool TryFindFreeUnitPointLikeOriginal(int wantedX, int wantedY, out int realX, out int realY)
        {
            C2NeutralPeasantUnitInfoV2LikeOriginal[] units = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            const int step = 288;
            const float minDistance2 = 220.0f * 220.0f;
            for (int radius = 0; radius <= 12; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (radius > 0 && Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius) continue;
                    int x = wantedX + dx * step;
                    int y = wantedY + dy * step;
                    if (C2BattleTerrainMode.C2BuildingMotionFieldV1IsBlockedForUnitRealLikeOriginal(x, y, 1)) continue;
                    bool occupied = false;
                    for (int i = 0; units != null && i < units.Length; i++)
                    {
                        C2NeutralPeasantUnitInfoV2LikeOriginal u = units[i];
                        if (u == null || !u.isActiveAndEnabled || u.IsDeadLikeOriginal) continue;
                        float ux = u.RealXFloat != 0.0f ? u.RealXFloat : u.RealX;
                        float uy = u.RealYFloat != 0.0f ? u.RealYFloat : u.RealY;
                        float ddx = ux - x;
                        float ddy = uy - y;
                        if (ddx * ddx + ddy * ddy < minDistance2) { occupied = true; break; }
                    }
                    if (occupied) continue;
                    realX = x;
                    realY = y;
                    return true;
                }
            }
            realX = wantedX;
            realY = wantedY;
            return false;
        }

        private void OnGUI()
        {
            Rect panel = C2EditorRuntimeStateV333LikeOriginal.CurrentPaletteRectV377LikeOriginal();
            GUI.Box(panel, string.Empty);
            GUI.Label(new Rect(panel.x + 10.0f, panel.y + 6.0f, panel.width - 112.0f, 24.0f), "РЕДАКТОР / ПОЛИГОН");
            if (GUI.Button(new Rect(panel.xMax - 98.0f, panel.y + 4.0f, 90.0f, 24.0f),
                C2EditorRuntimeStateV333LikeOriginal.CollapsedV377 ? "Развернуть" : "Свернуть"))
            {
                C2EditorRuntimeStateV333LikeOriginal.CollapsedV377 = !C2EditorRuntimeStateV333LikeOriginal.CollapsedV377;
                if (C2EditorRuntimeStateV333LikeOriginal.CollapsedV377)
                {
                    _selected = null;
                    C2BuildingPlacementPreviewV27.CancelBuildPreviewFromEditorV333LikeOriginal();
                    _status = "Размещение отменено";
                }
                return;
            }
            if (C2EditorRuntimeStateV333LikeOriginal.CollapsedV377) return;
            GUILayout.BeginArea(new Rect(panel.x + 10.0f, panel.y + 34.0f, panel.width - 20.0f, Mathf.Max(0.0f, panel.height - 42.0f)));
            GUILayout.Label("Владелец и цвет новых объектов:");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < C2PlayerColorsLikeOriginal.NatColors.Length; i++)
            {
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = C2PlayerColorsLikeOriginal.NatColors[i];
                if (GUILayout.Button(i == _ownerNation ? "●" : " ", GUILayout.Width(34), GUILayout.Height(28)))
                    SelectOwnerColorLikeOriginal(i);
                GUI.backgroundColor = old;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(30))) ChangeCountryLikeOriginal(-1);
            GUILayout.Label(CurrentCountry != null ? CurrentCountry.Id : "NO COUNTRY", GUILayout.ExpandWidth(true));
            if (GUILayout.Button(">", GUILayout.Width(30))) ChangeCountryLikeOriginal(1);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_tab == 0, "Юниты", GUI.skin.button)) _tab = 0;
            if (GUILayout.Toggle(_tab == 1, "Здания", GUI.skin.button)) _tab = 1;
            if (GUILayout.Toggle(_tab == 2, "Объекты", GUI.skin.button)) _tab = 2;
            if (GUILayout.Toggle(_tab == 3, "Земля", GUI.skin.button)) _tab = 3;
            GUILayout.EndHorizontal();
            _search = GUILayout.TextField(_search ?? string.Empty);

            if (_tab >= 2)
            {
                GUILayout.Space(12);
                GUILayout.Label(_tab == 2
                    ? "Деревья, мосты и нейтральные объекты используют отдельные каталоги 3DMapEd/Nature. В этой сборке их запись пока заблокирована, чтобы не повредить M3D."
                    : "Кисти высоты и текстур меняют массивы карты. Они будут включены после отдельного undo и сохранения в копию, а не поверх исходной карты.");
            }
            else
            {
                _scroll = GUILayout.BeginScrollView(_scroll);
                DrawCatalogGrid();
                GUILayout.EndScrollView();
            }

            GUILayout.FlexibleSpace();
            if (_selected != null)
                GUILayout.Label("Выбрано: " + DisplayName(_selected) +
                                "\nЛКМ — поставить; удерживать — серия; Ctrl+ЛКМ — отряд; ПКМ/Esc — отмена.");
            else if (C2BuildingPlacementPreviewV27.C2BuildPlacementActiveLikeOriginal)
                GUILayout.Label("Установка здания: ЛКМ — поставить; ПКМ/Esc — убрать призрак.");
            GUILayout.Label(_status ?? string.Empty);
            GUILayout.EndArea();
        }

        private void DrawCatalogGrid()
        {
            int column = 0;
            bool rowOpen = false;
            for (int i = 0; i < _all.Count; i++)
            {
                C2OriginalProduceItemV13 item = _all[i];
                if (item == null || item.Building != (_tab == 1)) continue;
                string name = DisplayName(item);
                if (!string.IsNullOrWhiteSpace(_search) &&
                    name.IndexOf(_search, StringComparison.CurrentCultureIgnoreCase) < 0 &&
                    (item.UnitId ?? string.Empty).IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (!rowOpen) { GUILayout.BeginHorizontal(); rowOpen = true; column = 0; }
                GUILayout.BeginVertical(GUILayout.Width(78));
                Texture2D icon = GetIcon(item);
                if (GUILayout.Button(icon != null ? new GUIContent(icon, name) : new GUIContent(name), GUILayout.Width(70), GUILayout.Height(58)))
                    SelectCatalogItemLikeOriginal(item);
                GUILayout.Label(name, GUILayout.Width(74), GUILayout.Height(34));
                GUILayout.EndVertical();
                column++;
                if (column >= 4) { GUILayout.EndHorizontal(); rowOpen = false; }
            }
            if (rowOpen) GUILayout.EndHorizontal();
        }

        private void SelectCatalogItemLikeOriginal(C2OriginalProduceItemV13 item)
        {
            if (item == null) return;
            if (item.Building)
            {
                _selected = null;
                C2BuildingPlacementPreviewV27.RequestBuildPreviewLikeOriginal(
                    item.UnitId, item.MdName, _ownerNation, string.Empty, string.Empty, "editor_country_minicon_v333");
                _status = "Включён призрак здания: " + DisplayName(item);
            }
            else
            {
                C2BuildingPlacementPreviewV27.CancelBuildPreviewFromEditorV333LikeOriginal();
                _selected = item;
                _status = "Юнит готов к размещению";
            }
        }

        private void SelectOwnerColorLikeOriginal(int owner)
        {
            _ownerNation = Mathf.Clamp(owner, 0, C2PlayerColorsLikeOriginal.MaxPlayers - 1);
            C2EditorRuntimeStateV333LikeOriginal.ControlledNation = _ownerNation;
            C2PlayerColorsLikeOriginal.SetPlayerColorId(_ownerNation, _ownerNation);
            C2UnitOriginalRuntimeAndRendererV1.RefreshNationColorsV332LikeOriginal();

            C2NeutralPeasantUnitInfoV2LikeOriginal[] units = C2NeutralPeasantUnitInfoV2LikeOriginal.C2GetActiveUnitsSnapshotV359LikeOriginal();
            for (int i = 0; units != null && i < units.Length; i++)
            {
                if (units[i] == null) continue;
                units[i].ControllableByPlayer = units[i].Nation == _ownerNation;
                if (units[i].Nation != _ownerNation) units[i].SetSelected(false);
            }
            C2SettlementBuildingSelectableV1LikeOriginal[] buildings = FindObjectsOfType<C2SettlementBuildingSelectableV1LikeOriginal>();
            for (int i = 0; buildings != null && i < buildings.Length; i++)
                if (buildings[i] != null && buildings[i].Nation != _ownerNation) buildings[i].SetSelected(false);
            _status = "Управление переключено на игрока " + (_ownerNation + 1).ToString();
        }

        private void ChangeCountryLikeOriginal(int delta)
        {
            if (_countries == null || _countries.Count == 0) return;
            _countryIndex = (_countryIndex + delta) % _countries.Count;
            if (_countryIndex < 0) _countryIndex += _countries.Count;
            ReloadCountryCatalogLikeOriginal("country_changed");
        }

        private Texture2D GetIcon(C2OriginalProduceItemV13 item)
        {
            string key = (item.IconFileId ?? string.Empty) + "|" + item.IconSpriteId.ToString() + "|editor_minicon_ongui_v333";
            Texture2D tex;
            if (_icons.TryGetValue(key, out tex)) return tex;
            tex = C2GameplayOriginalSpriteCacheV1.LoadTexture(
                item.IconFileId, item.IconSpriteId, "editor_minicon_ongui_v333_" + item.UnitId);
            _icons[key] = tex;
            return tex;
        }

        private static Vector2 ReadPointerPositionLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
#endif
            try { return Input.mousePosition; } catch { return Vector2.zero; }
        }

        private static void ReadPlaceButtonsLikeOriginal(out bool pressed, out bool held)
        {
            pressed = false;
            held = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                return;
            }
#endif
            try { pressed = Input.GetMouseButtonDown(0); held = Input.GetMouseButton(0); } catch { }
        }

        private static bool WasCancelPressedLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) return true;
#endif
            try { return Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1); } catch { return false; }
        }

        private static bool IsLeftControlHeldLikeOriginal()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null) return Keyboard.current.leftCtrlKey.isPressed;
#endif
            try { return Input.GetKey(KeyCode.LeftControl); } catch { return false; }
        }

        private static string DisplayName(C2OriginalProduceItemV13 item)
        {
            if (item == null) return string.Empty;
            return !string.IsNullOrWhiteSpace(item.DisplayText) ? item.DisplayText : (item.UnitId ?? item.MdName ?? string.Empty);
        }
    }
}
