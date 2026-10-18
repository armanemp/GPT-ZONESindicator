using System;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void DetectFvgAt(int index, double atr)
        {
            int first = index - 2;
            int middle = index - 1;
            FairValueGapEvaluation evaluation = FairValueGapDetectorEvaluator.Evaluate(new FairValueGapInput
            {
                OriginIndex = first,
                ConfirmationIndex = index,
                FirstCandleTime = T(first),
                ConfirmationCandleTime = T(index),
                TickSize = _tick,
                PipSize = _pip,
                Atr = atr,
                MinSizePips = FvgMinSizePips,
                MinSizeAtr = FvgMinSizeAtr,
                RequireDisplacement = FvgRequireDisplacement,
                MinBodyAtr = FvgMinBodyAtr,
                MinBodyRatio = FvgMinBodyRatio,
                MatchDirection = FvgMatchDirection,
                StartMode = FvgStartMode,
                FirstHigh = H(first),
                FirstLow = L(first),
                MiddleOpen = O(middle),
                MiddleClose = C(middle),
                MiddleHigh = H(middle),
                MiddleLow = L(middle),
                ThirdHigh = H(index),
                ThirdLow = L(index)
            });

            if (!evaluation.Detected)
                return;
            if (!evaluation.Accepted)
            {
                if (!string.IsNullOrEmpty(evaluation.Rejection))
                    Reject(evaluation.Rejection);
                return;
            }

            if (IsDuplicate(SznpKind.Fvg, evaluation.Bullish, evaluation.Lower, evaluation.Upper))
            {
                Reject("fvg: duplicate");
                return;
            }

            AddZone(new Zone
            {
                Kind = SznpKind.Fvg,
                Bull = evaluation.Bullish,
                Lower = evaluation.Lower,
                Upper = evaluation.Upper,
                OriginIdx = evaluation.OriginIndex,
                ConfirmIdx = evaluation.ConfirmationIndex,
                StartTime = evaluation.StartTime,
                Atr = atr,
                Momentum = evaluation.Momentum,
                StructFactor = StructFactorFor(evaluation.Bullish, index)
            });
        }
    }
}
