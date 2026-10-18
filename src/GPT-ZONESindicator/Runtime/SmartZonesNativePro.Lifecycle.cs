using System;
using System.Diagnostics;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void ApplyLineLabelPresentation()
        {
            // Preserve the user's zone detection, scoring, and non-zone overlay choices.
            // Keep zone tags visible; their vertical layout now avoids stacking them together.
            MidlineStyle = LineStyle.Dots;
            StructStyle = LineStyle.Dots;
            InternalStyle = LineStyle.Dots;
            LiquidityStyle = LineStyle.Dots;
            RefStyle = LineStyle.Dots;
            SessionStyle = LineStyle.Dots;
            RoundStyle = LineStyle.Dots;
        }

        protected override void Initialize()
        {
            long t0 = Stopwatch.GetTimestamp();
            _highPriceAt = H;
            _lowPriceAt = L;
            _openPriceAt = O;
            _closePriceAt = C;
            ApplyLineLabelPresentation();
            string tag = string.IsNullOrWhiteSpace(InstanceTag) ? "SZNP" : InstanceTag.Trim();
            _prefix = tag + "_";
            _pip = Symbol.PipSize > 0 ? Symbol.PipSize : 0.0001;
            _tick = Symbol.TickSize > 0 ? Symbol.TickSize : _pip / 10.0;

            BuildPalette();
            SetupM15Bars();
            RemoveOwnObjects();
            SetupHtf();
            SetupSessions();
            ConfigureFeaturePipeline();

            Bars.HistoryLoaded += OnBarsHistoryLoaded;
            Bars.Reloaded += OnBarsHistoryLoaded;

            ResetState();

            Chart.ScrollChanged += OnChartScrollChanged;
            Chart.ZoomChanged += OnChartZoomChanged;
            Chart.SizeChanged += OnChartSizeChanged;

            if (LoadHistoryToLookback && LookbackBars > 0 && Bars.Count < LookbackBars + 50)
            {
                _histLoading = true;
                _histRequests = 0;
                Bars.LoadMoreHistoryAsync(OnHistoryChunkLoaded);
            }

            _initTicks = Stopwatch.GetTimestamp() - t0;
        }

        protected override void OnDestroy()
        {
            try
            {
                Bars.HistoryLoaded -= OnBarsHistoryLoaded;
                Bars.Reloaded -= OnBarsHistoryLoaded;
                Chart.ScrollChanged -= OnChartScrollChanged;
                Chart.ZoomChanged -= OnChartZoomChanged;
                Chart.SizeChanged -= OnChartSizeChanged;
                if (_m15Bars != null)
                {
                    _m15Bars.HistoryLoaded -= OnM15HistoryChanged;
                    _m15Bars.Reloaded -= OnM15HistoryChanged;
                }
                RemoveOwnObjects();
            }
            catch (Exception)
            {
                // A chart can be tearing down while the indicator is being removed.
            }

            base.OnDestroy();
        }

        private void OnHistoryChunkLoaded(BarsHistoryLoadedEventArgs args)
        {
            _histRequests++;
            bool more = args != null && args.Count > 0
                && Bars.Count < LookbackBars + 50
                && _histRequests < 400;

            if (more)
            {
                Bars.LoadMoreHistoryAsync(OnHistoryChunkLoaded);
                return;
            }

            _histLoading = false;
            _rebuildPending = true;
            EnsureM15History();
        }

        private void OnBarsHistoryLoaded(BarsHistoryLoadedEventArgs args)
        {
            if (_histLoading)
                return;

            _rebuildPending = true;
            EnsureM15History();
        }

        private void RebuildAll()
        {
            _rebuildPending = false;
            RemoveOwnObjects();
            ResetState();
            Log("Rebuild: history changed, bars=" + Bars.Count);
        }

        private void ResetState()
        {
            _zones.Clear();
            _alive.Clear();
            ResetZoneLifecycleIndexes();
            _renderQueryScratch.Clear();
            _renderTracked.Clear();
            _dupe.Clear();
            _obByCandle.Clear();
            _eqs.Clear();
            _eqOpen.Clear();
            _sweeps.Clear();
            _refItems.Clear();
            _round.Clear();
            _roundCenter = long.MinValue;
            _status = null;
            _atr.Clear();
            _trSum = 0;
            _m15Atr.Clear();
            _m15TrSum = 0;
            _m15Swings.Clear();
            _m15OpenSwings.Clear();
            _m15LastHigh = null;
            _m15LastLow = null;
            _m15Trend = 0;
            _m15LastBullBreak = -1;
            _m15LastBearBreak = -1;
            _rej.Clear();

            _nextId = 1;
            _nameSeq = 0;
            _hiddenByBudget = 0;
            _shownZones = 0;
            _procTicks = 0;
            _procBars = 0;
            _renderTicks = 0;
            _renderCount = 0;
            _errorCount = 0;

            _engMajor = new Engine
            {
                Major = true,
                Left = MajorLeft,
                Right = MajorRight,
                MinAtr = MajorMinSwingAtr,
                MinPips = MajorMinSwingPips
            };
            _engInt = new Engine
            {
                Major = false,
                Left = InternalLeft,
                Right = InternalRight,
                MinAtr = InternalMinSwingAtr,
                MinPips = InternalMinSwingPips
            };

            int never = int.MinValue / 2;
            _lastBullBreak = never;
            _lastBearBreak = never;
            _lastSweepBull = never;
            _lastSweepBear = never;

            foreach (HtfTrack tr in _htf)
            {
                tr.Cur = -1;
                tr.Current = null;
                tr.Groups.Clear();
            }

            foreach (SessionTrack session in _sess)
            {
                session.WinStart = DateTime.MinValue;
                session.HiItem = null;
                session.LoItem = null;
                session.Past.Clear();
            }

            int n = Bars.Count;
            _firstOpenTime = n > 0 ? Bars.OpenTimes[0] : DateTime.MinValue;
            _startIdx = LookbackBars > 0 ? Math.Max(2, n - 1 - LookbackBars) : 2;
            _lastProcessed = _startIdx - 1;
            _m15ZoneStartTime = n > 0 ? Bars.OpenTimes[Math.Min(_startIdx, n - 1)] : DateTime.MinValue;
            if (_m15Bars != null && _m15Bars.Count > 0)
            {
                int htfAtLookback = FindM15IndexAtOrBefore(_m15ZoneStartTime);
                int warmupBars = Math.Max(100, Math.Max(AtrPeriod * 5, MajorLeft + MajorRight + ObMaxBarsFromSwing));
                _m15StartIndex = Math.Max(2, htfAtLookback - warmupBars);
            }
            else
            {
                _m15StartIndex = 0;
            }
            _lastM15Processed = _m15StartIndex - 1;
            _renderDirty = true;
            _viewportDirty = true;

            ComputeBarSpan();
            _futureTime = ComputeFutureZoneTime();

            if (n > 0)
            {
                Log("Forty-bar projection: bar interval=" + _barSpan
                    + ", latest candle open=" + Bars.OpenTimes[n - 1].ToString("yyyy-MM-dd HH:mm:ss")
                    + ", drawing endpoint=" + _futureTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        private void ComputeBarSpan()
        {
            int n = Bars.Count;
            TimeSpan best = TimeSpan.MaxValue;

            for (int k = Math.Max(1, n - 60); k < n; k++)
            {
                TimeSpan delta = Bars.OpenTimes[k] - Bars.OpenTimes[k - 1];
                if (delta > TimeSpan.Zero && delta < best)
                    best = delta;
            }

            _barSpan = best == TimeSpan.MaxValue ? TimeSpan.FromMinutes(5) : best;
        }

        public override void Calculate(int index)
        {
            try
            {
                if (Bars.Count < 5)
                    return;

                if (_rebuildPending || Bars.OpenTimes[0] != _firstOpenTime)
                    RebuildAll();

                int closed = index - 1;
                if (closed > _lastProcessed)
                {
                    long t0 = Stopwatch.GetTimestamp();

                    for (int i = _lastProcessed + 1; i <= closed; i++)
                    {
                        try
                        {
                            ProcessBar(i);
                        }
                        catch (Exception ex)
                        {
                            LogError("ProcessBar(" + i + ")", ex);
                        }
                    }

                    _procTicks += Stopwatch.GetTimestamp() - t0;
                    _procBars += closed - _lastProcessed;
                    _lastProcessed = closed;

                    if (closed >= 0)
                    {
                        _lastClose = C(closed);
                        _lastBarTime = T(closed);
                    }

                    _renderDirty = true;
                }

                if (index == Bars.Count - 1
                    && (_renderDirty || _viewportDirty)
                    && _lastProcessed >= _startIdx)
                {
                    long t1 = Stopwatch.GetTimestamp();
                    bool fullRender = _renderDirty;
                    UpdateItemViewport();
                    if (fullRender)
                    {
                        RenderAll();
                    }
                    else if (_featurePipeline != null)
                    {
                        _featurePipeline.Render(false);
                    }

                    _renderTicks += Stopwatch.GetTimestamp() - t1;
                    _renderCount++;
                    _renderDirty = false;
                    _viewportDirty = false;

                    if (_renderCount == 1)
                        PrintSummary();
                }
            }
            catch (Exception ex)
            {
                LogError("Calculate(" + index + ")", ex);
            }
        }

        private void OnChartScrollChanged(ChartScrollEventArgs args)
        {
            RefreshZoneViewport();
        }

        private void OnChartZoomChanged(ChartZoomEventArgs args)
        {
            RefreshZoneViewport();
        }

        private void OnChartSizeChanged(ChartSizeEventArgs args)
        {
            RefreshZoneViewport();
        }

        private void RefreshZoneViewport()
        {
            if (_histLoading || _rebuildPending || _lastProcessed < 0 || _lastProcessed < _startIdx)
            {
                _viewportDirty = true;
                return;
            }

            long t0 = Stopwatch.GetTimestamp();
            try
            {
                UpdateItemViewport();
                if (_featurePipeline != null)
                    _featurePipeline.Render(false);
                _renderTicks += Stopwatch.GetTimestamp() - t0;
                _viewportDirty = false;
            }
            catch (Exception ex)
            {
                _viewportDirty = true;
                LogError("RefreshZoneViewport", ex);
            }
        }

        /// <summary>Deterministic closed-bar processing pipeline; chart drawing is deferred to RenderAll.</summary>
        private void ProcessBar(int i)
        {
            if (i < _startIdx || i < 2)
                return;

            // Keep every still-valid zone (and its midline) extended past the latest
            // closed bar, even when the user configured a short future extension.
            _futureTime = ComputeFutureZoneTime();

            double atr = AtrAt(i);

            // Each feature is processed independently by the module pipeline. An exception
            // in one detector no longer skips zone lifecycle, HTF, references or other modules.
            if (_featurePipeline != null)
                _featurePipeline.ProcessBar(i, atr);
        }
    }
}