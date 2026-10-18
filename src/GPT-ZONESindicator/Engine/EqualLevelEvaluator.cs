using System;

namespace cAlgo
{
    internal struct EqualLevelPairEvaluation
    {
        public bool StopSearch;
        public bool Accepted;
        public double Level;
    }

    /// <summary>
    /// Deterministic equal-high/equal-low pair qualification. StopSearch preserves the
    /// production scan's early-exit behavior for an over-old or unclean matching swing.
    /// </summary>
    internal static class EqualLevelEvaluator
    {
        public static EqualLevelPairEvaluation Evaluate(
            bool candidateHigh,
            int candidateIndex,
            double candidatePrice,
            bool swingHigh,
            int swingIndex,
            double swingPrice,
            int minBarsApart,
            int maxBarsApart,
            double tolerance,
            Func<int, double> highAt,
            Func<int, double> lowAt)
        {
            if (highAt == null) throw new ArgumentNullException(nameof(highAt));
            if (lowAt == null) throw new ArgumentNullException(nameof(lowAt));
            if (minBarsApart < 0) throw new ArgumentOutOfRangeException(nameof(minBarsApart));
            if (maxBarsApart < 0) throw new ArgumentOutOfRangeException(nameof(maxBarsApart));

            EqualLevelPairEvaluation result = new EqualLevelPairEvaluation();
            if (candidateHigh != swingHigh)
                return result;

            int distance = swingIndex - candidateIndex;
            if (distance > maxBarsApart)
            {
                result.StopSearch = true;
                return result;
            }

            if (distance < minBarsApart || Math.Abs(candidatePrice - swingPrice) > tolerance)
                return result;

            double level = swingHigh
                ? Math.Max(candidatePrice, swingPrice)
                : Math.Min(candidatePrice, swingPrice);

            for (int bar = candidateIndex + 1; bar < swingIndex; bar++)
            {
                bool crossedToleranceBand = swingHigh
                    ? highAt(bar) > level + tolerance
                    : lowAt(bar) < level - tolerance;

                if (crossedToleranceBand)
                {
                    result.StopSearch = true;
                    return result;
                }
            }

            result.Accepted = true;
            result.Level = level;
            return result;
        }
    }
}
