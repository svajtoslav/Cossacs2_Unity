namespace G16AnalyzerLib
{
    public static class G16GlobalState
    {
        private static readonly ushort[] s_palette444 = new ushort[256];
        private static string s_lastUnpackError = string.Empty;

        public static void G16SetPalette444(ushort[] pal444)
        {
            for (int i = 0; i < s_palette444.Length; i++)
                s_palette444[i] = pal444 != null && i < pal444.Length ? pal444[i] : (ushort)0;
        }

        public static ushort[] G16GetPalette444()
        {
            ushort[] copy = new ushort[s_palette444.Length];
            System.Array.Copy(s_palette444, copy, copy.Length);
            return copy;
        }

        public static void SetLastUnpackError(string error)
        {
            s_lastUnpackError = error ?? string.Empty;
        }

        public static string GetLastUnpackError()
        {
            return s_lastUnpackError;
        }
    }
}
