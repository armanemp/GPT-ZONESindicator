using System;
using System.Collections.Generic;

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
                EqualLevelPairEvaluation pair = EqualLevelEvaluator.Evaluate(
                    candidate.High,
                    candidate.Idx,
                    candidate.Price,
                    swing.High,
                    swing.Idx,
                    swing.Price,
                    EqMinBarsApart,
                    EqMaxBarsApart,
                    tolerance,
                    _highPriceAt,
                    _lowPriceAt);

                if (pair.StopSearch)
                    break;
                if (!pair.Accepted)
                    continue;

                double level = pair.Level;

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
    }
}
