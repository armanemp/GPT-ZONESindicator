namespace cAlgo
{
    internal struct StructureBreakClassification
    {
        public bool IsChangeOfCharacter;
        public int NextTrend;
    }

    /// <summary>Pure close-crossing and trend-transition rules used by structure detection.</summary>
    internal static class StructureBreakEvaluator
    {
        public static bool IsLevelCrossed(bool swingHigh, double close, double swingPrice)
        {
            return swingHigh ? close > swingPrice : close < swingPrice;
        }

        public static StructureBreakClassification Classify(int priorTrend, bool bullishBreak)
        {
            return new StructureBreakClassification
            {
                IsChangeOfCharacter = bullishBreak ? priorTrend < 0 : priorTrend > 0,
                NextTrend = bullishBreak ? 1 : -1
            };
        }
    }
}