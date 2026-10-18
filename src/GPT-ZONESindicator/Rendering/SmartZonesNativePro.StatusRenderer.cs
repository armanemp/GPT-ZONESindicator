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
        private void RenderStatus()
        {
            if (!ShowStatusText)
            {
                if (_status != null)
                {
                    KillObject(_status, _prefix + "status");
                    _status = null;
                }
                return;
            }

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
