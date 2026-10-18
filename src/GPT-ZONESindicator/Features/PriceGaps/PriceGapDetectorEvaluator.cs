using System;

namespace cAlgo
{
    internal struct PriceGapInput
    {
        public double PipSize, TickSize, Atr, MinSizePips, MinSizeAtr;
        public SznpGapMethod Method;
        public SznpGapCombine Combine;
        public double PreviousClose, CurrentOpen;
        public double PreviousHigh, PreviousLow, CurrentHigh, CurrentLow;
    }

    internal struct PriceGapEvaluation
    {
        public bool HasOpenClose, OpenCloseBullish, RejectedOpenCloseMinimum;
        public double OpenCloseLower, OpenCloseUpper;
        public bool HasRange, RangeBullish, RejectedRangeMinimum;
        public double RangeLower, RangeUpper;
    }

    /// <summary>
    /// Pure open/close and range-gap detection shared by M5 and M15. Registration,
    /// deduplication, lifecycle replay, and rendering remain with the owning runtime.
    /// </summary>
    internal static class PriceGapDetectorEvaluator
    {
        public static PriceGapEvaluation Evaluate(PriceGapInput input)
        {
            double minSize = Math.Max(
                Math.Max(input.MinSizePips * input.PipSize, input.MinSizeAtr * input.Atr),
                input.TickSize);
            PriceGapEvaluation result = new PriceGapEvaluation();
            bool wantOpenClose = input.Method != SznpGapMethod.RangeGap;
            bool wantRange = input.Method != SznpGapMethod.OpenCloseGap;

            if (wantOpenClose)
            {
                if (input.CurrentOpen > input.PreviousClose)
                {
                    result.HasOpenClose = true;
                    result.OpenCloseBullish = true;
                    result.OpenCloseLower = input.PreviousClose;
                    result.OpenCloseUpper = input.CurrentOpen;
                }
                else if (input.CurrentOpen < input.PreviousClose)
                {
                    result.HasOpenClose = true;
                    result.OpenCloseBullish = false;
                    result.OpenCloseLower = input.CurrentOpen;
                    result.OpenCloseUpper = input.PreviousClose;
                }

                if (result.HasOpenClose && result.OpenCloseUpper - result.OpenCloseLower < minSize)
                {
                    result.HasOpenClose = false;
                    result.RejectedOpenCloseMinimum = true;
                }
            }

            if (wantRange)
            {
                if (input.CurrentLow > input.PreviousHigh)
                {
                    result.HasRange = true;
                    result.RangeBullish = true;
                    result.RangeLower = input.PreviousHigh;
                    result.RangeUpper = input.CurrentLow;
                }
                else if (input.CurrentHigh < input.PreviousLow)
                {
                    result.HasRange = true;
                    result.RangeBullish = false;
                    result.RangeLower = input.CurrentHigh;
                    result.RangeUpper = input.PreviousLow;
                }

                if (result.HasRange && result.RangeUpper - result.RangeLower < minSize)
                {
                    result.HasRange = false;
                    result.RejectedRangeMinimum = true;
                }
            }

            if (result.HasOpenClose && result.HasRange)
            {
                if (input.Combine == SznpGapCombine.PreferRangeGap)
                    result.HasOpenClose = false;
                else if (input.Combine == SznpGapCombine.PreferOpenCloseGap)
                    result.HasRange = false;
            }

            return result;
        }
    }
}
