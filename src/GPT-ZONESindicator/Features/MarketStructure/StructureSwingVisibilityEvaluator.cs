namespace cAlgo
{
    /// <summary>
    /// Pure visibility rules for market-structure swing levels.
    /// A swept or silently-broken swing is not an active level and cannot produce a BOS/CHoCH line.
    /// </summary>
    internal static class StructureSwingVisibilityEvaluator
    {
        public static bool ShouldShowLevel(
            bool broken,
            bool swept,
            bool silentBreak,
            bool changeOfCharacter,
            bool showBos,
            bool showChoch,
            bool showActiveLevels)
        {
            if (swept)
                return false;

            if (!broken)
                return showActiveLevels;

            if (silentBreak)
                return false;

            return changeOfCharacter ? showChoch : showBos;
        }
    }
}
