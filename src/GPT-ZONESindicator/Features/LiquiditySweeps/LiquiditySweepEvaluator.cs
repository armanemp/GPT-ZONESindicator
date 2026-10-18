namespace cAlgo
{
    /// <summary>Pure liquidity-sweep predicate, independent of chart and Bars APIs.</summary>
    internal static class LiquiditySweepEvaluator
    {
        public static bool IsSweep(
            bool highLevel,
            double level,
            double high,
            double low,
            double open,
            double close,
            double minPenetration,
            double minReturn,
            SznpSweepMeasure measure)
        {
            if (highLevel)
            {
                double extreme = measure == SznpSweepMeasure.Wick ? high : System.Math.Max(open, close);
                double penetration = extreme - level;
                return penetration > 0
                    && penetration >= minPenetration
                    && close < level
                    && level - close >= minReturn;
            }

            double lowExtreme = measure == SznpSweepMeasure.Wick ? low : System.Math.Min(open, close);
            double lowPenetration = level - lowExtreme;
            return lowPenetration > 0
                && lowPenetration >= minPenetration
                && close > level
                && close - level >= minReturn;
        }
    }
}