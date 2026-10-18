using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private const string G14 = "14 Rendering Performance";

        // Canvas is opt-in until the complete overlay is visually accepted on a live cTrader chart.
        // It batches visible zone rectangles, midlines and labels into one SVG-backed image control.
        [Parameter("Use Canvas zone overlay (experimental)", Group = G14, DefaultValue = false)]
        public bool UseCanvasZoneOverlay { get; set; }
    }
}
