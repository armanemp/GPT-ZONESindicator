using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G03 = "03 FVG";
        private const string G04 = "04 Price Gaps";
        private const string G05 = "05 Order Blocks";
        private const string G06 = "06 Breaker Blocks";

        [Parameter("Detect FVG", Group = G03, DefaultValue = true)]
        public bool DetectFvg { get; set; }

        [Parameter("Show FVG", Group = G03, DefaultValue = true)]
        public bool ShowFvg { get; set; }

        [Parameter("Zone starts at", Group = G03, DefaultValue = SznpStartMode.FirstCandle)]
        public SznpStartMode FvgStartMode { get; set; }

        [Parameter("Min size (ATR multiple)", Group = G03, DefaultValue = 0.10, MinValue = 0, Step = 0.01)]
        public double FvgMinSizeAtr { get; set; }

        [Parameter("Min size (pips)", Group = G03, DefaultValue = 0, MinValue = 0, Step = 0.1)]
        public double FvgMinSizePips { get; set; }

        [Parameter("Require displacement candle", Group = G03, DefaultValue = false)]
        public bool FvgRequireDisplacement { get; set; }

        [Parameter("Displacement min body (ATR multiple)", Group = G03, DefaultValue = 0.5, MinValue = 0, Step = 0.05)]
        public double FvgMinBodyAtr { get; set; }

        [Parameter("Displacement min body/range ratio", Group = G03, DefaultValue = 0.5, MinValue = 0, MaxValue = 1, Step = 0.05)]
        public double FvgMinBodyRatio { get; set; }

        [Parameter("Displacement candle must match direction", Group = G03, DefaultValue = true)]
        public bool FvgMatchDirection { get; set; }

        [Parameter("Break criterion", Group = G03, DefaultValue = SznpBreakMode.CandleClose)]
        public SznpBreakMode FvgBreakMode { get; set; }

        [Parameter("Detect price gaps", Group = G04, DefaultValue = true)]
        public bool DetectGap { get; set; }

        [Parameter("Show price gaps", Group = G04, DefaultValue = true)]
        public bool ShowGap { get; set; }

        [Parameter("Detection method", Group = G04, DefaultValue = SznpGapMethod.OpenCloseGap)]
        public SznpGapMethod GapMethod { get; set; }

        [Parameter("If both methods fire on one bar", Group = G04, DefaultValue = SznpGapCombine.PreferOpenCloseGap)]
        public SznpGapCombine GapCombine { get; set; }

        [Parameter("Min size (ATR multiple)", Group = G04, DefaultValue = 0.30, MinValue = 0, Step = 0.05)]
        public double GapMinSizeAtr { get; set; }

        [Parameter("Min size (pips)", Group = G04, DefaultValue = 0, MinValue = 0, Step = 0.1)]
        public double GapMinSizePips { get; set; }

        [Parameter("Break criterion", Group = G04, DefaultValue = SznpBreakMode.CandleClose)]
        public SznpBreakMode GapBreakMode { get; set; }

        [Parameter("Detect order blocks", Group = G05, DefaultValue = true)]
        public bool DetectOb { get; set; }

        [Parameter("Show order blocks", Group = G05, DefaultValue = true)]
        public bool ShowOb { get; set; }

        [Parameter("Structure source", Group = G05, DefaultValue = SznpStructureSource.MajorAndInternal)]
        public SznpStructureSource ObSource { get; set; }

        [Parameter("Zone range", Group = G05, DefaultValue = SznpObRange.FullCandle)]
        public SznpObRange ObRangeMode { get; set; }

        [Parameter("Max size (ATR multiple, 0 = off)", Group = G05, DefaultValue = 3.0, MinValue = 0, Step = 0.1)]
        public double ObMaxSizeAtr { get; set; }

        [Parameter("Oversized candle: use body instead of skipping", Group = G05, DefaultValue = true)]
        public bool ObUseBodyIfOversize { get; set; }

        [Parameter("Min size (ATR multiple)", Group = G05, DefaultValue = 0, MinValue = 0, Step = 0.05)]
        public double ObMinSizeAtr { get; set; }

        [Parameter("Min structure-break strength (ATR multiple)", Group = G05, DefaultValue = 0, MinValue = 0, Step = 0.05)]
        public double ObMinBreakAtr { get; set; }

        [Parameter("Require displacement on break candle", Group = G05, DefaultValue = false)]
        public bool ObRequireDisplacement { get; set; }

        [Parameter("Break candle min body (ATR multiple)", Group = G05, DefaultValue = 0.5, MinValue = 0, Step = 0.05)]
        public double ObMinBreakBodyAtr { get; set; }

        [Parameter("Max bars between swing and OB candle", Group = G05, DefaultValue = 100, MinValue = 1, MaxValue = 100000)]
        public int ObMaxBarsFromSwing { get; set; }

        [Parameter("Break criterion", Group = G05, DefaultValue = SznpBreakMode.CandleClose)]
        public SznpBreakMode ObBreakMode { get; set; }

        [Parameter("Convert broken OBs to breakers", Group = G06, DefaultValue = true)]
        public bool DetectBreaker { get; set; }

        [Parameter("Show breakers", Group = G06, DefaultValue = true)]
        public bool ShowBreaker { get; set; }

        [Parameter("Breaker invalidation criterion", Group = G06, DefaultValue = SznpBreakMode.CandleClose)]
        public SznpBreakMode BreakerBreakMode { get; set; }

        [Parameter("Show original OB history of a breaker", Group = G06, DefaultValue = false)]
        public bool ShowObHistoryOfBreakers { get; set; }
    }
}