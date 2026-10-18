using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G07 = "07 Major Structure";
        private const string G08 = "08 Internal Structure";

        [Parameter("Swing left bars", Group = G07, DefaultValue = 15, MinValue = 1, MaxValue = 1000)]
        public int MajorLeft { get; set; }

        [Parameter("Swing right bars (confirmation)", Group = G07, DefaultValue = 15, MinValue = 1, MaxValue = 1000)]
        public int MajorRight { get; set; }

        [Parameter("Min swing distance (ATR multiple)", Group = G07, DefaultValue = 0, MinValue = 0, Step = 0.05)]
        public double MajorMinSwingAtr { get; set; }

        [Parameter("Min swing distance (pips)", Group = G07, DefaultValue = 0, MinValue = 0, Step = 0.1)]
        public double MajorMinSwingPips { get; set; }

        [Parameter("Show BOS", Group = G07, DefaultValue = true)]
        public bool ShowMajorBos { get; set; }

        [Parameter("Show CHoCH", Group = G07, DefaultValue = true)]
        public bool ShowMajorChoch { get; set; }

        [Parameter("Show swing markers", Group = G07, DefaultValue = false)]
        public bool ShowMajorSwings { get; set; }

        [Parameter("Show active (unbroken) swing levels", Group = G07, DefaultValue = false)]
        public bool ShowMajorActiveLevels { get; set; }

        [Parameter("Max major-structure items in viewport (0 = all)", Group = G07, DefaultValue = 12, MinValue = 0, MaxValue = 100000)]
        public int StructMaxVisible { get; set; }

        [Parameter("Swing left bars", Group = G08, DefaultValue = 5, MinValue = 1, MaxValue = 1000)]
        public int InternalLeft { get; set; }

        [Parameter("Swing right bars (confirmation)", Group = G08, DefaultValue = 3, MinValue = 1, MaxValue = 1000)]
        public int InternalRight { get; set; }

        [Parameter("Min swing distance (ATR multiple)", Group = G08, DefaultValue = 0, MinValue = 0, Step = 0.05)]
        public double InternalMinSwingAtr { get; set; }

        [Parameter("Min swing distance (pips)", Group = G08, DefaultValue = 0, MinValue = 0, Step = 0.1)]
        public double InternalMinSwingPips { get; set; }

        [Parameter("Show internal BOS", Group = G08, DefaultValue = false)]
        public bool ShowInternalBos { get; set; }

        [Parameter("Show internal CHoCH", Group = G08, DefaultValue = false)]
        public bool ShowInternalChoch { get; set; }

        [Parameter("Show internal swing markers", Group = G08, DefaultValue = false)]
        public bool ShowInternalSwings { get; set; }

        [Parameter("Show active internal swing levels", Group = G08, DefaultValue = false)]
        public bool ShowInternalActiveLevels { get; set; }

        [Parameter("Max internal-structure items in viewport (0 = all)", Group = G08, DefaultValue = 8, MinValue = 0, MaxValue = 100000)]
        public int InternalStructMaxVisible { get; set; }
    }
}
