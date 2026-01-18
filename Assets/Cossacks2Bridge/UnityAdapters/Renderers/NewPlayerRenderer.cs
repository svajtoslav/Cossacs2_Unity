using Cossacks2Bridge.Core;

namespace Cossacks2Bridge.UnityAdapters.Renderers
{
    // Рисуем AddProfile через OptionsRenderer, но без Screen.resolutions
    public sealed class NewPlayerRenderer
    {
        private readonly OptionsRenderer _inner = new OptionsRenderer();

        public void Render(UiDesk desk, CoreFileSystem fs, BaseUiRenderer.RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            // копия опций, чтобы не затронуть остальные экраны
            var localOpt = new BaseUiRenderer.RenderOptions
            {
                FontResourcePath = opt.FontResourcePath,
                FontSize = opt.FontSize,
                NormalColor = opt.NormalColor,
                HoverColor = opt.HoverColor,
                DisabledColor = opt.DisabledColor,
                CanvasScaleMode = opt.CanvasScaleMode,
                ReferenceResolution = opt.ReferenceResolution,
                VerboseLogs = opt.VerboseLogs,
                DrawDebugOutline = opt.DrawDebugOutline,

                FillResolutionCombos = false
            };

            _inner.Render(desk, fs, localOpt, sink, loc);
        }
    }
}