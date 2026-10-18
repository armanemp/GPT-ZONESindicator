using System;
using System.Collections.Generic;
using System.Globalization;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void RenderRound()
        {
            if (!ShowRoundLevels)
            {
                foreach (KeyValuePair<long, Item> entry in _round)
                    SyncItem(entry.Value, false);
                return;
            }

            double price = _lastClose;
            if (price <= 0)
            {
                foreach (KeyValuePair<long, Item> entry in _round)
                    SyncItem(entry.Value, false);
                return;
            }

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
    }
}
