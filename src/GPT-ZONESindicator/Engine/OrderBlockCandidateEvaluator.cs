using System;

namespace cAlgo
{
    internal struct OrderBlockCandidateSearch
    {
        public int ExtremeIndex;
        public int CandidateIndex;
    }

    /// <summary>
    /// Finds the leg extreme and the nearest qualifying opposite candle searched back from it.
    /// This intentionally preserves the production scan order.
    /// </summary>
    internal static class OrderBlockCandidateEvaluator
    {
        public static OrderBlockCandidateSearch Find(
            bool bullish,
            int swingIndex,
            int breakIndex,
            Func<int, double> highAt,
            Func<int, double> lowAt,
            Func<int, double> openAt,
            Func<int, double> closeAt)
        {
            if (highAt == null) throw new ArgumentNullException(nameof(highAt));
            if (lowAt == null) throw new ArgumentNullException(nameof(lowAt));
            if (openAt == null) throw new ArgumentNullException(nameof(openAt));
            if (closeAt == null) throw new ArgumentNullException(nameof(closeAt));

            OrderBlockCandidateSearch result = new OrderBlockCandidateSearch
            {
                ExtremeIndex = -1,
                CandidateIndex = -1
            };

            if (swingIndex < 0 || breakIndex - swingIndex < 2)
                return result;

            result.ExtremeIndex = swingIndex;
            double extreme = bullish ? lowAt(swingIndex) : highAt(swingIndex);

            for (int index = swingIndex; index < breakIndex; index++)
            {
                double value = bullish ? lowAt(index) : highAt(index);
                if (bullish ? value < extreme : value > extreme)
                {
                    extreme = value;
                    result.ExtremeIndex = index;
                }
            }

            for (int index = result.ExtremeIndex; index >= swingIndex; index--)
            {
                bool oppositeCandle = bullish
                    ? closeAt(index) < openAt(index)
                    : closeAt(index) > openAt(index);
                if (oppositeCandle)
                {
                    result.CandidateIndex = index;
                    break;
                }
            }

            return result;
        }
    }
}
