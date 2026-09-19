using Cossacks2Bridge.Core;
using Cossacks2Bridge.Core.Loaders;
using Cossacks2Bridge.UnityAdapters.Renderers;
using Cossacks2Bridge.UnityAdapters.Battles;
using Cossacks2Bridge.UnityAdapters.Profiles;
using Cossacks2Bridge.UnityAdapters.BigMap;
using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cossacks2Bridge.UnityAdapters
{
    /// <summary>
    /// Координатор загрузки и рендеринга UI экранов
    /// </summary>
    public sealed class MenuBootstrap : MonoBehaviour
    {
        [Header("Data Source")]
        [Tooltip("If empty, uses StreamingAssets/Cossacks2/Data or default install path")]
        public string dataRootOverride = "";

        [Header("Start Screen")]
        public string startScreenId = "Main";

        [Header("Debug")]
        public bool verboseLogs = true;
        public bool drawDebugOutline = false;

        // Core
        private CoreFileSystem _fs;
        private LocDb _loc;

        public CoreFileSystem Fs => _fs;
        public LocDb Loc => _loc;

        // V388: one loader/parser for every menu XML.
        private Menu14UnifiedLoader _menuXmlLoader;

        // V395Q: EW2 campaign statistics XML is immutable during a session.
        // Reuse its parsed UiDesk after the first open instead of reparsing the
        // 400+ KB DialogsSystem file every time the user returns to Statistics.
        private UiDesk _campaignStatsDeskV395Q;

        // Renderers
        private MainMenuRenderer _mainMenuRenderer;
        private OptionsRenderer _optionsRenderer;

        // Добавлено: рендерер для создания игрока
        private readonly NewPlayerRenderer _newPlayer = new NewPlayerRenderer();
        private MbattlesScreenAdapter _mbattles;
        private readonly ProfileSelectionRenderer _profileSelection = new ProfileSelectionRenderer();
        private readonly C2BigMapRenderer14 _bigMap14 = new C2BigMapRenderer14();
        private readonly C2CampaignModalRenderer14 _campaignModal14 = new C2CampaignModalRenderer14();

        // Shared options
        private BaseUiRenderer.RenderOptions _renderOptions;

        // Navigation
        public string CurrentScreenId { get; private set; }
        public string PreviousScreenId { get; private set; }

        private void Start()
        {
            InitializeCore();
            InitializeLoaders();
            InitializeRenderers();

            // V387A3: create the action sink before the first screen routing so a
            // persisted profile can restore _hasAnyProfile even when the project
            // starts directly on the Single screen.
            GetOrCreateSink();
            RenderByScreenId(startScreenId);
        }

        private void Update()
        {
            bool leftControl;
            if (!WasQuickMapHotkeyPressed(out leftControl))
                return;

            if (leftControl)
                DebugOpenEditorTerrainLikeOriginal();
            else
                DebugOpenRubiconTerrain();
        }

        private static bool WasQuickMapHotkeyPressed(out bool leftControl)
        {
            leftControl = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
            {
                leftControl = Keyboard.current.leftCtrlKey.isPressed;
                return true;
            }
#endif
            try
            {
                bool pressed = Input.GetKeyDown(KeyCode.F12);
                if (pressed) leftControl = Input.GetKey(KeyCode.LeftControl);
                return pressed;
            }
            catch
            {
                return false;
            }
        }

        private void DebugOpenEditorTerrainLikeOriginal()
        {
            MenuActionSink.SingleBattlesShowBattles = false;
            MenuActionSink.SingleBattlesShowLoad = false;
            MenuActionSink.SingleBattlesArcadeModeEnabled = false;
            MenuActionSink.SingleBattlesSelectedId = "EditorPlateau";
            Debug.Log("[C2:EDITOR V332] LeftCtrl+F12 -> Models\\MapAutosave.m3d, original-like test palette");
            KillMenuCanvasesBeforeBattleLikeOriginal();
            Cossacks2Bridge.UnityAdapters.Maps.C2MapLoadLighting.ApplyMapLoadDefaultsLikeOriginal();
            Cossacks2Bridge.UnityAdapters.Maps.C2BattleTerrainMode.OpenFromBattles(this, true);
        }

        private void DebugOpenRubiconTerrain()
        {
            MenuActionSink.SingleBattlesShowBattles = false;
            MenuActionSink.SingleBattlesShowLoad = false;
            MenuActionSink.SingleBattlesArcadeModeEnabled = false;
            MenuActionSink.SingleBattlesSelectedId = "Skirmish2";
            Debug.Log("[C2:HOTKEY] F12 -> debugOpen map=Skirmish2 alias='Пересечь Рубикон' mode=terrain-view");
            KillMenuCanvasesBeforeBattleLikeOriginal();
            Cossacks2Bridge.UnityAdapters.Maps.C2MapLoadLighting.ApplyMapLoadDefaultsLikeOriginal();
            Cossacks2Bridge.UnityAdapters.Maps.C2BattleTerrainMode.OpenFromBattles(this);
        }

        private static void KillMenuCanvasesBeforeBattleLikeOriginal()
        {
            int killed = 0;
            try
            {
                Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                for (int i = 0; i < canvases.Length; i++)
                {
                    Canvas c = canvases[i];
                    if (c == null) continue;

                    GameObject go = c.gameObject;
                    if (go == null) continue;

                    string n = go.name ?? string.Empty;
                    bool kill =
                        n.StartsWith("C2_", StringComparison.Ordinal) ||
                        n.IndexOf("MainMenu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Options", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("AddProfile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("Mbattles", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!kill)
                        continue;

                    c.enabled = false;

                    UnityEngine.UI.GraphicRaycaster gr = go.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                    if (gr != null) gr.enabled = false;

                    Destroy(go);
                    killed++;
                }

                Debug.Log("[C2:HOTKEY] battle-ui cleanup before map load killed=" + killed.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[C2:HOTKEY] battle-ui cleanup failed: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void InitializeCore()
        {
            string dataRoot = dataRootOverride;

            if (string.IsNullOrWhiteSpace(dataRoot))
            {
                string guess = @"C:\GSC Game World\Cossacks II\Data";
                if (System.IO.Directory.Exists(guess))
                    dataRoot = guess;
                else
                    dataRoot = System.IO.Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data");
            }

            Debug.Log($"[MenuBootstrap] DataRoot = {dataRoot}");

            _fs = new CoreFileSystem(dataRoot);
            _loc = new LocDb();
            _loc.LoadDefault(_fs);

            Debug.Log($"[MenuBootstrap] LocDb loaded {_loc.Count} keys");
        }

        private void InitializeLoaders()
        {
            string clean14Root = System.IO.Path.Combine(Application.streamingAssetsPath, "Cossacks2", "Data");
            _menuXmlLoader = new Menu14UnifiedLoader(_fs, clean14Root);
            _menuXmlLoader.ValidateAllRoutes();
            C2Version14Context.AuditStartup(_fs, _loc, clean14Root);
        }

        private void InitializeRenderers()
        {
            _renderOptions = new BaseUiRenderer.RenderOptions
            {
                FontResourcePath = "Fonts/Slovic",
                FontSize = 29f,

                // ✅ Оригинальные цвета для главного меню
                NormalColor = new Color32(40, 10, 10, 255),
                HoverColor = new Color32(95, 30, 30, 255),
                DisabledColor = new Color32(90, 90, 90, 255),

                CanvasScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize,
                ReferenceResolution = new Vector2(1024, 768),
                VerboseLogs = verboseLogs,
                DrawDebugOutline = drawDebugOutline
            };

            _mainMenuRenderer = new MainMenuRenderer();
            _optionsRenderer = new OptionsRenderer();
            _mbattles = new MbattlesScreenAdapter();
        }

        public void RenderByScreenId(string screenId)
        {
            if (_fs == null)
            {
                Debug.LogError("[MenuBootstrap] Not initialized");
                return;
            }

            // Original cva_MM_SinStart contract: a profile is required before
            // entering the single-player desk.  V395 restores persistent CurPlayer.
            C2ProfileRuntime14.EnsureLoaded();
            if (string.Equals(screenId, "Single", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!HasAnyProfile())
                    screenId = "AddProfile";
                else
                    MenuActionSink.SetCurrentProfileFromRuntime(C2ProfileRuntime14.Current?.m_chName ?? string.Empty);
            }

            // Track navigation
            if (!string.IsNullOrWhiteSpace(screenId) && !string.Equals(CurrentScreenId, screenId))
            {
                PreviousScreenId = CurrentScreenId;
                CurrentScreenId = screenId;
            }

            var sink = GetOrCreateSink();
            UiDesk desk;

            // V395: original profile-selection desk.  No SelProfile->AddProfile shortcut.
            if (string.Equals(screenId, "SelProfile", StringComparison.OrdinalIgnoreCase))
            {
                desk = _menuXmlLoader.LoadScreen(screenId);
                Debug.Log($"[MenuBootstrap] SELPROFILE -> {desk.Children.Count} elements source={desk.XmlSource}");
                _profileSelection.Render(desk, _fs, _renderOptions, sink, _loc);
                return;
            }

            // V395B: ProcessBigMap non-battle campaign shell. Accept both the
            // original desk id and action/modal aliases used by M_Single 1.4.
            if (string.Equals(screenId, "SinGlobalMap", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(screenId, "BigMap", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(screenId, "Campaign", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(screenId, "ConquestOfEurope", StringComparison.OrdinalIgnoreCase))
            {
                C2ProfileRuntime14.EnsureLoaded();
                if (!C2ProfileRuntime14.HasProfiles || C2ProfileRuntime14.Current == null)
                {
                    Debug.LogWarning($"[C2:BIGMAP V395B] route '{screenId}' blocked: no CurPlayer -> AddProfile");
                    RenderByScreenId("AddProfile");
                    return;
                }

                Debug.Log($"[C2:BIGMAP V395B] route '{screenId}' -> C2BigMapRenderer14 profile='{C2ProfileRuntime14.Current.m_chName}'");
                _bigMap14.Render(_fs, _renderOptions, _loc);
                return;
            }

            // Изолированный движок только для окна "Сражения и Баталии"
            if (string.Equals(screenId, "SingleBattles", StringComparison.OrdinalIgnoreCase))
            {
                if (_mbattles == null) _mbattles = new MbattlesScreenAdapter();
                if (_mbattles.TryRender(_fs, _renderOptions, sink, _loc))
                    return;
            }

            // AddProfile рендерим отдельно, не трогая CanHandle(), чтобы не ломать другие окна
            if (string.Equals(screenId, "AddProfile", StringComparison.OrdinalIgnoreCase))
            {
                desk = _menuXmlLoader.LoadScreen(screenId);
                Debug.Log($"[MenuBootstrap] ADDPROFILE -> {desk.Children.Count} elements");
                _newPlayer.Render(desk, _fs, _renderOptions, sink, _loc);
                return;
            }

            // V388: loader/parser is the same for every menu screen. Only the
            // Unity renderer differs by screen family.
            bool isCampaignStatsV395Q = string.Equals(screenId, "EW2CampStat", StringComparison.OrdinalIgnoreCase);
            if (isCampaignStatsV395Q)
            {
                long loadStartV395Q = System.Diagnostics.Stopwatch.GetTimestamp();
                bool cacheHitV395Q = _campaignStatsDeskV395Q != null;
                if (!cacheHitV395Q)
                    _campaignStatsDeskV395Q = _menuXmlLoader.LoadScreen(screenId);
                desk = _campaignStatsDeskV395Q;
                double loadMsV395Q = (System.Diagnostics.Stopwatch.GetTimestamp() - loadStartV395Q)
                    * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                Debug.Log($"[C2:CAMPSTAT PERF V395Q] deskCache={(cacheHitV395Q ? "hit" : "miss")} loadMs={loadMsV395Q:F2} nodes={desk?.Children?.Count ?? 0}");
            }
            else
            {
                desk = _menuXmlLoader.LoadScreen(screenId);
            }

            string routedScreenId = screenId ?? string.Empty;
            bool useOptionsRenderer =
                string.Equals(routedScreenId, "Options", StringComparison.OrdinalIgnoreCase) ||
                routedScreenId.StartsWith("Options_", StringComparison.OrdinalIgnoreCase) ||
                routedScreenId.StartsWith("Options/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(routedScreenId, "Multi", StringComparison.OrdinalIgnoreCase) ||
                // EW2_CampaignStats.DialogsSystem.xml uses ComboBox/TextButton/GPPicture
                // actions that are bound by OptionsRenderer + Menu14ActionStateRuntime.
                string.Equals(routedScreenId, "EW2CampStat", StringComparison.OrdinalIgnoreCase);

            if (useOptionsRenderer)
            {
                Debug.Log($"[MenuBootstrap] XML MENU screen '{screenId}' -> {desk.Children.Count} elements source={desk.XmlSource}");
                _optionsRenderer.Render(desk, _fs, _renderOptions, sink, _loc);
                return;
            }

            Debug.Log($"[MenuBootstrap] XML MENU screen '{screenId}' -> {desk.Children.Count} elements source={desk.XmlSource}");
            _mainMenuRenderer.Render(desk, _fs, _renderOptions, sink, _loc);
            return;
        }

        private bool _hasAnyProfile;

        public void SetHasProfile(bool has) => _hasAnyProfile = has;

        private bool HasAnyProfile()
        {
            return _hasAnyProfile || C2ProfileRuntime14.HasProfiles;
        }

        public bool ShowCampaignModalOriginalV396A3(IUiActionSink sink)
        {
            if (_fs == null || _loc == null)
            {
                Debug.LogError("[C2:BFE14 CAMPAIGN XML V396A3] MenuBootstrap is not initialized; modal not shown");
                return false;
            }

            // Seed Menu14ActionStateRuntime with the same Single-screen source
            // context so source FileID/SpriteID banks can resolve against both the
            // clean 1.4 menu bundle and the real installed DataRoot.
            UiDesk sourceDesk = _menuXmlLoader.LoadScreen("Single");
            Menu14ActionStateRuntime.BeginScreen(sourceDesk, _fs, _loc);
            return _campaignModal14.Show(_fs, _loc, sink);
        }

        public void CloseCampaignModalOriginalV396A3()
        {
            _campaignModal14.Close();
        }

        public void RenderPreviousOrMain()
        {
            var id = string.IsNullOrWhiteSpace(PreviousScreenId) ? "Main" : PreviousScreenId;
            RenderByScreenId(id);
        }

        private IUiActionSink GetOrCreateSink()
        {
            var sink = UnityEngine.Object.FindFirstObjectByType<MenuActionSink>(FindObjectsInactive.Include);
            if (sink != null) return sink;

            var go = new GameObject("C2_MenuActionSink");
            sink = go.AddComponent<MenuActionSink>();

            return sink;
        }
    }
}
