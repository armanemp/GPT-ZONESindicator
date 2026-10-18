using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G11 = "11 Previous Day-Week-Month";

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
        [Parameter("Max reference items in viewport (0 = all)", Group = G11, DefaultValue = 10, MinValue = 0, MaxValue = 100000)] public int ReferenceMaxVisible { get; set; }
    }
}
