using System;

namespace cAlgo
{
    /// <summary>Computes a time span representing forty chart-bar intervals.</summary>
    internal static class FutureBarTimeEvaluator
    {
        public static TimeSpan GetFortyBarSpan(
            int barCount,
            TimeSpan typicalBarSpan,
            TimeSpan observedFortyBarSpan)
        {
            if (barCount > 40 && observedFortyBarSpan > TimeSpan.Zero)
                return observedFortyBarSpan;

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