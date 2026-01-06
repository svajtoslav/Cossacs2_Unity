using Cossacks2Bridge.Core;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Renderers
{
    /// <summary>
    /// Рендерер главного меню
    /// </summary>
    public sealed class MainMenuRenderer : BaseUiRenderer
    {
        public override void Render(UiDesk desk, CoreFileSystem fs, RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            RenderCounter++;
            Log(opt, $"[MainMenuRenderer] Render #{RenderCounter} source='{desk?.SourcePath}' children={desk?.Children?.Count ?? 0}");

            var root = CreateCanvas("C2_MainMenuCanvas", opt);

            // Рендерим фоны
            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is UiBitPicture pic)
                    CreateBitPicture(root, pic, fs, opt);
            }

            // Рендерим кнопки
            foreach (var node in desk.Children)
            {
                if (!node.Visible) continue;
                if (node is UiTextButton btn)
                    CreateTextButton(root, btn, opt, sink, loc, MenuOverrideDb.Resolve);
            }
        }
    }
}