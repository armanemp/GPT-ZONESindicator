using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G21 = "21 Higher Timeframe Zones";

        [Parameter("Detect and show 15-minute zones", Group = G21, DefaultValue = true)]
        public bool ShowM15Zones { get; set; }
    }
}