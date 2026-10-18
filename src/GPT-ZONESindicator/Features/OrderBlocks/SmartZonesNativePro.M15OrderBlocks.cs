using System;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void TryCreateM15OrderBlock(
            int swingIndex,
            int breakIndex,
            int confirmingChartIndex,
            double atr,
            bool bullish)
        {
            if (confirmingChartIndex < _startIdx || breakIndex - swingIndex < 2)
            {
                Reject("M15 OB: short/unavailable break");
                return;
            }

            double breakClose = _higherTimeframeState.Bars.ClosePrices[breakIndex];
            double swingPrice = _higherTimeframeState.Bars.HighPrices[swingIndex];
            if (!bullish)
                swingPrice = _higherTimeframeState.Bars.LowPrices[swingIndex];

            double strength = bullish ? breakClose - swingPrice : swingPrice - breakClose;
            if (strength < ObMinBreakAtr * atr)
                return;

            if (ObRequireDisplacement
                && Math.Abs(_higherTimeframeState.Bars.ClosePrices[breakIndex] - _higherTimeframeState.Bars.OpenPrices[breakIndex])
                    < ObMinBreakBodyAtr * atr)
                return;

            OrderBlockCandidateSearch search = OrderBlockCandidateEvaluator.Find(
                bullish,
                swingIndex,
                breakIndex,
                delegate (int barIndex) { return _higherTimeframeState.Bars.HighPrices[barIndex]; },
                delegate (int barIndex) { return _higherTimeframeState.Bars.LowPrices[barIndex]; },
                delegate (int barIndex) { return _higherTimeframeState.Bars.OpenPrices[barIndex]; },
                delegate (int barIndex) { return _higherTimeframeState.Bars.ClosePrices[barIndex]; });

            int candidate = search.CandidateIndex;
            if (candidate < 0 || candidate - swingIndex > ObMaxBarsFromSwing)
                return;

            OrderBlockBoundsResult bounds = OrderBlockRules.EvaluateBounds(
                _higherTimeframeState.Bars.LowPrices[candidate],
                _higherTimeframeState.Bars.HighPrices[candidate],
                _higherTimeframeState.Bars.OpenPrices[candidate],
                _higherTimeframeState.Bars.ClosePrices[candidate],
                ObRangeMode,
                ObMaxSizeAtr * atr,
                ObUseBodyIfOversize,
                Math.Max(_tick, ObMinSizeAtr * atr));

            if (!bounds.Accepted)
                return;

            double impulse = 0;
            for (int k = candidate; k <= breakIndex; k++)
            {
                double move = bullish
                    ? _higherTimeframeState.Bars.HighPrices[k] - bounds.Lower
                    : bounds.Upper - _higherTimeframeState.Bars.LowPrices[k];
                if (move > impulse)
                    impulse = move;
            }

            AddM15Zone(new Zone
            {
                Kind = SznpKind.OrderBlock,
                Bull = bullish,
                Lower = bounds.Lower,
                Upper = bounds.Upper,
                OriginIdx = FindMainIndexAtOrBefore(_higherTimeframeState.Bars.OpenTimes[candidate]),
                ConfirmIdx = confirmingChartIndex,
                StartTime = _higherTimeframeState.Bars.OpenTimes[candidate],
                Atr = atr,
                Momentum = impulse / atr,
                StructFactor = 1.0,
                Major = true,
                IsHigherTimeframe = true
            }, breakIndex, confirmingChartIndex);
        }
    }
}
