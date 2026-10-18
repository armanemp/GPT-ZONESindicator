using System;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void DetectM15GapAt(int index, int confirmingChartIndex, double atr)
        {
            if (index < 1 || confirmingChartIndex < _startIdx)
                return;

            PriceGapEvaluation evaluation = PriceGapDetectorEvaluator.Evaluate(new PriceGapInput
            {
                PipSize = _pip,
                TickSize = _tick,
                Atr = atr,
                MinSizePips = GapMinSizePips,
                MinSizeAtr = GapMinSizeAtr,
                Method = GapMethod,
                Combine = GapCombine,
                PreviousClose = _higherTimeframeState.Bars.ClosePrices[index - 1],
                CurrentOpen = _higherTimeframeState.Bars.OpenPrices[index],
                PreviousHigh = _higherTimeframeState.Bars.HighPrices[index - 1],
                PreviousLow = _higherTimeframeState.Bars.LowPrices[index - 1],
                CurrentHigh = _higherTimeframeState.Bars.HighPrices[index],
                CurrentLow = _higherTimeframeState.Bars.LowPrices[index]
            });

            if (evaluation.HasOpenClose)
                CreateM15Gap(index, confirmingChartIndex, atr, evaluation.OpenCloseBullish,
                    evaluation.OpenCloseLower, evaluation.OpenCloseUpper);
            if (evaluation.HasRange)
                CreateM15Gap(index, confirmingChartIndex, atr, evaluation.RangeBullish,
                    evaluation.RangeLower, evaluation.RangeUpper);
        }

        private void CreateM15Gap(
            int index,
            int confirmingChartIndex,
            double atr,
            bool bullish,
            double lower,
            double upper)
        {
            AddM15Zone(new Zone
            {
                Kind = SznpKind.Gap,
                Bull = bullish,
                Lower = lower,
                Upper = upper,
                OriginIdx = FindMainIndexAtOrBefore(_higherTimeframeState.Bars.OpenTimes[index]),
                ConfirmIdx = confirmingChartIndex,
                StartTime = _higherTimeframeState.Bars.OpenTimes[index],
                Atr = atr,
                Momentum = (upper - lower) / atr,
                StructFactor = M15StructureFactor(index),
                IsHigherTimeframe = true
            }, index, confirmingChartIndex);
        }
    }
}
