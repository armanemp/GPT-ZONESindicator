using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void ProcessSweeps(int index, double atr)
        {
            double minPenetration = Math.Max(SweepMinPenPips * _pip, SweepMinPenAtr * atr);
            double minReturn = SweepMinReturnAtr * atr;
            double high = H(index);
            double low = L(index);
            double open = O(index);
            double close = C(index);

            if (SweepMajor)
                SweepSwings(_marketStructureState.MajorEngine, index, high, low, open, close, minPenetration, minReturn);
            if (SweepInternal)
                SweepSwings(_marketStructureState.InternalEngine, index, high, low, open, close, minPenetration, minReturn);

            if (_equalLevelsState.OpenClusters.Count == 0)
                return;

            for (int k = 0; k < _equalLevelsState.OpenClusters.Count; k++)
            {
                EqCluster cluster = _equalLevelsState.OpenClusters[k];
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

            _equalLevelsState.OpenClusters.RemoveAll(IsClosedCluster);
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
            return LiquiditySweepEvaluator.IsSweep(
                highLevel,
                level,
                high,
                low,
                open,
                close,
                minPenetration,
                minReturn,
                SweepMeasure);
        }

        private void AddSweep(bool bull, double level, DateTime startTime, int index)
        {
            DateTime endTime = T(index);
            for (int k = _liquiditySweepState.Events.Count - 1; k >= 0; k--)
            {
                SweepEv previous = _liquiditySweepState.Events[k];
                if (previous.T2 != endTime)
                    break;

                if (LiquiditySweepDuplicateEvaluator.IsDuplicate(
                    bull, level, endTime,
                    previous.Bull, previous.Level, previous.T2,
                    _tick))
                    return;
            }

            _liquiditySweepState.Events.Add(new SweepEv { Bull = bull, Level = level, T1 = startTime, T2 = endTime });
            if (bull)
                _liquiditySweepState.LastBullSweep = index;
            else
                _liquiditySweepState.LastBearSweep = index;
        }
    }
}
