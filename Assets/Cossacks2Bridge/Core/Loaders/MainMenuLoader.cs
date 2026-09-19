using System;

namespace Cossacks2Bridge.Core.Loaders
{
    /// <summary>
    /// Compatibility wrapper. V388 has one real menu parser: Menu14UnifiedLoader.
    /// </summary>
    public sealed class MainMenuLoader
    {
        private readonly Menu14UnifiedLoader _unified;

        public MainMenuLoader(CoreFileSystem fs)
        {
            _unified = new Menu14UnifiedLoader(fs);
        }

        public bool CanHandle(string screenId)
        {
            return !string.IsNullOrWhiteSpace(screenId);
        }

        public UiDesk LoadScreen(string screenId) => _unified.LoadScreen(screenId);
    }
}
