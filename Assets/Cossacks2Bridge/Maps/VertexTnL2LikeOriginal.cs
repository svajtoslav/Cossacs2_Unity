using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    /// <summary>
    /// Unity-side analogue of gMotor VertexTnL2 for the surface path.
    /// Layout kept as close as possible to original: x,y,z,w,diffuse,u,v,u2,v2.
    /// In free mode X/Y/Z are world-like positions.
    /// In strict mode a separate transformed mesh is built from these canonical vertices.
    /// </summary>
    public readonly struct VertexTnL2LikeOriginal
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float W;
        public readonly uint Diffuse;
        public readonly float U;
        public readonly float V;
        public readonly float U2;
        public readonly float V2;

        public VertexTnL2LikeOriginal(
            float x,
            float y,
            float z,
            float w,
            uint diffuse,
            float u,
            float v,
            float u2,
            float v2)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
            Diffuse = diffuse;
            U = u;
            V = v;
            U2 = u2;
            V2 = v2;
        }

        public Vector3 GetWorldPosLikeOriginal() => new Vector3(X, Y, Z);
        public Vector2 GetUvLikeOriginal() => new Vector2(U, V);
        public Vector2 GetUv2LikeOriginal() => new Vector2(U2, V2);

        public static uint PackDiffuseLikeOriginal(Color32 color)
        {
            return ((uint)color.a << 24)
                 | ((uint)color.r << 16)
                 | ((uint)color.g << 8)
                 | color.b;
        }

        public static Color32 UnpackDiffuseLikeOriginal(uint diffuse)
        {
            return new Color32(
                (byte)((diffuse >> 16) & 0xFF),
                (byte)((diffuse >> 8) & 0xFF),
                (byte)(diffuse & 0xFF),
                (byte)((diffuse >> 24) & 0xFF));
        }
    }
}
