namespace cAlgo
{
    /// <summary>Controls rendering by lifecycle state; terminal history remains opt-in.</summary>
    internal static class ZoneVisibilityPolicy
    {
        public static bool ShouldRender(
            SznpState state,
            bool showTouchedZones,
            bool showFilledZones,
            bool showBrokenZones)
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
                    return showBrokenZones;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Uses the interval index only when all requested terminal states can be found in it.
    /// Filled and broken zones leave active indexes and must be selected from logical history.
    /// </summary>
    internal static class ZoneViewportSelectionPolicy
    {
        public static bool UseActiveZoneIndex(
            bool hasViewport,
            bool showFilledZones,
            bool showBrokenZones,
            bool showBreakerParentHistory = false)
        {
            return hasViewport
                && !showFilledZones
                && !showBrokenZones
                && !showBreakerParentHistory;
        }
    }
}