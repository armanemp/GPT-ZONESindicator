using System;

namespace cAlgo
{
    /// <summary>Computes a time span representing forty chart-bar intervals.</summary>
    internal static class FutureBarTimeEvaluator
    {
        public static TimeSpan GetFortyBarSpan(TimeSpan typicalBarSpan)
        {
            // The extension is a candle-count rule, not a wall-clock replay of the last
            // forty bars. Avoid carrying a historical weekend/session gap into the future
            // margin, especially when attaching the indicator at the start of a new week.
            long oneBarTicks = Math.Max(1L, typicalBarSpan.Ticks);
            try
            {
                return TimeSpan.FromTicks(checked(oneBarTicks * 40L));
            }
            catch (OverflowException)
            {
                return TimeSpan.FromDays(14);
            }
        }
        }
    }
}