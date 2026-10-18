using System;

namespace cAlgo
{
    /// <summary>Computes the future edge of active zones from the configured extension in days.</summary>
    internal static class FutureZoneTimeEvaluator
    {
        public static DateTime GetExtensionEnd(DateTime lastBarOpen, int extensionDays)
        {
            int days = Math.Max(1, extensionDays);
            try
            {
                return lastBarOpen.AddDays(days);
            }
            catch (ArgumentOutOfRangeException)
            {
                return DateTime.MaxValue;
            }
        }
    }
}