using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G20 = "20 Line Width and Style";

        [Parameter("Zone midline thickness", Group = G20, DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int MidlineThickness { get; set; }

        [Parameter("Zone midline style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle MidlineStyle { get; set; }

        [Parameter("Zone border thickness (0 = none)", Group = G20, DefaultValue = 0, MinValue = 0, MaxValue = 5)]
        public int ZoneBorderThickness { get; set; }

        [Parameter("Major structure thickness", Group = G20, DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int StructThickness { get; set; }

        [Parameter("Major structure style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle StructStyle { get; set; }

        [Parameter("Internal structure style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle InternalStyle { get; set; }

        [Parameter("Liquidity line style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle LiquidityStyle { get; set; }

        [Parameter("Reference line thickness", Group = G20, DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int RefThickness { get; set; }

        [Parameter("Reference line style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle RefStyle { get; set; }

        [Parameter("Session line style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle SessionStyle { get; set; }

        [Parameter("Round number style", Group = G20, DefaultValue = LineStyle.Dots)]
        public LineStyle RoundStyle { get; set; }

        [Parameter("Label font size", Group = G20, DefaultValue = 8, MinValue = 6, MaxValue = 24)]
        public int LabelFontSize { get; set; }
    }
}
