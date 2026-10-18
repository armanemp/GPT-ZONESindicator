using System;
using System.Collections.Generic;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void ProcessOrderBlockSignals(int throughIndex)
        {
            List<OrderBlockSignal> pending = _orderBlockState.PendingSignals;
            int i = 0;
            while (i < pending.Count)
            {
                OrderBlockSignal signal = pending[i];
                if (signal.BreakIndex > throughIndex)
                {
                    i++;
                    continue;
                }

                // Remove before processing so a malformed signal cannot be replayed forever.
                pending.RemoveAt(i);
                if (!DetectOb)
                    continue;

                try
                {
                    TryCreateOb(signal);
                }
                catch (Exception ex)
                {
                    LogError("Order block signal at bar " + signal.BreakIndex, ex);
                }
            }
        }

        private void TryCreateOb(OrderBlockSignal signal)
        {
            bool bullish = signal.SwingHigh;
            int swingIndex = signal.SwingIndex;
            int breakIndex = signal.BreakIndex;
            double atr = signal.Atr;
            double swingPrice = signal.SwingPrice;

            if (breakIndex - swingIndex < 2)
            {
                Reject("ob: leg too short");
                return;
            }

            double breakClose = C(breakIndex);
            double strength = bullish ? breakClose - swingPrice : swingPrice - breakClose;
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
            if (_orderBlockState.ByCandle.TryGetValue(key, out existing))
            {
                if (signal.Major)
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
                Major = signal.Major
            };

            AddZone(zone);
            _orderBlockState.ByCandle[key] = zone;
            ReplayZone(zone, candidate + 1, breakIndex);
        }
    }
}