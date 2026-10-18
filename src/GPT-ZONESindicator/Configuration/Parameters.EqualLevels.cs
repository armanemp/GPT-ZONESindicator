using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G10 = "10 Equal Highs and Lows";

        [Parameter("Detect equal highs/lows", Group = G10, DefaultValue = true)]
        public bool DetectEqual { get; set; }

        [Parameter("Show equal highs/lows", Group = G10, DefaultValue = true)]
        public bool ShowEqual { get; set; }

        [Parameter("Max liquidity items in viewport (0 = all)", Group = G10, DefaultValue = 12, MinValue = 0, MaxValue = 100000)]
        public int LiquidityMaxVisible { get; set; }

        [Parameter("Use major swings (off = internal)", Group = G10, DefaultValue = false)]
        public bool EqUseMajorSwings { get; set; }

        [Parameter("Tolerance mode", Group = G10, DefaultValue = SznpToleranceMode.Atr)]
        public SznpToleranceMode EqToleranceMode { get; set; }

        [Parameter("Tolerance (ATR multiple)", Group = G10, DefaultValue = 0.10, MinValue = 0, Step = 0.01)]
        public double EqTolAtr { get; set; }

        [Parameter("Tolerance (pips)", Group = G10, DefaultValue = 5, MinValue = 0, Step = 0.1)]
        public double EqTolPips { get; set; }

        [Parameter("Min bars between the two swings", Group = G10, DefaultValue = 5, MinValue = 1, MaxValue = 100000)]
        public int EqMinBarsApart { get; set; }

        [Parameter("Max bars between the two swings", Group = G10, DefaultValue = 300, MinValue = 2, MaxValue = 1000000)]
        public int EqMaxBarsApart { get; set; }

        [Parameter("Keep showing swept/broken levels", Group = G10, DefaultValue = false)]
        public bool ShowSweptEqual { get; set; }
    }
}
