using System;

namespace cAlgo
{
    internal struct ZoneMidlineRgb
    {
        public byte R, G, B;
    }

    /// <summary>Brightens the zone hue and applies a restrained per-kind colour tint.</summary>
    internal static class ZoneMidlineColorEvaluator
    {
        public static ZoneMidlineRgb Highlight(
            byte red, byte green, byte blue,
            byte tintRed, byte tintGreen, byte tintBlue)
        {
            return new ZoneMidlineRgb
            {
                R = HighlightChannel(red, tintRed),
                G = HighlightChannel(green, tintGreen),
                B = HighlightChannel(blue, tintBlue)
            };
        }

        private static byte HighlightChannel(byte source, byte tint)
        {
            const double lighten = 0.14;
            const double tintMix = 0.06;
            double light = source + (255.0 - source) * lighten;
            double blended = light * (1.0 - tintMix) + tint * tintMix;
            return (byte)Math.Max(source, Math.Min(255, Math.Round(blended)));
        }
    }
}