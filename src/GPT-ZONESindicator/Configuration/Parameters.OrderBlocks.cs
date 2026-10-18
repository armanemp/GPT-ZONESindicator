using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G05 = "05 Order Blocks";

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
    }
}
