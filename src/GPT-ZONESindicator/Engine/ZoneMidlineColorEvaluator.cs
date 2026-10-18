using System;

namespace cAlgo
{
    internal struct ZoneMidlineRgb
    {
        public byte R, G, B;
    }

    /// <summary>
    /// Preserves the zone's hue while lifting each RGB channel toward white for visibility.
    /// Alpha is applied by the cTrader renderer separately through MidlineOpacity.
    /// </summary>
    internal static class ZoneMidlineColorEvaluator
    {
        public const double Lightness = 0.14;

        public static ZoneMidlineRgb Lighten(byte red, byte green, byte blue)
        {
            return new ZoneMidlineRgb
            {
                R = (byte)Math.Round(red + (255 - red) * Lightness, MidpointRounding.AwayFromZero),
                G = (byte)Math.Round(green + (255 - green) * Lightness, MidpointRounding.AwayFromZero),
                B = (byte)Math.Round(blue + (255 - blue) * Lightness, MidpointRounding.AwayFromZero)
            };
        }
    }
}