using System;
using System.Collections.Generic;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void ProcessEqualLevelSignals(int throughIndex)
        {
            List<EqualLevelSignal> pending = _equalLevelsState.PendingSwings;
            int i = 0;
            while (i < pending.Count)
            {
                EqualLevelSignal signal = pending[i];
                if (signal.ConfirmedIndex > throughIndex)
                {
                    i++;
                    continue;
                }

                pending.RemoveAt(i);
                if (!DetectEqual)
                    continue;

                Engine engine = signal.Major
                    ? _marketStructureState.MajorEngine
                    : _marketStructureState.InternalEngine;
                Swing swing = null;
                for (int k = engine.Swings.Count - 1; k >= 0; k--)
                {
                    Swing candidate = engine.Swings[k];
                    if (candidate.Idx == signal.Index && candidate.High == signal.High)
                    {
                        swing = candidate;
                        break;
                    }
                }

                if (swing == null)
                    continue;

                try
                {
                    CheckEqual(engine, swing, signal.Atr);
                }
                catch (Exception ex)
                {
                    LogError("Equal level signal at swing " + signal.Index, ex);
                }
            }
        }

        private void CheckEqual(Engine engine, Swing swing, double atr)
        {
            double tolerance = EqToleranceMode == SznpToleranceMode.Atr
                ? EqTolAtr * atr
                : EqTolPips * _pip;

            if (tolerance <= 0)
                tolerance = _tick;

            List<Swing> swings = engine.Swings;
            for (int k = swings.Count - 1; k >= 0; k--)
            {
                Swing candidate = swings[k];
                // Candidate search is based on the pivot's chronological position,
                // not list position: high and low pivots can be confirmed together.
                if (candidate == swing || candidate.Idx >= swing.Idx)
                    continue;

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
                EqCluster cluster;
                bool hasCluster = _equalLevelsState.ClusterBySwing.TryGetValue(candidate, out cluster);
                if (hasCluster && cluster != null && !cluster.Swept && !cluster.Broken)
                {
                    cluster.Count++;
                    cluster.LastIdx = swing.Idx;
                    cluster.LastTime = swing.Time;
                    cluster.Level = swing.High
                        ? Math.Max(cluster.Level, swing.Price)
                        : Math.Min(cluster.Level, swing.Price);
                    _equalLevelsState.ClusterBySwing[swing] = cluster;
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

                    _equalLevelsState.ClusterBySwing[candidate] = cluster;
                    _equalLevelsState.ClusterBySwing[swing] = cluster;
                    _equalLevelsState.Clusters.Add(cluster);
                    _equalLevelsState.OpenClusters.Add(cluster);
                }

                return;
            }
        }
    }
}
