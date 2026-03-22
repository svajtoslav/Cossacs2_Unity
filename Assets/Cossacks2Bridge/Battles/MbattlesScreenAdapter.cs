
using Cossacks2Bridge.Core;
using Cossacks2Bridge.UnityAdapters.Renderers;

namespace Cossacks2Bridge.UnityAdapters.Battles
{
    public sealed class MbattlesScreenAdapter
    {
        private readonly MbattlesXmlRenderer _renderer = new MbattlesXmlRenderer();

        public bool TryRender(CoreFileSystem fs, BaseUiRenderer.RenderOptions opt, IUiActionSink sink, LocDb loc)
        {
            if (fs == null) return false;

            var loader = new MbattlesXmlLoader(fs, loc);
            MbScene scene = loader.Load();
            _renderer.Render(scene, fs, opt, sink, loc);
            return true;
        }
    }
}
