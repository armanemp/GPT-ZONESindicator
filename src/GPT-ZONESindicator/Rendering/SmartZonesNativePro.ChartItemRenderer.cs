using System;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void MakeItemText(Item item, DateTime labelTime, Color visibleColor)
        {
            ChartText text = Chart.DrawText(
                item.Name + "_t", item.Label, labelTime, item.LabelY, visibleColor);
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
            item.aLt = item.LabelTime;
            item.aLy = item.LabelY;
        }

        private void RemoveItem(Item item)
        {
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
                if (item.Shown || item.Line != null || item.Txt != null)
                    RemoveItem(item);
                return;
            }

            bool hasLabel = !string.IsNullOrEmpty(item.Label);
            // Preserve each line's own semantic end. Only active objects were assigned
            // _futureTime by their producer; a completed BOS/CHoCH stays at its break.
            DateTime end = item.T2 <= item.T1 ? item.T1.Add(_barSpan) : item.T2;
            DateTime labelTime = item.LabelTime;

            bool lineWanted = !item.TextOnly && ItemLineIntersectsViewport(item, end);
            bool textWanted = hasLabel && ItemLabelIntersectsViewport(labelTime, item.LabelY);

            // Virtualize line and text independently. Off-screen history retains its logical
            // state but does not keep chart objects alive; scrolling creates only the visible set.
            if (!lineWanted && item.Line != null)
            {
                KillObject(item.Line, item.Name);
                item.Line = null;
            }

            if (!textWanted && item.Txt != null)
            {
                KillObject(item.Txt, item.Name + "_t");
                item.Txt = null;
            }

            bool lineChanged = item.aT1 != item.T1
                || item.aT2 != end
                || item.aY != item.Y
                || !SameColor(item.aCol, item.Col);

            bool textChanged = item.aLabel != item.Label
                || item.aLt != item.LabelTime
                || item.aLy != item.LabelY
                || !SameColor(item.aCol, item.Col);

            if (lineWanted)
            {
                if (item.Line == null || !item.Line.IsAlive)
                {
                    ChartTrendLine line = Chart.DrawTrendLine(
                        item.Name, item.T1, item.Y, end, item.Y, item.Col, item.Thick, item.Style);
                    line.ExtendToInfinity = item.ExtendToInfinity;
                    line.IsInteractive = ObjectsInteractive;
                    item.Line = line;
                    Reg(line);
                }
                else if (lineChanged)
                {
                    item.Line.Time1 = item.T1;
                    item.Line.Time2 = end;
                    item.Line.Y1 = item.Y;
                    item.Line.Y2 = item.Y;
                    item.Line.Color = item.Col;
                }
            }

            if (textWanted)
            {
                if (item.Txt == null || !item.Txt.IsAlive)
                {
                    MakeItemText(item, labelTime, item.Col);
                }
                else if (textChanged)
                {
                    item.Txt.Text = item.Label;
                    item.Txt.Time = labelTime;
                    item.Txt.Y = item.LabelY;
                    item.Txt.Color = item.Col;
                }
            }

            item.Shown = (item.Line != null && item.Line.IsAlive)
                || (item.Txt != null && item.Txt.IsAlive);
            SnapItem(item, end);
        }

        private static DateTime MidTime(DateTime a, DateTime b)
        {
            return a.AddTicks((b - a).Ticks / 2);
        }
    }
}
