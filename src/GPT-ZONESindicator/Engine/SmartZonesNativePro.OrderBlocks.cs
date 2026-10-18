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

            int extremeIndex = swingIndex;
            double extreme = bullish ? L(swingIndex) : H(swingIndex);
            for (int k = swingIndex; k < breakIndex; k++)
            {
                double value = bullish ? L(k) : H(k);
                if (bullish ? value < extreme : value > extreme)
                {
                    extreme = value;
                    extremeIndex = k;
                }
            }

            int candidate = -1;
            for (int k = extremeIndex; k >= swingIndex; k--)
            {
                if (bullish ? C(k) < O(k) : C(k) > O(k))
                {
                    candidate = k;
                    break;
                }
            }

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

            double lower, upper;
            if (ObRangeMode == SznpObRange.FullCandle)
            {
                lower = L(candidate);
                upper = H(candidate);
            }
            else
            {
                lower = Math.Min(O(candidate), C(candidate));
                upper = Math.Max(O(candidate), C(candidate));
            }

            if (ObMaxSizeAtr > 0 && upper - lower > ObMaxSizeAtr * atr)
            {
                if (ObUseBodyIfOversize && ObRangeMode == SznpObRange.FullCandle)
                {
                    lower = Math.Min(O(candidate), C(candidate));
                    upper = Math.Max(O(candidate), C(candidate));
                    if (upper - lower > ObMaxSizeAtr * atr)
                    {
                        Reject("ob: oversize");
                        return;
                    }
                }
                else
                {
                    Reject("ob: oversize");
                    return;
                }
            }

            if (upper - lower < Math.Max(_tick, ObMinSizeAtr * atr))
            {
                Reject("ob: min size");
                return;
            }

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