namespace cAlgo
{
    /// <summary>Decides when the active-zone index is sufficient for viewport drawing.</summary>
    internal static class ZoneViewportSelectionPolicy
    {
        public static bool UseActiveZoneIndex(
            bool hasViewport,
            bool globalSelection,
            bool showFilledZones,
            bool showBrokenZones,
            bool showBreakerHistory)
        {
            // Active-zone indexes do not include terminal records. Terminal-history view
            // must scan the logical collection to avoid silently omitting filled/broken zones.
            return hasViewport
                && !globalSelection
                && !showFilledZones
                && !showBrokenZones
                && !showBreakerHistory;
        }
    }
}