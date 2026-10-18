using System;

namespace cAlgo
{
    /// <summary>Deterministic minimum-distance rules for confirmed structure swings.</summary>
    internal static class StructureSwingDistanceEvaluator
    {
        public static bool IsFarEnough(
            double price,
            bool hasOppositeSwing,
            double oppositePrice,
            double minimumDistance)
        {
            if (!IsFinite(price))
                return false;

            if (!hasOppositeSwing)
                return true;

            if (!IsFinite(oppositePrice) || !IsFinite(minimumDistance))
                return false;

            return minimumDistance <= 0
                || Math.Abs(price - oppositePrice) >= minimumDistance;
        }

        /// <summary>
        /// Dual high/low pivots come from the same candle. Reject the pair as a unit when
        /// its range is smaller than the configured minimum, avoiding high-first/low-first bias.
        /// </summary>
        public static bool RejectDualPivotPair(
            bool isHigh,
            bool isLow,
            double highPrice,
            double lowPrice,
            double minimumDistance)
        {
            if (!isHigh || !isLow || minimumDistance <= 0)
                return false;

            if (!IsFinite(highPrice) || !IsFinite(lowPrice) || !IsFinite(minimumDistance))
                return true;

            return highPrice < lowPrice
                || highPrice - lowPrice < minimumDistance;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
