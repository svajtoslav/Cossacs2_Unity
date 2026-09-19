using System;

namespace Cossacks2Bridge.Core.Loaders
{
    /// <summary>
    /// Compatibility wrapper. V388 has one real menu parser: Menu14UnifiedLoader.
    /// </summary>
    public sealed class OptionsLoader
    {
        private readonly Menu14UnifiedLoader _unified;

        public OptionsLoader(CoreFileSystem fs)
        {
            _unified = new Menu14UnifiedLoader(fs);
        }

        public bool CanHandle(string screenId)
        {
            if (string.IsNullOrWhiteSpace(screenId)) return false;
            return screenId.Equals("Options", StringComparison.OrdinalIgnoreCase)
                || screenId.StartsWith("Options_", StringComparison.OrdinalIgnoreCase)
                || screenId.StartsWith("Options/", StringComparison.OrdinalIgnoreCase)
                || screenId.Equals("Multi", StringComparison.OrdinalIgnoreCase);
        }

        public UiDesk LoadScreen(string screenId) => _unified.LoadScreen(screenId);
    }
}
