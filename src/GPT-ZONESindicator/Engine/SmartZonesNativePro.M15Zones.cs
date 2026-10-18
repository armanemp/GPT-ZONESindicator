using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private DateTime M15CloseTimeAt(int index)
        {
            return _m15Bars.OpenTimes[index].AddMinutes(15);
        }

        private DateTime ComputeFutureZoneTime()
        {
            if (Bars.Count <= 0)
                return DateTime.UtcNow.Add(TimeSpan.FromMinutes(200));

            int last = Bars.Count - 1;
            long spanTicks = Math.Max(1L, _barSpan.Ticks);
            if (Bars.Count > 40)
            {
                long observedTicks = (Bars.OpenTimes[last] - Bars.OpenTimes[last - 40]).Ticks;
                if (observedTicks > 0)
                    spanTicks = observedTicks;
            }
            else
            {
                try
                {
                    spanTicks = checked(spanTicks * 40L);
                }
                catch (OverflowException)
                {
                    spanTicks = TimeSpan.FromDays(14).Ticks;
                }
            }

            try
            {
                return Bars.OpenTimes[last].AddTicks(spanTicks);
            }
            catch (ArgumentOutOfRangeException)
            {
                return Bars.OpenTimes[last].AddDays(14);
            }
        }

        private void SetupM15Bars()
        {
            if (!ShowM15Zones)
                return;

            try
            {
                _m15Bars = MarketData.GetBars(TimeFrame.Minute15);
                if (_m15Bars != null)
                {
                    _m15Bars.HistoryLoaded += OnM15HistoryChanged;
                    _m15Bars.Reloaded += OnM15HistoryChanged;
                }
            }
            catch (Exception ex)
            {
                _m15Bars = null;
                LogError("GetBars(M15)", ex);
            }
        }

        private void OnM15HistoryChanged(BarsHistoryLoadedEventArgs args)
        {
            if (!_histLoading)
                _rebuildPending = true;
        }

        private int FindM15IndexAtOrBefore(DateTime time)
        {
            if (_m15Bars == null || _m15Bars.Count == 0)
                return 0;

            int low = 0;
            int high = _m15Bars.Count - 1;
            int best = 0;

            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                if (_m15Bars.OpenTimes[middle] <= time)
                {
                    best = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return best;
        }

        private int FindMainIndexAtOrBefore(DateTime time)
        {
            if (Bars.Count == 0)
                return 0;

            int low = 0;
            int high = Bars.Count - 1;
            int best = 0;

            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                if (Bars.OpenTimes[middle] <= time)
                {
                    best = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return best;
        }

        private int FindMainIndexForClosedTime(DateTime closeTime)
        {
            return HigherTimeframeTimingEvaluator.FindClosedIndex(
                Bars.Count,
                closeTime,
                delegate (int index) { return Bars.OpenTimes[index]; });
        }

        private double M15AtrAt(int index)
        {
            if (_m15Bars == null || index < 0)
                return _tick;

            int period = Math.Max(1, AtrPeriod);
            while (_m15Atr.Count <= index)
            {
                int k = _m15Atr.Count;
                double tr = _m15Bars.HighPrices[k] - _m15Bars.LowPrices[k];

                if (k > 0)
                {
                    double previousClose = _m15Bars.ClosePrices[k - 1];
                    tr = Math.Max(tr, Math.Max(
                        Math.Abs(_m15Bars.HighPrices[k] - previousClose),
                        Math.Abs(_m15Bars.LowPrices[k] - previousClose)));
                }

                double atr;
                if (k < period)
                {
                    _m15TrSum += tr;
                    atr = _m15TrSum / (k + 1);
                }
                else
                {
                    atr = (_m15Atr[k - 1] * (period - 1) + tr) / period;
                }

                _m15Atr.Add(atr);
            }

            return Math.Max(_m15Atr[index], _tick);
        }

        /// <summary>
        /// A 15-minute bar is passed to the zone engine only after the next M15 bar has
        /// opened and its close time is no later than the already-closed chart bar.
        /// Therefore the still-forming M15 candle is never used to confirm a zone.
        /// </summary>
        private void ProcessClosedM15Bars(int chartIndex)
        {
            if (!ShowM15Zones || _m15Bars == null || _m15Bars.Count < 4 || chartIndex < 0)
                return;

            DateTime chartCloseTime = CloseTimeOf(chartIndex);
            while (_lastM15Processed + 1 < _m15Bars.Count)
            {
                int m15Index = _lastM15Processed + 1;
                DateTime m15CloseTime = M15CloseTimeAt(m15Index);
                if (!HigherTimeframeTimingEvaluator.IsClosed(m15CloseTime, chartCloseTime))
                    break;

                _lastM15Processed = m15Index;
                int confirmingChartIndex = FindMainIndexForClosedTime(m15CloseTime);
                ProcessM15Bar(m15Index, confirmingChartIndex);
            }
        }

        private void ProcessM15Bar(int index, int confirmingChartIndex)
        {
            double atr = M15AtrAt(index);

            ScanM15Pivots(index, atr);
            CheckM15Breaks(index, confirmingChartIndex, atr);

            if (DetectFvg)
                DetectM15FvgAt(index, confirmingChartIndex, atr);

            if (DetectGap)
                DetectM15GapAt(index, confirmingChartIndex, atr);
        }

        private void ScanM15Pivots(int index, double atr)
        {
            int pivotIndex = index - MajorRight;
            PivotEvaluation evaluation = PivotEvaluator.Evaluate(
                pivotIndex,
                MajorLeft,
                MajorRight,
                index,
                _m15StartIndex,
                delegate (int barIndex) { return _m15Bars.HighPrices[barIndex]; },
                delegate (int barIndex) { return _m15Bars.LowPrices[barIndex]; });

            if (!evaluation.IsReady)
                return;

            if (evaluation.IsHigh)
                AddM15Swing(evaluation.PivotIndex, true, atr);
            if (evaluation.IsLow)
                AddM15Swing(evaluation.PivotIndex, false, atr);
        }

        private void AddM15Swing(int index, bool high, double atr)
        {
            HigherTimeframeSwing opposite = high ? _m15LastLow : _m15LastHigh;
            if (opposite != null)
            {
                double price = high ? _m15Bars.HighPrices[index] : _m15Bars.LowPrices[index];
                double minimum = Math.Max(MajorMinSwingPips * _pip, MajorMinSwingAtr * atr);
                if (minimum > 0 && Math.Abs(price - opposite.Price) < minimum)
                    return;
            }

            HigherTimeframeSwing swing = new HigherTimeframeSwing
            {
                Index = index,
                High = high,
                Price = high ? _m15Bars.HighPrices[index] : _m15Bars.LowPrices[index]
            };

            _m15Swings.Add(swing);
            _m15OpenSwings.Add(swing);
            if (high)
                _m15LastHigh = swing;
            else
                _m15LastLow = swing;
        }

        private void CheckM15Breaks(int index, int confirmingChartIndex, double atr)
        {
            double close = _m15Bars.ClosePrices[index];
            for (int k = 0; k < _m15OpenSwings.Count; k++)
            {
                HigherTimeframeSwing swing = _m15OpenSwings[k];
                if (swing.Broken || !StructureBreakEvaluator.IsLevelCrossed(swing.High, close, swing.Price))
                    continue;

                swing.Broken = true;
                bool latestSwing = swing.High
                    ? swing == _m15LastHigh
                    : swing == _m15LastLow;

                if (!latestSwing)
                    continue;

                bool bullishBreak = swing.High;
                StructureBreakClassification classification =
                    StructureBreakEvaluator.Classify(_m15Trend, bullishBreak);
                _m15Trend = classification.NextTrend;

                if (bullishBreak)
                    _m15LastBullBreak = index;
                else
                    _m15LastBearBreak = index;

                if (DetectOb && OrderBlockRules.IsSourceAllowed(ObSource, true))
                    TryCreateM15OrderBlock(swing.Index, index, confirmingChartIndex, atr, bullishBreak);
            }

            _m15OpenSwings.RemoveAll(delegate (HigherTimeframeSwing swing) { return swing.Broken; });
        }

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

            double breakClose = _m15Bars.ClosePrices[breakIndex];
            double swingPrice = _m15Bars.HighPrices[swingIndex];
            if (!bullish)
                swingPrice = _m15Bars.LowPrices[swingIndex];

            double strength = bullish ? breakClose - swingPrice : swingPrice - breakClose;
            if (strength < ObMinBreakAtr * atr)
                return;

            if (ObRequireDisplacement
                && Math.Abs(_m15Bars.ClosePrices[breakIndex] - _m15Bars.OpenPrices[breakIndex])
                    < ObMinBreakBodyAtr * atr)
                return;

            OrderBlockCandidateSearch search = OrderBlockCandidateEvaluator.Find(
                bullish,
                swingIndex,
                breakIndex,
                delegate (int barIndex) { return _m15Bars.HighPrices[barIndex]; },
                delegate (int barIndex) { return _m15Bars.LowPrices[barIndex]; },
                delegate (int barIndex) { return _m15Bars.OpenPrices[barIndex]; },
                delegate (int barIndex) { return _m15Bars.ClosePrices[barIndex]; });

            int candidate = search.CandidateIndex;
            if (candidate < 0 || candidate - swingIndex > ObMaxBarsFromSwing)
                return;

            OrderBlockBoundsResult bounds = OrderBlockRules.EvaluateBounds(
                _m15Bars.LowPrices[candidate],
                _m15Bars.HighPrices[candidate],
                _m15Bars.OpenPrices[candidate],
                _m15Bars.ClosePrices[candidate],
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
                    ? _m15Bars.HighPrices[k] - bounds.Lower
                    : bounds.Upper - _m15Bars.LowPrices[k];
                if (move > impulse)
                    impulse = move;
            }

            AddM15Zone(new Zone
            {
                Kind = SznpKind.OrderBlock,
                Bull = bullish,
                Lower = bounds.Lower,
                Upper = bounds.Upper,
                OriginIdx = FindMainIndexAtOrBefore(_m15Bars.OpenTimes[candidate]),
                ConfirmIdx = confirmingChartIndex,
                StartTime = _m15Bars.OpenTimes[candidate],
                Atr = atr,
                Momentum = impulse / atr,
                StructFactor = 1.0,
                Major = true,
                IsHigherTimeframe = true
            }, breakIndex, confirmingChartIndex);
        }

        private void DetectM15FvgAt(int index, int confirmingChartIndex, double atr)
        {
            if (index < 2 || confirmingChartIndex < _startIdx)
                return;

            int first = index - 2;
            int middle = index - 1;
            double firstHigh = _m15Bars.HighPrices[first];
            double firstLow = _m15Bars.LowPrices[first];
            double thirdHigh = _m15Bars.HighPrices[index];
            double thirdLow = _m15Bars.LowPrices[index];

            bool bullish = thirdLow > firstHigh;
            bool bearish = thirdHigh < firstLow;
            if (!bullish && !bearish)
                return;

            double lower = bullish ? firstHigh : thirdHigh;
            double upper = bullish ? thirdLow : firstLow;
            double size = upper - lower;
            double minSize = Math.Max(FvgMinSizePips * _pip, FvgMinSizeAtr * atr);
            if (size < _tick * 0.5 || size < minSize)
                return;

            double open = _m15Bars.OpenPrices[middle];
            double close = _m15Bars.ClosePrices[middle];
            double body = Math.Abs(close - open);
            double range = _m15Bars.HighPrices[middle] - _m15Bars.LowPrices[middle];

            if (FvgRequireDisplacement
                && (body < FvgMinBodyAtr * atr
                    || range <= 0
                    || body / range < FvgMinBodyRatio
                    || (FvgMatchDirection && (bullish ? close <= open : close >= open))))
                return;

            DateTime zoneStart = FvgStartMode == SznpStartMode.FirstCandle
                ? _m15Bars.OpenTimes[first]
                : _m15Bars.OpenTimes[index];

            AddM15Zone(new Zone
            {
                Kind = SznpKind.Fvg,
                Bull = bullish,
                Lower = lower,
                Upper = upper,
                OriginIdx = FindMainIndexAtOrBefore(_m15Bars.OpenTimes[first]),
                ConfirmIdx = confirmingChartIndex,
                StartTime = zoneStart,
                Atr = atr,
                Momentum = body / atr,
                StructFactor = M15StructureFactor(index),
                IsHigherTimeframe = true
            }, index, confirmingChartIndex);
        }

        private void DetectM15GapAt(int index, int confirmingChartIndex, double atr)
        {
            if (index < 1 || confirmingChartIndex < _startIdx)
                return;

            double previousClose = _m15Bars.ClosePrices[index - 1];
            double currentOpen = _m15Bars.OpenPrices[index];
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
                    hasOpenClose = false;
            }

            if (wantRange)
            {
                double previousHigh = _m15Bars.HighPrices[index - 1];
                double previousLow = _m15Bars.LowPrices[index];
                if (previousLow > previousHigh)
                {
                    hasRange = true;
                    rangeBull = true;
                    rangeLower = previousHigh;
                    rangeUpper = previousLow;
                }
                else if (_m15Bars.HighPrices[index] < _m15Bars.LowPrices[index - 1])
                {
                    hasRange = true;
                    rangeBull = false;
                    rangeLower = _m15Bars.HighPrices[index];
                    rangeUpper = _m15Bars.LowPrices[index - 1];
                }

                if (hasRange && rangeUpper - rangeLower < minSize)
                    hasRange = false;
            }

            if (hasOpenClose && hasRange)
            {
                if (GapCombine == SznpGapCombine.PreferRangeGap)
                    hasOpenClose = false;
                else if (GapCombine == SznpGapCombine.PreferOpenCloseGap)
                    hasRange = false;
            }

            if (hasOpenClose)
                CreateM15Gap(index, confirmingChartIndex, atr, openCloseBull, openCloseLower, openCloseUpper);
            if (hasRange)
                CreateM15Gap(index, confirmingChartIndex, atr, rangeBull, rangeLower, rangeUpper);
        }

        private void CreateM15Gap(
            int index,
            int confirmingChartIndex,
            double atr,
            bool bullish,
            double lower,
            double upper)
        {
            AddM15Zone(new Zone
            {
                Kind = SznpKind.Gap,
                Bull = bullish,
                Lower = lower,
                Upper = upper,
                OriginIdx = FindMainIndexAtOrBefore(_m15Bars.OpenTimes[index - 1]),
                ConfirmIdx = confirmingChartIndex,
                StartTime = _m15Bars.OpenTimes[index],
                Atr = atr,
                Momentum = (upper - lower) / atr,
                StructFactor = M15StructureFactor(index),
                IsHigherTimeframe = true
            }, index, confirmingChartIndex);
        }

        private void AddM15Zone(Zone zone, int m15Index, int confirmingChartIndex)
        {
            if (!ShowM15Zones || _m15Bars == null
                || confirmingChartIndex < _startIdx
                || m15Index < 0 || m15Index >= _m15Bars.Count)
                return;

            DateTime m15CloseTime = M15CloseTimeAt(m15Index);
            if (m15CloseTime < _m15ZoneStartTime)
                return;

            if (IsDuplicate(zone.Kind, zone.Bull, zone.Lower, zone.Upper, true))
            {
                Reject("M15 zone: duplicate");
                return;
            }

            zone.IsHigherTimeframe = true;
            zone.ConfirmIdx = confirmingChartIndex;
            zone.ConfirmTime = m15CloseTime;
            zone.OriginIdx = Math.Max(0, Math.Min(Bars.Count - 1, zone.OriginIdx));
            AddZone(zone);
        }

        private double M15StructureFactor(int index)
        {
            if (index - _m15LastBullBreak <= 2 || index - _m15LastBearBreak <= 2)
                return 1.0;
            return 0.0;
        }
    }
}