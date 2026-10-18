using System;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void DetectGapAt(int index, double atr)
        {
            PriceGapEvaluation evaluation = PriceGapDetectorEvaluator.Evaluate(new PriceGapInput
            {
                PipSize = _pip,
                TickSize = _tick,
                Atr = atr,
                MinSizePips = GapMinSizePips,
                MinSizeAtr = GapMinSizeAtr,
                Method = GapMethod,
                Combine = GapCombine,
                PreviousClose = C(index - 1),
                CurrentOpen = O(index),
                PreviousHigh = H(index - 1),
                PreviousLow = L(index - 1),
                CurrentHigh = H(index),
                CurrentLow = L(index)
            });

            if (evaluation.RejectedOpenCloseMinimum)
                Reject("gap: open/close min size");
            if (evaluation.RejectedRangeMinimum)
                Reject("gap: range min size");

            if (evaluation.HasOpenClose)
                CreateGap(index, atr, evaluation.OpenCloseBullish,
                    evaluation.OpenCloseLower, evaluation.OpenCloseUpper);
            if (evaluation.HasRange)
                CreateGap(index, atr, evaluation.RangeBullish,
                    evaluation.RangeLower, evaluation.RangeUpper);
        }

        private void CreateGap(int index, double atr, bool bull, double lower, double upper)
        {
            if (IsDuplicate(SznpKind.Gap, bull, lower, upper))
            {
                Reject("gap: duplicate");
                return;
            }

            Zone zone = new Zone
            {
                Kind = SznpKind.Gap,
                Bull = bull,
                Lower = lower,
                Upper = upper,
                OriginIdx = index - 1,
                ConfirmIdx = index,
                StartTime = T(index),
                Atr = atr,
                Momentum = (upper - lower) / atr,
                StructFactor = StructFactorFor(bull, index)
            };

            AddZone(zone);
            UpdateZone(zone, index, false);
        }
    }
}
