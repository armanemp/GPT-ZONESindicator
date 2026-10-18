using System;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void DetectFvgAt(int index, double atr)
        {
            int first = index - 2;
            int middle = index - 1;

            double firstHigh = H(first);
            double firstLow = L(first);
            double thirdHigh = H(index);
            double thirdLow = L(index);

            bool bullish = thirdLow > firstHigh;
            bool bearish = thirdHigh < firstLow;
            if (!bullish && !bearish)
                return;

            double lower = bullish ? firstHigh : thirdHigh;
            double upper = bullish ? thirdLow : firstLow;
            double size = upper - lower;

            if (size < _tick * 0.5)
            {
                Reject("fvg: below tick");
                return;
            }

            double minSize = Math.Max(FvgMinSizePips * _pip, FvgMinSizeAtr * atr);
            if (size < minSize)
            {
                Reject("fvg: min size");
                return;
            }

            double middleOpen = O(middle);
            double middleClose = C(middle);
            double body = Math.Abs(middleClose - middleOpen);
            double range = H(middle) - L(middle);

            if (FvgRequireDisplacement)
            {
                if (body < FvgMinBodyAtr * atr)
                {
                    Reject("fvg: displacement body");
                    return;
                }

                if (range <= 0 || body / range < FvgMinBodyRatio)
                {
                    Reject("fvg: body/range ratio");
                    return;
                }

                if (FvgMatchDirection && (bullish ? middleClose <= middleOpen : middleClose >= middleOpen))
                {
                    Reject("fvg: displacement direction");
                    return;
                }
            }

            if (IsDuplicate(SznpKind.Fvg, bullish, lower, upper))
            {
                Reject("fvg: duplicate");
                return;
            }

            Zone zone = new Zone
            {
                Kind = SznpKind.Fvg,
                Bull = bullish,
                Lower = lower,
                Upper = upper,
                OriginIdx = first,
                ConfirmIdx = index,
                StartTime = FvgStartMode == SznpStartMode.FirstCandle ? T(first) : T(index),
                Atr = atr,
                Momentum = body / atr,
                StructFactor = StructFactorFor(bullish, index)
            };

            AddZone(zone);
        }

        /// <summary>Price gaps are kept independent from three-candle fair-value gaps.</summary>
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