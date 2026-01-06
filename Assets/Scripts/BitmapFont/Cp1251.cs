using System;
using System.Collections.Generic;

namespace TemnyLess.BitmapFonts
{
    /// <summary>
    /// Minimal CP1251 encoder for Cossacks2 bitmap-fonts:
    /// frameIndex == byte(cp1251).
    /// Covers ASCII + Cyrillic + a few common symbols used by the game UI.
    /// </summary>
    public static class Cp1251
    {
        public static byte[] Encode(string s)
        {
            if (string.IsNullOrEmpty(s))
                return Array.Empty<byte>();

            var bytes = new byte[s.Length];
            int n = 0;

            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];

                // ASCII
                if (ch <= 0x7F)
                {
                    bytes[n++] = (byte)ch;
                    continue;
                }

                // Cyrillic А..Я (U+0410..U+042F) -> 192..223
                if (ch >= '\u0410' && ch <= '\u042F')
                {
                    bytes[n++] = (byte)(192 + (ch - '\u0410'));
                    continue;
                }

                // Cyrillic а..я (U+0430..U+044F) -> 224..255
                if (ch >= '\u0430' && ch <= '\u044F')
                {
                    bytes[n++] = (byte)(224 + (ch - '\u0430'));
                    continue;
                }

                // Ё / ё
                if (ch == '\u0401') { bytes[n++] = 168; continue; }
                if (ch == '\u0451') { bytes[n++] = 184; continue; }

                // Common UI symbols
                if (ch == '№') { bytes[n++] = 185; continue; }
                if (ch == '—') { bytes[n++] = 151; continue; } // em dash
                if (ch == '–') { bytes[n++] = 150; continue; } // en dash
                if (ch == '«') { bytes[n++] = 171; continue; }
                if (ch == '»') { bytes[n++] = 187; continue; }
                if (ch == '…') { bytes[n++] = 133; continue; }

                // Fallback: replace with '?'
                bytes[n++] = (byte)'?';
            }

            if (n == bytes.Length) return bytes;

            var trimmed = new byte[n];
            Buffer.BlockCopy(bytes, 0, trimmed, 0, n);
            return trimmed;
        }
    }
}
