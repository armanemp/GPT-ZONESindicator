using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G03 = "03 FVG";

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
    }
}
