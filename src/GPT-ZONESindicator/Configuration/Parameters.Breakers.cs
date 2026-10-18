using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G06 = "06 Breaker Blocks";

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
