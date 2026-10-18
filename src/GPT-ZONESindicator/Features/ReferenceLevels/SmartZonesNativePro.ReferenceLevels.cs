using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void SetupHtf()
        {
            _referenceLevelState.Tracks.Clear();
            AddHtfTrack("PD", "Day Open", TimeFrame.Daily, ShowPrevDay, ShowPrevDayMid, ShowDayOpen, DailyPeriodsKept, _cPrevDay);
            AddHtfTrack("PW", "Week Open", TimeFrame.Weekly, ShowPrevWeek, ShowPrevWeekMid, ShowWeekOpen, WeeklyPeriodsKept, _cPrevWeek);
            AddHtfTrack("PM", "Month Open", TimeFrame.Monthly, ShowPrevMonth, ShowPrevMonthMid, ShowMonthOpen, MonthlyPeriodsKept, _cPrevMonth);
        }

        private void AddHtfTrack(
            string shortName,
            string openName,
            TimeFrame timeframe,
            bool showHighLow,
            bool showMid,
            bool showOpen,
            int keep,
            Color color)
        {
            if (!showHighLow && !showMid && !showOpen)
                return;

            HtfTrack track = new HtfTrack
            {
                Short = shortName,
                OpenName = openName,
                ShowHL = showHighLow,
                ShowMid = showMid,
                ShowOpen = showOpen,
                Keep = Math.Max(1, keep),
                Col = color
            };

            try
            {
                track.Data = MarketData.GetBars(timeframe);
            }
            catch (Exception ex)
            {
                LogError("GetBars(" + timeframe + ")", ex);
                track.Data = null;
            }

            if (track.Data != null)
                _referenceLevelState.Tracks.Add(track);
        }

        private void SetupSessions()
        {
            _referenceLevelState.Sessions.Clear();
            _referenceLevelState.Sessions.Add(new SessionTrack
            {
                Name = "Asia", Enabled = ShowAsia, StartH = AsiaStartHour, EndH = AsiaEndHour,
                Dst = SznpDst.None, Col = _cAsia
            });
            _referenceLevelState.Sessions.Add(new SessionTrack
            {
                Name = "London", Enabled = ShowLondon, StartH = LondonStartHour, EndH = LondonEndHour,
                Dst = LondonDst, Col = _cLondon
            });
            _referenceLevelState.Sessions.Add(new SessionTrack
            {
                Name = "NewYork", Enabled = ShowNewYork, StartH = NyStartHour, EndH = NyEndHour,
                Dst = NyDst, Col = _cNy
            });
        }

        private void UpdateReference(int index)
        {
            DateTime time = T(index);
            for (int k = 0; k < _referenceLevelState.Tracks.Count; k++)
                AdvanceHtf(_referenceLevelState.Tracks[k], time);

            for (int k = 0; k < _referenceLevelState.Sessions.Count; k++)
            {
                if (_referenceLevelState.Sessions[k].Enabled)
                    UpdateSession(_referenceLevelState.Sessions[k], index);
            }
        }

        private Item NewRefItem(string label, double price, DateTime start, Color color, LineStyle style, bool follow)
        {
            Item item = new Item
            {
                Name = NextName("L"),
                Y = price,
                T1 = start,
                T2 = _futureTime,
                Col = color,
                Thick = RefThickness,
                Style = style,
                Label = label,
                LabelTime = start,
                LabelY = price,
                Follow = follow,
                LabelH = HorizontalAlignment.Left,
                LabelV = VerticalAlignment.Bottom
            };

            _referenceLevelState.Items.Add(item);
            return item;
        }

        /// <summary>
        /// HTF levels use the prior completed bar and the active period's open.
        /// The current HTF index only advances with chart time to prevent future data leakage.
        /// </summary>
        private void AdvanceHtf(HtfTrack track, DateTime time)
        {
            Bars data = track.Data;
            int count = data.Count;
            int current = track.Cur;

            while (current + 1 < count && data.OpenTimes[current + 1] <= time)
                current++;

            if (current == track.Cur)
                return;

            track.Cur = current;
            if (current < 1)
                return;

            DateTime periodStart = data.OpenTimes[current];
            if (track.Current != null)
            {
                foreach (Item item in track.Current)
                {
                    item.T2 = periodStart;
                    item.Follow = false;
                    // Keep the completed period's label attached to its historical line.
                }
            }

            double previousHigh = data.HighPrices[current - 1];
            double previousLow = data.LowPrices[current - 1];
            Color color = WithOpacity(track.Col, RefOpacity);
            List<Item> group = new List<Item>();

            if (track.ShowHL)
            {
                group.Add(NewRefItem(track.Short + "H", previousHigh, periodStart, color, RefStyle, true));
                group.Add(NewRefItem(track.Short + "L", previousLow, periodStart, color, RefStyle, true));
            }

            if (track.ShowMid)
            {
                group.Add(NewRefItem(
                    track.Short + " 50%",
                    (previousHigh + previousLow) * 0.5,
                    periodStart,
                    color,
                    LineStyle.Dots,
                    true));
            }

            if (track.ShowOpen)
            {
                group.Add(NewRefItem(
                    track.OpenName,
                    data.OpenPrices[current],
                    periodStart,
                    WithOpacity(_cOpen, RefOpacity),
                    LineStyle.Dots,
                    true));
            }

            track.Current = group;
            track.Groups.Enqueue(group);

            while (track.Groups.Count > track.Keep)
            {
                List<Item> old = track.Groups.Dequeue();
                foreach (Item item in old)
                    item.Dead = true;
            }
        }

        private bool FindWindow(DateTime time, SessionTrack session, out DateTime windowStart, out DateTime windowEnd)
        {
            double duration = session.EndH - session.StartH;
            if (duration <= 0)
                duration += 24.0;
            if (duration > 24.0)
                duration = 24.0;

            for (int dayOffset = 1; dayOffset >= -1; dayOffset--)
            {
                DateTime day = time.Date.AddDays(dayOffset);
                double startUtc = session.StartH - SessionClockOffset;
                if (IsDst(day, session.Dst))
                    startUtc -= 1.0;

                DateTime start = day.AddHours(startUtc);
                DateTime end = start.AddHours(duration);
                if (time >= start && time < end)
                {
                    windowStart = start;
                    windowEnd = end;
                    return true;
                }
            }

            windowStart = DateTime.MinValue;
            windowEnd = DateTime.MinValue;
            return false;
        }

        private void StartSession(SessionTrack session, DateTime windowStart, DateTime windowEnd, int index)
        {
            if (session.HiItem != null)
                session.Past.Enqueue(new[] { session.HiItem, session.LoItem });

            while (session.Past.Count > Math.Max(0, SessionsKept - 1))
            {
                Item[] old = session.Past.Dequeue();
                old[0].Dead = true;
                old[1].Dead = true;
            }

            DateTime start = T(index);
            DateTime end = windowEnd.AddHours(SessionExtendHours);
            if (end <= start)
                end = start.Add(_barSpan);

            Color color = WithOpacity(session.Col, RefOpacity);
            session.WinStart = windowStart;
            session.Hi = H(index);
            session.Lo = L(index);

            session.HiItem = new Item
            {
                Name = NextName("L"),
                Y = session.Hi,
                T1 = start,
                T2 = end,
                Col = color,
                Thick = RefThickness,
                Style = SessionStyle,
                Label = session.Name + " H",
                LabelTime = start,
                LabelY = session.Hi,
                LabelH = HorizontalAlignment.Left,
                LabelV = VerticalAlignment.Bottom
            };

            session.LoItem = new Item
            {
                Name = NextName("L"),
                Y = session.Lo,
                T1 = start,
                T2 = end,
                Col = color,
                Thick = RefThickness,
                Style = SessionStyle,
                Label = session.Name + " L",
                LabelTime = start,
                LabelY = session.Lo,
                LabelH = HorizontalAlignment.Left,
                LabelV = VerticalAlignment.Top
            };

            _referenceLevelState.Items.Add(session.HiItem);
            _referenceLevelState.Items.Add(session.LoItem);
        }

        /// <summary>Only bars inside the active session window contribute to its range.</summary>
        private void UpdateSession(SessionTrack session, int index)
        {
            DateTime windowStart, windowEnd;
            if (!FindWindow(T(index), session, out windowStart, out windowEnd))
                return;

            if (windowStart != session.WinStart)
                StartSession(session, windowStart, windowEnd, index);

            double high = H(index);
            double low = L(index);

            if (high > session.Hi)
            {
                session.Hi = high;
                session.HiItem.Y = high;
                session.HiItem.LabelY = high;
            }

            if (low < session.Lo)
            {
                session.Lo = low;
                session.LoItem.Y = low;
                session.LoItem.LabelY = low;
            }
        }
    }
}