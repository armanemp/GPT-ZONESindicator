using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G07 = "07 Major Structure";
        private const string G08 = "08 Internal Structure";
        private const string G09 = "09 Liquidity and Sweeps";
        private const string G10 = "10 Equal Highs and Lows";

        [Parameter("Swing left bars", Group = G07, DefaultValue = 15, MinValue = 1, MaxValue = 1000)]
        public int MajorLeft { get; set; }

        [Parameter("Swing right bars (confirmation)", Group = G07, DefaultValue = 15, MinValue = 1, MaxValue = 1000)]
        public int MajorRight { get; set; }

        [Parameter("Min swing distance (ATR multiple)", Group = G07, DefaultValue = 0, MinValue = 0, Step = 0.05)]
        public double MajorMinSwingAtr { get; set; }

        [Parameter("Min swing distance (pips)", Group = G07, DefaultValue = 0, MinValue = 0, Step = 0.1)]
        public double MajorMinSwingPips { get; set; }

        [Parameter("Show BOS", Group = G07, DefaultValue = true)]
        public bool ShowMajorBos { get; set; }

        [Parameter("Show CHoCH", Group = G07, DefaultValue = true)]
        public bool ShowMajorChoch { get; set; }

        [Parameter("Show swing markers", Group = G07, DefaultValue = false)]
        public bool ShowMajorSwings { get; set; }

        [Parameter("Show active (unbroken) swing levels", Group = G07, DefaultValue = false)]
        public bool ShowMajorActiveLevels { get; set; }

        [Parameter("Max structure items in viewport (0 = all)", Group = G07, DefaultValue = 20, MinValue = 0, MaxValue = 100000)]
        public int StructMaxVisible { get; set; }

        [Parameter("Swing left bars", Group = G08, DefaultValue = 5, MinValue = 1, MaxValue = 1000)]
        public int InternalLeft { get; set; }

        [Parameter("Swing right bars (confirmation)", Group = G08, DefaultValue = 3, MinValue = 1, MaxValue = 1000)]
        public int InternalRight { get; set; }

        [Parameter("Min swing distance (ATR multiple)", Group = G08, DefaultValue = 0, MinValue = 0, Step = 0.05)]
        public double InternalMinSwingAtr { get; set; }

        [Parameter("Min swing distance (pips)", Group = G08, DefaultValue = 0, MinValue = 0, Step = 0.1)]
        public double InternalMinSwingPips { get; set; }

        [Parameter("Show internal BOS", Group = G08, DefaultValue = false)]
        public bool ShowInternalBos { get; set; }

        [Parameter("Show internal CHoCH", Group = G08, DefaultValue = false)]
        public bool ShowInternalChoch { get; set; }

        [Parameter("Show internal swing markers", Group = G08, DefaultValue = false)]
        public bool ShowInternalSwings { get; set; }

        [Parameter("Show active internal swing levels", Group = G08, DefaultValue = false)]
        public bool ShowInternalActiveLevels { get; set; }

        [Parameter("Detect liquidity sweeps", Group = G09, DefaultValue = true)]
        public bool DetectSweeps { get; set; }

        [Parameter("Show sweeps", Group = G09, DefaultValue = true)]
        public bool ShowSweeps { get; set; }

        [Parameter("Source: major swings", Group = G09, DefaultValue = true)]
        public bool SweepMajor { get; set; }

        [Parameter("Source: internal swings", Group = G09, DefaultValue = false)]
        public bool SweepInternal { get; set; }

        [Parameter("Source: equal highs/lows", Group = G09, DefaultValue = true)]
        public bool SweepEqual { get; set; }

        [Parameter("Penetration measured by", Group = G09, DefaultValue = SznpSweepMeasure.Wick)]
        public SznpSweepMeasure SweepMeasure { get; set; }

        [Parameter("Min penetration (ATR multiple)", Group = G09, DefaultValue = 0.05, MinValue = 0, Step = 0.01)]
        public double SweepMinPenAtr { get; set; }

        [Parameter("Min penetration (pips)", Group = G09, DefaultValue = 0, MinValue = 0, Step = 0.1)]
        public double SweepMinPenPips { get; set; }

        [Parameter("Min close-back distance (ATR multiple)", Group = G09, DefaultValue = 0, MinValue = 0, Step = 0.01)]
        public double SweepMinReturnAtr { get; set; }

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