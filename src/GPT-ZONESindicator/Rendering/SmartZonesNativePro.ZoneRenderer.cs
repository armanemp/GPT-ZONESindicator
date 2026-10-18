using System;
using System.Globalization;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void RenderAll()
        {
            UpdateItemViewport();
            if (_featurePipeline != null)
                _featurePipeline.Render(true);
        }

        private static int CompareScoreDesc(Zone a, Zone b)
        {
            int comparison = b.Score.CompareTo(a.Score);
            return comparison != 0 ? comparison : b.ConfirmIdx.CompareTo(a.ConfirmIdx);
        }

        private bool ZoneWanted(Zone zone, double price, double atrNow)
        {
            if (zone.IsHigherTimeframe && !ShowM15Zones)
                return false;

            switch (zone.Kind)
            {
                case SznpKind.Fvg:
                    if (!ShowFvg) return false;
                    break;
                case SznpKind.Gap:
                    if (!ShowGap) return false;
                    break;
                case SznpKind.OrderBlock:
                    if (!ShowOb) return false;
                    break;
                default:
                    if (!ShowBreaker) return false;
                    break;
            }

            switch (zone.State)
            {
                case SznpState.Fresh:
                    break;
                case SznpState.Touched:
                case SznpState.Partial:
                    if (!ShowTouchedZones) return false;
                    break;
                case SznpState.Filled:
                    if (!ShowFilledZones) return false;
                    break;
                default:
                    bool breakerHistory = zone.Kind == SznpKind.OrderBlock
                        && zone.ChildId > 0
                        && ShowObHistoryOfBreakers
                        && ShowBreaker;
                    if (!ShowBrokenZones && !breakerHistory)
                        return false;
                    break;
            }

            if (DisplayMode == SznpDisplayMode.FreshOnly && zone.State != SznpState.Fresh)
                return false;

            if (DisplayMode == SznpDisplayMode.NearPrice && zone.State != SznpState.Broken)
            {
                double distance = price > zone.Upper
                    ? price - zone.Upper
                    : (price < zone.Lower ? zone.Lower - price : 0.0);

                if (distance > NearPriceAtr * atrNow)
                    return false;
            }

            return true;
        }

        private DateTime ZoneEndTime(Zone zone)
        {
            // Valid active zones project forty bars right. Terminal history ends at the
            // actual fill/break event so old invalid zones do not create long extra lines.
            DateTime end = zone.State == SznpState.Broken
                ? zone.BreakTime
                : (zone.State == SznpState.Filled ? zone.FillTime : _futureTime);
            return end <= zone.StartTime ? zone.StartTime.Add(_barSpan) : end;
        }

        /// <summary>
        /// Reads a buffered chart viewport. Logical zones remain in history; only zones intersecting
        /// the visible time/price range (plus a small cache margin) need chart objects.
        /// </summary>
        private bool TryGetZoneViewport(
            out DateTime timeStart,
            out DateTime timeEnd,
            out double priceTop,
            out double priceBottom)
        {
            timeStart = DateTime.MinValue;
            timeEnd = DateTime.MaxValue;
            priceTop = double.MaxValue;
            priceBottom = double.MinValue;

            int count = Bars.Count;
            if (count <= 0 || Chart.Width <= 0 || Chart.Height <= 0)
                return false;

            int first = Chart.FirstVisibleBarIndex;
            int last = Chart.LastVisibleBarIndex;
            if (first < 0 || last < first || first >= count)
                return false;

            if (last >= count)
                last = count - 1;

            int visibleBars = last - first + 1;
            int bufferBars = Math.Max(5, (int)Math.Ceiling(visibleBars * 0.10));
            first = Math.Max(0, first - bufferBars);
            last = Math.Min(count - 1, last + bufferBars);

            double top = Chart.TopY;
            double bottom = Chart.BottomY;
            if (double.IsNaN(top) || double.IsInfinity(top)
                || double.IsNaN(bottom) || double.IsInfinity(bottom)
                || top <= bottom)
                return false;

            double priceBuffer = (top - bottom) * 0.10;
            timeStart = Bars.OpenTimes[first];
            timeEnd = Bars.OpenTimes[last];
            priceTop = top + priceBuffer;
            priceBottom = bottom - priceBuffer;
            return true;
        }

        private bool ZoneIntersectsViewport(
            Zone zone,
            DateTime timeStart,
            DateTime timeEnd,
            double priceTop,
            double priceBottom)
        {
            if (zone.Upper < priceBottom || zone.Lower > priceTop)
                return false;

            DateTime end = ZoneEndTime(zone);
            return zone.StartTime <= timeEnd && end >= timeStart;
        }

        private bool TryAddRenderCandidate(
            Zone zone,
            double price,
            double atrNow,
            int nowIndex,
            bool hasViewport,
            bool globalSelection,
            DateTime viewStart,
            DateTime viewEnd,
            double viewTop,
            double viewBottom,
            bool needScore)
        {
            zone.Want = ZoneWanted(zone, price, atrNow);
            if (!zone.Want)
                return false;

            if (hasViewport && !globalSelection
                && !ZoneIntersectsViewport(zone, viewStart, viewEnd, viewTop, viewBottom))
            {
                zone.Want = false;
                return false;
            }

            if (needScore && zone.State != SznpState.Broken)
                zone.Score = ComputeScore(zone, nowIndex, price, atrNow);

            if (MinScore > 0 && zone.Score < MinScore)
            {
                zone.Want = false;
                return false;
            }

            _cand.Add(zone);
            return true;
        }

        private void RenderZones(bool refreshMetadata)
        {
            int nowIndex = _lastProcessed;
            double price = _lastClose;
            double atrNow = AtrAt(nowIndex);

            DateTime viewStart, viewEnd;
            double viewTop, viewBottom;
            bool hasViewport = TryGetZoneViewport(
                out viewStart, out viewEnd, out viewTop, out viewBottom);

            // Global modes must inspect the complete logical history to preserve their ranking semantics.
            bool globalSelection = DisplayMode == SznpDisplayMode.TopRanked
                || MaxZonesPerKind > 0;

            // In the usual viewport/all-zones mode, use the lifecycle interval index to
            // find zones intersecting visible prices instead of scanning all historical zones.
            // Explicit terminal-history display falls back to the complete collection so those
            // opt-in history records remain available.
            bool indexedViewportSelection = ZoneViewportSelectionPolicy.UseActiveZoneIndex(
                hasViewport,
                globalSelection,
                ShowFilledZones,
                ShowBrokenZones,
                ShowObHistoryOfBreakers);

            bool needScore = MinScore > 0
                || DisplayMode == SznpDisplayMode.TopRanked
                || MaxZonesPerKind > 0
                || MaxChartObjects > 0
                || OverlapDeclutter
                || ShowZoneLabels;

            // Retire the previous viewport selection first. The tracked set contains only
            // zones that were wanted/shown last time, not every historical record.
            for (int k = 0; k < _renderTracked.Count; k++)
                _renderTracked[k].Want = false;

            _cand.Clear();
            if (indexedViewportSelection)
            {
                _renderQueryScratch.Clear();
                _zoneIntervalIndex.Query(viewBottom, viewTop, _renderQueryScratch);

                for (int k = 0; k < _renderQueryScratch.Count; k++)
                {
                    TryAddRenderCandidate(
                        _renderQueryScratch[k], price, atrNow, nowIndex,
                        hasViewport, globalSelection, viewStart, viewEnd,
                        viewTop, viewBottom, needScore);
                }
            }
            else
            {
                for (int k = 0; k < _zones.Count; k++)
                {
                    TryAddRenderCandidate(
                        _zones[k], price, atrNow, nowIndex,
                        hasViewport, globalSelection, viewStart, viewEnd,
                        viewTop, viewBottom, needScore);
                }
            }

            int perKind = MaxZonesPerKind > 0 ? MaxZonesPerKind : int.MaxValue;
            if (DisplayMode == SznpDisplayMode.TopRanked)
                perKind = Math.Min(perKind, TopRankedCount);

            if (perKind != int.MaxValue)
                LimitPerKind(perKind);

            // Global ranking is preserved, but only viewport-intersecting rectangles are instantiated.
            if (hasViewport && globalSelection)
            {
                for (int k = 0; k < _cand.Count; k++)
                {
                    Zone zone = _cand[k];
                    if (zone.Want && !ZoneIntersectsViewport(
                        zone, viewStart, viewEnd, viewTop, viewBottom))
                        zone.Want = false;
                }
            }

            if (OverlapDeclutter)
                Declutter();

            // Render budget only: keep every detected zone in the logical state/history.
            // Lower-ranked zones are hidden as chart objects, never removed from _zones/_alive.
            _hiddenByBudget = 0;
            if (MaxChartObjects > 0)
            {
                _tmp.Clear();
                for (int k = 0; k < _cand.Count; k++)
                {
                    if (_cand[k].Want)
                        _tmp.Add(_cand[k]);
                }

                if (_tmp.Count > MaxChartObjects)
                {
                    _tmp.Sort(CompareScoreDesc);
                    for (int k = MaxChartObjects; k < _tmp.Count; k++)
                        _tmp[k].Want = false;

                    _hiddenByBudget = _tmp.Count - MaxChartObjects;
                }
            }

            // Track only the previous and current viewport candidates. This avoids
            // synchronizing thousands of off-screen historical records on every new bar.
            for (int k = 0; k < _cand.Count; k++)
            {
                Zone zone = _cand[k];
                if ((zone.Want || zone.Shown) && !zone.RenderTracked)
                {
                    zone.RenderTracked = true;
                    _renderTracked.Add(zone);
                }
            }

            int shown = 0;
            int write = 0;
            int trackedCount = _renderTracked.Count;
            // Keep older visible zones first so their label positions are reserved before
            // newer overlapping M5/M15 zones are laid out.
            _renderTracked.Sort(delegate (Zone a, Zone b) { return a.Id.CompareTo(b.Id); });

            PrepareZoneLabelRows();

            for (int k = 0; k < trackedCount; k++)
            {
                Zone zone = _renderTracked[k];
                SyncZone(zone, refreshMetadata);

                if (zone.Shown)
                    shown++;

                if (zone.Want || zone.Shown)
                {
                    _renderTracked[write++] = zone;
                }
                else
                {
                    zone.RenderTracked = false;
                }
            }

            if (write < _renderTracked.Count)
                _renderTracked.RemoveRange(write, _renderTracked.Count - write);

            _shownZones = shown;
        }

        private void LimitPerKind(int limit)
        {
            for (int kind = 0; kind < 4; kind++)
            {
                _tmp.Clear();
                for (int k = 0; k < _cand.Count; k++)
                {
                    Zone zone = _cand[k];
                    if (zone.Want && (int)zone.Kind == kind)
                        _tmp.Add(zone);
                }

                if (_tmp.Count <= limit)
                    continue;

                _tmp.Sort(CompareScoreDesc);
                for (int k = limit; k < _tmp.Count; k++)
                    _tmp[k].Want = false;
            }
        }

        /// <summary>Optionally hide lower-ranked zones of different kinds that overlap materially.</summary>
        private void Declutter()
        {
            _tmp.Clear();
            for (int k = 0; k < _cand.Count; k++)
            {
                if (_cand[k].Want)
                    _tmp.Add(_cand[k]);
            }

            _tmp.Sort(CompareScoreDesc);
            _acc.Clear();
            double threshold = OverlapMinPct / 100.0;

            for (int k = 0; k < _tmp.Count; k++)
            {
                Zone zone = _tmp[k];
                bool drop = false;

                for (int j = 0; j < _acc.Count && !drop; j++)
                {
                    Zone accepted = _acc[j];
                    if (accepted.Kind == zone.Kind || accepted.Bull != zone.Bull)
                        continue;

                    double overlap = Math.Min(accepted.Upper, zone.Upper)
                        - Math.Max(accepted.Lower, zone.Lower);
                    if (overlap <= 0)
                        continue;

                    double smaller = Math.Min(
                        accepted.Upper - accepted.Lower,
                        zone.Upper - zone.Lower);

                    if (smaller <= 0 || overlap / smaller < threshold)
                        continue;

                    if (ZoneEndTime(accepted) <= zone.StartTime || ZoneEndTime(zone) <= accepted.StartTime)
                        continue;

                    drop = true;
                }

                if (drop)
                    zone.Want = false;
                else
                    _acc.Add(zone);
            }
        }

        private Color ZoneColor(Zone zone)
        {
            // Breakers use a slightly stronger fill so they remain distinguishable
            // without reducing visibility of the underlying candles.
            double baseOpacity = zone.Kind == SznpKind.Breaker
                ? Math.Min(100.0, ZoneOpacity * 1.35)
                : ZoneOpacity;

            switch (zone.State)
            {
                case SznpState.Filled:
                    return WithOpacity(_cFilled, BrokenOpacity);
                case SznpState.Broken:
                    return WithOpacity(_cBroken, BrokenOpacity);
                case SznpState.Touched:
                case SznpState.Partial:
                    return WithOpacity(
                        _zoneBase[ZoneIdx(zone.Kind, zone.Bull)],
                        baseOpacity * TouchedOpacityPct / 100.0);
                default:
                    return WithOpacity(_zoneBase[ZoneIdx(zone.Kind, zone.Bull)], baseOpacity);
            }
        }

        private Color ZoneMidlineColor(Zone zone)
        {
            // Midline RGB always matches the zone exactly; only alpha is independently
            // configurable so the same hue remains legible over candle bodies.
            Color source = ZoneColor(zone);
            ZoneMidlineRgb matching = ZoneMidlineColorEvaluator.Match(
                source.R, source.G, source.B);
            byte alpha = (byte)Math.Max(0, Math.Min(255, Math.Round(MidlineOpacity * 2.55)));
            return Color.FromArgb(alpha, matching.R, matching.G, matching.B);
        }

        private static string KindTag(Zone zone)
        {
            string tag;
            switch (zone.Kind)
            {
                case SznpKind.Fvg: tag = "FVG"; break;
                case SznpKind.Gap: tag = "GAP"; break;
                case SznpKind.OrderBlock: tag = zone.Major ? "OB*" : "OB"; break;
                default: tag = "BB"; break;
            }

            return (zone.IsHigherTimeframe ? "M15 " : "") + tag + (zone.Bull ? "+" : "-");
        }

        private string LabelText(Zone zone)
        {
            return zone.Score > 0
                ? KindTag(zone) + " " + ((int)Math.Round(zone.Score)).ToString(CultureInfo.InvariantCulture)
                : KindTag(zone);
        }

        /// <summary>
        /// Places zone labels just off the midpoint and vertically separates labels that
        /// would otherwise overlap at the common forty-bar right edge.
        /// </summary>
        private void PrepareZoneLabelRows()
        {
            _zoneLabelRows.Clear();
            long tolerance = Math.Max(1L, _barSpan.Ticks) * 2L;

            // Reserve positions used by other feature labels near zone right edges.
            // Zone labels are intentionally skipped: this pass recomputes all of them in order.
            foreach (ChartObject chartObject in _reg.Values)
            {
                ChartText text = chartObject as ChartText;
                if (text == null || !text.IsAlive
                    || text.Name.StartsWith(_prefix + "Z", StringComparison.Ordinal))
                    continue;

                _zoneLabelRows.Add(new ZoneLabelSlot { Time = text.Time, Y = text.Y });
            }
        }

        private bool ZoneLabelRowCollides(DateTime time, double price, double separation, long tolerance)
        {
            for (int i = 0; i < _zoneLabelRows.Count; i++)
            {
                ZoneLabelSlot row = _zoneLabelRows[i];
                if (Math.Abs((row.Time - time).Ticks) <= tolerance
                    && Math.Abs(row.Y - price) < separation)
                    return true;
            }

            return false;
        }

        private void ReserveZoneLabelRow(DateTime time, double price)
        {
            _zoneLabelRows.Add(new ZoneLabelSlot { Time = time, Y = price });
        }

        /// <summary>
        /// Places zone labels just off the midpoint and vertically separates labels that
        /// would otherwise overlap at the common active-zone right edge.
        /// </summary>
        private double ComputeZoneLabelY(Zone zone, DateTime end)
        {
            double middle = (zone.Upper + zone.Lower) * 0.5;
            if (!ShowZoneLabels)
                return middle;

            double separation = Math.Max(_tick * 3.0, (zone.Upper - zone.Lower) * 0.10);
            double top = Chart.TopY;
            double bottom = Chart.BottomY;
            double height = Chart.Height;

            if (height > 0 && top > bottom
                && !double.IsNaN(top) && !double.IsInfinity(top)
                && !double.IsNaN(bottom) && !double.IsInfinity(bottom))
            {
                double labelPixels = Math.Max(14, LabelFontSize + 6);
                separation = Math.Max(separation, (top - bottom) * labelPixels / height);
            }

            long tolerance = Math.Max(1L, _barSpan.Ticks) * 2L;
            double preferred = zone.Bull ? 1.0 : -1.0;
            int attempts = Math.Min(48, Math.Max(8, _renderTracked.Count * 2 + 2));

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int level = attempt / 2 + 1;
                double direction = attempt % 2 == 0 ? preferred : -preferred;
                double candidate = middle + direction * level * separation;

                if (ZoneLabelRowCollides(end, candidate, separation, tolerance))
                    continue;

                ReserveZoneLabelRow(end, candidate);
                return candidate;
            }

            // In an extremely dense price band, prefer a visible offset over stacking labels.
            double fallback = middle + preferred * attempts * separation;
            ReserveZoneLabelRow(end, fallback);
            return fallback;
        }

        private string ZoneComment(Zone zone)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} | {1} | {2:0.#####} - {3:0.#####} | touches {4} | {5}",
                KindTag(zone), zone.State, zone.Lower, zone.Upper, zone.TouchCount, ScoreText(zone));
        }

        private void SyncZone(Zone zone, bool refreshMetadata)
        {
            if (!zone.Want)
            {
                if (zone.Shown)
                    RemoveZoneObjects(zone);
                return;
            }

            DateTime start = zone.StartTime;
            DateTime end = ZoneEndTime(zone);
            double labelY = ComputeZoneLabelY(zone, end);
            Color color = ZoneColor(zone);

            bool missingRectangle = zone.Rect == null || !zone.Rect.IsAlive;
            bool missingMidline = ShowMidline && (zone.Mid == null || !zone.Mid.IsAlive);
            bool missingLabel = ShowZoneLabels && (zone.Lbl == null || !zone.Lbl.IsAlive);

            // Reconcile the entire zone view if any enabled child object was removed.
            if (zone.Shown && (missingRectangle || missingMidline || missingLabel))
                RemoveZoneObjects(zone);

            bool createView = !zone.Shown;
            string comment = refreshMetadata || createView
                ? ZoneComment(zone)
                : zone.AppComment;
            string labelText = !ShowZoneLabels
                ? null
                : (refreshMetadata || createView ? LabelText(zone) : zone.AppLabelText);

            if (!zone.Shown)
            {
                string name = _prefix + "Z" + zone.Id.ToString(CultureInfo.InvariantCulture);
                int borderThickness = zone.IsHigherTimeframe
                    ? Math.Max(1, ZoneBorderThickness)
                    : ZoneBorderThickness;
                LineStyle borderStyle = zone.IsHigherTimeframe
                    ? LineStyle.Dots
                    : LineStyle.Solid;
                ChartRectangle rectangle = Chart.DrawRectangle(
                    name, start, zone.Upper, end, zone.Lower, color, borderThickness, borderStyle);

                rectangle.IsFilled = true;
                rectangle.IsInteractive = ObjectsInteractive;
                rectangle.ZIndex = ZoneZIndex;
                rectangle.Comment = comment;
                zone.Rect = rectangle;
                Reg(rectangle);

                if (ShowMidline)
                {
                    double middle = (zone.Upper + zone.Lower) * 0.5;
                    ChartTrendLine midline = Chart.DrawTrendLine(
                        name + "_m", start, middle, end, middle,
                        ZoneMidlineColor(zone), MidlineThickness, MidlineStyle);
                    midline.ExtendToInfinity = false;
                    midline.IsInteractive = ObjectsInteractive;
                    midline.ZIndex = ZoneZIndex + 1;
                    zone.Mid = midline;
                    Reg(midline);
                }

                if (ShowZoneLabels)
                {
                    ChartText label = Chart.DrawText(
                        name + "_t",
                        labelText,
                        end,
                        labelY,
                        WithOpacity(color, 100));
                    label.FontSize = Math.Max(11, LabelFontSize + 2);
                    label.FontFamily = "Segoe UI Light";
                    label.IsBold = false;
                    label.HorizontalAlignment = HorizontalAlignment.Left;
                    label.VerticalAlignment = VerticalAlignment.Center;
                    label.IsInteractive = ObjectsInteractive;
                    zone.Lbl = label;
                    Reg(label);
                }

                zone.Shown = true;
                zone.AppT2 = end;
                zone.AppLabelY = labelY;
                zone.AppCol = color;
                zone.AppComment = comment;
                zone.AppLabelText = labelText;
                return;
            }

            bool timeChanged = zone.AppT2 != end;
            bool colorChanged = !SameColor(zone.AppCol, color);
            bool commentChanged = !string.Equals(zone.AppComment, comment, StringComparison.Ordinal);
            bool labelChanged = ShowZoneLabels
                && !string.Equals(zone.AppLabelText, labelText, StringComparison.Ordinal);

            if (timeChanged)
            {
                zone.Rect.Time2 = end;
                if (zone.Mid != null && zone.Mid.IsAlive)
                    zone.Mid.Time2 = end;
                if (zone.Lbl != null && zone.Lbl.IsAlive)
                {
                    zone.Lbl.Time = end;
                    zone.Lbl.Y = labelY;
                    zone.AppLabelY = labelY;
                }
                zone.AppT2 = end;
            }

            if (colorChanged)
            {
                zone.Rect.Color = color;
                zone.AppCol = color;

                if (zone.Mid != null && zone.Mid.IsAlive)
                    zone.Mid.Color = ZoneMidlineColor(zone);
                if (zone.Lbl != null && zone.Lbl.IsAlive)
                    zone.Lbl.Color = WithOpacity(color, 100);
            }

            if (ShowZoneLabels && Math.Abs(zone.AppLabelY - labelY) > Math.Max(1e-12, _tick * 0.1))
            {
                if (zone.Lbl != null && zone.Lbl.IsAlive)
                    zone.Lbl.Y = labelY;
                zone.AppLabelY = labelY;
            }

            if (commentChanged)
            {
                zone.Rect.Comment = comment;
                zone.AppComment = comment;
            }

            if (labelChanged && zone.Lbl != null && zone.Lbl.IsAlive)
            {
                zone.Lbl.Text = labelText;
                zone.AppLabelText = labelText;
            }
        }

        private void RemoveZoneObjects(Zone zone)
        {
            string name = _prefix + "Z" + zone.Id.ToString(CultureInfo.InvariantCulture);
            KillObject(zone.Rect, name);
            if (zone.Mid != null)
                KillObject(zone.Mid, name + "_m");
            if (zone.Lbl != null)
                KillObject(zone.Lbl, name + "_t");

            zone.Rect = null;
            zone.Mid = null;
            zone.Lbl = null;
            zone.AppComment = null;
            zone.AppLabelText = null;
            zone.AppLabelY = 0;
            zone.Shown = false;
        }
    }
}