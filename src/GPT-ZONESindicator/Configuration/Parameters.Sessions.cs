using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G12 = "12 Trading Sessions";

        [Parameter("Show Asian session high/low", Group = G12, DefaultValue = true)] public bool ShowAsia { get; set; }
        [Parameter("Show London session high/low", Group = G12, DefaultValue = true)] public bool ShowLondon { get; set; }
        [Parameter("Show New York session high/low", Group = G12, DefaultValue = false)] public bool ShowNewYork { get; set; }
        [Parameter("Asia start (hours, winter UTC)", Group = G12, DefaultValue = 0.0, MinValue = 0, MaxValue = 24, Step = 0.25)] public double AsiaStartHour { get; set; }
        [Parameter("Asia end (hours, winter UTC)", Group = G12, DefaultValue = 8.0, MinValue = 0, MaxValue = 24, Step = 0.25)] public double AsiaEndHour { get; set; }
        [Parameter("London start (hours, winter UTC)", Group = G12, DefaultValue = 8.0, MinValue = 0, MaxValue = 24, Step = 0.25)] public double LondonStartHour { get; set; }
        [Parameter("London end (hours, winter UTC)", Group = G12, DefaultValue = 16.0, MinValue = 0, MaxValue = 24, Step = 0.25)] public double LondonEndHour { get; set; }
        [Parameter("New York start (hours, winter UTC)", Group = G12, DefaultValue = 13.0, MinValue = 0, MaxValue = 24, Step = 0.25)] public double NyStartHour { get; set; }
        [Parameter("New York end (hours, winter UTC)", Group = G12, DefaultValue = 21.0, MinValue = 0, MaxValue = 24, Step = 0.25)] public double NyEndHour { get; set; }
        [Parameter("Session clock offset from UTC (hours)", Group = G12, DefaultValue = 0.0, MinValue = -12, MaxValue = 14, Step = 0.25)] public double SessionClockOffset { get; set; }
        [Parameter("London daylight-saving rule", Group = G12, DefaultValue = SznpDst.UK)] public SznpDst LondonDst { get; set; }
        [Parameter("New York daylight-saving rule", Group = G12, DefaultValue = SznpDst.US)] public SznpDst NyDst { get; set; }
        [Parameter("Extend lines after session end (hours)", Group = G12, DefaultValue = 16, MinValue = 0, MaxValue = 720)] public int SessionExtendHours { get; set; }
        [Parameter("Sessions kept per type (incl. current)", Group = G12, DefaultValue = 3, MinValue = 1, MaxValue = 1000)] public int SessionsKept { get; set; }
    }
}
