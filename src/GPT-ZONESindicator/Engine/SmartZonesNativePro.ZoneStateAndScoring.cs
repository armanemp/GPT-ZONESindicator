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
            ZoneLifecycleResult result = ZoneLifecycleEvaluator.Evaluate(new ZoneLifecycleInput
            {
                Index = index,
                Bull = zone.Bull,
                Lower = zone.Lower,
                Upper = zone.Upper,
                Open = O(index),
                High = H(index),
                Low = L(index),
                Close = C(index),
                BreakMode = ModeFor(zone.Kind),
                State = zone.State,
                MaxDepth = zone.MaxDepth,
                TouchCount = zone.TouchCount,
                LastEnteredIndex = zone.LastEnteredIdx,
                PartialFillPct = PartialFillPct,
                CountTouch = countTouch
            });

            if (!result.Processed)
                return;

            zone.MaxDepth = result.MaxDepth;
            zone.TouchCount = result.TouchCount;
            zone.LastEnteredIdx = result.LastEnteredIndex;
            zone.State = result.State;

            if (result.BrokeNow)
            {
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

            if (result.FilledNow)
            {
                zone.FillIdx = index;
                zone.FillTime = CloseTimeOf(index);
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
            // Guard the creation boundary itself, not just the caller: this prevents
            // duplicate or stale breaker children if this path is ever reused.
            if (orderBlock == null
                || orderBlock.Kind != SznpKind.OrderBlock
                || orderBlock.State != SznpState.Broken
                || orderBlock.BreakIdx != index
                || orderBlock.ChildId > 0)
            {
                Reject("breaker: parent not eligible");
                return;
            }

            ZoneBreakerSpec spec = ZoneBreakerFactory.Create(new ZoneBreakerInput
            {
                ParentId = orderBlock.Id,
                ParentKind = orderBlock.Kind,
                ParentState = orderBlock.State,
                ExistingChildId = orderBlock.ChildId,
                ParentBull = orderBlock.Bull,
                Lower = orderBlock.Lower,
                Upper = orderBlock.Upper,
                OriginIndex = orderBlock.OriginIdx,
                ConfirmationIndex = index,
                ConfirmationTime = CloseTimeOf(index),
                Atr = orderBlock.Atr,
                Momentum = orderBlock.Momentum,
                StructFactor = orderBlock.StructFactor,
                Major = orderBlock.Major
            });

            Zone breaker = new Zone
            {
                Kind = SznpKind.Breaker,
                Bull = spec.Bull,
                Lower = spec.Lower,
                Upper = spec.Upper,
                OriginIdx = spec.OriginIndex,
                ConfirmIdx = spec.ConfirmationIndex,
                StartTime = spec.StartTime,
                Atr = spec.Atr,
                Momentum = spec.Momentum,
                StructFactor = spec.StructFactor,
                Major = spec.Major,
                IsHigherTimeframe = orderBlock.IsHigherTimeframe,
                ParentId = spec.ParentId
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