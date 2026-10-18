using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void CheckEqual(Engine engine, Swing swing, double atr)
        {
            double tolerance = EqToleranceMode == SznpToleranceMode.Atr
                ? EqTolAtr * atr
                : EqTolPips * _pip;

            if (tolerance <= 0)
                tolerance = _tick;

            List<Swing> swings = engine.Swings;
            for (int k = swings.Count - 2; k >= 0; k--)
            {
                Swing candidate = swings[k];
                if (candidate.High != swing.High)
                    continue;

                int distance = swing.Idx - candidate.Idx;
                if (distance > EqMaxBarsApart)
                    break;
                if (distance < EqMinBarsApart)
                    continue;
                if (Math.Abs(candidate.Price - swing.Price) > tolerance)
                    continue;

                double level = swing.High
                    ? Math.Max(candidate.Price, swing.Price)
                    : Math.Min(candidate.Price, swing.Price);

                bool clean = true;
                for (int bar = candidate.Idx + 1; bar < swing.Idx; bar++)
                {
                    if (swing.High ? H(bar) > level + tolerance : L(bar) < level - tolerance)
                    {
                        clean = false;
                        break;
                    }
                }

                if (!clean)
                    break;

                EqCluster cluster = candidate.Cluster;
                if (cluster != null && !cluster.Swept && !cluster.Broken)
                {
                    cluster.Count++;
                    cluster.LastIdx = swing.Idx;
                    cluster.LastTime = swing.Time;
                    cluster.Level = swing.High
                        ? Math.Max(cluster.Level, swing.Price)
                        : Math.Min(cluster.Level, swing.Price);
                    swing.Cluster = cluster;
                }
                else
                {
                    cluster = new EqCluster
                    {
                        High = swing.High,
                        Level = level,
                        Count = 2,
                        FirstIdx = candidate.Idx,
                        FirstTime = candidate.Time,
                        LastIdx = swing.Idx,
                        LastTime = swing.Time
                    };

                    candidate.Cluster = cluster;
                    swing.Cluster = cluster;
                    _eqs.Add(cluster);
                    _eqOpen.Add(cluster);
                }

                return;
            }
        }

        private void ProcessSweeps(int index, double atr)
        {
            double minPenetration = Math.Max(SweepMinPenPips * _pip, SweepMinPenAtr * atr);
            double minReturn = SweepMinReturnAtr * atr;
            double high = H(index);
            double low = L(index);
            double open = O(index);
            double close = C(index);

            if (SweepMajor)
                SweepSwings(_engMajor, index, high, low, open, close, minPenetration, minReturn);
            if (SweepInternal)
                SweepSwings(_engInt, index, high, low, open, close, minPenetration, minReturn);

            if (_eqOpen.Count == 0)
                return;

            for (int k = 0; k < _eqOpen.Count; k++)
            {
                EqCluster cluster = _eqOpen[k];
                if (cluster.Swept || cluster.Broken)
                    continue;

                if (cluster.High ? close > cluster.Level : close < cluster.Level)
                {
                    cluster.Broken = true;
                    cluster.EndIdx = index;
                    cluster.EndTime = T(index);
                    continue;
                }

                if (!SweepEqual)
                    continue;

                if (IsSweep(cluster.High, cluster.Level, high, low, open, close, minPenetration, minReturn))
                {
                    cluster.Swept = true;
                    cluster.EndIdx = index;
                    cluster.EndTime = T(index);
                    AddSweep(!cluster.High, cluster.Level, cluster.FirstTime, index);
                }
            }

            _eqOpen.RemoveAll(IsClosedCluster);
        }

        private void SweepSwings(
            Engine engine,
            int index,
            double high,
            double low,
            double open,
            double close,
            double minPenetration,
            double minReturn)
        {
            bool any = false;

            for (int k = 0; k < engine.Open.Count; k++)
            {
                Swing swing = engine.Open[k];
                if (swing.Broken || swing.Swept || swing.ConfIdx >= index)
                    continue;

                if (SweepEqual && swing.Cluster != null && !swing.Cluster.Swept && !swing.Cluster.Broken)
                    continue;

                if (!IsSweep(swing.High, swing.Price, high, low, open, close, minPenetration, minReturn))
                    continue;

                swing.Swept = true;
                any = true;
                AddSweep(!swing.High, swing.Price, swing.Time, index);
            }

            if (any)
                engine.Open.RemoveAll(IsClosedSwing);
        }

        private bool IsSweep(
            bool highLevel,
            double level,
            double high,
            double low,
            double open,
            double close,
            double minPenetration,
            double minReturn)
        {
            if (highLevel)
            {
                double extreme = SweepMeasure == SznpSweepMeasure.Wick ? high : Math.Max(open, close);
                double penetration = extreme - level;
                return penetration > 0
                    && penetration >= minPenetration
                    && close < level
                    && level - close >= minReturn;
            }

            double lowExtreme = SweepMeasure == SznpSweepMeasure.Wick ? low : Math.Min(open, close);
            double lowPenetration = level - lowExtreme;
            return lowPenetration > 0
                && lowPenetration >= minPenetration
                && close > level
                && close - level >= minReturn;
        }

        private void AddSweep(bool bull, double level, DateTime startTime, int index)
        {
            DateTime endTime = T(index);
            for (int k = _sweeps.Count - 1; k >= 0; k--)
            {
                SweepEv previous = _sweeps[k];
                if (previous.T2 != endTime)
                    break;

                if (previous.Bull == bull && Math.Abs(previous.Level - level) < _tick)
                    return;
            }

            _sweeps.Add(new SweepEv { Bull = bull, Level = level, T1 = startTime, T2 = endTime });
            if (bull)
                _lastSweepBull = index;
            else
                _lastSweepBear = index;
        }
    }
}