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
            Func<int, DateTime> barCloseTimeAt)
        {
            if (barCloseTimeAt == null)
                throw new ArgumentNullException(nameof(barCloseTimeAt));
            if (barsCount < 1)
                return -1;

            int low = 0;
            int high = barsCount - 1;
            int best = -1;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                if (barCloseTimeAt(middle) <= closeTime)
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