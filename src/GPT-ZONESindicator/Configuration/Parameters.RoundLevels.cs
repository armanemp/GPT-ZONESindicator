using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G13 = "13 Psychological Levels";

        [Parameter("Show round-number levels", Group = G13, DefaultValue = false)] public bool ShowRoundLevels { get; set; }
        [Parameter("Step in price units (0 = auto)", Group = G13, DefaultValue = 0, MinValue = 0, Step = 0.0001)] public double RoundStep { get; set; }
        [Parameter("Major level every N steps", Group = G13, DefaultValue = 5, MinValue = 1, MaxValue = 1000)] public int RoundMajorEvery { get; set; }
        [Parameter("Levels drawn on each side of price", Group = G13, DefaultValue = 6, MinValue = 1, MaxValue = 200)] public int RoundLevelsEachSide { get; set; }
    }
}
