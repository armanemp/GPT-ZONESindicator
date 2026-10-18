using System;

namespace cAlgo
{
    internal struct ZoneMidlineRgb
    {
        public byte R, G, B;
    }

    /// <summary>
    /// Preserves the zone's RGB ratios (same hue) while darkening each channel slightly.
    /// Alpha is applied by the cTrader renderer separately through MidlineOpacity.
    /// </summary>
    internal static class ZoneMidlineColorEvaluator
    {
        public const double Darkness = 0.26;

        public static ZoneMidlineRgb Darken(byte red, byte green, byte blue)
        {
            double scale = 1.0 - Darkness;
            return new ZoneMidlineRgb
            {
                R = (byte)Math.Round(red * scale, MidpointRounding.AwayFromZero),
                G = (byte)Math.Round(green * scale, MidpointRounding.AwayFromZero),
                B = (byte)Math.Round(blue * scale, MidpointRounding.AwayFromZero)
            };
        }
    }
}