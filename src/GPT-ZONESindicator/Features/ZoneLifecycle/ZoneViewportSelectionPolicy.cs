namespace cAlgo
{
    /// <summary>
    /// Default lifecycle-state visibility. Broken zones stay hidden here; the breaker module
    /// has a separate, tightly scoped exception for a linked parent OB when history is requested.
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
    /// Filled zones and explicitly requested breaker-parent history require logical history.
    /// </summary>
    internal static class ZoneViewportSelectionPolicy
    {
        public static bool UseActiveZoneIndex(
            bool hasViewport,
            bool showFilledZones,
            bool showBreakerParentHistory = false)
        {
            return hasViewport && !showFilledZones && !showBreakerParentHistory;
        }
    }
}