using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G14 = "14 Zone Ranking and Overlap";
        private const string G15 = "15 Active-Broken Zone Display";
        private const string G16 = "16 Zone Colours";
        private const string G17 = "17 Midline Colours";
        private const string G18 = "18 Structure Colours";
        private const string G19 = "19 Reference Level Colours";
        private const string G20 = "20 Line Width and Style";

        [Parameter("Weight: size vs ATR", Group = G14, DefaultValue = 20, MinValue = 0, MaxValue = 100)]
        public double WSize { get; set; }

        [Parameter("Weight: creating momentum", Group = G14, DefaultValue = 20, MinValue = 0, MaxValue = 100)]
        public double WMomentum { get; set; }

        [Parameter("Weight: structure / liquidity context", Group = G14, DefaultValue = 15, MinValue = 0, MaxValue = 100)]
        public double WStructure { get; set; }

        [Parameter("Weight: freshness", Group = G14, DefaultValue = 15, MinValue = 0, MaxValue = 100)]
        public double WFresh { get; set; }

        [Parameter("Weight: touches", Group = G14, DefaultValue = 10, MinValue = 0, MaxValue = 100)]
        public double WTouch { get; set; }

        [Parameter("Weight: distance to price", Group = G14, DefaultValue = 10, MinValue = 0, MaxValue = 100)]
        public double WDistance { get; set; }

        [Parameter("Weight: confluence", Group = G14, DefaultValue = 10, MinValue = 0, MaxValue = 100)]
        public double WConfluence { get; set; }

        [Parameter("Size reference (ATR multiple = full score)", Group = G14, DefaultValue = 1.0, MinValue = 0.05, Step = 0.05)]
        public double ScoreSizeRefAtr { get; set; }

        [Parameter("Momentum reference (ATR multiple = full score)", Group = G14, DefaultValue = 1.5, MinValue = 0.05, Step = 0.05)]
        public double ScoreMomentumRefAtr { get; set; }

        [Parameter("Freshness half-life (bars)", Group = G14, DefaultValue = 500, MinValue = 1, MaxValue = 1000000)]
        public int FreshHalfLifeBars { get; set; }

        [Parameter("Score penalty per touch (0-1)", Group = G14, DefaultValue = 0.25, MinValue = 0, MaxValue = 1, Step = 0.05)]
        public double TouchPenalty { get; set; }

        [Parameter("Distance reference (ATR multiple = half score)", Group = G14, DefaultValue = 10, MinValue = 0.5, Step = 0.5)]
        public double DistanceRefAtr { get; set; }

        [Parameter("Filter duplicate zones", Group = G14, DefaultValue = true)]
        public bool FilterDuplicateZones { get; set; }

        [Parameter("Declutter overlapping zones of different kinds", Group = G14, DefaultValue = false)]
        public bool OverlapDeclutter { get; set; }

        [Parameter("Overlap threshold (% of smaller zone)", Group = G14, DefaultValue = 70, MinValue = 1, MaxValue = 100)]
        public double OverlapMinPct { get; set; }

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

        [Parameter("Show zone labels", Group = G15, DefaultValue = false)]
        public bool ShowZoneLabels { get; set; }

        [Parameter("Show zone midline", Group = G15, DefaultValue = true)]
        public bool ShowMidline { get; set; }

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

        [Parameter("FVG midline", Group = G17, DefaultValue = "#4FB89B")]
        public string FvgMidColor { get; set; }

        [Parameter("Price gap midline", Group = G17, DefaultValue = "#C9A55A")]
        public string GapMidColor { get; set; }

        [Parameter("Order block midline", Group = G17, DefaultValue = "#5B9BD5")]
        public string ObMidColor { get; set; }

        [Parameter("Breaker midline", Group = G17, DefaultValue = "#4CB3B8")]
        public string BreakerMidColor { get; set; }

        [Parameter("Midline opacity (%)", Group = G17, DefaultValue = 85, MinValue = 0, MaxValue = 100)]
        public double MidlineOpacity { get; set; }

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

        [Parameter("Previous day", Group = G19, DefaultValue = "#4A5A70")]
        public string PrevDayColor { get; set; }

        [Parameter("Previous week", Group = G19, DefaultValue = "#566A86")]
        public string PrevWeekColor { get; set; }

        [Parameter("Previous month", Group = G19, DefaultValue = "#63799A")]
        public string PrevMonthColor { get; set; }

        [Parameter("Period opens", Group = G19, DefaultValue = "#6B7A8F")]
        public string OpenLevelColor { get; set; }

        [Parameter("Asia session", Group = G19, DefaultValue = "#4F6D7A")]
        public string AsiaColor { get; set; }

        [Parameter("London session", Group = G19, DefaultValue = "#5E5E8A")]
        public string LondonColor { get; set; }

        [Parameter("New York session", Group = G19, DefaultValue = "#7A5C4F")]
        public string NyColor { get; set; }

        [Parameter("Round number", Group = G19, DefaultValue = "#3E4756")]
        public string RoundColor { get; set; }

        [Parameter("Round number (major)", Group = G19, DefaultValue = "#566274")]
        public string RoundMajorColor { get; set; }

        [Parameter("Reference line opacity (%)", Group = G19, DefaultValue = 90, MinValue = 0, MaxValue = 100)]
        public double RefOpacity { get; set; }

        [Parameter("Zone midline thickness", Group = G20, DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int MidlineThickness { get; set; }

        [Parameter("Zone midline style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle MidlineStyle { get; set; }

        [Parameter("Zone border thickness (0 = none)", Group = G20, DefaultValue = 0, MinValue = 0, MaxValue = 5)]
        public int ZoneBorderThickness { get; set; }

        [Parameter("Major structure thickness", Group = G20, DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int StructThickness { get; set; }

        [Parameter("Major structure style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle StructStyle { get; set; }

        [Parameter("Internal structure style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle InternalStyle { get; set; }

        [Parameter("Liquidity line style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle LiquidityStyle { get; set; }

        [Parameter("Reference line thickness", Group = G20, DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int RefThickness { get; set; }

        [Parameter("Reference line style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle RefStyle { get; set; }

        [Parameter("Session line style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle SessionStyle { get; set; }

        [Parameter("Round number style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle RoundStyle { get; set; }

        [Parameter("Label font size", Group = G20, DefaultValue = 8, MinValue = 6, MaxValue = 24)]
        public int LabelFontSize { get; set; }
    }
}