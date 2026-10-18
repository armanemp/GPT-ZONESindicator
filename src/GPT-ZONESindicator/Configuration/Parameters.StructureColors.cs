using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G18 = "18 Structure Colours";

        [Parameter("Bullish BOS", Group = G18, DefaultValue = "#2F8F6B")]
        public string StructBullColor { get; set; }

        [Parameter("Bearish BOS", Group = G18, DefaultValue = "#B03A4C")]
        public string StructBearColor { get; set; }

        [Parameter("CHoCH", Group = G18, DefaultValue = "#B8923A")]
        public string ChochColor { get; set; }

        [Parameter("Swing high marker", Group = G18, DefaultValue = "#8B2E3C")]
        public string SwingHighColor { get; set; }

        [Parameter("Swing low marker", Group = G18, DefaultValue = "#2E7D5B")]
        public string SwingLowColor { get; set; }

        [Parameter("Liquidity (equal levels, sweeps)", Group = G18, DefaultValue = "#8F7F5F")]
        public string LiquidityColor { get; set; }

        [Parameter("Structure line opacity (%)", Group = G18, DefaultValue = 85, MinValue = 0, MaxValue = 100)]
        public double StructOpacity { get; set; }
    }
}
