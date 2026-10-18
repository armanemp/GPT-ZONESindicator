using System;
using System.Diagnostics;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        protected override void Initialize()
        {
            long t0 = Stopwatch.GetTimestamp();
            string tag = string.IsNullOrWhiteSpace(InstanceTag) ? "SZNP" : InstanceTag.Trim();
            _prefix = tag + "_";
            _pip = Symbol.PipSize > 0 ? Symbol.PipSize : 0.0001;
            _tick = Symbol.TickSize > 0 ? Symbol.TickSize : _pip / 10.0;

            BuildPalette();
            RemoveOwnObjects();
            SetupHtf();
            SetupSessions();

            Bars.HistoryLoaded += OnBarsHistoryLoaded;
            Bars.Reloaded += OnBarsHistoryLoaded;

            ResetState();

            Chart.ScrollChanged += OnChartScrollChanged;
            Chart.ZoomChanged += OnChartZoomChanged;
            Chart.SizeChanged += OnChartSizeChanged;
            Chart.PriceScaleChanged += OnChartPriceScaleChanged;

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
                Chart.PriceScaleChanged -= OnChartPriceScaleChanged;
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
        }

        private void OnBarsHistoryLoaded(BarsHistoryLoadedEventArgs args)
        {
            if (_histLoading)
                return;

            _rebuildPending = true;
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
            _renderDirty = true;

            ComputeBarSpan();
            _futureTime = (n > 0 ? Bars.OpenTimes[n - 1] : DateTime.UtcNow)
                .AddDays(ExtensionDays);
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
                    if (fullRender)
                    {
                        RenderAll();
                    }
                    else
                    {
                        RenderZones();
                        RenderStatus();
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

        private void OnChartPriceScaleChanged(ChartPriceScaleEventArgs args)
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
                RenderZones();
                RenderStatus();
                _renderTicks += Stopwatch.GetTimestamp() - t0;
                _renderCount++;
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

            double atr = AtrAt(i);

            int aliveCount = _alive.Count;
            for (int k = 0; k < aliveCount; k++)
                UpdateZone(_alive[k], i, true);

            _alive.RemoveAll(IsDeadZone);

            ProcessStructure(i, atr);

            if (DetectSweeps)
                ProcessSweeps(i, atr);

            if (DetectFvg)
                DetectFvgAt(i, atr);

            if (DetectGap)
                DetectGapAt(i, atr);

            UpdateReference(i);
        }
    }
}