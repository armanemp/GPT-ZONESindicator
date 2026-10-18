using System;

namespace cAlgo
{
    internal struct PivotEvaluation
    {
        public int PivotIndex;
        public bool IsReady;
        public bool IsHigh;
        public bool IsLow;
    }

    /// <summary>
    /// Pure pivot confirmation rules. A candidate is not evaluated until all right-hand
    /// bars have closed. Tie handling intentionally matches the indicator's existing rule:
    /// equal highs/lows to the left disqualify; equal highs/lows to the right do not.
    /// </summary>
    internal static class PivotEvaluator
    {
        public static PivotEvaluation Evaluate(
            int pivotIndex,
            int leftBars,
            int rightBars,
            int closedThroughIndex,
            int firstAllowedPivotIndex,
            Func<int, double> highAt,
            Func<int, double> lowAt)
        {
            if (highAt == null) throw new ArgumentNullException(nameof(highAt));
            if (lowAt == null) throw new ArgumentNullException(nameof(lowAt));
            if (leftBars < 0) throw new ArgumentOutOfRangeException(nameof(leftBars));
            if (rightBars < 0) throw new ArgumentOutOfRangeException(nameof(rightBars));

            PivotEvaluation result = new PivotEvaluation { PivotIndex = pivotIndex };
            if (pivotIndex < firstAllowedPivotIndex
                || pivotIndex - leftBars < 0
                || closedThroughIndex < pivotIndex + rightBars)
                return result;

            result.IsReady = true;
            double pivotHigh = highAt(pivotIndex);
            double pivotLow = lowAt(pivotIndex);
            bool pivotRangeValid = IsValidRange(pivotHigh, pivotLow);
            bool isHigh = pivotRangeValid;
            bool isLow = pivotRangeValid;

            for (int offset = 1; offset <= leftBars && (isHigh || isLow); offset++)
            {
                double leftHigh = highAt(pivotIndex - offset);
                double leftLow = lowAt(pivotIndex - offset);
                bool leftRangeValid = IsValidRange(leftHigh, leftLow);

                // NaN comparisons are always false; explicitly reject malformed price
                // samples instead of accidentally accepting them as an extreme.
                if (!leftRangeValid || leftHigh >= pivotHigh)
                    isHigh = false;
                if (!leftRangeValid || leftLow <= pivotLow)
                    isLow = false;
            }

            for (int offset = 1; offset <= rightBars && (isHigh || isLow); offset++)
            {
                double rightHigh = highAt(pivotIndex + offset);
                double rightLow = lowAt(pivotIndex + offset);
                bool rightRangeValid = IsValidRange(rightHigh, rightLow);

                if (!rightRangeValid || rightHigh > pivotHigh)
                    isHigh = false;
                if (!rightRangeValid || rightLow < pivotLow)
                    isLow = false;
            }

            result.IsHigh = isHigh;
            result.IsLow = isLow;
            return result;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsValidRange(double high, double low)
        {
            return IsFinite(high) && IsFinite(low) && high >= low;
        }
    }
}
