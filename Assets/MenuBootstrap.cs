using Cossacks2Bridge.Core;
using Cossacks2Bridge.Core.Loaders;
using Cossacks2Bridge.UnityAdapters.Renderers;
using System;
using UnityEngine;

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

        // Loaders
        private MainMenuLoader _mainMenuLoader;
        private OptionsLoader _optionsLoader;

        // Renderers
        private MainMenuRenderer _mainMenuRenderer;
        private OptionsRenderer _optionsRenderer;

        // Добавлено: рендерер для создания игрока
        private readonly NewPlayerRenderer _newPlayer = new NewPlayerRenderer();

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

            RenderByScreenId(startScreenId);
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
            _mainMenuLoader = new MainMenuLoader(_fs);
            _optionsLoader = new OptionsLoader(_fs);
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
        }

        public void RenderByScreenId(string screenId)
        {
            if (_fs == null)
            {
                Debug.LogError("[MenuBootstrap] Not initialized");
                return;
            }

            // 1=1: Single -> если профиля нет, открываем создание нового игрока
            if (string.Equals(screenId, "Single", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!HasAnyProfile())
                    screenId = "AddProfile"; // <AddProfile> -> M_PROF_ADD...
            }

            // Track navigation
            if (!string.IsNullOrWhiteSpace(screenId) && !string.Equals(CurrentScreenId, screenId))
            {
                PreviousScreenId = CurrentScreenId;
                CurrentScreenId = screenId;
            }

            var sink = GetOrCreateSink();
            UiDesk desk;

            // AddProfile рендерим отдельно, не трогая CanHandle(), чтобы не ломать другие окна
            if (string.Equals(screenId, "AddProfile", StringComparison.OrdinalIgnoreCase))
            {
                desk = _mainMenuLoader.LoadScreen(screenId);
                Debug.Log($"[MenuBootstrap] ADDPROFILE -> {desk.Children.Count} elements");
                _newPlayer.Render(desk, _fs, _renderOptions, sink, _loc);
                return;
            }

            // Выбираем loader и renderer
            if (_optionsLoader.CanHandle(screenId))
            {
                desk = _optionsLoader.LoadScreen(screenId);
                Debug.Log($"[MenuBootstrap] OPTIONS screen '{screenId}' -> {desk.Children.Count} elements");
                _optionsRenderer.Render(desk, _fs, _renderOptions, sink, _loc);
            }
            else if (_mainMenuLoader.CanHandle(screenId))
            {
                desk = _mainMenuLoader.LoadScreen(screenId);
                if (string.Equals(screenId, "AddProfile", System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log("=== ADDPROFILE NODES ===");
                    foreach (var n in desk.Children)
                        Debug.Log($"[ADD] {n.GetType().Name} name='{n.Name}' x={n.X} y={n.Y} w={n.Width} h={n.Height} vis={n.Visible}");
                }
                Debug.Log($"[MenuBootstrap] MAIN MENU screen '{screenId}' -> {desk.Children.Count} elements");

                // Выбираем рендерер: обычный или для создания игрока
                // AddProfile/M_PROF_ADD рисуем через OptionsRenderer (там есть все примитивы)
                if(screenId.Equals("AddProfile", StringComparison.OrdinalIgnoreCase))
{
                    _newPlayer.Render(desk, _fs, _renderOptions, sink, _loc);
                    return;
                }

                // обычные экраны главного меню
                _mainMenuRenderer.Render(desk, _fs, _renderOptions, sink, _loc);
                return;
            }
            else
            {
                Debug.LogWarning($"[MenuBootstrap] Unknown screen: '{screenId}', falling back to Main");
                desk = _mainMenuLoader.LoadScreen("Main");
                _mainMenuRenderer.Render(desk, _fs, _renderOptions, sink, _loc);
            }
        }

        private bool HasAnyProfile()
        {
            // Профили пока не реализованы — значит всегда "нет"
            return false;
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