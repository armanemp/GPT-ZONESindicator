using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G17 = "17 Midline Colours";

        [Parameter("FVG midline", Group = G17, DefaultValue = "#4FB89B")]
        public string FvgMidColor { get; set; }

        [Parameter("Price gap midline", Group = G17, DefaultValue = "#C9A55A")]
        public string GapMidColor { get; set; }

        [Parameter("Order block midline", Group = G17, DefaultValue = "#5B9BD5")]
        public string ObMidColor { get; set; }

        [Parameter("Breaker midline", Group = G17, DefaultValue = "#4CB3B8")]
        public string BreakerMidColor { get; set; }

        [Parameter("Midline opacity (%)", Group = G17, DefaultValue = 90, MinValue = 0, MaxValue = 100)]
        public double MidlineOpacity { get; set; }
    }
}
