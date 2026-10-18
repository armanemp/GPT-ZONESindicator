using System;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void TryCreateOb(Engine engine, Swing swing, int breakIndex, double atr)
        {
            bool bullish = swing.High;
            int swingIndex = swing.Idx;

            if (breakIndex - swingIndex < 2)
            {
                Reject("ob: leg too short");
                return;
            }

            double breakClose = C(breakIndex);
            double strength = bullish ? breakClose - swing.Price : swing.Price - breakClose;
            if (strength < ObMinBreakAtr * atr)
            {
                Reject("ob: break strength");
                return;
            }

            if (ObRequireDisplacement && Math.Abs(C(breakIndex) - O(breakIndex)) < ObMinBreakBodyAtr * atr)
            {
                Reject("ob: break displacement");
                return;
            }

            OrderBlockCandidateSearch search = OrderBlockCandidateEvaluator.Find(
                bullish,
                swingIndex,
                breakIndex,
                _highPriceAt,
                _lowPriceAt,
                _openPriceAt,
                _closePriceAt);

            int candidate = search.CandidateIndex;

            if (candidate < 0)
            {
                Reject("ob: no opposite candle");
                return;
            }

            if (candidate - swingIndex > ObMaxBarsFromSwing)
            {
                Reject("ob: too far from swing");
                return;
            }

            OrderBlockBoundsResult bounds = OrderBlockRules.EvaluateBounds(
                L(candidate),
                H(candidate),
                O(candidate),
                C(candidate),
                ObRangeMode,
                ObMaxSizeAtr * atr,
                ObUseBodyIfOversize,
                Math.Max(_tick, ObMinSizeAtr * atr));

            if (!bounds.Accepted)
            {
                Reject(bounds.Failure == OrderBlockBoundsFailure.TooSmall
                    ? "ob: min size"
                    : "ob: oversize");
                return;
            }

            double lower = bounds.Lower;
            double upper = bounds.Upper;

            int key = candidate * 2 + (bullish ? 1 : 0);
            Zone existing;
            if (_obByCandle.TryGetValue(key, out existing))
            {
                if (engine.Major)
                    existing.Major = true;
                Reject("ob: duplicate candle");
                return;
            }

            double impulse = 0;
            for (int k = candidate; k <= breakIndex; k++)
            {
                double move = bullish ? H(k) - lower : upper - L(k);
                if (move > impulse)
                    impulse = move;
            }

            Zone zone = new Zone
            {
                Kind = SznpKind.OrderBlock,
                Bull = bullish,
                Lower = lower,
                Upper = upper,
                OriginIdx = candidate,
                ConfirmIdx = breakIndex,
                StartTime = T(candidate),
                Atr = atr,
                Momentum = impulse / atr,
                StructFactor = 1.0,
                Major = engine.Major
            };

            AddZone(zone);
            _obByCandle[key] = zone;
            ReplayZone(zone, candidate + 1, breakIndex);
        }
    }
}