using System;
using System.Collections.Generic;
using System.Text;

namespace Cossacks2Bridge.UnityAdapters
{
    /// <summary>
    /// Original Cossacks II text helpers used by the 1.4 bridge.
    /// The game resources are CP1251, and legacy rich-text commands use {..} tokens
    /// plus a backslash as an explicit line break.
    /// </summary>
    public static class C2LegacyText14
    {
        // CP1251 0x80..0xBF. 0xC0..0xFF are А..я and are handled arithmetically.
        private static readonly char[] Cp1251High = new char[]
        {
            '\u0402','\u0403','\u201A','\u0453','\u201E','\u2026','\u2020','\u2021',
            '\u20AC','\u2030','\u0409','\u2039','\u040A','\u040C','\u040B','\u040F',
            '\u0452','\u2018','\u2019','\u201C','\u201D','\u2022','\u2013','\u2014',
            '\u0000','\u2122','\u0459','\u203A','\u045A','\u045C','\u045B','\u045F',
            '\u00A0','\u040E','\u045E','\u0408','\u00A4','\u0490','\u00A6','\u00A7',
            '\u0401','\u00A9','\u0404','\u00AB','\u00AC','\u00AD','\u00AE','\u0407',
            '\u00B0','\u00B1','\u0406','\u0456','\u0491','\u00B5','\u00B6','\u00B7',
            '\u0451','\u2116','\u0454','\u00BB','\u0458','\u0405','\u0455','\u0457'
        };

        private static readonly Dictionary<char, byte> EncodeHigh = BuildEncodeHigh();

        private static Dictionary<char, byte> BuildEncodeHigh()
        {
            var d = new Dictionary<char, byte>();
            for (int i = 0; i < Cp1251High.Length; i++)
            {
                char c = Cp1251High[i];
                if (c != '\0' && !d.ContainsKey(c)) d[c] = (byte)(0x80 + i);
            }
            return d;
        }

        public static bool TryEncodeChar(char c, out byte b)
        {
            if (c <= 0x7F)
            {
                b = (byte)c;
                return true;
            }
            if (c >= '\u0410' && c <= '\u044F')
            {
                b = (byte)(0xC0 + (c - '\u0410'));
                return true;
            }
            if (EncodeHigh.TryGetValue(c, out b)) return true;
            b = (byte)'?';
            return false;
        }

        public static byte[] EncodeCp1251(string s)
        {
            if (string.IsNullOrEmpty(s)) return Array.Empty<byte>();
            var bytes = new byte[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                TryEncodeChar(s[i], out byte b);
                bytes[i] = b;
            }
            return bytes;
        }

        public static string DecodeCp1251(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;
            var chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                int v = bytes[i];
                if (v < 0x80) chars[i] = (char)v;
                else if (v >= 0xC0) chars[i] = (char)('\u0410' + (v - 0xC0));
                else
                {
                    char c = Cp1251High[v - 0x80];
                    chars[i] = c == '\0' ? '?' : c;
                }
            }
            return new string(chars);
        }

        /// <summary>
        /// Removes legacy DrawMultilineText commands for UI fields that are still
        /// rendered by TMP. This is intentionally only a plain-text view; rich help
        /// screens use the bitmap parser in C2CampaignModalRenderer14.
        /// </summary>
        public static string StripFormatting(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '{')
                {
                    int end = s.IndexOf('}', i + 1);
                    if (end >= 0)
                    {
                        i = end;
                        continue;
                    }
                }
                if (c == '\\')
                {
                    sb.Append('\n');
                    continue;
                }
                if (c != '\r') sb.Append(c);
            }
            return sb.ToString().Trim();
        }
    }
}
