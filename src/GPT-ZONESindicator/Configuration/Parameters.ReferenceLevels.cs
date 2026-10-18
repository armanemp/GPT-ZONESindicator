using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G11 = "11 Previous Day-Week-Month";
        private const string G12 = "12 Trading Sessions";
        private const string G13 = "13 Psychological Levels";

        [Parameter("Show previous day high/low", Group = G11, DefaultValue = true)] public bool ShowPrevDay { get; set; }
        [Parameter("Show previous day 50%", Group = G11, DefaultValue = false)] public bool ShowPrevDayMid { get; set; }
        [Parameter("Show current day open", Group = G11, DefaultValue = true)] public bool ShowDayOpen { get; set; }
        [Parameter("Show previous week high/low", Group = G11, DefaultValue = true)] public bool ShowPrevWeek { get; set; }
        [Parameter("Show previous week 50%", Group = G11, DefaultValue = false)] public bool ShowPrevWeekMid { get; set; }
        [Parameter("Show current week open", Group = G11, DefaultValue = false)] public bool ShowWeekOpen { get; set; }
        [Parameter("Show previous month high/low", Group = G11, DefaultValue = false)] public bool ShowPrevMonth { get; set; }
        [Parameter("Show previous month 50%", Group = G11, DefaultValue = false)] public bool ShowPrevMonthMid { get; set; }
        [Parameter("Show current month open", Group = G11, DefaultValue = false)] public bool ShowMonthOpen { get; set; }
        [Parameter("Daily periods drawn (incl. current)", Group = G11, DefaultValue = 2, MinValue = 1, MaxValue = 1000)] public int DailyPeriodsKept { get; set; }
        [Parameter("Weekly periods drawn (incl. current)", Group = G11, DefaultValue = 1, MinValue = 1, MaxValue = 1000)] public int WeeklyPeriodsKept { get; set; }
        [Parameter("Monthly periods drawn (incl. current)", Group = G11, DefaultValue = 1, MinValue = 1, MaxValue = 1000)] public int MonthlyPeriodsKept { get; set; }
        [Parameter("Show level labels", Group = G11, DefaultValue = true)] public bool ShowLevelLabels { get; set; }

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

        [Parameter("Show round-number levels", Group = G13, DefaultValue = false)] public bool ShowRoundLevels { get; set; }
        [Parameter("Step in price units (0 = auto)", Group = G13, DefaultValue = 0, MinValue = 0, Step = 0.0001)] public double RoundStep { get; set; }
        [Parameter("Major level every N steps", Group = G13, DefaultValue = 5, MinValue = 1, MaxValue = 1000)] public int RoundMajorEvery { get; set; }
        [Parameter("Levels drawn on each side of price", Group = G13, DefaultValue = 6, MinValue = 1, MaxValue = 200)] public int RoundLevelsEachSide { get; set; }
    }
}