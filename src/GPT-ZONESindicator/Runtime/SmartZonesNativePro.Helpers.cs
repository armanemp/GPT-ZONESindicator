using System;
using System.Collections.Generic;
using System.Globalization;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private double H(int i) { return Bars.HighPrices[i]; }
        private double L(int i) { return Bars.LowPrices[i]; }
        private double O(int i) { return Bars.OpenPrices[i]; }
        private double C(int i) { return Bars.ClosePrices[i]; }
        private DateTime T(int i) { return Bars.OpenTimes[i]; }

        private DateTime CloseTimeOf(int i)
        {
            DateTime barOpen = Bars.OpenTimes[i];
            DateTime nominalClose = barOpen.Add(_barSpan);
            if (i + 1 < Bars.Count)
            {
                DateTime nextOpen = Bars.OpenTimes[i + 1];
                TimeSpan delta = nextOpen - barOpen;
                TimeSpan maximumContinuousStep = _barSpan.Add(
                    TimeSpan.FromTicks(Math.Max(1L, _barSpan.Ticks / 10L)));
                if (delta > TimeSpan.Zero && delta <= maximumContinuousStep)
                    return nextOpen;
            }

            // A weekend or market closure is not the duration of the previous candle.
            return nominalClose;
        }

        private double AtrAt(int i)
        {
            int period = Math.Max(1, AtrPeriod);

            while (_atr.Count <= i)
            {
                int k = _atr.Count;
                double tr = H(k) - L(k);

                if (k > 0)
                {
                    double previousClose = C(k - 1);
                    tr = Math.Max(tr, Math.Max(
                        Math.Abs(H(k) - previousClose),
                        Math.Abs(L(k) - previousClose)));
                }

                double value;
                if (k < period)
                {
                    _trSum += tr;
                    value = _trSum / (k + 1);
                }
                else
                {
                    value = (_atr[k - 1] * (period - 1) + tr) / period;
                }

                _atr.Add(value);
            }

            return Math.Max(_atr[i], _tick);
        }

        private static double Clamp01(double value)
        {
            return value < 0 ? 0 : (value > 1 ? 1 : value);
        }

        private void Reject(string reason)
        {
            int count;
            _rej.TryGetValue(reason, out count);
            _rej[reason] = count + 1;
        }

        private void Log(string message)
        {
            if (PrintDiagnostics)
                Print("[SZNP] " + message);
        }

        private void LogError(string where, Exception ex)
        {
            _errorCount++;
            if (_errorCount <= 5)
                Print("[SZNP] ERROR in " + where + ": " + ex.GetType().Name + " - " + ex.Message);
            if (_errorCount == 6)
                Print("[SZNP] further errors suppressed");
        }

        private string NextName(string kind)
        {
            _nameSeq++;
            return _prefix + kind + _nameSeq.ToString(CultureInfo.InvariantCulture);
        }

        private static Color ParseColor(string value, Color fallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                    return fallback;

                value = value.Trim();
                return value[0] == '#' ? Color.FromHex(value) : Color.FromName(value);
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private static Color Rgb(int r, int g, int b)
        {
            return Color.FromArgb((byte)255, (byte)r, (byte)g, (byte)b);
        }

        private static Color WithOpacity(Color color, double pct)
        {
            int alpha = (int)Math.Round(Math.Min(255.0, Math.Max(0.0, pct * 2.55)));
            return Color.FromArgb((byte)alpha, color.R, color.G, color.B);
        }

        private static bool SameColor(Color a, Color b)
        {
            return a.A == b.A && a.R == b.R && a.G == b.G && a.B == b.B;
        }

        private static int ZoneIdx(SznpKind kind, bool bull)
        {
            return (int)kind * 2 + (bull ? 1 : 0);
        }

        private void BuildPalette()
        {
            _zoneBase[ZoneIdx(SznpKind.Fvg, true)] = ParseColor(FvgBullColor, Rgb(10, 78, 62));
            _zoneBase[ZoneIdx(SznpKind.Fvg, false)] = ParseColor(FvgBearColor, Rgb(94, 12, 37));
            _zoneBase[ZoneIdx(SznpKind.Gap, true)] = ParseColor(GapBullColor, Rgb(184, 134, 11));
            _zoneBase[ZoneIdx(SznpKind.Gap, false)] = ParseColor(GapBearColor, Rgb(139, 69, 19));
            _zoneBase[ZoneIdx(SznpKind.OrderBlock, true)] = ParseColor(ObBullColor, Rgb(25, 72, 110));
            _zoneBase[ZoneIdx(SznpKind.OrderBlock, false)] = ParseColor(ObBearColor, Rgb(72, 59, 50));
            _zoneBase[ZoneIdx(SznpKind.Breaker, true)] = ParseColor(BreakerBullColor, Rgb(22, 160, 166));
            _zoneBase[ZoneIdx(SznpKind.Breaker, false)] = ParseColor(BreakerBearColor, Rgb(128, 100, 181));

            _midBase[(int)SznpKind.Fvg] = ParseColor(FvgMidColor, Rgb(60, 179, 113));
            _midBase[(int)SznpKind.Gap] = ParseColor(GapMidColor, Rgb(218, 165, 32));
            _midBase[(int)SznpKind.OrderBlock] = ParseColor(ObMidColor, Rgb(100, 149, 237));
            _midBase[(int)SznpKind.Breaker] = ParseColor(BreakerMidColor, Rgb(100, 200, 204));

            _cFilled = ParseColor(FilledZoneColor, Rgb(112, 128, 144));
            _cBroken = ParseColor(BrokenZoneColor, Rgb(105, 105, 105));
            _cStructBull = ParseColor(StructBullColor, Rgb(46, 139, 87));
            _cStructBear = ParseColor(StructBearColor, Rgb(178, 34, 34));
            _cChoch = ParseColor(ChochColor, Rgb(184, 134, 11));
            _cSwingHigh = ParseColor(SwingHighColor, Rgb(178, 34, 34));
            _cSwingLow = ParseColor(SwingLowColor, Rgb(46, 139, 87));
            _cLiquidity = ParseColor(LiquidityColor, Rgb(210, 180, 140));

            _cPrevDay = ParseColor(PrevDayColor, Rgb(112, 128, 144));
            _cPrevWeek = ParseColor(PrevWeekColor, Rgb(112, 128, 144));
            _cPrevMonth = ParseColor(PrevMonthColor, Rgb(112, 128, 144));
            _cOpen = ParseColor(OpenLevelColor, Rgb(128, 128, 128));
            _cAsia = ParseColor(AsiaColor, Rgb(112, 128, 144));
            _cLondon = ParseColor(LondonColor, Rgb(106, 90, 205));
            _cNy = ParseColor(NyColor, Rgb(188, 143, 143));
            _cRound = ParseColor(RoundColor, Rgb(105, 105, 105));
            _cRoundMajor = ParseColor(RoundMajorColor, Rgb(128, 128, 128));
        }

        /// <summary>Remove only this instance's chart objects; unrelated indicators' drawings remain untouched.</summary>
        private void RemoveOwnObjects()
        {
            try
            {
                List<string> names = new List<string>();
                foreach (ChartObject chartObject in Chart.Objects)
                {
                    string name = chartObject.Name;
                    if (name != null && name.StartsWith(_prefix, StringComparison.Ordinal))
                        names.Add(name);
                }

                foreach (string name in names)
                    Chart.RemoveObject(name);
            }
            catch (Exception ex)
            {
                LogError("RemoveOwnObjects", ex);
            }

            try
            {
                List<KeyValuePair<string, ChartObject>> mine =
                    new List<KeyValuePair<string, ChartObject>>(_reg);

                foreach (KeyValuePair<string, ChartObject> entry in mine)
                    KillObject(entry.Value, entry.Key);

                _reg.Clear();
                Chart.RemoveObject(_prefix + "status");
            }
            catch (Exception ex)
            {
                LogError("RemoveOwnObjects(reg)", ex);
            }
        }

        private void Reg(ChartObject chartObject)
        {
            if (chartObject != null && chartObject.Name != null)
                _reg[chartObject.Name] = chartObject;
        }

        /// <summary>Remove by name and hide as a fallback if a non-interactive object remains alive.</summary>
        private void KillObject(ChartObject chartObject, string name)
        {
            try
            {
                Chart.RemoveObject(name);
                _reg.Remove(name);
                if (chartObject != null && chartObject.IsAlive)
                    chartObject.IsHidden = true;
            }
            catch (Exception)
            {
                // Cleanup should remain best-effort during chart teardown.
            }
        }

        private static DateTime LastSundayOf(int year, int month)
        {
            DateTime day = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            while (day.DayOfWeek != DayOfWeek.Sunday)
                day = day.AddDays(-1);
            return day;
        }

        private static DateTime NthSundayOf(int year, int month, int n)
        {
            DateTime day = new DateTime(year, month, 1);
            while (day.DayOfWeek != DayOfWeek.Sunday)
                day = day.AddDays(1);
            return day.AddDays(7 * (n - 1));
        }

        private static bool IsDst(DateTime day, SznpDst rule)
        {
            if (rule == SznpDst.None)
                return false;

            DateTime start, end;
            if (rule == SznpDst.UK)
            {
                start = LastSundayOf(day.Year, 3);
                end = LastSundayOf(day.Year, 10);
            }
            else
            {
                start = NthSundayOf(day.Year, 3, 2);
                end = NthSundayOf(day.Year, 11, 1);
            }

            DateTime date = day.Date;
            return date >= start && date < end;
        }
    }
}