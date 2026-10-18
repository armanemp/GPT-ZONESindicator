using System;

namespace cAlgo
{
    /// <summary>Closed-bar timing and confirmation-index mapping for higher-timeframe series.</summary>
    internal static class HigherTimeframeTimingEvaluator
    {
        public static bool IsClosed(DateTime nextBarOpenTime, DateTime chartBarCloseTime)
        {
            return nextBarOpenTime <= chartBarCloseTime;
        }

        public static int FindClosedIndex(
            int barsCount,
            DateTime closeTime,
            Func<int, DateTime> openTimeAt)
        {
            if (openTimeAt == null)
                throw new ArgumentNullException(nameof(openTimeAt));
            if (barsCount < 2)
                return -1;

            int low = 0;
            int high = barsCount - 2;
            int best = -1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                if (openTimeAt(middle + 1) <= closeTime)
                {
                    best = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return best;
        }
    }
}