using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    // sgNode::SerializeSubtree, sgGeometry, kIOHelpers and sgController.
    // Node sizes delimit records; scanning for magic strings inside vertex data
    // loses ordinal references and skips LCTR parents used by artillery models.
    internal sealed class C2AnimatedModelV437LikeOriginal
    {
        internal sealed class Node
        {
            internal string Tag, Name;
            internal int Parent, Payload, End;
            internal int[] Children;
            internal ushort Flags;
            internal Matrix4x4 Local = Matrix4x4.identity;
        }
        internal sealed class Geometry
        {
            internal int NodeIndex;
            internal string Texture, EnvironmentTexture, DeviceState;
            internal Vector3[] Vertices, Normals;
            internal Vector2[] UV;
            internal Color32[] Colors;
            internal int[] Triangles;
        }
        internal Node[] Nodes;
        internal Geometry[] Meshes;

        internal static Node[] ReadNodes(byte[] bytes)
        {
            var nodes = new List<Node>();
            using (var r = new BinaryReader(new MemoryStream(bytes), Encoding.ASCII))
            {
                while (r.BaseStream.Position < bytes.Length)
                {
                    long start = r.BaseStream.Position;
                    string tag = Encoding.ASCII.GetString(r.ReadBytes(4));
                    int size = r.ReadInt32();
                    long end = start + 8L + size;
                    if (size < 14 || end > bytes.Length) throw new InvalidDataException("C2M node boundary: " + tag);
                    int length = r.ReadInt32();
                    if (length < 0 || length > 4096 || r.BaseStream.Position + length + 10 > end) throw new InvalidDataException("C2M node name");
                    string name = Encoding.ASCII.GetString(r.ReadBytes(length));
                    ushort flags = r.ReadUInt16(); int parent = r.ReadInt32(), count = r.ReadInt32();
                    if (count < 0 || r.BaseStream.Position + 4L * count > end) throw new InvalidDataException("C2M node children");
                    var children = new int[count]; for (int i = 0; i < count; i++) children[i] = r.ReadInt32();
                    nodes.Add(new Node { Tag = tag, Name = name, Flags = flags, Parent = parent, Children = children,
                        Payload = checked((int)r.BaseStream.Position), End = checked((int)end) });
                    r.BaseStream.Position = end;
                }
            }
            foreach (var node in nodes)
                if (node.Parent < -1 || node.Parent >= nodes.Count) throw new InvalidDataException("C2M parent outside tree");
            return nodes.ToArray();
        }

        internal static C2AnimatedModelV437LikeOriginal Read(byte[] bytes)
        {
            var model = new C2AnimatedModelV437LikeOriginal { Nodes = ReadNodes(bytes) };
            var meshes = new List<Geometry>();
            using (var r = new BinaryReader(new MemoryStream(bytes)))
            {
                // Rendering visits owned nodes and references in child order. A
                // texture state can be referenced by multiple geometry parents.
                string texture = null, environmentTexture = null, deviceState = null;
                var order = new List<int>(); CollectDrawOrder(model.Nodes, 0, order, new HashSet<int>());
                for (int i = 0; i < model.Nodes.Length; i++)
                {
                    var n = model.Nodes[i]; r.BaseStream.Position = n.Payload;
                    if (n.Tag == "MOVB" || n.Tag == "LCTR")
                    {
                        if (n.Payload + 48 > n.End) throw new InvalidDataException("Truncated transform: " + n.Name);
                        var matrix = Matrix4x4.identity;
                        // Source Matrix4D rows -> Unity columns.
                        for (int row = 0; row < 4; row++) for (int col = 0; col < 3; col++) matrix[col, row] = r.ReadSingle();
                        n.Local = matrix;
                    }
                }
                foreach (int i in order)
                {
                    var n = model.Nodes[i]; r.BaseStream.Position = n.Payload;
                    if (n.Tag == "TXRE")
                    {
                        // sgTexture: WORD stage. Stage 1 environment maps do not
                        // replace the stage 0 diffuse texture.
                        if (n.Payload + 2 <= n.End)
                        {
                            int stage=r.ReadUInt16();
                            if(stage==0) texture=n.Name;
                            else if(stage==1) environmentTexture=n.Name;
                        }
                        continue;
                    }
                    if(n.Tag=="DSST") {deviceState=n.Name;continue;}
                    if (n.Tag != "GEOM") continue;
                    if (n.Name.IndexOf("Navimesh", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.Name.IndexOf("Lockmesh", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if ((n.Flags & 0x20) != 0) continue;
                    var mesh = ReadGeometry(r, n, i); mesh.Texture = texture;
                    mesh.EnvironmentTexture=environmentTexture;mesh.DeviceState=deviceState;
                    meshes.Add(mesh);
                }
            }
            model.Meshes = meshes.ToArray(); return model;
        }

        private static void CollectDrawOrder(Node[] nodes, int index, List<int> result, HashSet<int> path)
        {
            if (index < 0 || index >= nodes.Length || !path.Add(index)) throw new InvalidDataException("C2M cyclic draw graph");
            if ((nodes[index].Flags & 0x20) != 0) { path.Remove(index); return; }
            result.Add(index);
            foreach (int child in nodes[index].Children)
                if (child != -1) CollectDrawOrder(nodes, child, result, path);
            path.Remove(index);
        }

        private static Geometry ReadGeometry(BinaryReader r, Node n, int index)
        {
            int nv = r.ReadInt32(), ni = r.ReadInt32(), np = r.ReadInt32(); r.ReadByte();
            int format = r.ReadInt32(), primitive = r.ReadInt32();
            // Actual artillery GEOM records use vfN (3). Explicitly reject other
            // formats instead of guessing a stride or displaying corrupt meshes.
            if (format != 3 || primitive != 4) throw new InvalidDataException("Unsupported artillery vertex/primitive format: " + format + "/" + primitive + " " + n.Name);
            if (nv < 0 || ni < 0 || ni % 3 != 0 || np != ni / 3 || r.BaseStream.Position + 32L * nv + 2L * ni != n.End)
                throw new InvalidDataException("C2M geometry bounds: " + n.Name);
            var mesh = new Geometry { NodeIndex = index, Vertices = new Vector3[nv], Normals = new Vector3[nv], UV = new Vector2[nv],
                Colors = new Color32[nv], Triangles = new int[ni] };
            for (int v = 0; v < nv; v++)
            {
                mesh.Vertices[v] = Vector(r); mesh.Normals[v] = Vector(r);
                mesh.UV[v] = new Vector2(r.ReadSingle(), 1 - r.ReadSingle()); mesh.Colors[v] = new Color32(255,255,255,255);
            }
            for (int i = 0; i < ni; i++)
            {
                int vertex = r.ReadUInt16(); if (vertex >= nv) throw new InvalidDataException("C2M triangle index");
                mesh.Triangles[i] = vertex;
            }
            return mesh;
        }
        private static Vector3 Vector(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

        internal void Evaluate(C2ModelAnimationV437LikeOriginal animation, float time, Matrix4x4[] output)
        {
            if (output.Length != Nodes.Length) throw new ArgumentException("Model matrix count");
            // Parents precede children in Node::PreSerialize. Reject incompatible
            // input explicitly rather than silently using an old matrix.
            for (int i = 0; i < Nodes.Length; i++)
            {
                var n = Nodes[i];
                if (n.Parent >= i) throw new InvalidDataException("Model parent must precede child: " + n.Name);
                C2ModelAnimationV437LikeOriginal.Track track;
                var local = animation != null && animation.Tracks.TryGetValue(n.Name, out track) ? track.At(time) : n.Local;
                output[i] = n.Parent < 0 ? local : output[n.Parent] * local;
            }
        }
    }

    internal sealed class C2ModelAnimationV437LikeOriginal
    {
        internal sealed class Curve
        {
            internal Vector4 Default;
            internal float[] Times;
            internal Vector4[] Values;
            internal bool Quaternion;
            internal Vector4 At(float time)
            {
                if (Times.Length == 0) return Default;
                int i = Array.BinarySearch(Times, time); if (i >= 0) return Values[i]; i = ~i - 1;
                if (i < 0) return Values[0]; if (i + 1 >= Times.Length) return Values[i];
                float t = (time - Times[i]) / (Times[i+1] - Times[i]);
                var a = Values[i]; var b = Values[i+1];
                if (!Quaternion) return Vector4.LerpUnclamped(a, b, t);
                var q = UnityEngine.Quaternion.SlerpUnclamped(new Quaternion(a.x,a.y,a.z,a.w), new Quaternion(b.x,b.y,b.z,b.w), t);
                return new Vector4(q.x,q.y,q.z,q.w);
            }
        }
        internal sealed class Track
        {
            internal Curve X,Y,Z,Rotation,SX,SY,SZ;
            internal Matrix4x4 At(float time)
            {
                var q = Rotation.At(time);
                return Matrix4x4.TRS(new Vector3(X.At(time).x,Y.At(time).x,Z.At(time).x),
                    new Quaternion(q.x,q.y,q.z,q.w), new Vector3(SX.At(time).x,SY.At(time).x,SZ.At(time).x));
            }
        }
        internal readonly Dictionary<string, Track> Tracks = new Dictionary<string, Track>(StringComparer.OrdinalIgnoreCase);
        internal float Duration;
        internal static C2ModelAnimationV437LikeOriginal Read(byte[] bytes)
        {
            var animation = new C2ModelAnimationV437LikeOriginal();
            var nodes = C2AnimatedModelV437LikeOriginal.ReadNodes(bytes);
            using (var r = new BinaryReader(new MemoryStream(bytes)))
            foreach (var n in nodes)
            {
                if (n.Tag != "PRSA" && n.Tag != "ANMB" && n.Tag != "ANIM") continue;
                r.BaseStream.Position = n.Payload; r.ReadSingle(); float duration = r.ReadSingle(); r.ReadSingle();
                if (n.Parent < 0) animation.Duration = duration;
                if (n.Tag != "PRSA") continue;
                var track = new Track { X = ReadCurve(r, false, n.End), Y = ReadCurve(r, false, n.End), Z = ReadCurve(r, false, n.End),
                    Rotation = ReadCurve(r, true, n.End), SX = ReadCurve(r, false, n.End), SY = ReadCurve(r, false, n.End), SZ = ReadCurve(r, false, n.End) };
                int length = r.ReadInt32();
                if (length < 0 || r.BaseStream.Position + length != n.End) throw new InvalidDataException("PRS tail boundary: " + n.Name);
                r.ReadBytes(length);
                // FindByName binding in AnimBlock is case insensitive.
                if (!animation.Tracks.ContainsKey(n.Name)) animation.Tracks.Add(n.Name, track);
            }
            return animation;
        }
        private static Curve ReadCurve(BinaryReader r, bool quaternion, int end)
        {
            var curve = new Curve { Quaternion = quaternion, Default = ReadValue(r, quaternion) };
            int count = r.ReadInt32();
            if (count < 0 || r.BaseStream.Position + count * 4L + 4 > end) throw new InvalidDataException("PRS time bounds");
            curve.Times = new float[count];
            for (int i = 0; i < count; i++)
            {
                curve.Times[i] = r.ReadSingle();
                if (i != 0 && curve.Times[i] < curve.Times[i-1]) throw new InvalidDataException("PRS time order");
            }
            int values = r.ReadInt32();
            if (values != count || r.BaseStream.Position + count * (quaternion ? 16L : 4L) > end) throw new InvalidDataException("PRS value bounds");
            curve.Values = new Vector4[count];
            for (int i = 0; i < count; i++) curve.Values[i] = ReadValue(r, quaternion);
            return curve;
        }
        private static Vector4 ReadValue(BinaryReader r, bool quaternion)
        {
            return quaternion ? new Vector4(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle()) : new Vector4(r.ReadSingle(),0,0,0);
        }
    }
}
