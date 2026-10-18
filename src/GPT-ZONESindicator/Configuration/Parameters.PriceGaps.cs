using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G04 = "04 Price Gaps";

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
    }
}
