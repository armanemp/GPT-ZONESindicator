using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G16 = "16 Zone Colours";

        [Parameter("FVG bullish", Group = G16, DefaultValue = "#0A4E3E")]
        public string FvgBullColor { get; set; }

        [Parameter("FVG bearish", Group = G16, DefaultValue = "#5E0C25")]
        public string FvgBearColor { get; set; }

        [Parameter("Price gap bullish", Group = G16, DefaultValue = "#9A7B2F")]
        public string GapBullColor { get; set; }

        [Parameter("Price gap bearish", Group = G16, DefaultValue = "#7A4F2A")]
        public string GapBearColor { get; set; }

        [Parameter("Order block bullish", Group = G16, DefaultValue = "#19486E")]
        public string ObBullColor { get; set; }

        [Parameter("Order block bearish", Group = G16, DefaultValue = "#483B32")]
        public string ObBearColor { get; set; }

        [Parameter("Breaker bullish", Group = G16, DefaultValue = "#16A0A6")]
        public string BreakerBullColor { get; set; }

        [Parameter("Breaker bearish", Group = G16, DefaultValue = "#8064B5")]
        public string BreakerBearColor { get; set; }

        [Parameter("Filled zone", Group = G16, DefaultValue = "#3A4452")]
        public string FilledZoneColor { get; set; }

        [Parameter("Broken zone", Group = G16, DefaultValue = "#2B2F38")]
        public string BrokenZoneColor { get; set; }

        [Parameter("Zone opacity (%)", Group = G16, DefaultValue = 35, MinValue = 0, MaxValue = 100)]
        public double ZoneOpacity { get; set; }
    }
}
