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

        private ZoneRenderState GetZoneRenderState(Zone zone)
        {
            ZoneRenderState view;
            if (!_zoneRenderingState.States.TryGetValue(zone.Id, out view))
            {
                view = new ZoneRenderState();
                _zoneRenderingState.States.Add(zone.Id, view);
            }

            return view;
        }

        private int CompareScoreDesc(Zone a, Zone b)
        {
            int comparison = GetZoneRenderState(b).Score.CompareTo(GetZoneRenderState(a).Score);
            return comparison != 0 ? comparison : b.ConfirmIdx.CompareTo(a.ConfirmIdx);
        }

        private bool IsBreakerParentHistory(Zone zone)
        {
            if (zone == null
                || !ShowObHistoryOfBreakers
                || zone.Kind != SznpKind.OrderBlock
                || zone.State != SznpState.Broken
                || zone.ChildId <= 0
                || zone.ChildId > _zones.Count)
                return false;

            // IDs are assigned monotonically by AddZone. Still verify the reciprocal link so
            // a stale/malformed child ID can never make an unrelated broken OB visible.
            Zone child = _zones[zone.ChildId - 1];
            bool matchingChild = child != null
                && child.Id == zone.ChildId
                && child.Kind == SznpKind.Breaker
                && child.ParentId == zone.Id;

            return ZoneBreakerVisibilityPolicy.ShouldShowParentHistory(
                zone.Kind, zone.State, zone.ChildId, matchingChild, ShowObHistoryOfBreakers);
        }

        private bool ZoneWanted(Zone zone, double price, double atrNow)
        {
            if (zone.IsHigherTimeframe && !ShowM15Zones)
                return false;

            bool showParentHistory = IsBreakerParentHistory(zone);

            switch (zone.Kind)
            {
                case SznpKind.Fvg:
                    if (!ShowFvg) return false;
                    break;
                case SznpKind.Gap:
                    if (!ShowGap) return false;
                    break;
                case SznpKind.OrderBlock:
                    if (!ShowOb && !showParentHistory) return false;
                    break;
                default:
                    if (!ShowBreaker) return false;
                    break;
            }

            // Broken zones are never valid chart areas. Their logical records stay in
            // history for lifecycle diagnostics and breaker-parent lineage, but display
            // switches cannot resurrect their rectangles, midlines, or labels.
            if (!showParentHistory
                && !ZoneVisibilityPolicy.ShouldRender(zone.State, ShowTouchedZones, ShowFilledZones))
                return false;

            if (!showParentHistory
                && DisplayMode == SznpDisplayMode.FreshOnly && zone.State != SznpState.Fresh)
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
            DateTime viewStart,
            DateTime viewEnd,
            double viewTop,
            double viewBottom,
            bool needScore)
        {
            // Always filter by visible time/price before ranking or applying quotas.
            if (!ZoneWanted(zone, price, atrNow)
                || !ZoneIntersectsViewport(zone, viewStart, viewEnd, viewTop, viewBottom))
                return false;

            double score = zone.Score;
            if (needScore && zone.State != SznpState.Broken)
                score = ComputeScore(zone, nowIndex, price, atrNow);

            if (MinScore > 0 && score < MinScore)
                return false;

            ZoneRenderState view = GetZoneRenderState(zone);
            view.Score = score;
            view.Want = true;
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

            // A transient invalid viewport must not trigger a full-history drawing burst.
            // Existing views are reconciled on the next scroll/zoom/resize or valid render.
            if (!hasViewport)
                return;

            bool indexedViewportSelection = ZoneViewportSelectionPolicy.UseActiveZoneIndex(
                hasViewport,
                ShowFilledZones,
                ShowObHistoryOfBreakers);

            bool needScore = MinScore > 0
                || DisplayMode == SznpDisplayMode.TopRanked
                || MaxZonesPerKind > 0
                || MaxChartObjects > 0
                || OverlapDeclutter
                || ShowZoneLabels;

            for (int k = 0; k < _zoneRenderingState.Tracked.Count; k++)
            {
                ZoneRenderState previous;
                if (_zoneRenderingState.States.TryGetValue(_zoneRenderingState.Tracked[k].Id, out previous))
                    previous.Want = false;
            }

            _cand.Clear();
            if (indexedViewportSelection)
            {
                _zoneRenderingState.QueryScratch.Clear();
                _zoneIntervalIndex.Query(viewBottom, viewTop, _zoneRenderingState.QueryScratch);

                for (int k = 0; k < _zoneRenderingState.QueryScratch.Count; k++)
                {
                    TryAddRenderCandidate(
                        _zoneRenderingState.QueryScratch[k], price, atrNow, nowIndex,
                        viewStart, viewEnd, viewTop, viewBottom, needScore);
                }
            }
            else
            {
                for (int k = 0; k < _zones.Count; k++)
                {
                    TryAddRenderCandidate(
                        _zones[k], price, atrNow, nowIndex,
                        viewStart, viewEnd, viewTop, viewBottom, needScore);
                }
            }

            // Ranking and per-kind limits apply only to zones intersecting the viewport.
            int perKind = MaxZonesPerKind > 0 ? MaxZonesPerKind : int.MaxValue;
            if (DisplayMode == SznpDisplayMode.TopRanked)
                perKind = Math.Min(perKind, TopRankedCount);
            if (perKind != int.MaxValue)
                LimitPerKind(perKind);

            if (OverlapDeclutter)
                Declutter();

            // This is a chart-object budget only: it never deletes/mutates logical zones.
            _zoneRenderingState.HiddenByBudget = 0;
            if (MaxChartObjects > 0)
            {
                _tmp.Clear();
                for (int k = 0; k < _cand.Count; k++)
                {
                    if (GetZoneRenderState(_cand[k]).Want)
                        _tmp.Add(_cand[k]);
                }

                if (_tmp.Count > MaxChartObjects)
                {
                    _tmp.Sort(CompareScoreDesc);
                    for (int k = MaxChartObjects; k < _tmp.Count; k++)
                        GetZoneRenderState(_tmp[k]).Want = false;
                    _zoneRenderingState.HiddenByBudget = _tmp.Count - MaxChartObjects;
                }
            }

            // Only zones in the previous/current viewport retain native chart-object handles.
            for (int k = 0; k < _cand.Count; k++)
            {
                Zone zone = _cand[k];
                ZoneRenderState view = GetZoneRenderState(zone);
                if ((view.Want || view.Shown) && !view.RenderTracked)
                {
                    view.RenderTracked = true;
                    _zoneRenderingState.Tracked.Add(zone);
                }
            }

            // Candidates rejected by the viewport-local rank/caps need no retained cache.
            // Keep state only for currently shown zones or the prior view being retired.
            for (int k = 0; k < _cand.Count; k++)
            {
                Zone zone = _cand[k];
                ZoneRenderState view;
                if (_zoneRenderingState.States.TryGetValue(zone.Id, out view)
                    && !view.Want && !view.Shown && !view.RenderTracked)
                    _zoneRenderingState.States.Remove(zone.Id);
            }

            int shown = 0;
            int write = 0;
            int trackedCount = _zoneRenderingState.Tracked.Count;
            if (ShowZoneLabels)
            {
                _zoneRenderingState.Tracked.Sort(delegate (Zone a, Zone b) { return a.Id.CompareTo(b.Id); });
                PrepareZoneLabelRows();
            }
            else
            {
                _zoneRenderingState.LabelRows.Clear();
            }

            for (int k = 0; k < trackedCount; k++)
            {
                Zone zone = _zoneRenderingState.Tracked[k];
                SyncZone(zone, refreshMetadata);
                ZoneRenderState view = GetZoneRenderState(zone);

                if (view.Shown)
                    shown++;

                if (view.Want || view.Shown)
                {
                    _zoneRenderingState.Tracked[write++] = zone;
                }
                else
                {
                    view.RenderTracked = false;
                    _zoneRenderingState.States.Remove(zone.Id);
                }
            }

            if (write < _zoneRenderingState.Tracked.Count)
                _zoneRenderingState.Tracked.RemoveRange(write, _zoneRenderingState.Tracked.Count - write);
            _zoneRenderingState.ShownZones = shown;
        }

        private void LimitPerKind(int limit)
        {
            for (int kind = 0; kind < 4; kind++)
            {
                _tmp.Clear();
                for (int k = 0; k < _cand.Count; k++)
                {
                    Zone zone = _cand[k];
                    if (GetZoneRenderState(zone).Want && (int)zone.Kind == kind)
                        _tmp.Add(zone);
                }

                if (_tmp.Count <= limit)
                    continue;

                _tmp.Sort(CompareScoreDesc);
                for (int k = limit; k < _tmp.Count; k++)
                    GetZoneRenderState(_tmp[k]).Want = false;
            }
        }

        /// <summary>Optionally hide lower-ranked zones of different kinds that overlap materially.</summary>
        private void Declutter()
        {
            _tmp.Clear();
            for (int k = 0; k < _cand.Count; k++)
            {
                if (GetZoneRenderState(_cand[k]).Want)
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
                    GetZoneRenderState(zone).Want = false;
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

        private string KindTag(Zone zone)
        {
            string tag;
            bool showParentHistory = IsBreakerParentHistory(zone);
            switch (zone.Kind)
            {
                case SznpKind.Fvg: tag = "FVG"; break;
                case SznpKind.Gap: tag = "GAP"; break;
                case SznpKind.OrderBlock:
                    tag = showParentHistory
                        ? (zone.Major ? "OB*→BB" : "OB→BB")
                        : (zone.Major ? "OB*" : "OB");
                    break;
                default: tag = "BB"; break;
            }

            bool taggedDirection = showParentHistory && zone.Kind == SznpKind.OrderBlock
                ? !zone.Bull
                : zone.Bull;
            return (zone.IsHigherTimeframe ? "M15 " : "") + tag + (taggedDirection ? "+" : "-");
        }

        private string LabelText(Zone zone)
        {
            double score = GetZoneRenderState(zone).Score;
            return score > 0
                ? KindTag(zone) + " " + ((int)Math.Round(score)).ToString(CultureInfo.InvariantCulture)
                : KindTag(zone);
        }

        /// <summary>
        /// Places zone labels just off the midpoint and vertically separates labels that
        /// would otherwise overlap at the common forty-bar right edge.
        /// </summary>
        private void PrepareZoneLabelRows()
        {
            _zoneRenderingState.LabelRows.Clear();
            long tolerance = Math.Max(1L, _barSpan.Ticks) * 2L;

            // Reserve positions used by other feature labels near zone right edges.
            // Zone labels are intentionally skipped: this pass recomputes all of them in order.
            foreach (ChartObject chartObject in _reg.Values)
            {
                ChartText text = chartObject as ChartText;
                if (text == null || !text.IsAlive
                    || text.Name.StartsWith(_prefix + "Z", StringComparison.Ordinal))
                    continue;

                _zoneRenderingState.LabelRows.Add(new ZoneLabelSlot { Time = text.Time, Y = text.Y });
            }
        }

        private bool ZoneLabelRowCollides(DateTime time, double price, double separation, long tolerance)
        {
            for (int i = 0; i < _zoneRenderingState.LabelRows.Count; i++)
            {
                ZoneLabelSlot row = _zoneRenderingState.LabelRows[i];
                if (Math.Abs((row.Time - time).Ticks) <= tolerance
                    && Math.Abs(row.Y - price) < separation)
                    return true;
            }

            return false;
        }

        private void ReserveZoneLabelRow(DateTime time, double price)
        {
            _zoneRenderingState.LabelRows.Add(new ZoneLabelSlot { Time = time, Y = price });
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
            int attempts = Math.Min(48, Math.Max(8, _zoneRenderingState.Tracked.Count * 2 + 2));

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
            ZoneRenderState view = GetZoneRenderState(zone);
            if (!view.Want)
            {
                if (view.Shown) RemoveZoneObjects(zone);
                return;
            }

            DateTime start = zone.StartTime;
            DateTime end = ZoneEndTime(zone);
            double labelY = ComputeZoneLabelY(zone, end);
            Color color = ZoneColor(zone);
            string name = _prefix + "Z" + zone.Id.ToString(CultureInfo.InvariantCulture);

            // A live settings change from extra objects to rectangle-only mode must
            // actively remove the old children, not merely stop creating new ones.
            if (!ShowMidline && view.Midline != null)
            {
                KillObject(view.Midline, name + "_m");
                view.Midline = null;
            }
            if (!ShowZoneLabels && view.Label != null)
            {
                KillObject(view.Label, name + "_t");
                view.Label = null;
            }

            bool missingRectangle = view.Rectangle == null || !view.Rectangle.IsAlive;
            bool missingMidline = ShowMidline && (view.Midline == null || !view.Midline.IsAlive);
            bool missingLabel = ShowZoneLabels && (view.Label == null || !view.Label.IsAlive);

            // Reconcile the full view if any currently enabled child has gone missing.
            if (view.Shown && (missingRectangle || missingMidline || missingLabel))
                RemoveZoneObjects(zone);

            bool createView = !view.Shown;
            string comment = refreshMetadata || createView ? ZoneComment(zone) : view.AppliedComment;
            string labelText = !ShowZoneLabels ? null
                : (refreshMetadata || createView ? LabelText(zone) : view.AppliedLabelText);

            if (!view.Shown)
            {
                int borderThickness = zone.IsHigherTimeframe ? Math.Max(1, ZoneBorderThickness) : ZoneBorderThickness;
                LineStyle borderStyle = zone.IsHigherTimeframe ? LineStyle.Dots : LineStyle.Solid;
                ChartRectangle rectangle = Chart.DrawRectangle(
                    name, start, zone.Upper, end, zone.Lower, color, borderThickness, borderStyle);

                rectangle.IsFilled = true;
                rectangle.IsInteractive = ObjectsInteractive;
                rectangle.ZIndex = ZoneZIndex;
                rectangle.Comment = comment;
                view.Rectangle = rectangle;
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
                    view.Midline = midline;
                    Reg(midline);
                }

                if (ShowZoneLabels)
                {
                    ChartText label = Chart.DrawText(
                        name + "_t", labelText, end, labelY, WithOpacity(color, 100));
                    label.FontSize = Math.Max(11, LabelFontSize + 2);
                    label.FontFamily = "Segoe UI Light";
                    label.IsBold = false;
                    label.HorizontalAlignment = HorizontalAlignment.Left;
                    label.VerticalAlignment = VerticalAlignment.Center;
                    label.IsInteractive = ObjectsInteractive;
                    view.Label = label;
                    Reg(label);
                }

                view.Shown = true;
                view.AppliedEndTime = end;
                view.AppliedLabelY = labelY;
                view.AppliedColor = color;
                view.AppliedComment = comment;
                view.AppliedLabelText = labelText;
                return;
            }

            bool timeChanged = view.AppliedEndTime != end;
            bool colorChanged = !SameColor(view.AppliedColor, color);
            bool commentChanged = !string.Equals(view.AppliedComment, comment, StringComparison.Ordinal);
            bool labelChanged = ShowZoneLabels && !string.Equals(view.AppliedLabelText, labelText, StringComparison.Ordinal);

            if (timeChanged)
            {
                view.Rectangle.Time2 = end;
                if (view.Midline != null && view.Midline.IsAlive) view.Midline.Time2 = end;
                if (view.Label != null && view.Label.IsAlive)
                {
                    view.Label.Time = end;
                    view.Label.Y = labelY;
                    view.AppliedLabelY = labelY;
                }
                view.AppliedEndTime = end;
            }

            if (colorChanged)
            {
                view.Rectangle.Color = color;
                view.AppliedColor = color;
                if (view.Midline != null && view.Midline.IsAlive) view.Midline.Color = ZoneMidlineColor(zone);
                if (view.Label != null && view.Label.IsAlive) view.Label.Color = WithOpacity(color, 100);
            }

            if (ShowZoneLabels && Math.Abs(view.AppliedLabelY - labelY) > Math.Max(1e-12, _tick * 0.1))
            {
                if (view.Label != null && view.Label.IsAlive) view.Label.Y = labelY;
                view.AppliedLabelY = labelY;
            }

            if (commentChanged)
            {
                view.Rectangle.Comment = comment;
                view.AppliedComment = comment;
            }

            if (labelChanged && view.Label != null && view.Label.IsAlive)
            {
                view.Label.Text = labelText;
                view.AppliedLabelText = labelText;
            }
        }

        private void RemoveZoneObjects(Zone zone)
        {
            ZoneRenderState view;
            if (!_zoneRenderingState.States.TryGetValue(zone.Id, out view)) return;

            string name = _prefix + "Z" + zone.Id.ToString(CultureInfo.InvariantCulture);
            KillObject(view.Rectangle, name);
            if (view.Midline != null) KillObject(view.Midline, name + "_m");
            if (view.Label != null) KillObject(view.Label, name + "_t");

            view.Rectangle = null;
            view.Midline = null;
            view.Label = null;
            view.AppliedComment = null;
            view.AppliedLabelText = null;
            view.AppliedLabelY = 0;
            view.Shown = false;
        }
    }
}