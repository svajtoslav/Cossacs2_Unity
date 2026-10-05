// C2UnitPortStage1SettlementReflectionAdapterV235.cs
// Minimal reflection bridge for C2UnitOriginalRuntimeAndRendererV1.
// Does NOT draw buildings and does NOT replace current V234 building renderer.
// It only exposes the old method names that the unit runtime expects:
//   C2Settlement3InuMdV2TryParseRecordsLikeOriginal
//   C2Settlement3InuMdV2ResolveMdLikeOriginal
//   C2Settlement3InuMdV2WorldLikeOriginal

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const int C2UnitPortV235_3InuRecordSize = 54;

        private enum C2Settlement3InuMdV2Kind
        {
            Unknown,
            SettlementBuilding,
            Building,
            ResourceBuilding,
            Unit,
            Animal,
            SpriteObject
        }

        private struct C2Settlement3InuMdV2Record
        {
            public int Index;
            public byte Nation;
            public ushort NIndex;
            public int RealX;
            public int RealY;
            public ushort Life;
            public ushort Stage;
            public short WallX;
            public short WallY;
            public byte RealDir;
            public byte Flags;
            public string MonsterId;
        }

        private sealed class C2Settlement3InuMdV2Info
        {
            public bool Found;
            public string MdPath = string.Empty;
            public string MdName = string.Empty;
            public string Package = string.Empty;
            public C2Settlement3InuMdV2Kind Kind = C2Settlement3InuMdV2Kind.Unknown;
            public bool Building;
            public bool SpriteObject;
            public bool Peasant;
            public bool NotSelectable;
            public string Audit = string.Empty;
        }

        private static readonly Dictionary<string, C2Settlement3InuMdV2Info> s_C2UnitPortV235MdCache =
            new Dictionary<string, C2Settlement3InuMdV2Info>(StringComparer.OrdinalIgnoreCase);

        private static bool C2Settlement3InuMdV2TryParseRecordsLikeOriginal(
            string absMap,
            out List<C2Settlement3InuMdV2Record> records,
            out string audit)
        {
            records = new List<C2Settlement3InuMdV2Record>();
            audit = string.Empty;

            if (string.IsNullOrWhiteSpace(absMap) || !File.Exists(absMap))
            {
                audit = "map_not_found:" + (absMap ?? string.Empty);
                return false;
            }

            byte[] raw;
            try { raw = File.ReadAllBytes(absMap); }
            catch (Exception e)
            {
                audit = "read_error=" + e.GetType().Name + ":" + e.Message;
                return false;
            }

            byte[] data = MaybeDecompressM3d(raw, out string error);
            if (data == null || data.Length < 16)
            {
                audit = "bad_data:" + (error ?? string.Empty);
                return false;
            }

            using (var ms = new MemoryStream(data, false))
            using (var br = new BinaryReader(ms))
            {
                string magic = ReadTag(br);
                int storedVertInLine = 0;
                int storedMaxTh = 0;
                if (ms.Position + 8 <= ms.Length)
                {
                    storedVertInLine = br.ReadInt32();
                    storedMaxTh = br.ReadInt32();
                }

                int chunks = 0;
                int unitChunks = 0;
                var seen = new List<string>(64);

                while (ms.Position + 8 <= ms.Length)
                {
                    long chunkStart = ms.Position;
                    string tag = ReadTag(br);

                    if (string.Equals(tag, "ENDM", StringComparison.Ordinal) ||
                        string.Equals(tag, "MDNE", StringComparison.Ordinal))
                        break;

                    int sizeField = br.ReadInt32();
                    int payloadLen = Mathf.Max(0, sizeField - 4);
                    long payloadStart = ms.Position;
                    long payloadEnd = payloadStart + payloadLen;
                    if (payloadEnd > ms.Length)
                    {
                        audit = "broken_chunk tag=" + tag +
                                " start=" + chunkStart.ToString(CultureInfo.InvariantCulture) +
                                " size=" + sizeField.ToString(CultureInfo.InvariantCulture) +
                                " parsed=" + records.Count.ToString(CultureInfo.InvariantCulture);
                        return records.Count > 0;
                    }

                    chunks++;
                    if (seen.Count < 64)
                        seen.Add(tag + ":" + sizeField.ToString(CultureInfo.InvariantCulture));

                    if (TagEqualsLikeOriginal(tag, "3INU", "UNI3") && payloadLen >= 4)
                    {
                        int declared = br.ReadInt32();
                        int possible = Mathf.Max(0, (payloadLen - 4) / C2UnitPortV235_3InuRecordSize);
                        int count = Mathf.Clamp(declared, 0, possible);
                        long recordsEnd = Math.Min(payloadEnd, ms.Position + (long)count * C2UnitPortV235_3InuRecordSize);

                        for (int i = 0; i < count; i++)
                        {
                            if (ms.Position + C2UnitPortV235_3InuRecordSize > recordsEnd)
                                break;

                            var r = new C2Settlement3InuMdV2Record();
                            r.Index = records.Count;
                            r.Nation = br.ReadByte();
                            r.NIndex = br.ReadUInt16();
                            r.RealX = br.ReadInt32();
                            r.RealY = br.ReadInt32();
                            r.Life = br.ReadUInt16();
                            r.Stage = br.ReadUInt16();
                            r.WallX = br.ReadInt16();
                            r.WallY = br.ReadInt16();
                            r.RealDir = br.ReadByte();
                            r.Flags = br.ReadByte();
                            r.MonsterId = DecodeCString1251LikeOriginal(br.ReadBytes(33));
                            records.Add(r);
                        }

                        unitChunks++;
                    }

                    ms.Position = payloadEnd;
                }

                audit = "v235_unit_adapter magic=" + magic +
                        " stored=" + storedVertInLine.ToString(CultureInfo.InvariantCulture) + "x" + storedMaxTh.ToString(CultureInfo.InvariantCulture) +
                        " chunks=" + chunks.ToString(CultureInfo.InvariantCulture) +
                        " unitChunks=" + unitChunks.ToString(CultureInfo.InvariantCulture) +
                        " records=" + records.Count.ToString(CultureInfo.InvariantCulture) +
                        " seen=" + string.Join(",", seen.ToArray());

                return records.Count > 0;
            }
        }

        private static C2Settlement3InuMdV2Info C2Settlement3InuMdV2ResolveMdLikeOriginal(string monsterId)
        {
            string key = string.IsNullOrWhiteSpace(monsterId) ? "<empty>" : monsterId.Trim();
            if (s_C2UnitPortV235MdCache.TryGetValue(key, out C2Settlement3InuMdV2Info cached))
                return cached;

            var info = new C2Settlement3InuMdV2Info();
            info.MdName = key;
            info.Kind = C2UnitPortV235GuessKindLikeOriginal(key);

            string path = string.Empty;
            try { path = FindBuildingMdPathLikeOriginal(key); }
            catch { path = string.Empty; }

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                info.Found = false;
                info.Audit = "md_not_found";
                s_C2UnitPortV235MdCache[key] = info;
                return info;
            }

            info.Found = true;
            info.MdPath = path;
            info.MdName = Path.GetFileNameWithoutExtension(path);
            C2UnitPortV235ParseMdLightLikeOriginal(info, path);
            if (info.Kind == C2Settlement3InuMdV2Kind.Unknown)
                info.Kind = C2UnitPortV235GuessKindLikeOriginal(key);

            s_C2UnitPortV235MdCache[key] = info;
            return info;
        }

        private Vector3 C2Settlement3InuMdV2WorldLikeOriginal(C2Settlement3InuMdV2Record r)
        {
            return WallOriginalXYToWorldV1LikeOriginal(r.RealX >> 4, r.RealY >> 4, 0.0f);
        }

        private static C2Settlement3InuMdV2Kind C2UnitPortV235GuessKindLikeOriginal(string name)
        {
            string n = (name ?? string.Empty).ToLowerInvariant();

            if (n.StartsWith("unit", StringComparison.Ordinal) ||
                n.IndexOf("kri", StringComparison.Ordinal) >= 0 ||
                n.IndexOf("sold", StringComparison.Ordinal) >= 0 ||
                n.IndexOf("fus", StringComparison.Ordinal) >= 0 ||
                n.IndexOf("gren", StringComparison.Ordinal) >= 0 ||
                n.IndexOf("drag", StringComparison.Ordinal) >= 0 ||
                n.IndexOf("horse", StringComparison.Ordinal) >= 0 ||
                n.IndexOf("cannon", StringComparison.Ordinal) >= 0)
                return C2Settlement3InuMdV2Kind.Unit;

            if (n.StartsWith("anm", StringComparison.Ordinal) ||
                n.IndexOf("ovc", StringComparison.Ordinal) >= 0 ||
                n.IndexOf("swi", StringComparison.Ordinal) >= 0 ||
                n.IndexOf("kor", StringComparison.Ordinal) >= 0)
                return C2Settlement3InuMdV2Kind.Animal;

            if (n.IndexOf("seldom", StringComparison.Ordinal) >= 0 ||
                (n.StartsWith("sel", StringComparison.Ordinal) && n.IndexOf("dom", StringComparison.Ordinal) >= 0))
                return C2Settlement3InuMdV2Kind.SettlementBuilding;

            if (n.StartsWith("bld", StringComparison.Ordinal) ||
                n.IndexOf("build", StringComparison.Ordinal) >= 0 ||
                n.IndexOf("mine", StringComparison.Ordinal) >= 0 ||
                n.IndexOf("rud", StringComparison.Ordinal) >= 0 ||
                n.IndexOf("mel", StringComparison.Ordinal) >= 0)
                return C2Settlement3InuMdV2Kind.Building;

            return C2Settlement3InuMdV2Kind.Unknown;
        }

        private static void C2UnitPortV235ParseMdLightLikeOriginal(C2Settlement3InuMdV2Info info, string path)
        {
            string[] lines;
            try { lines = File.ReadAllLines(path, Encoding.GetEncoding(1251)); }
            catch
            {
                try { lines = File.ReadAllLines(path); }
                catch { return; }
            }

            for (int i = 0; lines != null && i < lines.Length; i++)
            {
                string line = StripCommentLikeOriginal(lines[i]).Trim();
                if (line.Length == 0 || line[0] == '/')
                    continue;

                string[] t = SplitTokensLikeOriginal(line);
                if (t == null || t.Length == 0)
                    continue;

                string cmd = (t[0] ?? string.Empty).Trim().ToUpperInvariant();

                if (cmd == "BUILDING")
                {
                    info.Building = true;
                    if (info.Kind == C2Settlement3InuMdV2Kind.Unknown)
                        info.Kind = C2Settlement3InuMdV2Kind.Building;
                }
                else if (cmd == "SPRITEOBJECT")
                {
                    info.Building = true;
                    info.SpriteObject = true;
                    info.Kind = C2Settlement3InuMdV2Kind.SpriteObject;
                }
                else if (cmd == "PEASANT" || cmd == "UNIT")
                {
                    info.Peasant = cmd == "PEASANT";
                    info.Building = false;
                    info.Kind = C2Settlement3InuMdV2Kind.Unit;
                }
                else if (cmd == "NOTSELECTABLE")
                {
                    info.NotSelectable = true;
                }
                else if ((cmd == "USERLC" || cmd == "USERLCEXT") && string.IsNullOrWhiteSpace(info.Package))
                {
                    for (int k = 1; k < t.Length; k++)
                    {
                        string v = (t[k] ?? string.Empty).Trim();
                        if (v.Length == 0)
                            continue;
                        if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                            continue;
                        info.Package = v;
                        break;
                    }
                }
            }
        }

        public static bool C2BuildingMotionFieldV46TryGetProducedUnitExitPathForBuildingRealLikeOriginal(
            int buildingRecordIndex,
            string buildingMd,
            int buildingRealX,
            int buildingRealY,
            out Vector2[] path,
            out string audit)
        {
            return C2BuildingRuntimeInfoV247LikeOriginal.TryGetProducedUnitExitPathForBuildingRealLikeOriginal(
                buildingRecordIndex,
                buildingMd,
                buildingRealX,
                buildingRealY,
                out path,
                out audit);
        }
    }
}
