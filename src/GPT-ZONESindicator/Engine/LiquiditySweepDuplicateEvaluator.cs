using System;

namespace cAlgo
{
    /// <summary>Pure duplicate identity predicate for same-bar liquidity sweep events.</summary>
    internal static class LiquiditySweepDuplicateEvaluator
    {
        public static bool IsDuplicate(
            bool candidateBull,
            double candidateLevel,
            DateTime candidateEndTime,
            bool existingBull,
            double existingLevel,
            DateTime existingEndTime,
            double tickSize)
        {
            return candidateEndTime == existingEndTime
                && candidateBull == existingBull
                && Math.Abs(candidateLevel - existingLevel) < tickSize;
        }
    }
}
