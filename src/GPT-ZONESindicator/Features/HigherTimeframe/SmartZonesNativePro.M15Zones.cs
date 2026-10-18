using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private DateTime M15CloseTimeAt(int index)
        {
            return _higherTimeframeState.Bars.OpenTimes[index].AddMinutes(15);
        }

        private DateTime ComputeFutureZoneTime()
        {
            if (Bars.Count <= 0)
                return DateTime.UtcNow.Add(TimeSpan.FromMinutes(200));

            int last = Bars.Count - 1;
            return FutureBarTimeEvaluator.GetFortyBarEnd(Bars.OpenTimes[last], _barSpan);
        }

        private void SetupM15Bars()
        {
            if (!DetectM15Zones)
                return;

            try
            {
                _higherTimeframeState.Bars = MarketData.GetBars(TimeFrame.Minute15);
                if (_higherTimeframeState.Bars != null)
                {
                    _higherTimeframeState.Bars.HistoryLoaded += OnM15HistoryChanged;
                    _higherTimeframeState.Bars.Reloaded += OnM15HistoryChanged;
                    EnsureM15History();
                }
            }
            catch (Exception ex)
            {
                _higherTimeframeState.Bars = null;
                LogError("GetBars(M15)", ex);
            }
        }

        private void OnM15HistoryChanged(BarsHistoryLoadedEventArgs args)
        {
            if (!_histLoading && !_higherTimeframeState.HistoryLoading)
                _rebuildPending = true;
        }

        private void EnsureM15History()
        {
            if (_higherTimeframeState.Bars == null || _higherTimeframeState.Bars.Count == 0 || Bars.Count == 0 || _higherTimeframeState.HistoryLoading)
                return;

            if (_higherTimeframeState.Bars.OpenTimes[0] <= Bars.OpenTimes[0])
                return;

            _higherTimeframeState.HistoryLoading = true;
            _higherTimeframeState.HistoryRequests = 0;
            RequestMoreM15History();
        }

        private void RequestMoreM15History()
        {
            if (_higherTimeframeState.Bars == null || _higherTimeframeState.Bars.Count == 0
                || _higherTimeframeState.Bars.OpenTimes[0] <= Bars.OpenTimes[0]
                || _higherTimeframeState.HistoryRequests >= 400)
            {
                _higherTimeframeState.HistoryLoading = false;
                _rebuildPending = true;
                return;
            }

            _higherTimeframeState.HistoryRequests++;
            _higherTimeframeState.Bars.LoadMoreHistoryAsync(OnM15HistoryChunkLoaded);
        }

        private void OnM15HistoryChunkLoaded(BarsHistoryLoadedEventArgs args)
        {
            bool canContinue = args != null && args.Count > 0
                && _higherTimeframeState.Bars != null
                && _higherTimeframeState.Bars.Count > 0
                && _higherTimeframeState.Bars.OpenTimes[0] > Bars.OpenTimes[0]
                && _higherTimeframeState.HistoryRequests < 400;

            if (canContinue)
            {
                RequestMoreM15History();
                return;
            }

            _higherTimeframeState.HistoryLoading = false;
            _rebuildPending = true;
        }

        private int FindM15IndexAtOrBefore(DateTime time)
        {
            if (_higherTimeframeState.Bars == null || _higherTimeframeState.Bars.Count == 0)
                return 0;

            int low = 0;
            int high = _higherTimeframeState.Bars.Count - 1;
            int best = 0;

            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                if (_higherTimeframeState.Bars.OpenTimes[middle] <= time)
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
                delegate (int index) { return CloseTimeOf(index); });
        }

        private double M15AtrAt(int index)
        {
            if (_higherTimeframeState.Bars == null || index < 0)
                return _tick;

            int period = Math.Max(1, AtrPeriod);
            while (_higherTimeframeState.Atr.Count <= index)
            {
                int k = _higherTimeframeState.Atr.Count;
                double tr = _higherTimeframeState.Bars.HighPrices[k] - _higherTimeframeState.Bars.LowPrices[k];

                if (k > 0)
                {
                    double previousClose = _higherTimeframeState.Bars.ClosePrices[k - 1];
                    tr = Math.Max(tr, Math.Max(
                        Math.Abs(_higherTimeframeState.Bars.HighPrices[k] - previousClose),
                        Math.Abs(_higherTimeframeState.Bars.LowPrices[k] - previousClose)));
                }

                double atr;
                if (k < period)
                {
                    _higherTimeframeState.TrueRangeSum += tr;
                    atr = _higherTimeframeState.TrueRangeSum / (k + 1);
                }
                else
                {
                    atr = (_higherTimeframeState.Atr[k - 1] * (period - 1) + tr) / period;
                }

                _higherTimeframeState.Atr.Add(atr);
            }

            return Math.Max(_higherTimeframeState.Atr[index], _tick);
        }

        /// <summary>
        /// A 15-minute bar is passed to the zone engine only after the next M15 bar has
        /// opened and its close time is no later than the already-closed chart bar.
        /// Therefore the still-forming M15 candle is never used to confirm a zone.
        /// </summary>
        private void ProcessClosedM15Bars(int chartIndex)
        {
            if (!DetectM15Zones || _higherTimeframeState.Bars == null || _higherTimeframeState.Bars.Count < 4 || chartIndex < 0)
                return;

            DateTime chartCloseTime = CloseTimeOf(chartIndex);
            while (_higherTimeframeState.LastProcessed + 1 < _higherTimeframeState.Bars.Count)
            {
                int m15Index = _higherTimeframeState.LastProcessed + 1;
                DateTime m15CloseTime = M15CloseTimeAt(m15Index);
                if (!HigherTimeframeTimingEvaluator.IsClosed(m15CloseTime, chartCloseTime))
                    break;

                _higherTimeframeState.LastProcessed = m15Index;
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
                _higherTimeframeState.StartIndex,
                delegate (int barIndex) { return _higherTimeframeState.Bars.HighPrices[barIndex]; },
                delegate (int barIndex) { return _higherTimeframeState.Bars.LowPrices[barIndex]; });

            if (!evaluation.IsReady)
                return;

            if (evaluation.IsHigh)
                AddM15Swing(evaluation.PivotIndex, true, atr);
            if (evaluation.IsLow)
                AddM15Swing(evaluation.PivotIndex, false, atr);
        }

        private void AddM15Swing(int index, bool high, double atr)
        {
            HigherTimeframeSwing opposite = high ? _higherTimeframeState.LastLow : _higherTimeframeState.LastHigh;
            if (opposite != null)
            {
                double price = high ? _higherTimeframeState.Bars.HighPrices[index] : _higherTimeframeState.Bars.LowPrices[index];
                double minimum = Math.Max(MajorMinSwingPips * _pip, MajorMinSwingAtr * atr);
                if (minimum > 0 && Math.Abs(price - opposite.Price) < minimum)
                    return;
            }

            HigherTimeframeSwing swing = new HigherTimeframeSwing
            {
                Index = index,
                High = high,
                Price = high ? _higherTimeframeState.Bars.HighPrices[index] : _higherTimeframeState.Bars.LowPrices[index]
            };

            _higherTimeframeState.Swings.Add(swing);
            _higherTimeframeState.OpenSwings.Add(swing);
            if (high)
                _higherTimeframeState.LastHigh = swing;
            else
                _higherTimeframeState.LastLow = swing;
        }

        private void CheckM15Breaks(int index, int confirmingChartIndex, double atr)
        {
            double close = _higherTimeframeState.Bars.ClosePrices[index];
            for (int k = 0; k < _higherTimeframeState.OpenSwings.Count; k++)
            {
                HigherTimeframeSwing swing = _higherTimeframeState.OpenSwings[k];
                if (swing.Broken || !StructureBreakEvaluator.IsLevelCrossed(swing.High, close, swing.Price))
                    continue;

                swing.Broken = true;
                bool latestSwing = swing.High
                    ? swing == _higherTimeframeState.LastHigh
                    : swing == _higherTimeframeState.LastLow;

                if (!latestSwing)
                    continue;

                bool bullishBreak = swing.High;
                StructureBreakClassification classification =
                    StructureBreakEvaluator.Classify(_higherTimeframeState.Trend, bullishBreak);
                _higherTimeframeState.Trend = classification.NextTrend;

                if (bullishBreak)
                    _higherTimeframeState.LastBullBreak = index;
                else
                    _higherTimeframeState.LastBearBreak = index;

                if (DetectOb && OrderBlockRules.IsSourceAllowed(ObSource, true))
                    TryCreateM15OrderBlock(swing.Index, index, confirmingChartIndex, atr, bullishBreak);
            }

            _higherTimeframeState.OpenSwings.RemoveAll(delegate (HigherTimeframeSwing swing) { return swing.Broken; });
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

            double breakClose = _higherTimeframeState.Bars.ClosePrices[breakIndex];
            double swingPrice = _higherTimeframeState.Bars.HighPrices[swingIndex];
            if (!bullish)
                swingPrice = _higherTimeframeState.Bars.LowPrices[swingIndex];

            double strength = bullish ? breakClose - swingPrice : swingPrice - breakClose;
            if (strength < ObMinBreakAtr * atr)
                return;

            if (ObRequireDisplacement
                && Math.Abs(_higherTimeframeState.Bars.ClosePrices[breakIndex] - _higherTimeframeState.Bars.OpenPrices[breakIndex])
                    < ObMinBreakBodyAtr * atr)
                return;

            OrderBlockCandidateSearch search = OrderBlockCandidateEvaluator.Find(
                bullish,
                swingIndex,
                breakIndex,
                delegate (int barIndex) { return _higherTimeframeState.Bars.HighPrices[barIndex]; },
                delegate (int barIndex) { return _higherTimeframeState.Bars.LowPrices[barIndex]; },
                delegate (int barIndex) { return _higherTimeframeState.Bars.OpenPrices[barIndex]; },
                delegate (int barIndex) { return _higherTimeframeState.Bars.ClosePrices[barIndex]; });

            int candidate = search.CandidateIndex;
            if (candidate < 0 || candidate - swingIndex > ObMaxBarsFromSwing)
                return;

            OrderBlockBoundsResult bounds = OrderBlockRules.EvaluateBounds(
                _higherTimeframeState.Bars.LowPrices[candidate],
                _higherTimeframeState.Bars.HighPrices[candidate],
                _higherTimeframeState.Bars.OpenPrices[candidate],
                _higherTimeframeState.Bars.ClosePrices[candidate],
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
                    ? _higherTimeframeState.Bars.HighPrices[k] - bounds.Lower
                    : bounds.Upper - _higherTimeframeState.Bars.LowPrices[k];
                if (move > impulse)
                    impulse = move;
            }

            AddM15Zone(new Zone
            {
                Kind = SznpKind.OrderBlock,
                Bull = bullish,
                Lower = bounds.Lower,
                Upper = bounds.Upper,
                OriginIdx = FindMainIndexAtOrBefore(_higherTimeframeState.Bars.OpenTimes[candidate]),
                ConfirmIdx = confirmingChartIndex,
                StartTime = _higherTimeframeState.Bars.OpenTimes[candidate],
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
            double firstHigh = _higherTimeframeState.Bars.HighPrices[first];
            double firstLow = _higherTimeframeState.Bars.LowPrices[first];
            double thirdHigh = _higherTimeframeState.Bars.HighPrices[index];
            double thirdLow = _higherTimeframeState.Bars.LowPrices[index];

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

            double open = _higherTimeframeState.Bars.OpenPrices[middle];
            double close = _higherTimeframeState.Bars.ClosePrices[middle];
            double body = Math.Abs(close - open);
            double range = _higherTimeframeState.Bars.HighPrices[middle] - _higherTimeframeState.Bars.LowPrices[middle];

            if (FvgRequireDisplacement
                && (body < FvgMinBodyAtr * atr
                    || range <= 0
                    || body / range < FvgMinBodyRatio
                    || (FvgMatchDirection && (bullish ? close <= open : close >= open))))
                return;

            DateTime zoneStart = FvgStartMode == SznpStartMode.FirstCandle
                ? _higherTimeframeState.Bars.OpenTimes[first]
                : _higherTimeframeState.Bars.OpenTimes[index];

            AddM15Zone(new Zone
            {
                Kind = SznpKind.Fvg,
                Bull = bullish,
                Lower = lower,
                Upper = upper,
                OriginIdx = FindMainIndexAtOrBefore(_higherTimeframeState.Bars.OpenTimes[first]),
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

            double previousClose = _higherTimeframeState.Bars.ClosePrices[index - 1];
            double currentOpen = _higherTimeframeState.Bars.OpenPrices[index];
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
                double previousHigh = _higherTimeframeState.Bars.HighPrices[index - 1];
                double previousLow = _higherTimeframeState.Bars.LowPrices[index];
                if (previousLow > previousHigh)
                {
                    hasRange = true;
                    rangeBull = true;
                    rangeLower = previousHigh;
                    rangeUpper = previousLow;
                }
                else if (_higherTimeframeState.Bars.HighPrices[index] < _higherTimeframeState.Bars.LowPrices[index - 1])
                {
                    hasRange = true;
                    rangeBull = false;
                    rangeLower = _higherTimeframeState.Bars.HighPrices[index];
                    rangeUpper = _higherTimeframeState.Bars.LowPrices[index - 1];
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
                OriginIdx = FindMainIndexAtOrBefore(_higherTimeframeState.Bars.OpenTimes[index]),
                ConfirmIdx = confirmingChartIndex,
                StartTime = _higherTimeframeState.Bars.OpenTimes[index],
                Atr = atr,
                Momentum = (upper - lower) / atr,
                StructFactor = M15StructureFactor(index),
                IsHigherTimeframe = true
            }, index, confirmingChartIndex);
        }

        private void AddM15Zone(Zone zone, int m15Index, int confirmingChartIndex)
        {
            if (!DetectM15Zones || _higherTimeframeState.Bars == null
                || confirmingChartIndex < _startIdx
                || m15Index < 0 || m15Index >= _higherTimeframeState.Bars.Count)
                return;

            DateTime m15CloseTime = M15CloseTimeAt(m15Index);
            if (m15CloseTime < _higherTimeframeState.ZoneStartTime)
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

            // M15 order blocks are identified retrospectively after a confirmed major
            // structure break. Replay already-closed M5 candles since their source candle
            // so a zone that has already filled/broken is not incorrectly shown as Fresh.
            // A session gap is born at the M15 open, so replay it from that open; FVG
            // regions begin at the third-candle confirmation and must not inherit earlier touches.
            int replayFrom = -1;
            if (zone.Kind == SznpKind.OrderBlock)
                replayFrom = Math.Max(_startIdx, zone.OriginIdx + 1);
            else if (zone.Kind == SznpKind.Gap)
                replayFrom = Math.Max(_startIdx, FindMainIndexAtOrBefore(zone.StartTime));

            if (replayFrom >= 0 && replayFrom <= confirmingChartIndex)
                ReplayZone(zone, replayFrom, confirmingChartIndex);
        }

        private double M15StructureFactor(int index)
        {
            if (index - _higherTimeframeState.LastBullBreak <= 2 || index - _higherTimeframeState.LastBearBreak <= 2)
                return 1.0;
            return 0.0;
        }
    }
}