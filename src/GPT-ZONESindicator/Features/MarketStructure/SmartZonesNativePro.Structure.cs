using System;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void ProcessMajorStructure(int index, double atr)
        {
            ProcessStructureEngine(_marketStructureState.MajorEngine, index, atr);
        }

        private void ProcessInternalStructure(int index, double atr)
        {
            ProcessStructureEngine(_marketStructureState.InternalEngine, index, atr);
        }

        private void ProcessStructureEngine(Engine engine, int index, double atr)
        {
            ScanPivots(engine, index, atr);
            CheckBreaks(engine, index, atr);
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

            // Both pivot candidates are evaluated against the engine state from before
            // this candle. Adding the high first must not change the low candidate's filter.
            Swing previousLastHigh = engine.LastHigh;
            Swing previousLastLow = engine.LastLow;
            bool isHigh = evaluation.IsHigh;
            bool isLow = evaluation.IsLow;
            double minimumDistance = Math.Max(engine.MinPips * _pip, engine.MinAtr * atr);

            if (StructureSwingDistanceEvaluator.RejectDualPivotPair(
                    isHigh, isLow, H(evaluation.PivotIndex), L(evaluation.PivotIndex), minimumDistance))
            {
                Reject(engine.Major
                    ? "swing: major dual-pivot minimum distance"
                    : "swing: internal dual-pivot minimum distance");
                return;
            }

            if (isHigh)
                AddSwing(engine, evaluation.PivotIndex, index, true, atr, previousLastLow);
            if (isLow)
                AddSwing(engine, evaluation.PivotIndex, index, false, atr, previousLastHigh);
        }

        private void AddSwing(
            Engine engine,
            int pivot,
            int confirmedAt,
            bool high,
            double atr,
            Swing opposite)
        {
            double price = high ? H(pivot) : L(pivot);
            double minimumDistance = Math.Max(engine.MinPips * _pip, engine.MinAtr * atr);

            if (!StructureSwingDistanceEvaluator.IsFarEnough(
                    price,
                    opposite != null,
                    opposite == null ? 0 : opposite.Price,
                    minimumDistance))
            {
                Reject(engine.Major ? "swing: major min distance" : "swing: internal min distance");
                return;
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