using System;
using System.Xml.Linq;
using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    public sealed partial class C2BattleTerrainMode
    {
        private const int StrictSurfaceTriUnitLikeOriginal = 16;

        private Color32 _strictSurfaceSunColor = new Color32(128, 128, 128, 255);
        private Color32 _strictSurfaceShadowColor = new Color32(58, 66, 72, 255);
        private byte[] _strictSurfaceLightMap;
        private bool _strictSurfaceLightMapReady;
        private bool _strictSurfaceLightingStateLoaded;
        private string _strictSurfaceLightMapSourceKey = string.Empty;
        private bool _strictSurfaceLightingDebugLogged;

        private void EnsureStrictSurfaceLightingStateLikeOriginal()
        {
            if (_strictSurfaceLightingStateLoaded)
                return;

            _strictSurfaceLightingStateLoaded = true;
            _strictSurfaceSunColor = new Color32(128, 128, 128, 255);
            _strictSurfaceShadowColor = new Color32(58, 66, 72, 255);

            if (!C2GlobalLighting.IsInitialized)
                C2GlobalLighting.SetLightLikeOriginal(0, 20, 30);

            var fs = _bootstrap != null ? _bootstrap.Fs : null;
            if (fs == null)
                return;

            string settingsPath = fs.Exists("EngineSettings.xml") ? "EngineSettings.xml" : (fs.Exists("enginesettings.xml") ? "enginesettings.xml" : null);
            if (string.IsNullOrWhiteSpace(settingsPath))
                return;

            try
            {
                string xml = fs.ReadAllText(settingsPath);
                if (string.IsNullOrWhiteSpace(xml))
                    return;

                var doc = XDocument.Parse(xml);
                XElement root = doc.Root;
                if (root == null)
                    return;

                _strictSurfaceShadowColor = ReadEngineSettingsColorLikeOriginal(doc, "ShadowsColor", _strictSurfaceShadowColor);
                _strictSurfaceSunColor = ReadEngineSettingsColorLikeOriginal(doc, "SunColor", _strictSurfaceSunColor);
            }
            catch
            {
            }
        }

        private void EnsureStrictSurfaceLightMapLikeOriginal()
        {
            EnsureStrictSurfaceLightingStateLikeOriginal();

            ParsedMap runtimeMapLikeOriginal = ResolveLiteralITerraRuntimeMapLikeOriginal();
            if (runtimeMapLikeOriginal == null || runtimeMapLikeOriginal.Heights == null || runtimeMapLikeOriginal.Heights.Length == 0 || runtimeMapLikeOriginal.VertInLine <= 0 || runtimeMapLikeOriginal.MaxTH <= 0)
            {
                _strictSurfaceLightMap = null;
                _strictSurfaceLightMapReady = true;
                _strictSurfaceLightMapSourceKey = string.Empty;
                return;
            }

            string mapKey = BuildStrictSurfaceLightMapSourceKeyLikeOriginal(runtimeMapLikeOriginal);
            int expectedSize = runtimeMapLikeOriginal.VertInLine * runtimeMapLikeOriginal.MaxTH;
            if (_strictSurfaceLightMapReady && string.Equals(_strictSurfaceLightMapSourceKey, mapKey, StringComparison.Ordinal) && _strictSurfaceLightMap != null && _strictSurfaceLightMap.Length == expectedSize)
                return;

            _strictSurfaceLightMapSourceKey = mapKey;
            _strictSurfaceLightMapReady = true;
            CreateLightMapLikeOriginal();
            LogStrictSurfaceLightingDebugV2LikeAdapted(runtimeMapLikeOriginal);
        }

        private static string BuildStrictSurfaceLightMapSourceKeyLikeOriginal(ParsedMap map)
        {
            if (map == null)
                return string.Empty;
            string src = map.SourcePath ?? string.Empty;
            int hlen = map.Heights != null ? map.Heights.Length : 0;
            return src + "|" + map.VertInLine + "|" + map.MaxTH + "|" + hlen;
        }

        private int _GetHiLikeOriginal(int i)
        {
            ParsedMap runtimeMapLikeOriginal = ResolveLiteralITerraRuntimeMapLikeOriginal();
            if (runtimeMapLikeOriginal == null || runtimeMapLikeOriginal.Heights == null || runtimeMapLikeOriginal.Heights.Length == 0)
                return 0;
            if (i < 0 || i >= runtimeMapLikeOriginal.Heights.Length)
                return 0;
            return runtimeMapLikeOriginal.Heights[i];
        }

        private int GetLighting3D0LikeOriginal()
        {
            int lig = C2GlobalLighting.LightDZ;
            if (lig < 150) lig = 150;
            if (lig > 250) lig = 250;
            return lig;
        }

        private int GetLighting3DVLikeOriginal(int dx, int dy, int dz)
        {
            double denom = Math.Sqrt((double)dx * dx + (double)dy * dy + (double)dz * dz);
            if (denom < 1.0)
                denom = 1.0;
            int lig = (int)((dx * C2GlobalLighting.LightDX + dy * C2GlobalLighting.LightDY + dz * C2GlobalLighting.LightDZ) / denom);
            if (lig < 120) lig = 120;
            if (lig > 250) lig = 250;
            return lig;
        }

        private void ScanLightOffsetLikeOriginal(int x0, int y0)
        {
            ParsedMap runtimeMapLikeOriginal = ResolveLiteralITerraRuntimeMapLikeOriginal();
            if (runtimeMapLikeOriginal == null || _strictSurfaceLightMap == null)
                return;

            const int dd = 10;
            int hMax = 0;
            int ofs = x0 + y0 * runtimeMapLikeOriginal.VertInLine;
            int hp = 0;
            int h = 0;
            while (x0 >= 0 && y0 >= 0)
            {
                hp = h;
                int x = ofs % runtimeMapLikeOriginal.VertInLine;
                int y = ofs / runtimeMapLikeOriginal.VertInLine;
                y = (x & 1) != 0 ? (y << 5) - 16 : (y << 5);
                x = x << 5;
                h = GetStrictTotalHeightLikeOriginal(x, y);
                if (h > hMax) hMax = h;
                int dh = hMax - h;
                if (dh > 0)
                {
                    dh *= 2 + Math.Abs(hp - h) / 2;
                    if (dh > 100) dh = 100;
                    _strictSurfaceLightMap[ofs] = (byte)(255 - dh);
                }
                else
                {
                    _strictSurfaceLightMap[ofs] = 255;
                }
                if ((x0 & 1) != 0)
                {
                    ofs -= runtimeMapLikeOriginal.VertInLine + 1;
                    y0--;
                }
                else
                {
                    ofs--;
                }
                x0--;
                hMax -= dd;
            }
        }

        private void CreateLightMapLikeOriginal()
        {
            ParsedMap runtimeMapLikeOriginal = ResolveLiteralITerraRuntimeMapLikeOriginal();
            if (runtimeMapLikeOriginal == null || runtimeMapLikeOriginal.VertInLine <= 0 || runtimeMapLikeOriginal.MaxTH <= 0)
            {
                _strictSurfaceLightMap = null;
                return;
            }

            int size = runtimeMapLikeOriginal.VertInLine * runtimeMapLikeOriginal.MaxTH;
            byte[] tempL = new byte[size];
            if (_strictSurfaceLightMap == null || _strictSurfaceLightMap.Length != size)
                _strictSurfaceLightMap = new byte[size];
            for (int i = 0; i < size; i++)
                tempL[i] = 255;

            for (int y = runtimeMapLikeOriginal.MaxTH - 1; y > 0; y--)
                ScanLightOffsetLikeOriginal(runtimeMapLikeOriginal.VertInLine - 1, y);
            for (int x = 0; x < runtimeMapLikeOriginal.VertInLine - 1; x++)
                ScanLightOffsetLikeOriginal(x, runtimeMapLikeOriginal.MaxTH - 1);

            for (int t = 0; t < 1; t++)
            {
                int ofs = 0;
                for (int iy = 0; iy < runtimeMapLikeOriginal.MaxTH; iy++)
                {
                    for (int ix = 0; ix < runtimeMapLikeOriginal.VertInLine; ix++)
                    {
                        if (ix > 0 && iy > 0 && ix < runtimeMapLikeOriginal.VertInLine - 3 && iy < runtimeMapLikeOriginal.MaxTH - 3)
                        {
                            if ((ix & 1) != 0)
                            {
                                tempL[ofs] = (byte)(
                                    ((int)_strictSurfaceLightMap[ofs + runtimeMapLikeOriginal.VertInLine] +
                                     (int)_strictSurfaceLightMap[ofs] +
                                     (int)_strictSurfaceLightMap[ofs - 1] +
                                     (int)_strictSurfaceLightMap[ofs + 1] +
                                     (int)_strictSurfaceLightMap[ofs - runtimeMapLikeOriginal.VertInLine - 1] +
                                     (int)_strictSurfaceLightMap[ofs - runtimeMapLikeOriginal.VertInLine] +
                                     (int)_strictSurfaceLightMap[ofs - runtimeMapLikeOriginal.VertInLine + 1]) / 7);
                            }
                            else
                            {
                                tempL[ofs] = (byte)(
                                    ((int)_strictSurfaceLightMap[ofs - runtimeMapLikeOriginal.VertInLine] +
                                     (int)_strictSurfaceLightMap[ofs] +
                                     (int)_strictSurfaceLightMap[ofs - 1] +
                                     (int)_strictSurfaceLightMap[ofs + 1] +
                                     (int)_strictSurfaceLightMap[ofs + runtimeMapLikeOriginal.VertInLine - 1] +
                                     (int)_strictSurfaceLightMap[ofs + runtimeMapLikeOriginal.VertInLine] +
                                     (int)_strictSurfaceLightMap[ofs + runtimeMapLikeOriginal.VertInLine + 1]) / 7);
                            }
                        }
                        ofs++;
                    }
                }
                Buffer.BlockCopy(tempL, 0, _strictSurfaceLightMap, 0, size);
            }
        }

        private int GetLighting3DLikeOriginal(int i)
        {
            ParsedMap runtimeMapLikeOriginal = ResolveLiteralITerraRuntimeMapLikeOriginal();
            if (runtimeMapLikeOriginal == null || runtimeMapLikeOriginal.Heights == null || runtimeMapLikeOriginal.Heights.Length == 0 || runtimeMapLikeOriginal.VertInLine <= 0)
                return 128;

            EnsureStrictSurfaceLightMapLikeOriginal();

            int h1, h2, h3, h4, h5, h6;
            int vp = i % runtimeMapLikeOriginal.VertInLine;
            if ((vp & 1) == 0)
            {
                h1 = _GetHiLikeOriginal(i + runtimeMapLikeOriginal.VertInLine);
                h2 = _GetHiLikeOriginal(i + runtimeMapLikeOriginal.VertInLine + 1);
                h3 = _GetHiLikeOriginal(i + 1);
                h4 = _GetHiLikeOriginal(i - runtimeMapLikeOriginal.VertInLine);
                h5 = _GetHiLikeOriginal(i - 1);
                h6 = _GetHiLikeOriginal(i + runtimeMapLikeOriginal.VertInLine - 1);
            }
            else
            {
                h1 = _GetHiLikeOriginal(i + runtimeMapLikeOriginal.VertInLine);
                h2 = _GetHiLikeOriginal(i + 1);
                h3 = _GetHiLikeOriginal(i - runtimeMapLikeOriginal.VertInLine + 1);
                h4 = _GetHiLikeOriginal(i - runtimeMapLikeOriginal.VertInLine);
                h5 = _GetHiLikeOriginal(i - runtimeMapLikeOriginal.VertInLine - 1);
                h6 = _GetHiLikeOriginal(i - 1);
            }

            int dy = h4 - h1;
            int dx = (h2 - h5 + h3 - h6) >> 1;
            int dz = StrictSurfaceTriUnitLikeOriginal + StrictSurfaceTriUnitLikeOriginal + StrictSurfaceTriUnitLikeOriginal + StrictSurfaceTriUnitLikeOriginal;
            int lig = GetLighting3DVLikeOriginal(dx, dy, dz);
            if (_strictSurfaceLightMap != null)
            {
                int maxPointIndex = runtimeMapLikeOriginal.Heights.Length;
                int l0;
                if (i >= runtimeMapLikeOriginal.VertInLine * 2 && i < maxPointIndex - 4) l0 = _strictSurfaceLightMap[i - runtimeMapLikeOriginal.VertInLine];
                else l0 = 0;
                if (l0 < lig) lig = l0 - ((255 - lig) / 5);
            }
            if (C2GlobalLighting.TL0 == -1)
                C2GlobalLighting.TL0 = GetLighting3D0LikeOriginal();
            lig = (lig << 7) / Mathf.Max(1, C2GlobalLighting.TL0);
            if (lig < 0) lig = 0;
            if (lig > 255) lig = 255;
            return lig;
        }

        private Color32 GetStrictSurfaceLightColorLikeOriginal(int vertex)
        {
            int l = GetLighting3DLikeOriginal(vertex);
            Color32 middle = new Color32(128, 128, 128, 255);
            EnsureStrictSurfaceLightingStateLikeOriginal();
            if (l < 128)
            {
                int w = (128 - l) * 4;
                if (w > 255) w = 255;
                return MixColor32LikeOriginal(_strictSurfaceShadowColor, middle, w, 255 - w);
            }
            else
            {
                int w = (l - 128) * 4;
                if (w > 255) w = 255;
                return MixColor32LikeOriginal(_strictSurfaceSunColor, middle, w, 255 - w);
            }
        }

        private Color32 BuildStrictSurfaceVertexColorLikeOriginal(int vertex, int alpha)
        {
            Color32 c = GetStrictSurfaceLightColorLikeOriginal(vertex);
            c.a = (byte)Mathf.Clamp(alpha, 0, 255);
            return c;
        }

        private int GetStrictTotalHeightLikeOriginal(int x, int y)
        {
            ParsedMap runtimeMapLikeOriginal = ResolveLiteralITerraRuntimeMapLikeOriginal();
            if (runtimeMapLikeOriginal == null || runtimeMapLikeOriginal.Heights == null || runtimeMapLikeOriginal.Heights.Length == 0)
                return 0;
            int maxX = Mathf.Max(0, (runtimeMapLikeOriginal.VertInLine - 1) * 32);
            int maxY = Mathf.Max(0, (runtimeMapLikeOriginal.MaxTH - 1) * 32 + 32);
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x > maxX) x = maxX;
            if (y > maxY) y = maxY;
            return GetInterpolatedHeightLikeOriginal(runtimeMapLikeOriginal, x, y);
        }

        private static int GetTriXLikeOriginal(ParsedMap map, int vertex)
        {
            if (map == null || map.VertInLine <= 0 || vertex < 0)
                return 0;
            return (vertex % map.VertInLine) * 32;
        }

        private static int GetTriYLikeOriginal(ParsedMap map, int vertex)
        {
            if (map == null || map.VertInLine <= 0 || vertex < 0)
                return 0;
            int vx = vertex % map.VertInLine;
            int vy = vertex / map.VertInLine;
            return (vy << 5) - (((vx & 1) != 0) ? 16 : 0);
        }

        private static int GetInterpolatedHeightLikeOriginal(ParsedMap map, int x, int y)
        {
            if (map == null || map.Heights == null || map.Heights.Length == 0 || map.VertInLine <= 1 || map.MaxTH <= 1)
                return 0;

            int maxX = Mathf.Max(0, (map.VertInLine - 1) * 32);
            int maxY = Mathf.Max(32, (map.MaxTH - 1) * 32 + 32);
            if (x < 0) x = 0;
            if (y < 32) y = 32;
            if (x > maxX) x = maxX;
            if (y > maxY) y = maxY;
            int nx = Mathf.Clamp(x >> 5, 0, map.VertInLine - 1);

            if ((nx & 1) != 0)
            {
                int dd = x & 31;
                int dy = dd >> 1;
                int oy = 15 - dy;
                int y1 = Mathf.Clamp((y + oy) >> 5, 0, map.MaxTH - 2);
                int dy1 = (y + oy) & 31;
                if (dy1 > 32 - dd)
                {
                    int vert2 = Mathf.Clamp(nx + y1 * map.VertInLine + 1, 0, map.Heights.Length - 1);
                    int vert3 = Mathf.Clamp(vert2 + map.VertInLine, 0, map.Heights.Length - 1);
                    int vert1 = Mathf.Clamp(vert3 - 1, 0, map.Heights.Length - 1);
                    int h1 = map.Heights[vert1];
                    int h2 = map.Heights[vert2];
                    int h3 = map.Heights[vert3];
                    int x0 = nx << 5;
                    int y0 = (y1 << 5) + 16;
                    return h1 + (((x - x0) * (((h2 + h3) >> 1) - h1)) >> 5) + (((y - y0) * (h3 - h2)) >> 5);
                }
                else
                {
                    int vert2 = Mathf.Clamp(nx + y1 * map.VertInLine, 0, map.Heights.Length - 1);
                    int vert3 = Mathf.Clamp(vert2 + map.VertInLine, 0, map.Heights.Length - 1);
                    int vert1 = Mathf.Clamp(vert2 + 1, 0, map.Heights.Length - 1);
                    int h1 = map.Heights[vert1];
                    int h2 = map.Heights[vert2];
                    int h3 = map.Heights[vert3];
                    int x0 = (nx << 5) + 32;
                    int y0 = y1 << 5;
                    return h1 - (((x - x0) * (((h2 + h3) >> 1) - h1)) >> 5) + (((y - y0) * (h3 - h2)) >> 5);
                }
            }
            else
            {
                int dd = x & 31;
                int dy = dd >> 1;
                int y1 = Mathf.Clamp((y + dy) >> 5, 0, map.MaxTH - 2);
                int dy1 = (y + dy) & 31;
                if (dy1 < dd)
                {
                    int vert1 = Mathf.Clamp(nx + y1 * map.VertInLine, 0, map.Heights.Length - 1);
                    int vert2 = Mathf.Clamp(vert1 + 1, 0, map.Heights.Length - 1);
                    int vert3 = Mathf.Clamp(vert2 + map.VertInLine, 0, map.Heights.Length - 1);
                    int h1 = map.Heights[vert1];
                    int h2 = map.Heights[vert2];
                    int h3 = map.Heights[vert3];
                    int x0 = nx << 5;
                    int y0 = y1 << 5;
                    return h1 + (((x - x0) * (((h2 + h3) >> 1) - h1)) >> 5) + (((y - y0) * (h3 - h2)) >> 5);
                }
                else
                {
                    int vert2 = Mathf.Clamp(nx + y1 * map.VertInLine, 0, map.Heights.Length - 1);
                    int vert3 = Mathf.Clamp(vert2 + map.VertInLine, 0, map.Heights.Length - 1);
                    int vert1 = Mathf.Clamp(vert3 + 1, 0, map.Heights.Length - 1);
                    int h1 = map.Heights[vert1];
                    int h2 = map.Heights[vert2];
                    int h3 = map.Heights[vert3];
                    int x0 = (nx << 5) + 32;
                    int y0 = (y1 << 5) + 16;
                    return h1 - (((x - x0) * (((h2 + h3) >> 1) - h1)) >> 5) + (((y - y0) * (h3 - h2)) >> 5);
                }
            }
        }

        private void LogStrictSurfaceLightingDebugV2LikeAdapted(ParsedMap map)
        {
            if (_strictSurfaceLightingDebugLogged)
                return;

            _strictSurfaceLightingDebugLogged = true;

            int min = 255;
            int max = 0;
            long sum = 0;
            int count = 0;
            if (_strictSurfaceLightMap != null)
            {
                for (int i = 0; i < _strictSurfaceLightMap.Length; i++)
                {
                    int v = _strictSurfaceLightMap[i];
                    if (v < min) min = v;
                    if (v > max) max = v;
                    sum += v;
                    count++;
                }
            }

            float avg = count > 0 ? (float)sum / count : 0.0f;
            UnityEngine.Debug.Log(
                $"[C2:STRICT LIGHTING V2] ready. map={map?.VertInLine}x{map?.MaxTH} light=({C2GlobalLighting.LightDX},{C2GlobalLighting.LightDY},{C2GlobalLighting.LightDZ}) " +
                $"shadowColor=#{_strictSurfaceShadowColor.r:X2}{_strictSurfaceShadowColor.g:X2}{_strictSurfaceShadowColor.b:X2} sunColor=#{_strictSurfaceSunColor.r:X2}{_strictSurfaceSunColor.g:X2}{_strictSurfaceSunColor.b:X2} " +
                $"lightMapMinMaxAvg={min}/{max}/{avg:F1}");
        }

        private static Color32 ReadEngineSettingsColorLikeOriginal(XDocument doc, string memberName, Color32 fallback)
        {
            if (doc == null || string.IsNullOrWhiteSpace(memberName))
                return fallback;

            try
            {
                foreach (XElement element in doc.Descendants())
                {
                    if (!string.Equals(element.Name.LocalName, memberName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string value = (element.Value ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                        return ParseArgbColorLikeOriginal(value, fallback);

                    foreach (XAttribute attr in element.Attributes())
                    {
                        string attrValue = (attr.Value ?? string.Empty).Trim();
                        if (!string.IsNullOrWhiteSpace(attrValue))
                            return ParseArgbColorLikeOriginal(attrValue, fallback);
                    }
                }

                foreach (XElement element in doc.Descendants())
                {
                    foreach (XAttribute attr in element.Attributes())
                    {
                        if (!string.Equals(attr.Name.LocalName, memberName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        string attrValue = (attr.Value ?? string.Empty).Trim();
                        if (!string.IsNullOrWhiteSpace(attrValue))
                            return ParseArgbColorLikeOriginal(attrValue, fallback);
                    }
                }
            }
            catch
            {
            }

            return fallback;
        }

        private static Color32 ParseArgbColorLikeOriginal(string hex, Color32 fallback)
        {
            try
            {
                string s = hex.Trim();
                if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    s = s.Substring(2);
                uint v = Convert.ToUInt32(s, 16);
                byte a = (byte)((v >> 24) & 0xFF);
                byte r = (byte)((v >> 16) & 0xFF);
                byte g = (byte)((v >> 8) & 0xFF);
                byte b = (byte)(v & 0xFF);
                return new Color32(r, g, b, a);
            }
            catch
            {
                return fallback;
            }
        }

        private static Color32 MixColor32LikeOriginal(Color32 a, Color32 b, int wa, int wb)
        {
            wa = Mathf.Clamp(wa, 0, 255);
            wb = Mathf.Clamp(wb, 0, 255);
            int r = ((a.r * wa) + (b.r * wb)) >> 8;
            int g = ((a.g * wa) + (b.g * wb)) >> 8;
            int bl = ((a.b * wa) + (b.b * wb)) >> 8;
            int al = ((a.a * wa) + (b.a * wb)) >> 8;
            return new Color32((byte)Mathf.Clamp(r, 0, 255), (byte)Mathf.Clamp(g, 0, 255), (byte)Mathf.Clamp(bl, 0, 255), (byte)Mathf.Clamp(al, 0, 255));
        }
    }
}
