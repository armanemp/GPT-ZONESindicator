using System;
using System.Globalization;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private SznpBreakMode ModeFor(SznpKind kind)
        {
            switch (kind)
            {
                case SznpKind.Fvg: return FvgBreakMode;
                case SznpKind.Gap: return GapBreakMode;
                case SznpKind.OrderBlock: return ObBreakMode;
                default: return BreakerBreakMode;
            }
        }

        /// <summary>
        /// Zone boundaries are immutable. Price interaction updates lifecycle state only.
        /// Fresh -> Touched -> Partial -> Filled -> Broken.
        /// </summary>
        private void UpdateZone(Zone zone, int index, bool countTouch)
        {
            if (zone.State == SznpState.Broken)
                return;

            double size = zone.Upper - zone.Lower;
            if (size <= 0)
                return;

            double high = H(index);
            double low = L(index);
            double open = O(index);
            double close = C(index);
            SznpBreakMode breakMode = ModeFor(zone.Kind);
            bool entered, fullyFilled, broken;
            double depth;

            if (zone.Bull)
            {
                entered = low < zone.Upper;
                depth = entered ? Math.Min(zone.Upper - low, size) : 0.0;
                fullyFilled = low <= zone.Lower;

                switch (breakMode)
                {
                    case SznpBreakMode.CandleClose: broken = close < zone.Lower; break;
                    case SznpBreakMode.BodyBeyond: broken = Math.Max(open, close) < zone.Lower; break;
                    default: broken = low < zone.Lower; break;
                }
            }
            else
            {
                entered = high > zone.Lower;
                depth = entered ? Math.Min(high - zone.Lower, size) : 0.0;
                fullyFilled = high >= zone.Upper;

                switch (breakMode)
                {
                    case SznpBreakMode.CandleClose: broken = close > zone.Upper; break;
                    case SznpBreakMode.BodyBeyond: broken = Math.Min(open, close) > zone.Upper; break;
                    default: broken = high > zone.Upper; break;
                }
            }

            if (broken)
            {
                if (depth > zone.MaxDepth)
                    zone.MaxDepth = depth;

                zone.State = SznpState.Broken;
                zone.BreakIdx = index;
                zone.BreakTime = CloseTimeOf(index);

                // Broken zones leave active lookup indexes immediately, but remain
                // in _zones for historical rendering and diagnostics.
                UnregisterZoneForLifecycle(zone);
                _alive.Remove(zone);

                if (zone.FillIdx < 0)
                {
                    zone.FillIdx = index;
                    zone.FillTime = zone.BreakTime;
                }

                OnZoneBroken(zone, index);
                return;
            }

            if (entered)
            {
                if (depth > zone.MaxDepth)
                    zone.MaxDepth = depth;
                // The indexed path does not visit untouched zones each bar.
                // Store the last entered bar so consecutive in-zone bars still count as
                // one touch, while a later re-entry after any gap counts as a new touch.
                if (countTouch && zone.LastEnteredIdx != index - 1)
                    zone.TouchCount++;
                zone.LastEnteredIdx = index;
            }
            }

            if (zone.State == SznpState.Filled)
                return;

            if (fullyFilled)
            {
                zone.State = SznpState.Filled;
                zone.FillIdx = index;
                zone.FillTime = CloseTimeOf(index);
                return;
            }

            if (zone.MaxDepth > 0)
            {
                zone.State = zone.MaxDepth / size >= PartialFillPct / 100.0
                    ? SznpState.Partial
                    : SznpState.Touched;
            }
        }

        /// <summary>Replay bars after an OB candle was discovered retrospectively.</summary>
        private void ReplayZone(Zone zone, int from, int to)
        {
            for (int index = from; index <= to; index++)
            {
                UpdateZone(zone, index, true);
                if (zone.State != SznpState.Broken)
                    continue;

                if (zone.ChildId > 0)
                    ReplayZone(_zones[zone.ChildId - 1], index + 1, to);
                return;
            }
        }

        private void OnZoneBroken(Zone zone, int index)
        {
            zone.Score = ComputeScore(zone, index, C(index), AtrAt(index));
            if (zone.Kind == SznpKind.OrderBlock && DetectBreaker)
                CreateBreaker(zone, index);
        }

        /// <summary>A broken order block can create a same-boundary breaker with reversed direction.</summary>
        private void CreateBreaker(Zone orderBlock, int index)
        {
            Zone breaker = new Zone
            {
                Kind = SznpKind.Breaker,
                Bull = !orderBlock.Bull,
                Lower = orderBlock.Lower,
                Upper = orderBlock.Upper,
                OriginIdx = orderBlock.OriginIdx,
                ConfirmIdx = index,
                StartTime = CloseTimeOf(index),
                Atr = orderBlock.Atr,
                Momentum = orderBlock.Momentum,
                StructFactor = orderBlock.StructFactor,
                Major = orderBlock.Major,
                ParentId = orderBlock.Id
            };

            AddZone(breaker);
            orderBlock.ChildId = breaker.Id;
        }

        private void FillFactors(Zone zone, int nowIndex, double price, double atrNow)
        {
            double size = zone.Upper - zone.Lower;

            _fac[0] = Clamp01(size / Math.Max(1e-12, ScoreSizeRefAtr * zone.Atr));
            _fac[1] = Clamp01(zone.Momentum / Math.Max(1e-9, ScoreMomentumRefAtr));
            _fac[2] = Clamp01(zone.StructFactor);

            double age = Math.Max(0, nowIndex - zone.ConfirmIdx);
            _fac[3] = 1.0 / (1.0 + age / Math.Max(1, FreshHalfLifeBars));
            _fac[4] = Math.Max(0.0, 1.0 - TouchPenalty * zone.TouchCount);

            double distance = price > zone.Upper
                ? price - zone.Upper
                : (price < zone.Lower ? zone.Lower - price : 0.0);
            _fac[5] = 1.0 / (1.0 + distance / Math.Max(1e-9, DistanceRefAtr * atrNow));
            _fac[6] = Clamp01(zone.Confluence / 2.0);
        }

        private double ComputeScore(Zone zone, int nowIndex, double price, double atrNow)
        {
            double weightSum = WSize + WMomentum + WStructure + WFresh + WTouch + WDistance + WConfluence;
            if (weightSum <= 0)
                return 0;

            FillFactors(zone, nowIndex, price, atrNow);
            double score = WSize * _fac[0]
                + WMomentum * _fac[1]
                + WStructure * _fac[2]
                + WFresh * _fac[3]
                + WTouch * _fac[4]
                + WDistance * _fac[5]
                + WConfluence * _fac[6];

            return score / weightSum * 100.0;
        }

        private string ScoreText(Zone zone)
        {
            if (_lastProcessed < 0)
                return string.Empty;

            int at = zone.State == SznpState.Broken ? zone.BreakIdx : _lastProcessed;
            double price = zone.State == SznpState.Broken ? C(at) : _lastClose;
            double score = ComputeScore(zone, at, price, AtrAt(at));

            return string.Format(CultureInfo.InvariantCulture,
                "score {0:0} (size {1:0.00} mom {2:0.00} ctx {3:0.00} fresh {4:0.00} touch {5:0.00} dist {6:0.00} conf {7:0.00})",
                score, _fac[0], _fac[1], _fac[2], _fac[3], _fac[4], _fac[5], _fac[6]);
        }
    }
}