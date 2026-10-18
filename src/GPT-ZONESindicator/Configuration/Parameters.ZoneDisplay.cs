using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G15 = "15 Active-Broken Zone Display";

        [Parameter("Display mode", Group = G15, DefaultValue = SznpDisplayMode.AllInLookback)]
        public SznpDisplayMode DisplayMode { get; set; }

        [Parameter("Top-ranked count per kind (TopRanked mode)", Group = G15, DefaultValue = 10, MinValue = 1, MaxValue = 100000)]
        public int TopRankedCount { get; set; }

        [Parameter("Min importance score (0-100)", Group = G15, DefaultValue = 0, MinValue = 0, MaxValue = 100)]
        public double MinScore { get; set; }

        [Parameter("Max zones per kind (0 = unlimited)", Group = G15, DefaultValue = 0, MinValue = 0, MaxValue = 100000)]
        public int MaxZonesPerKind { get; set; }

        [Parameter("Near-price distance (ATR multiple, NearPrice mode)", Group = G15, DefaultValue = 15, MinValue = 0.5, Step = 0.5)]
        public double NearPriceAtr { get; set; }

        [Parameter("Show touched / partially filled zones", Group = G15, DefaultValue = true)]
        public bool ShowTouchedZones { get; set; }

        [Parameter("Show fully filled zones", Group = G15, DefaultValue = false)]
        public bool ShowFilledZones { get; set; }

        [Parameter("Show broken zones", Group = G15, DefaultValue = false)]
        public bool ShowBrokenZones { get; set; }

        [Parameter("Partial-fill threshold (% of zone)", Group = G15, DefaultValue = 50, MinValue = 1, MaxValue = 99)]
        public double PartialFillPct { get; set; }

        [Parameter("Touched zone opacity (% of base)", Group = G15, DefaultValue = 70, MinValue = 5, MaxValue = 100)]
        public double TouchedOpacityPct { get; set; }

        [Parameter("Filled / broken zone opacity (%)", Group = G15, DefaultValue = 22, MinValue = 0, MaxValue = 100)]
        public double BrokenOpacity { get; set; }

        [Parameter("Show zone labels", Group = G15, DefaultValue = true)]
        public bool ShowZoneLabels { get; set; }

        [Parameter("Show zone midline", Group = G15, DefaultValue = true)]
        public bool ShowMidline { get; set; }
    }
}
