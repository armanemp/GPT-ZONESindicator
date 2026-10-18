using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G19 = "19 Reference Level Colours";

        [Parameter("Previous day", Group = G19, DefaultValue = "#4A5A70")]
        public string PrevDayColor { get; set; }

        [Parameter("Previous week", Group = G19, DefaultValue = "#566A86")]
        public string PrevWeekColor { get; set; }

        [Parameter("Previous month", Group = G19, DefaultValue = "#63799A")]
        public string PrevMonthColor { get; set; }

        [Parameter("Period opens", Group = G19, DefaultValue = "#6B7A8F")]
        public string OpenLevelColor { get; set; }

        [Parameter("Asia session", Group = G19, DefaultValue = "#4F6D7A")]
        public string AsiaColor { get; set; }

        [Parameter("London session", Group = G19, DefaultValue = "#5E5E8A")]
        public string LondonColor { get; set; }

        [Parameter("New York session", Group = G19, DefaultValue = "#7A5C4F")]
        public string NyColor { get; set; }

        [Parameter("Round number", Group = G19, DefaultValue = "#3E4756")]
        public string RoundColor { get; set; }

        [Parameter("Round number (major)", Group = G19, DefaultValue = "#566274")]
        public string RoundMajorColor { get; set; }

        [Parameter("Reference line opacity (%)", Group = G19, DefaultValue = 90, MinValue = 0, MaxValue = 100)]
        public double RefOpacity { get; set; }
    }
}
