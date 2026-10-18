namespace cAlgo
{
    internal struct ZoneMidlineRgb
    {
        public byte R, G, B;
    }

    /// <summary>Returns the exact zone RGB so its midpoint line matches the zone.</summary>
    internal static class ZoneMidlineColorEvaluator
    {
        public static ZoneMidlineRgb Match(byte red, byte green, byte blue)
        {
            return new ZoneMidlineRgb { R = red, G = green, B = blue };
        }
    }
}