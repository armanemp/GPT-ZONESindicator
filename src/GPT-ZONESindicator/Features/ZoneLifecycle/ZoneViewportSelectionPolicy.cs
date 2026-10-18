namespace cAlgo
{
    /// <summary>Decides when the active-zone index is sufficient for viewport drawing.</summary>
    internal static class ZoneViewportSelectionPolicy
    {
        public static bool UseActiveZoneIndex(
            bool hasViewport,
            bool showFilledZones,
            bool showBrokenZones,
            bool showBreakerHistory)
        {
            // Rank/cap operations are viewport-local. The active index is therefore valid
            // for ranked modes too, unless terminal records are explicitly requested.
            return hasViewport
                && !showFilledZones
                && !showBrokenZones
                && !showBreakerHistory;
        }
    }
}