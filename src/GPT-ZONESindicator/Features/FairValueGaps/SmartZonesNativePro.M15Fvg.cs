using System;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void DetectM15FvgAt(int index, int confirmingChartIndex, double atr)
        {
            if (index < 2 || confirmingChartIndex < _startIdx)
                return;

            int first = index - 2;
            int middle = index - 1;
            FairValueGapEvaluation evaluation = FairValueGapDetectorEvaluator.Evaluate(new FairValueGapInput
            {
                OriginIndex = FindMainIndexAtOrBefore(_higherTimeframeState.Bars.OpenTimes[first]),
                ConfirmationIndex = confirmingChartIndex,
                FirstCandleTime = _higherTimeframeState.Bars.OpenTimes[first],
                ConfirmationCandleTime = _higherTimeframeState.Bars.OpenTimes[index],
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
                FirstHigh = _higherTimeframeState.Bars.HighPrices[first],
                FirstLow = _higherTimeframeState.Bars.LowPrices[first],
                MiddleOpen = _higherTimeframeState.Bars.OpenPrices[middle],
                MiddleClose = _higherTimeframeState.Bars.ClosePrices[middle],
                MiddleHigh = _higherTimeframeState.Bars.HighPrices[middle],
                MiddleLow = _higherTimeframeState.Bars.LowPrices[middle],
                ThirdHigh = _higherTimeframeState.Bars.HighPrices[index],
                ThirdLow = _higherTimeframeState.Bars.LowPrices[index]
            });

            if (!evaluation.Detected || !evaluation.Accepted)
                return;

            AddM15Zone(new Zone
            {
                Kind = SznpKind.Fvg,
                Bull = evaluation.Bullish,
                Lower = evaluation.Lower,
                Upper = evaluation.Upper,
                OriginIdx = evaluation.OriginIndex,
                ConfirmIdx = confirmingChartIndex,
                StartTime = evaluation.StartTime,
                Atr = atr,
                Momentum = evaluation.Momentum,
                StructFactor = M15StructureFactor(index),
                IsHigherTimeframe = true
            }, index, confirmingChartIndex);
        }
    }
}
