using System;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    internal sealed class C2SurfaceDrawCall
    {
        public string NameLikeOriginal { get; }
        public C2SurfaceMeshBuilder Builder { get; }

        public C2SurfaceDrawCall(string nameLikeOriginal, C2SurfaceMeshBuilder builder)
        {
            NameLikeOriginal = string.IsNullOrWhiteSpace(nameLikeOriginal) ? "surface" : nameLikeOriginal;
            Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }
    }
}
