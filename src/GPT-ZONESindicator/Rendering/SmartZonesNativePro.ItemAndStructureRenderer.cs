using System;
using System.Collections.Generic;
using System.Globalization;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void MakeItemText(Item item)
        {
            ChartText text = Chart.DrawText(
                item.Name + "_t", item.Label, item.RenderLabelTime, item.RenderLabelY, item.Col);
            text.FontSize = Math.Max(11, LabelFontSize + 2);
            text.FontFamily = "Segoe UI Light";
            text.IsBold = false;
            text.HorizontalAlignment = item.LabelH;
            text.VerticalAlignment = item.LabelV;
            text.IsInteractive = ObjectsInteractive;
            item.Txt = text;
            Reg(text);
        }

        private static void SnapItem(Item item, DateTime end)
        {
            item.aT1 = item.T1;
            item.aT2 = end;
            item.aY = item.Y;
            item.aCol = item.Col;
            item.aLabel = item.Label;
            item.aLt = item.RenderLabelTime;
            item.aLy = item.RenderLabelY;
        }

        private void RemoveItem(Item item)
        {
            UntrackLineLabel(item);
            if (item.Line != null)
                KillObject(item.Line, item.Name);
            if (item.Txt != null)
                KillObject(item.Txt, item.Name + "_t");

            item.Line = null;
            item.Txt = null;
            item.Shown = false;
        }

        private void SyncItem(Item item, bool want)
        {
            if (item == null)
                return;

            if (!want || item.Dead)
            {
                if (item.Shown)
                    RemoveItem(item);
                return;
            }

            bool hasLabel = !string.IsNullOrEmpty(item.Label);
            if (hasLabel && !item.TextOnly)
            {
                TrackLineLabel(item);
                item.LabelH = HorizontalAlignment.Left;
                item.RenderLabelTime = LineLabelAnchorTime();
                item.RenderLabelY = ResolveLineLabelY(item.LabelY);
            }
            else
            {
                UntrackLineLabel(item);
                item.RenderLabelTime = item.LabelTime;
                item.RenderLabelY = item.LabelY;
            }

            DateTime end = item.T2 <= item.T1 ? item.T1.Add(_barSpan) : item.T2;

            if (item.Shown)
            {
                bool lineGone = !item.TextOnly && (item.Line == null || !item.Line.IsAlive);
                bool textGone = hasLabel && (item.Txt == null || !item.Txt.IsAlive);
                if (lineGone || textGone)
                    RemoveItem(item);
            }

            if (!item.Shown)
            {
                if (!item.TextOnly)
                {
                    ChartTrendLine line = Chart.DrawTrendLine(
                        item.Name, item.T1, item.Y, end, item.Y, item.Col, item.Thick, item.Style);
                    line.ExtendToInfinity = item.ExtendToInfinity;
                    line.IsInteractive = ObjectsInteractive;
                    item.Line = line;
                    Reg(line);
                }

                if (hasLabel)
                    MakeItemText(item);

                item.Shown = true;
                SnapItem(item, end);
                return;
            }

            bool changed = item.aT1 != item.T1
                || item.aT2 != end
                || item.aY != item.Y
                || !SameColor(item.aCol, item.Col)
                || item.aLabel != item.Label
                || item.aLt != item.RenderLabelTime
                || item.aLy != item.RenderLabelY;

            if (!changed)
                return;

            if (item.Line != null)
            {
                item.Line.Time1 = item.T1;
                item.Line.Time2 = end;
                item.Line.Y1 = item.Y;
                item.Line.Y2 = item.Y;
                item.Line.Color = item.Col;
            }

            if (hasLabel)
            {
                if (item.Txt == null)
                {
                    MakeItemText(item);
                }
                else
                {
                    item.Txt.Text = item.Label;
                    item.Txt.Time = item.RenderLabelTime;
                    item.Txt.Y = item.RenderLabelY;
                    item.Txt.Color = item.Col;
                }
            }
            else if (item.Txt != null)
            {
                KillObject(item.Txt, item.Name + "_t");
                item.Txt = null;
            }

            SnapItem(item, end);
        }

        private DateTime LineLabelAnchorTime()
        {
            DateTime current = Bars.Count > 0 ? Bars.OpenTimes[Bars.Count - 1] : _lastBarTime;
            long stepTicks = Math.Max(1L, _barSpan.Ticks);
            try
            {
                return current.AddTicks(checked(stepTicks * 40L));
            }
            catch (ArgumentOutOfRangeException)
            {
                return current.AddDays(40);
            }
            catch (OverflowException)
            {
                return current.AddDays(40);
            }
        }

        private double ResolveLineLabelY(double targetY)
        {
            double top = Chart.TopY;
            double bottom = Chart.BottomY;
            if (Chart.Height <= 0 || double.IsNaN(top) || double.IsInfinity(top)
                || double.IsNaN(bottom) || double.IsInfinity(bottom) || top <= bottom
                || targetY < bottom || targetY > top)
                return targetY;

            double pricePerPixel = (top - bottom) / Math.Max(1, Chart.Height);
            double gap = pricePerPixel * Math.Max(18.0, (Math.Max(11, LabelFontSize + 2) * 1.65) + 3.0);
            double placed = LineLabelLayoutEvaluator.Place(
                targetY, bottom, top, gap, _occupiedLineLabelPrices);

            if (placed >= bottom && placed <= top)
                _occupiedLineLabelPrices.Add(placed);

            return placed;
        }

        private void TrackLineLabel(Item item)
        {
            if (item.LabelLayoutTracked)
                return;

            item.LabelLayoutTracked = true;
            _lineLabelItems.Add(item);
        }

        private void UntrackLineLabel(Item item)
        {
            if (!item.LabelLayoutTracked)
                return;

            item.LabelLayoutTracked = false;
            _lineLabelItems.Remove(item);
        }

        private void ReflowLineLabels()
        {
            _occupiedLineLabelPrices.Clear();
            Item[] items = _lineLabelItems.ToArray();
            for (int index = 0; index < items.Length; index++)
            {
                Item item = items[index];
                if (item == null)
                    continue;

                if (!item.Shown || item.TextOnly || string.IsNullOrEmpty(item.Label))
                {
                    UntrackLineLabel(item);
                    continue;
                }

                SyncItem(item, true);
            }
        }

        private static DateTime MidTime(DateTime a, DateTime b)
        {
            return a.AddTicks((b - a).Ticks / 2);
        }

        private void RenderStructure()
        {
            SyncEngine(_engMajor, ShowMajorBos, ShowMajorChoch, ShowMajorSwings, ShowMajorActiveLevels);
            SyncEngine(_engInt, ShowInternalBos, ShowInternalChoch, ShowInternalSwings, ShowInternalActiveLevels);
        }

        private void SyncEngine(Engine engine, bool showBos, bool showChoch, bool showMarks, bool showActive)
        {
            if (!showBos && !showChoch && !showMarks && !showActive)
                return;

            int cap = StructMaxVisible > 0 ? StructMaxVisible : int.MaxValue;
            int levelCount = 0;
            int markCount = 0;

            for (int k = engine.Swings.Count - 1; k >= 0; k--)
            {
                Swing swing = engine.Swings[k];

                bool wantLevel = false;
                if (swing.Broken)
                {
                    if (!swing.SilentBreak)
                        wantLevel = swing.Choch ? showChoch : showBos;
                }
                else if (showActive)
                {
                    wantLevel = true;
                }

                if (wantLevel && levelCount < cap)
                {
                    levelCount++;
                    EnsureLevelItem(swing, engine.Major);
                    SyncItem(swing.Level, true);
                }
                else if (swing.Level != null && swing.Level.Shown)
                {
                    SyncItem(swing.Level, false);
                }

                if (showMarks && markCount < cap)
                {
                    markCount++;
                    EnsureMarkItem(swing);
                    SyncItem(swing.Mark, true);
                }
                else if (swing.Mark != null && swing.Mark.Shown)
                {
                    SyncItem(swing.Mark, false);
                }
            }
        }

        private void EnsureLevelItem(Swing swing, bool major)
        {
            if (swing.Level == null)
                swing.Level = new Item { Name = NextName("S") };

            Item item = swing.Level;
            item.Y = swing.Price;
            item.T1 = swing.Time;
            item.Thick = StructThickness;
            item.Style = LineStyle.Dots;

            if (swing.Broken)
            {
                item.T2 = swing.BreakTime;
                Color baseColor = swing.Choch
                    ? _cChoch
                    : (swing.BreakBull ? _cStructBull : _cStructBear);

                item.Col = WithOpacity(baseColor, StructOpacity);
                item.Label = swing.Choch ? "CHoCH" : "BOS";
                item.LabelTime = MidTime(item.T1, item.T2);
                item.LabelY = swing.Price;
                item.LabelH = HorizontalAlignment.Center;
                item.LabelV = swing.BreakBull ? VerticalAlignment.Bottom : VerticalAlignment.Top;
            }
            else
            {
                item.T2 = _futureTime;
                item.Col = WithOpacity(swing.High ? _cSwingHigh : _cSwingLow, StructOpacity);
                item.Label = (major ? "Major " : "Internal ")
                    + (swing.High ? "Swing High" : "Swing Low");
                item.LabelTime = _lastBarTime;
                item.LabelY = swing.Price;
                item.LabelH = HorizontalAlignment.Left;
                item.LabelV = swing.High ? VerticalAlignment.Bottom : VerticalAlignment.Top;
            }
        }

        private void EnsureMarkItem(Swing swing)
        {
            if (swing.Mark == null)
            {
                swing.Mark = new Item
                {
                    Name = NextName("S"),
                    TextOnly = true,
                    LabelH = HorizontalAlignment.Center
                };
            }

            Item item = swing.Mark;
            item.Y = swing.Price;
            item.T1 = swing.Time;
            item.T2 = swing.Time;
            item.Label = swing.High ? "v" : "^";
            item.LabelTime = swing.Time;
            item.LabelY = swing.Price;
            item.LabelV = swing.High ? VerticalAlignment.Bottom : VerticalAlignment.Top;
            item.Col = WithOpacity(swing.High ? _cSwingHigh : _cSwingLow, 95);
        }

        private void RenderEqualAndSweeps()
        {
            int cap = StructMaxVisible > 0 ? StructMaxVisible : int.MaxValue;
            int count = 0;

            for (int k = _eqs.Count - 1; k >= 0; k--)
            {
                EqCluster cluster = _eqs[k];
                bool ended = cluster.Swept || cluster.Broken;
                bool want = ShowEqual && (!ended || ShowSweptEqual) && count < cap;

                if (want)
                {
                    count++;
                    if (cluster.Vis == null)
                        cluster.Vis = new Item { Name = NextName("E") };

                    Item item = cluster.Vis;
                    item.Y = cluster.Level;
                    item.T1 = cluster.FirstTime;
                    item.T2 = ended ? cluster.EndTime : _futureTime;
                    item.Thick = 1;
                    item.Style = LineStyle.Dots;
                    item.Col = WithOpacity(_cLiquidity, StructOpacity);
                    item.Label = (cluster.High ? "EQH" : "EQL")
                        + (cluster.Count > 2 ? " x" + cluster.Count.ToString(CultureInfo.InvariantCulture) : "");
                    item.LabelTime = MidTime(cluster.FirstTime, cluster.LastTime);
                    item.LabelY = cluster.Level;
                    item.LabelH = HorizontalAlignment.Center;
                    item.LabelV = cluster.High ? VerticalAlignment.Bottom : VerticalAlignment.Top;
                    SyncItem(item, true);
                }
                else if (cluster.Vis != null && cluster.Vis.Shown)
                {
                    SyncItem(cluster.Vis, false);
                }
            }

            count = 0;
            for (int k = _sweeps.Count - 1; k >= 0; k--)
            {
                SweepEv sweep = _sweeps[k];
                bool want = ShowSweeps && count < cap;

                if (want)
                {
                    count++;
                    if (sweep.Vis == null)
                        sweep.Vis = new Item { Name = NextName("W") };

                    Item item = sweep.Vis;
                    item.Y = sweep.Level;
                    item.T1 = sweep.T1;
                    item.T2 = sweep.T2;
                    item.Thick = 1;
                    item.Style = LineStyle.Dots;
                    item.Col = WithOpacity(_cLiquidity, 95);
                    item.Label = sweep.Bull ? "SSL sweep" : "BSL sweep";
                    item.LabelTime = sweep.T2;
                    item.LabelY = sweep.Level;
                    item.LabelH = HorizontalAlignment.Center;
                    item.LabelV = sweep.Bull ? VerticalAlignment.Top : VerticalAlignment.Bottom;
                    SyncItem(item, true);
                }
                else if (sweep.Vis != null && sweep.Vis.Shown)
                {
                    SyncItem(sweep.Vis, false);
                }
            }
        }

        private void RenderRef()
        {
            for (int k = _refItems.Count - 1; k >= 0; k--)
            {
                Item item = _refItems[k];

                if (item.Dead)
                {
                    SyncItem(item, false);
                    _refItems.RemoveAt(k);
                    continue;
                }

                if (item.Follow)
                {
                    item.LabelTime = _lastBarTime;
                    item.LabelY = item.Y;
                }

                SyncItem(item, true);
            }
        }
    }
}