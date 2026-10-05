using System;

namespace Cossacks2Bridge.Core.Loaders
{
    /// <summary>
    /// V388 tombstone for the old partial direct-child reader.
    /// All menu XML parsing now lives in Menu14UnifiedLoader.
    /// Kept as an empty type only so Unity keeps the existing script/meta asset
    /// stable across the patch instead of leaving a missing-script GUID behind.
    /// </summary>
    [Obsolete("Use Menu14UnifiedLoader; this parser was retired in V388.")]
    internal static class Menu14DirectXml
    {
    }
}
