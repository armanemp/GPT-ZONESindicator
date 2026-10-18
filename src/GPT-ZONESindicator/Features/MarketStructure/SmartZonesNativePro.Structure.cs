using System;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void ProcessStructure(int index, double atr)
        {
            ScanPivots(_marketStructureState.MajorEngine, index, atr);
            ScanPivots(_marketStructureState.InternalEngine, index, atr);
            CheckBreaks(_marketStructureState.MajorEngine, index, atr);
            CheckBreaks(_marketStructureState.InternalEngine, index, atr);
        }

        /// <summary>A pivot is confirmed only after all configured right-hand bars have closed.</summary>
        private void ScanPivots(Engine engine, int index, double atr)
        {
            int pivot = index - engine.Right;
            PivotEvaluation evaluation = PivotEvaluator.Evaluate(
                pivot,
                engine.Left,
                engine.Right,
                index,
                _startIdx,
                _highPriceAt,
                _lowPriceAt);

            if (!evaluation.IsReady)
                return;

            if (evaluation.IsHigh)
                AddSwing(engine, evaluation.PivotIndex, index, true, atr);
            if (evaluation.IsLow)
                AddSwing(engine, evaluation.PivotIndex, index, false, atr);
        }

        private void AddSwing(Engine engine, int pivot, int confirmedAt, bool high, double atr)
        {
            double price = high ? H(pivot) : L(pivot);
            Swing opposite = high ? engine.LastLow : engine.LastHigh;

            if (opposite != null)
            {
                double minDistance = Math.Max(engine.MinPips * _pip, engine.MinAtr * atr);
                if (minDistance > 0 && Math.Abs(price - opposite.Price) < minDistance)
                {
                    Reject(engine.Major ? "swing: major min distance" : "swing: internal min distance");
                    return;
                }
            }

            Swing swing = new Swing
            {
                Idx = pivot,
                ConfIdx = confirmedAt,
                Time = T(pivot),
                Price = price,
                High = high,
                Major = engine.Major
            };

            engine.Swings.Add(swing);
            engine.Open.Add(swing);
            if (high)
                engine.LastHigh = swing;
            else
                engine.LastLow = swing;

            if (DetectEqual && engine.Major == EqUseMajorSwings)
            {
                _equalLevelsState.PendingSwings.Add(new EqualLevelSignal(
                    engine.Major,
                    swing.High,
                    swing.Idx,
                    swing.ConfIdx,
                    swing.Time,
                    swing.Price,
                    atr));
            }
        }

        private void CheckBreaks(Engine engine, int index, double atr)
        {
            double close = C(index);
            bool any = false;

            for (int k = 0; k < engine.Open.Count; k++)
            {
                Swing swing = engine.Open[k];
                if (swing.Broken || swing.Swept)
                    continue;

                bool crossed = StructureBreakEvaluator.IsLevelCrossed(swing.High, close, swing.Price);
                if (!crossed)
                    continue;

                any = true;
                if ((swing.High && swing == engine.LastHigh) || (!swing.High && swing == engine.LastLow))
                {
                    RegisterBreak(engine, swing, index, atr);
                }
                else
                {
                    swing.Broken = true;
                    swing.SilentBreak = true;
                    swing.BreakIdx = index;
                    swing.BreakTime = T(index);
                }
            }

            if (any)
                engine.Open.RemoveAll(IsClosedSwing);
        }

        /// <summary>BOS continues the prior trend; CHoCH breaks against it.</summary>
        private void RegisterBreak(Engine engine, Swing swing, int index, double atr)
        {
            bool bullishBreak = swing.High;
            StructureBreakClassification classification =
                StructureBreakEvaluator.Classify(engine.Trend, bullishBreak);

            swing.Broken = true;
            swing.BreakIdx = index;
            swing.BreakTime = T(index);
            swing.Choch = classification.IsChangeOfCharacter;
            swing.BreakBull = bullishBreak;
            engine.Trend = classification.NextTrend;

            if (bullishBreak)
                _marketStructureState.LastBullBreak = index;
            else
                _marketStructureState.LastBearBreak = index;

            bool sourceAllowed = OrderBlockRules.IsSourceAllowed(ObSource, engine.Major);

            if (DetectOb && sourceAllowed)
            {
                _orderBlockState.PendingSignals.Add(new OrderBlockSignal(
                    engine.Major,
                    swing.High,
                    swing.Price,
                    atr,
                    swing.Idx,
                    index));
            }
        }

        private double StructFactorFor(bool bull, int index)
        {
            int breakIndex = bull ? _marketStructureState.LastBullBreak : _marketStructureState.LastBearBreak;
            if (breakIndex >= index - 2)
                return 1.0;

            int sweepIndex = bull ? _liquiditySweepState.LastBullSweep : _liquiditySweepState.LastBearSweep;
            if (sweepIndex >= index - 10)
                return 0.6;

            return 0.0;
        }
    }
}