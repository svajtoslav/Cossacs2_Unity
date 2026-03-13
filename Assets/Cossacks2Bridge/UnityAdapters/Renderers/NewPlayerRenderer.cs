using Cossacks2Bridge.Core;
using UnityEngine;
using Cossacks2Bridge.UnityAdapters.AddProfile;

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

            // AddProfile extras: commander portraits + description + scrollers
            var canvas = UnityEngine.GameObject.Find("C2_OptionsCanvas");
            if (canvas != null)
            {
                if (canvas.GetComponent<Cossacks2Bridge.UnityAdapters.AddProfile.AddProfileCommanderController>() == null)
                    canvas.AddComponent<Cossacks2Bridge.UnityAdapters.AddProfile.AddProfileCommanderController>();
            }
        }
    }
}