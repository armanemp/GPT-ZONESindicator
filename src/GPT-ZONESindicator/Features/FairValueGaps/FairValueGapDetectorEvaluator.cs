using System;

namespace cAlgo
{
    internal struct FairValueGapInput
    {
        public int OriginIndex, ConfirmationIndex;
        public DateTime FirstCandleTime, ConfirmationCandleTime;
        public double TickSize, PipSize, Atr;
        public double MinSizePips, MinSizeAtr;
        public bool RequireDisplacement, MatchDirection;
        public double MinBodyAtr, MinBodyRatio;
        public SznpStartMode StartMode;
        public double FirstHigh, FirstLow;
        public double MiddleOpen, MiddleClose, MiddleHigh, MiddleLow;
        public double ThirdHigh, ThirdLow;
    }

    internal struct FairValueGapEvaluation
    {
        public bool Detected, Accepted, Bullish;
        public double Lower, Upper, Momentum;
        public int OriginIndex, ConfirmationIndex;
        public DateTime StartTime;
        public string Rejection;
    }

    /// <summary>
    /// Pure three-candle FVG rules shared by chart-timeframe and higher-timeframe
    /// detection. It reads only the supplied closed-candle snapshot and never mutates
    /// zones, detector state, or chart objects.
    /// </summary>
    internal static class FairValueGapDetectorEvaluator
    {
        public static FairValueGapEvaluation Evaluate(FairValueGapInput input)
        {
            bool bullish = input.ThirdLow > input.FirstHigh;
            bool bearish = input.ThirdHigh < input.FirstLow;
            if (!bullish && !bearish)
                return new FairValueGapEvaluation();

            double lower = bullish ? input.FirstHigh : input.ThirdHigh;
            double upper = bullish ? input.ThirdLow : input.FirstLow;
            double size = upper - lower;
            FairValueGapEvaluation result = new FairValueGapEvaluation
            {
                Detected = true,
                Bullish = bullish,
                Lower = lower,
                Upper = upper,
                OriginIndex = input.OriginIndex,
                ConfirmationIndex = input.ConfirmationIndex,
                StartTime = input.StartMode == SznpStartMode.FirstCandle
                    ? input.FirstCandleTime
                    : input.ConfirmationCandleTime
            };

            if (size < input.TickSize * 0.5)
            {
                result.Rejection = "fvg: below tick";
                return result;
            }

            double minSize = Math.Max(input.MinSizePips * input.PipSize, input.MinSizeAtr * input.Atr);
            if (size < minSize)
            {
                result.Rejection = "fvg: min size";
                return result;
            }

            double body = Math.Abs(input.MiddleClose - input.MiddleOpen);
            double range = input.MiddleHigh - input.MiddleLow;
            result.Momentum = body / input.Atr;

            if (input.RequireDisplacement)
            {
                if (body < input.MinBodyAtr * input.Atr)
                {
                    result.Rejection = "fvg: displacement body";
                    return result;
                }

                if (range <= 0 || body / range < input.MinBodyRatio)
                {
                    result.Rejection = "fvg: body/range ratio";
                    return result;
                }

                if (input.MatchDirection
                    && (bullish ? input.MiddleClose <= input.MiddleOpen : input.MiddleClose >= input.MiddleOpen))
                {
                    result.Rejection = "fvg: displacement direction";
                    return result;
                }
            }

            result.Accepted = true;
            return result;
        }
    }
}
