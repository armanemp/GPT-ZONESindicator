using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G09 = "09 Liquidity and Sweeps";

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
    }
}
