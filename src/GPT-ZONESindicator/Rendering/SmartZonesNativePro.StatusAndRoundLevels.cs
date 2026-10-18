using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void RenderRound()
        {
            if (!ShowRoundLevels)
                return;

            double price = _lastClose;
            if (price <= 0)
                return;

            double step = RoundStep > 0
                ? RoundStep
                : Math.Pow(10.0, Math.Floor(Math.Log10(price)) - 2.0);

            if (step <= 0 || double.IsNaN(step) || double.IsInfinity(step))
                return;

            long center = (long)Math.Round(price / step);
            bool stepChanged = Math.Abs(step - _roundStepUsed) > 1e-12;
            bool refreshLevels = center != _roundCenter || stepChanged;
            int radius = RoundLevelsEachSide;

            if (refreshLevels)
            {
                _roundCenter = center;
                _roundStepUsed = step;

                List<long> remove = new List<long>();
                foreach (KeyValuePair<long, Item> entry in _round)
                {
                    if (stepChanged || entry.Key < center - radius || entry.Key > center + radius)
                        remove.Add(entry.Key);
                }

                for (int k = 0; k < remove.Count; k++)
                {
                    long key = remove[k];
                    SyncItem(_round[key], false);
                    _round.Remove(key);
                }

                for (long key = center - radius; key <= center + radius; key++)
                {
                    if (key <= 0 || _round.ContainsKey(key))
                        continue;

                    _round[key] = new Item
                    {
                        Name = _prefix + "RN" + key.ToString(CultureInfo.InvariantCulture),
                        T1 = Bars.OpenTimes[0],
                        T2 = _futureTime,
                        Thick = 1,
                        Style = LineStyle.Dots,
                        ExtendToInfinity = true,
                        LabelH = HorizontalAlignment.Left,
                        LabelV = VerticalAlignment.Bottom
                    };
                }
            }

            foreach (KeyValuePair<long, Item> entry in _round)
            {
                long key = entry.Key;
                Item item = entry.Value;
                double level = key * step;
                bool major = key % RoundMajorEvery == 0;

                item.Y = level;
                item.Col = WithOpacity(major ? _cRoundMajor : _cRound, RefOpacity);
                item.Style = RoundStyle;
                item.T1 = Bars.OpenTimes[0];
                item.T2 = _futureTime;
                item.Label = "RN " + level.ToString("0.#####", CultureInfo.InvariantCulture);
                item.LabelTime = _lastBarTime;
                item.LabelY = level;

                SyncItem(item, true);
            }
        }

        private void RenderStatus()
        {
            if (!ShowStatusText)
                return;

            string text = "SZNP  zones shown " + _shownZones + " / tracked " + _zones.Count
                + (_hiddenByBudget > 0 ? "  |  hidden by object budget: " + _hiddenByBudget : "");

            if (_status == null)
            {
                _status = Chart.DrawStaticText(
                    _prefix + "status",
                    text,
                    VerticalAlignment.Bottom,
                    HorizontalAlignment.Left,
                    Rgb(120, 130, 145));
                _status.FontSize = 8;
            }
            else if (_status.Text != text)
            {
                _status.Text = text;
            }
        }

        private void PrintSummary()
        {
            if (!PrintDiagnostics)
                return;

            int[] byKind = new int[4];
            int[] byState = new int[5];

            for (int k = 0; k < _zones.Count; k++)
            {
                byKind[(int)_zones[k].Kind]++;
                byState[(int)_zones[k].State]++;
            }

            double millisecondsPerTick = 1000.0 / Stopwatch.Frequency;
            Print(string.Format(
                CultureInfo.InvariantCulture,
                "[SZNP] ready | bars processed {0} (from index {1}) | process {2:0.0} ms total, {3:0.000} ms/bar | first render {4:0.0} ms | init {5:0.0} ms",
                _procBars,
                _startIdx,
                _procTicks * millisecondsPerTick,
                _procBars > 0 ? _procTicks * millisecondsPerTick / _procBars : 0.0,
                _renderTicks * millisecondsPerTick,
                _initTicks * millisecondsPerTick));

            Print(string.Format(
                CultureInfo.InvariantCulture,
                "[SZNP] zones tracked {0}: FVG {1}, Gap {2}, OB {3}, Breaker {4} | fresh {5}, touched {6}, partial {7}, filled {8}, broken {9}",
                _zones.Count,
                byKind[0], byKind[1], byKind[2], byKind[3],
                byState[0], byState[1], byState[2], byState[3], byState[4]));

            Print(string.Format(
                CultureInfo.InvariantCulture,
                "[SZNP] chart rectangles drawn {0} | hidden by object budget {1} | swings major {2} / internal {3} | equal levels {4} | sweeps {5} | reference items {6}",
                _shownZones,
                _hiddenByBudget,
                _engMajor.Swings.Count,
                _engInt.Swings.Count,
                _eqs.Count,
                _sweeps.Count,
                _refItems.Count));

            if (_rej.Count > 0)
            {
                StringBuilder builder = new StringBuilder(
                    "[SZNP] rejected by optional filters (counted, not drawn): ");
                bool first = true;

                foreach (KeyValuePair<string, int> entry in _rej)
                {
                    if (!first)
                        builder.Append("; ");

                    builder.Append(entry.Key).Append(" = ").Append(entry.Value);
                    first = false;
                }

                Print(builder.ToString());
            }
        }
    }
}