using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G14 = "14 Zone Ranking and Overlap";

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
    }
}
