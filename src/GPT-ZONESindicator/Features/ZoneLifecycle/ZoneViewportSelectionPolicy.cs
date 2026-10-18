namespace cAlgo
{
    /// <summary>
    /// Controls lifecycle-state visibility. Broken zones are terminal and must never be
    /// drawn again, regardless of legacy ShowBrokenZones/history display settings.
    /// </summary>
    internal static class ZoneVisibilityPolicy
    {
        public static bool ShouldRender(
            SznpState state,
            bool showTouchedZones,
            bool showFilledZones)
        {
            switch (state)
            {
                case SznpState.Fresh:
                    return true;
                case SznpState.Touched:
                case SznpState.Partial:
                    return showTouchedZones;
                case SznpState.Filled:
                    return showFilledZones;
                case SznpState.Broken:
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Decides when the active-zone index is sufficient for viewport drawing.
    /// Broken zones are never rendered, so legacy broken-history switches do not require
    /// scanning the full logical history. Filled zones remain opt-in history.
    /// </summary>
    internal static class ZoneViewportSelectionPolicy
    {
        public static bool UseActiveZoneIndex(bool hasViewport, bool showFilledZones)
        {
            return hasViewport && !showFilledZones;
        }
    }
}