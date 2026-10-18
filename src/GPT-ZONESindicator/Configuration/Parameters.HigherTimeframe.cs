using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G21 = "21 Higher Timeframe Zones";

        [Parameter("Detect 15-minute zones", Group = G21, DefaultValue = true)]
        public bool DetectM15Zones { get; set; }

        [Parameter("Show 15-minute zones", Group = G21, DefaultValue = true)]
        public bool ShowM15Zones { get; set; }
    }
}