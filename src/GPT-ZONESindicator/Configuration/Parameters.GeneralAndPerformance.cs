using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G01 = "01 General";
        private const string G02 = "02 Performance and History";

        [Parameter("Instance tag (object name prefix)", Group = G01, DefaultValue = "SZNP")]
        public string InstanceTag { get; set; }

        [Parameter("ATR period", Group = G01, DefaultValue = 14, MinValue = 1, MaxValue = 500)]
        public int AtrPeriod { get; set; }

        [Parameter("Chart objects selectable (interactive)", Group = G01, DefaultValue = false)]
        public bool ObjectsInteractive { get; set; }

        [Parameter("Show status text on chart", Group = G01, DefaultValue = true)]
        public bool ShowStatusText { get; set; }

        [Parameter("Print diagnostics to log", Group = G01, DefaultValue = true)]
        public bool PrintDiagnostics { get; set; }

        [Parameter("Lookback bars (0 = all loaded bars)", Group = G02, DefaultValue = 10000, MinValue = 0, MaxValue = 5000000)]
        public int LookbackBars { get; set; }

        [Parameter("Load broker history up to lookback", Group = G02, DefaultValue = true)]
        public bool LoadHistoryToLookback { get; set; }

        [Parameter("Legacy extension days (ignored; zones use 40 bars)", Group = G02, DefaultValue = 40, MinValue = 1, MaxValue = 3650)]
        public int ExtensionDays { get; set; }

        [Parameter("Max zone rectangles shown (0 = unlimited)", Group = G02, DefaultValue = 0, MinValue = 0, MaxValue = 100000)]
        public int MaxChartObjects { get; set; }

        [Parameter("Zone Z-index (negative = behind)", Group = G02, DefaultValue = -100)]
        public int ZoneZIndex { get; set; }
    }
}