using System;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void DetectGapAt(int index, double atr)
        {
            double previousClose = C(index - 1);
            double currentOpen = O(index);
            double minSize = Math.Max(
                Math.Max(GapMinSizePips * _pip, GapMinSizeAtr * atr),
                _tick);

            bool wantOpenClose = GapMethod != SznpGapMethod.RangeGap;
            bool wantRange = GapMethod != SznpGapMethod.OpenCloseGap;

            bool hasOpenClose = false;
            bool openCloseBull = false;
            bool hasRange = false;
            bool rangeBull = false;
            double openCloseLower = 0, openCloseUpper = 0, rangeLower = 0, rangeUpper = 0;

            if (wantOpenClose)
            {
                if (currentOpen > previousClose)
                {
                    hasOpenClose = true;
                    openCloseBull = true;
                    openCloseLower = previousClose;
                    openCloseUpper = currentOpen;
                }
                else if (currentOpen < previousClose)
                {
                    hasOpenClose = true;
                    openCloseBull = false;
                    openCloseLower = currentOpen;
                    openCloseUpper = previousClose;
                }

                if (hasOpenClose && openCloseUpper - openCloseLower < minSize)
                {
                    hasOpenClose = false;
                    Reject("gap: open/close min size");
                }
            }

            if (wantRange)
            {
                if (L(index) > H(index - 1))
                {
                    hasRange = true;
                    rangeBull = true;
                    rangeLower = H(index - 1);
                    rangeUpper = L(index);
                }
                else if (H(index) < L(index - 1))
                {
                    hasRange = true;
                    rangeBull = false;
                    rangeLower = H(index);
                    rangeUpper = L(index - 1);
                }

                if (hasRange && rangeUpper - rangeLower < minSize)
                {
                    hasRange = false;
                    Reject("gap: range min size");
                }
            }

            if (hasOpenClose && hasRange)
            {
                if (GapCombine == SznpGapCombine.PreferRangeGap)
                    hasOpenClose = false;
                else if (GapCombine == SznpGapCombine.PreferOpenCloseGap)
                    hasRange = false;
            }

            if (hasOpenClose)
                CreateGap(index, atr, openCloseBull, openCloseLower, openCloseUpper);
            if (hasRange)
                CreateGap(index, atr, rangeBull, rangeLower, rangeUpper);
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
