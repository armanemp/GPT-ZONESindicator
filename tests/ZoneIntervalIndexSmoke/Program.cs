using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo;

namespace ZoneIntervalIndexSmoke
{
    internal sealed class Probe
    {
        public int Id;
        public double Lower;
        public double Upper;
    }

    internal static class Program
    {
        private static int _queryCount;

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static ZoneIntervalIndex<Probe> NewIndex()
        {
            return new ZoneIntervalIndex<Probe>(
                delegate (Probe item) { return item.Lower; },
                delegate (Probe item) { return item.Upper; },
                delegate (Probe item) { return item.Id; });
        }

        private static void VerifyBoundaryAndMutationCases()
        {
            ZoneIntervalIndex<Probe> index = NewIndex();
            List<Probe> actual = new List<Probe>();
            Probe first = new Probe { Id = 1, Lower = 1.0, Upper = 2.0 };
            Probe touching = new Probe { Id = 2, Lower = 2.0, Upper = 3.0 };
            Probe sameLower = new Probe { Id = 3, Lower = 1.0, Upper = 1.5 };
            Probe point = new Probe { Id = 4, Lower = 7.0, Upper = 7.0 };

            index.Add(first);
            index.Add(touching);
            index.Add(sameLower);
            index.Add(point);
            Assert(index.Count == 4, "Count must track insertions.");

            index.Query(2.0, 2.0, actual);
            Assert(actual.Select(x => x.Id).SequenceEqual(new[] { 1, 2 }),
                "Closed interval endpoints must count as intersections.");

            actual.Clear();
            index.Query(1.0, 1.5, actual);
            Assert(actual.Select(x => x.Id).SequenceEqual(new[] { 1, 3 }),
                "Equal lower bounds must have stable identity ordering.");

            actual.Clear();
            index.Query(7.0, 7.0, actual);
            Assert(actual.Select(x => x.Id).SequenceEqual(new[] { 4 }),
                "A zero-width query must find a matching point interval.");

            Assert(index.Remove(first), "An existing interval must be removed.");
            Assert(!index.Remove(first), "Removing the same interval twice must report false.");
            Assert(index.Count == 3, "Count must track removal.");

            actual.Clear();
            index.Query(2.0, 2.0, actual);
            Assert(actual.Select(x => x.Id).SequenceEqual(new[] { 2 }),
                "Removed intervals must no longer be returned.");

            actual.Clear();
            index.Query(4.0, 6.0, actual);
            Assert(actual.Count == 0, "Disjoint query must return no intervals.");

            actual.Clear();
            index.Query(5.0, 4.0, actual);
            Assert(actual.Count == 0, "Reversed query bounds must return no intervals.");

            index.Clear();
            Assert(index.Count == 0, "Clear must reset the index.");
            actual.Clear();
            index.Query(0.0, 10.0, actual);
            Assert(actual.Count == 0, "A cleared index must return no intervals.");

            bool rejected = false;
            try
            {
                index.Add(new Probe { Id = 9, Lower = 3.0, Upper = 2.0 });
            }
            catch (ArgumentException)
            {
                rejected = true;
            }
            Assert(rejected, "Invalid intervals must be rejected.");
        }

        private static void VerifyRandomizedQueries()
        {
            const int operations = 50000;
            const int maxLive = 4000;
            Random random = new Random(20261018);
            ZoneIntervalIndex<Probe> index = NewIndex();
            List<Probe> live = new List<Probe>();
            List<Probe> actual = new List<Probe>();
            int nextId = 1;

            for (int operation = 0; operation < operations; operation++)
            {
                int choice = random.Next(100);
                bool canAdd = live.Count < maxLive;

                if ((choice < 36 && canAdd) || live.Count < 3)
                {
                    double lower = random.Next(-10000, 10001) / 100.0;
                    double width = random.Next(0, 501) / 100.0;
                    Probe item = new Probe
                    {
                        Id = nextId++,
                        Lower = lower,
                        Upper = lower + width
                    };

                    index.Add(item);
                    live.Add(item);
                }
                else if (choice < 57)
                {
                    int position = random.Next(live.Count);
                    Probe item = live[position];
                    Assert(index.Remove(item), "Random removal failed for a live item.");
                    live.RemoveAt(position);
                }
                else
                {
                    double low;
                    double high;

                    // Regularly query exact interval boundaries to exercise inclusive
                    // overlap semantics, not only values between randomly chosen prices.
                    if (live.Count > 0 && operation % 7 == 0)
                    {
                        Probe anchor = live[random.Next(live.Count)];
                        if (operation % 2 == 0)
                        {
                            low = anchor.Lower;
                            high = anchor.Lower;
                        }
                        else
                        {
                            low = anchor.Upper;
                            high = anchor.Upper;
                        }
                    }
                    else
                    {
                        low = random.Next(-11000, 11001) / 100.0;
                        high = low + random.Next(0, 1001) / 100.0;
                    }

                    actual.Clear();
                    index.Query(low, high, actual);

                    List<Probe> expected = live
                        .Where(x => x.Lower <= high && x.Upper >= low)
                        .OrderBy(x => x.Lower)
                        .ThenBy(x => x.Id)
                        .ToList();

                    Assert(actual.Count == expected.Count,
                        "Query count mismatch at operation " + operation
                        + "; range=[" + low + ", " + high + "]"
                        + "; actual=" + actual.Count + "; expected=" + expected.Count);

                    for (int i = 0; i < expected.Count; i++)
                    {
                        Assert(actual[i].Id == expected[i].Id,
                            "Query membership/order mismatch at operation " + operation
                            + ", result index " + i);
                    }

                    _queryCount++;
                }

                Assert(index.Count == live.Count,
                    "Index Count disagrees with reference list at operation " + operation);
            }
        }

        private static ZoneLifecycleInput NewLifecycleInput(
            bool bull,
            SznpBreakMode mode,
            double open,
            double high,
            double low,
            double close)
        {
            return new ZoneLifecycleInput
            {
                Index = 1,
                Bull = bull,
                Lower = 100.0,
                Upper = 105.0,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                BreakMode = mode,
                State = SznpState.Fresh,
                MaxDepth = 0.0,
                TouchCount = 0,
                LastEnteredIndex = -1,
                PartialFillPct = 50.0,
                CountTouch = true
            };
        }

        private static ZoneLifecycleInput CarryForward(
            ZoneLifecycleInput previous,
            ZoneLifecycleResult result,
            int nextIndex)
        {
            previous.Index = nextIndex;
            previous.State = result.State;
            previous.MaxDepth = result.MaxDepth;
            previous.TouchCount = result.TouchCount;
            previous.LastEnteredIndex = result.LastEnteredIndex;
            return previous;
        }

        private static void AssertBreak(
            bool bull,
            SznpBreakMode mode,
            double open,
            double high,
            double low,
            double close,
            bool expected,
            string scenario)
        {
            ZoneLifecycleInput input = NewLifecycleInput(bull, mode, open, high, low, close);
            ZoneLifecycleResult result = ZoneLifecycleEvaluator.Evaluate(input);
            Assert(result.Processed, scenario + ": a valid active zone should be processed.");
            Assert(result.BrokeNow == expected,
                scenario + ": expected BrokeNow=" + expected + " but got " + result.BrokeNow + ".");
        }

        private static void VerifyBreakModePredicates()
        {
            // Bullish zone [100,105]: each mode has a boundary/equality case and a
            // beyond-boundary case. This also checks strict-vs-inclusive comparisons.
            AssertBreak(true, SznpBreakMode.CandleClose, 102, 103, 99, 101, false,
                "Bull CandleClose ignores a lower wick when close is above lower bound");
            AssertBreak(true, SznpBreakMode.CandleClose, 102, 103, 100, 100, false,
                "Bull CandleClose equality is not a break");
            AssertBreak(true, SznpBreakMode.CandleClose, 101, 102, 98, 99, true,
                "Bull CandleClose closes beyond lower bound");

            AssertBreak(true, SznpBreakMode.BodyBeyond, 99, 101, 98, 100, false,
                "Bull BodyBeyond equality is not a break");
            AssertBreak(true, SznpBreakMode.BodyBeyond, 99, 99.6, 98, 99.5, true,
                "Bull BodyBeyond body is strictly below lower bound");

            AssertBreak(true, SznpBreakMode.WickBeyond, 102, 103, 100, 101, false,
                "Bull WickBeyond equality is not a break");
            AssertBreak(true, SznpBreakMode.WickBeyond, 102, 103, 99.99, 101, true,
                "Bull WickBeyond lower wick crosses boundary");

            // Bearish zone [100,105].
            AssertBreak(false, SznpBreakMode.CandleClose, 104, 105, 103, 105, false,
                "Bear CandleClose equality is not a break");
            AssertBreak(false, SznpBreakMode.CandleClose, 104, 107, 104, 106, true,
                "Bear CandleClose closes beyond upper bound");

            AssertBreak(false, SznpBreakMode.BodyBeyond, 106, 107, 104, 105, false,
                "Bear BodyBeyond equality is not a break");
            AssertBreak(false, SznpBreakMode.BodyBeyond, 106, 107, 105.1, 105.1, true,
                "Bear BodyBeyond body is strictly above upper bound");

            AssertBreak(false, SznpBreakMode.WickBeyond, 104, 105, 103, 104.5, false,
                "Bear WickBeyond equality is not a break");
            AssertBreak(false, SznpBreakMode.WickBeyond, 104, 105.01, 103, 104.5, true,
                "Bear WickBeyond upper wick crosses boundary");
        }

        private static void VerifyLifecycleSequenceAndTouchCounts()
        {
            ZoneLifecycleInput input = NewLifecycleInput(
                true, SznpBreakMode.CandleClose, 104.5, 104.9, 104.0, 104.6);
            input.Index = 10;

            ZoneLifecycleResult result = ZoneLifecycleEvaluator.Evaluate(input);
            Assert(result.State == SznpState.Touched, "First shallow entry should mark zone Touched.");
            Assert(result.TouchCount == 1, "First entry should count as one touch.");
            Assert(Math.Abs(result.MaxDepth - 1.0) < 1e-9, "First entry should record depth 1.0.");

            input = CarryForward(input, result, 11);
            input.Open = 104.2;
            input.High = 104.7;
            input.Low = 103.8;
            input.Close = 104.1;
            result = ZoneLifecycleEvaluator.Evaluate(input);
            Assert(result.State == SznpState.Touched, "A second shallow bar should remain Touched.");
            Assert(result.TouchCount == 1, "Consecutive entry bars must count as one touch.");

            input = CarryForward(input, result, 12);
            input.Open = 105.4;
            input.High = 106.0;
            input.Low = 105.2;
            input.Close = 105.8;
            result = ZoneLifecycleEvaluator.Evaluate(input);
            Assert(result.State == SznpState.Touched, "An outside bar must preserve the current state.");
            Assert(result.TouchCount == 1, "Leaving the zone must not create a touch.");
            Assert(result.LastEnteredIndex == 11, "Outside bars must not overwrite the last-entry index.");

            input = CarryForward(input, result, 13);
            input.Open = 103.0;
            input.High = 104.0;
            input.Low = 102.0;
            input.Close = 103.5;
            result = ZoneLifecycleEvaluator.Evaluate(input);
            Assert(result.State == SznpState.Partial, "Re-entry past the threshold should mark Partial.");
            Assert(result.TouchCount == 2, "Re-entry after a full outside bar must count as a new touch.");
            Assert(Math.Abs(result.MaxDepth - 3.0) < 1e-9, "Maximum penetration should be retained.");

            input = CarryForward(input, result, 14);
            input.Open = 101.0;
            input.High = 102.0;
            input.Low = 99.5;
            input.Close = 101.5;
            result = ZoneLifecycleEvaluator.Evaluate(input);
            Assert(result.FilledNow && result.State == SznpState.Filled,
                "Crossing the far boundary without the configured close-break should fill the zone.");
            Assert(Math.Abs(result.MaxDepth - 5.0) < 1e-9, "Full fill should cap depth at zone size.");
            Assert(result.TouchCount == 2, "Consecutive fill bar must not add a touch.");

            input = CarryForward(input, result, 15);
            input.Open = 99.0;
            input.High = 101.0;
            input.Low = 98.0;
            input.Close = 99.0;
            result = ZoneLifecycleEvaluator.Evaluate(input);
            Assert(result.BrokeNow && result.State == SznpState.Broken,
                "A Filled zone must still become Broken when its break predicate occurs.");
            Assert(result.TouchCount == 2, "A break candle returns before touch counting.");
            Assert(result.LastEnteredIndex == 14, "A break candle must not rewrite last-entry index.");

            input = CarryForward(input, result, 16);
            input.Open = 101.0;
            input.High = 103.0;
            input.Low = 102.0;
            input.Close = 102.5;
            result = ZoneLifecycleEvaluator.Evaluate(input);
            Assert(!result.Processed && result.State == SznpState.Broken,
                "Broken zones are terminal and must not be processed again.");
        }

        private static void VerifyInvalidLifecycleInput()
        {
            ZoneLifecycleInput input = NewLifecycleInput(
                true, SznpBreakMode.CandleClose, 102, 103, 101, 102);
            input.Lower = 105;
            input.Upper = 100;
            input.TouchCount = 4;
            input.MaxDepth = 1.25;

            ZoneLifecycleResult result = ZoneLifecycleEvaluator.Evaluate(input);
            Assert(!result.Processed, "An invalid-size zone must not be processed.");
            Assert(result.State == input.State, "Invalid input must preserve lifecycle state.");
            Assert(result.TouchCount == 4 && Math.Abs(result.MaxDepth - 1.25) < 1e-9,
                "Invalid input must preserve touch count and depth.");
        }

        private static void VerifyBreakerFactory()
        {
            DateTime confirmedAt = new DateTime(2026, 10, 18, 12, 30, 0, DateTimeKind.Utc);
            ZoneBreakerInput input = new ZoneBreakerInput
            {
                ParentId = 42,
                ParentBull = true,
                Lower = 1.1000,
                Upper = 1.1050,
                OriginIndex = 5,
                ConfirmationIndex = 17,
                ConfirmationTime = confirmedAt,
                Atr = 0.0035,
                Momentum = 1.8,
                StructFactor = 0.75,
                Major = true
            };

            ZoneBreakerSpec spec = ZoneBreakerFactory.Create(input);
            Assert(spec.ParentId == 42, "Breaker spec must preserve parent zone identity.");
            Assert(!spec.Bull, "A breaker must reverse the parent zone direction.");
            Assert(spec.Lower == input.Lower && spec.Upper == input.Upper,
                "A breaker must preserve the parent's immutable price boundaries.");
            Assert(spec.OriginIndex == input.OriginIndex
                && spec.ConfirmationIndex == input.ConfirmationIndex,
                "Breaker origin and confirmation indices must be preserved.");
            Assert(spec.StartTime == confirmedAt,
                "Breaker start time must equal the closed-bar confirmation time.");
            Assert(spec.Atr == input.Atr && spec.Momentum == input.Momentum
                && spec.StructFactor == input.StructFactor && spec.Major == input.Major,
                "Breaker scoring/context inputs must be carried from the parent.");

            input.ParentBull = false;
            ZoneBreakerSpec bullishBreaker = ZoneBreakerFactory.Create(input);
            Assert(bullishBreaker.Bull,
                "Breaking a bearish order block must create a bullish breaker.");

            bool rejectedParent = false;
            try
            {
                input.ParentId = 0;
                ZoneBreakerFactory.Create(input);
            }
            catch (ArgumentOutOfRangeException)
            {
                rejectedParent = true;
            }
            Assert(rejectedParent, "Breaker specs must reject missing parent identity.");

            bool rejectedBounds = false;
            try
            {
                input.ParentId = 42;
                input.Lower = input.Upper;
                ZoneBreakerFactory.Create(input);
            }
            catch (ArgumentException)
            {
                rejectedBounds = true;
            }
            Assert(rejectedBounds, "Breaker specs must reject non-positive-width bounds.");
        }

        private static void VerifyStructureBreakClassification()
        {
            Assert(!StructureBreakEvaluator.IsLevelCrossed(true, 100.0, 100.0),
                "A bullish close exactly at a swing high is not a cross.");
            Assert(StructureBreakEvaluator.IsLevelCrossed(true, 100.01, 100.0),
                "A bullish close above swing high is a cross.");
            Assert(!StructureBreakEvaluator.IsLevelCrossed(true, 99.99, 100.0),
                "A close below swing high is not a bullish cross.");

            Assert(!StructureBreakEvaluator.IsLevelCrossed(false, 100.0, 100.0),
                "A bearish close exactly at a swing low is not a cross.");
            Assert(StructureBreakEvaluator.IsLevelCrossed(false, 99.99, 100.0),
                "A bearish close below swing low is a cross.");
            Assert(!StructureBreakEvaluator.IsLevelCrossed(false, 100.01, 100.0),
                "A close above swing low is not a bearish cross.");

            StructureBreakClassification neutralBull = StructureBreakEvaluator.Classify(0, true);
            Assert(!neutralBull.IsChangeOfCharacter && neutralBull.NextTrend == 1,
                "A first bullish break from neutral establishes bullish trend without CHoCH.");

            StructureBreakClassification bullContinuation = StructureBreakEvaluator.Classify(1, true);
            Assert(!bullContinuation.IsChangeOfCharacter && bullContinuation.NextTrend == 1,
                "A bullish break during bullish trend is continuation/BOS.");

            StructureBreakClassification bullReversal = StructureBreakEvaluator.Classify(-1, true);
            Assert(bullReversal.IsChangeOfCharacter && bullReversal.NextTrend == 1,
                "A bullish break against bearish trend is CHoCH and sets bullish trend.");

            StructureBreakClassification bearContinuation = StructureBreakEvaluator.Classify(-1, false);
            Assert(!bearContinuation.IsChangeOfCharacter && bearContinuation.NextTrend == -1,
                "A bearish break during bearish trend is continuation/BOS.");

            StructureBreakClassification bearReversal = StructureBreakEvaluator.Classify(1, false);
            Assert(bearReversal.IsChangeOfCharacter && bearReversal.NextTrend == -1,
                "A bearish break against bullish trend is CHoCH and sets bearish trend.");
            
            StructureBreakClassification neutralBear = StructureBreakEvaluator.Classify(0, false);
            Assert(!neutralBear.IsChangeOfCharacter && neutralBear.NextTrend == -1,
                "A first bearish break from neutral establishes bearish trend without CHoCH.");
        }

        private static bool Sweep(
            bool highLevel,
            double level,
            double high,
            double low,
            double open,
            double close,
            double minPenetration,
            double minReturn,
            SznpSweepMeasure measure)
        {
            return LiquiditySweepEvaluator.IsSweep(
                highLevel, level, high, low, open, close,
                minPenetration, minReturn, measure);
        }

        private static void VerifyLiquiditySweepPredicates()
        {
            // High-liquidity sweep: price pierces above the level then closes back below.
            Assert(Sweep(true, 100.0, 101.2, 99.2, 99.5, 99.4, 0.5, 0.3, SznpSweepMeasure.Wick),
                "Wick-measured high sweep should qualify.");
            Assert(!Sweep(true, 100.0, 101.2, 99.2, 99.5, 99.4, 0.5, 0.3, SznpSweepMeasure.Body),
                "A wick-only excursion must not qualify in body-measure mode.");
            Assert(Sweep(true, 100.0, 101.0, 99.0, 100.8, 99.4, 0.5, 0.3, SznpSweepMeasure.Body),
                "Body-measured high sweep should qualify when the body pierced the level.");
            Assert(Sweep(true, 100.0, 100.5, 99.0, 99.4, 99.5, 0.5, 0.5, SznpSweepMeasure.Wick),
                "Exact minimum penetration and return thresholds should qualify.");
            Assert(!Sweep(true, 100.0, 100.49, 99.0, 99.4, 99.5, 0.5, 0.1, SznpSweepMeasure.Wick),
                "Penetration below the minimum must be rejected.");
            Assert(!Sweep(true, 100.0, 101.0, 99.0, 99.5, 99.9, 0.5, 0.2, SznpSweepMeasure.Wick),
                "Insufficient close-back return must be rejected.");
            Assert(!Sweep(true, 100.0, 101.0, 99.0, 99.5, 100.0, 0.5, 0.0, SznpSweepMeasure.Wick),
                "A candle that does not close below a high level is not a sweep.");
            Assert(!Sweep(true, 100.0, 100.0, 99.0, 99.5, 99.4, 0.0, 0.0, SznpSweepMeasure.Wick),
                "Zero penetration must fail even when configured minimum is zero.");

            // Low-liquidity sweep is the mirrored predicate.
            Assert(Sweep(false, 100.0, 101.0, 98.8, 100.5, 100.7, 0.5, 0.3, SznpSweepMeasure.Wick),
                "Wick-measured low sweep should qualify.");
            Assert(!Sweep(false, 100.0, 101.0, 98.8, 100.5, 100.7, 0.5, 0.3, SznpSweepMeasure.Body),
                "A wick-only lower excursion must not qualify in body-measure mode.");
            Assert(Sweep(false, 100.0, 101.0, 99.0, 99.2, 100.7, 0.5, 0.3, SznpSweepMeasure.Body),
                "Body-measured low sweep should qualify when the body pierced the level.");
            Assert(Sweep(false, 100.0, 101.0, 99.5, 100.5, 100.5, 0.5, 0.5, SznpSweepMeasure.Wick),
                "Exact low-sweep penetration and return thresholds should qualify.");
            Assert(!Sweep(false, 100.0, 101.0, 99.51, 100.5, 100.7, 0.5, 0.1, SznpSweepMeasure.Wick),
                "Low-sweep penetration below the minimum must be rejected.");
            Assert(!Sweep(false, 100.0, 101.0, 98.8, 100.5, 100.1, 0.5, 0.2, SznpSweepMeasure.Wick),
                "Insufficient close-above return must be rejected.");
            Assert(!Sweep(false, 100.0, 101.0, 98.8, 100.5, 100.0, 0.5, 0.0, SznpSweepMeasure.Wick),
                "A candle that does not close above a low level is not a sweep.");
            Assert(!Sweep(false, 100.0, 101.0, 100.0, 100.5, 100.7, 0.0, 0.0, SznpSweepMeasure.Wick),
                "Zero lower penetration must fail even when configured minimum is zero.");
        }


        private sealed class ReplayBar
        {
            public double Open, High, Low, Close;
        }

        private sealed class ReplayZone
        {
            public int Id, Kind, ConfirmIdx;
            public bool Bull, Indexed;
            public double Lower, Upper, MaxDepth;
            public SznpState State = SznpState.Fresh;
            public int TouchCount, LastEnteredIndex = -1;
            public int FillIdx = -1, BreakIdx = -1;
        }

        private sealed class ReplayZoneSeed
        {
            public int Id, Kind, ConfirmIdx;
            public bool Bull;
            public double Lower, Upper;
        }

        private static ZoneIntervalIndex<ReplayZone> NewReplayIntervalIndex()
        {
            return new ZoneIntervalIndex<ReplayZone>(
                delegate (ReplayZone zone) { return zone.Lower; },
                delegate (ReplayZone zone) { return zone.Upper; },
                delegate (ReplayZone zone) { return zone.Id; });
        }

        private static ZoneBreakCandidateIndex<ReplayZone> NewReplayBreakIndex()
        {
            return new ZoneBreakCandidateIndex<ReplayZone>(
                delegate (ReplayZone zone) { return zone.Lower; },
                delegate (ReplayZone zone) { return zone.Upper; },
                delegate (ReplayZone zone) { return zone.Id; },
                delegate (ReplayZone zone) { return zone.ConfirmIdx; },
                delegate (bool lowerBound, double price, int id)
                {
                    return lowerBound
                        ? new ReplayZone { Lower = price, Id = id }
                        : new ReplayZone { Upper = price, Id = id };
                });
        }

        private static ReplayZone NewReplayZone(ReplayZoneSeed seed)
        {
            return new ReplayZone
            {
                Id = seed.Id,
                Kind = seed.Kind,
                ConfirmIdx = seed.ConfirmIdx,
                Bull = seed.Bull,
                Lower = seed.Lower,
                Upper = seed.Upper
            };
        }

        private static ZoneLifecycleInput ReplayInput(
            ReplayZone zone, int index, ReplayBar bar, SznpBreakMode[] modes)
        {
            return new ZoneLifecycleInput
            {
                Index = index,
                Bull = zone.Bull,
                Lower = zone.Lower,
                Upper = zone.Upper,
                Open = bar.Open,
                High = bar.High,
                Low = bar.Low,
                Close = bar.Close,
                BreakMode = modes[zone.Kind],
                State = zone.State,
                MaxDepth = zone.MaxDepth,
                TouchCount = zone.TouchCount,
                LastEnteredIndex = zone.LastEnteredIndex,
                PartialFillPct = 50.0,
                CountTouch = true
            };
        }

        private static ZoneLifecycleResult EvaluateReplayZone(
            ReplayZone zone, int index, ReplayBar bar, SznpBreakMode[] modes)
        {
            return ZoneLifecycleEvaluator.Evaluate(ReplayInput(zone, index, bar, modes));
        }

        private static void ApplyReplayResult(ReplayZone zone, int index, ZoneLifecycleResult result)
        {
            if (!result.Processed)
                return;

            zone.MaxDepth = result.MaxDepth;
            zone.TouchCount = result.TouchCount;
            zone.LastEnteredIndex = result.LastEnteredIndex;
            zone.State = result.State;

            if (result.BrokeNow)
            {
                zone.BreakIdx = index;
                if (zone.FillIdx < 0)
                    zone.FillIdx = index;
            }

            if (result.FilledNow)
                zone.FillIdx = index;
        }

        private static void RegisterReplayZone(
            ReplayZone zone,
            ZoneIntervalIndex<ReplayZone> intervals,
            ZoneBreakCandidateIndex<ReplayZone> breaks)
        {
            intervals.Add(zone);
            breaks.Add(zone, zone.Kind, zone.Bull);
            zone.Indexed = true;
        }

        private static void UnregisterReplayZone(
            ReplayZone zone,
            ZoneIntervalIndex<ReplayZone> intervals,
            ZoneBreakCandidateIndex<ReplayZone> breaks)
        {
            intervals.Remove(zone);
            breaks.Remove(zone, zone.Kind, zone.Bull);
            zone.Indexed = false;
        }

        private static void ProcessReferenceReplayBar(
            List<ReplayZone> zones, int index, ReplayBar bar, SznpBreakMode[] modes)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                ReplayZone zone = zones[i];
                if (zone.ConfirmIdx >= index || zone.State == SznpState.Broken)
                    continue;

                ZoneLifecycleResult result = EvaluateReplayZone(zone, index, bar, modes);
                ApplyReplayResult(zone, index, result);
            }
        }

        private static void ProcessIndexedReplayBar(
            int index,
            ReplayBar bar,
            SznpBreakMode[] modes,
            List<ReplayZone> zones,
            ZoneIntervalIndex<ReplayZone> intervals,
            ZoneBreakCandidateIndex<ReplayZone> breaks,
            List<ReplayZone> breakScratch,
            List<ReplayZone> overlapScratch)
        {
            breaks.QueryCandidates(
                index, bar.Open, bar.High, bar.Low, bar.Close, modes, breakScratch);

            for (int i = 0; i < breakScratch.Count; i++)
            {
                ReplayZone zone = breakScratch[i];
                if (!zone.Indexed || zone.ConfirmIdx >= index || zone.State == SznpState.Broken)
                    continue;

                ZoneLifecycleResult result = EvaluateReplayZone(zone, index, bar, modes);
                Assert(result.BrokeNow,
                    "The break index returned a non-breaking candidate at bar " + index
                    + ", zone " + zone.Id + ".");
                ApplyReplayResult(zone, index, result);
                UnregisterReplayZone(zone, intervals, breaks);
            }

            overlapScratch.Clear();
            intervals.Query(bar.Low, bar.High, overlapScratch);
            for (int i = 0; i < overlapScratch.Count; i++)
            {
                ReplayZone zone = overlapScratch[i];
                if (!zone.Indexed || zone.ConfirmIdx >= index || zone.State == SznpState.Broken)
                    continue;

                ZoneLifecycleResult result = EvaluateReplayZone(zone, index, bar, modes);
                ApplyReplayResult(zone, index, result);
                if (result.BrokeNow)
                    UnregisterReplayZone(zone, intervals, breaks);
            }
        }

        private static void AssertReplayStateEqual(
            List<ReplayZone> reference, List<ReplayZone> indexed, int index)
        {
            for (int i = 0; i < reference.Count; i++)
            {
                ReplayZone expected = reference[i];
                ReplayZone actual = indexed[i];
                string where = "bar " + index + ", zone " + expected.Id + ": ";

                Assert(expected.Id == actual.Id, where + "identity mismatch.");
                Assert(expected.State == actual.State,
                    where + "state mismatch (" + expected.State + " vs " + actual.State + ").");
                Assert(Math.Abs(expected.MaxDepth - actual.MaxDepth) < 1e-9,
                    where + "maximum penetration mismatch.");
                Assert(expected.TouchCount == actual.TouchCount, where + "touch count mismatch.");
                Assert(expected.LastEnteredIndex == actual.LastEnteredIndex,
                    where + "last-entered index mismatch.");
                Assert(expected.FillIdx == actual.FillIdx, where + "fill index mismatch.");
                Assert(expected.BreakIdx == actual.BreakIdx, where + "break index mismatch.");

                bool shouldBeIndexed = expected.ConfirmIdx <= index
                    && expected.State != SznpState.Broken;
                Assert(actual.Indexed == shouldBeIndexed,
                    where + "active-index membership mismatch.");
            }
        }

        private static ReplayBar[] BuildReplayBars(int count, int seed)
        {
            Random random = new Random(seed);
            ReplayBar[] bars = new ReplayBar[count];

            bars[0] = new ReplayBar { Open = 109, High = 111, Low = 108, Close = 110 };
            bars[1] = new ReplayBar { Open = 90, High = 92, Low = 89, Close = 91 };
            bars[2] = new ReplayBar { Open = 120, High = 122, Low = 119, Close = 121 };
            bars[3] = new ReplayBar { Open = 108, High = 110, Low = 107, Close = 109 };
            bars[4] = new ReplayBar { Open = 104.8, High = 106, Low = 104, Close = 105.2 };
            bars[5] = new ReplayBar { Open = 106.5, High = 108, Low = 106, Close = 107 };
            bars[6] = new ReplayBar { Open = 104, High = 106, Low = 102.5, Close = 104 };

            double priorClose = bars[6].Close;
            for (int i = 7; i < count; i++)
            {
                double open;
                if (i % 37 == 0)
                {
                    double sign = (i / 37) % 2 == 0 ? 1.0 : -1.0;
                    open = priorClose + sign * (5.0 + random.NextDouble() * 10.0);
                }
                else
                {
                    open = priorClose + (random.NextDouble() - 0.5) * 0.8;
                }

                double close = open + (random.NextDouble() - 0.5) * 2.2;
                double high = Math.Max(open, close) + random.NextDouble() * 1.5;
                double low = Math.Min(open, close) - random.NextDouble() * 1.5;
                bars[i] = new ReplayBar { Open = open, High = high, Low = low, Close = close };
                priorClose = close;
            }

            return bars;
        }

        private static List<ReplayZoneSeed> BuildReplaySeeds(ReplayBar[] bars, int count, int seed)
        {
            List<ReplayZoneSeed> seeds = new List<ReplayZoneSeed>();
            int id = 1;

            // Crafted cases: a downward gap completely below bullish zones, an upward gap
            // completely above bearish zones, and a touch -> exit -> re-entry lifecycle.
            for (int kind = 0; kind < 4; kind++)
            {
                seeds.Add(new ReplayZoneSeed
                {
                    Id = id++, Kind = kind, Bull = true,
                    Lower = 100.0, Upper = 101.0, ConfirmIdx = 0
                });
                seeds.Add(new ReplayZoneSeed
                {
                    Id = id++, Kind = kind, Bull = false,
                    Lower = 105.0, Upper = 106.0, ConfirmIdx = 0
                });
            }

            seeds.Add(new ReplayZoneSeed
            {
                Id = id++, Kind = 0, Bull = true,
                Lower = 100.0, Upper = 105.0, ConfirmIdx = 3
            });

            Random random = new Random(seed);
            for (int i = 0; i < count; i++)
            {
                int confirm = random.Next(0, bars.Length - 1);
                double center = bars[confirm].Close + (random.NextDouble() - 0.5) * 20.0;
                double width = 0.05 + random.NextDouble() * 3.0;
                seeds.Add(new ReplayZoneSeed
                {
                    Id = id++,
                    Kind = i % 4,
                    Bull = random.Next(2) == 0,
                    Lower = center,
                    Upper = center + width,
                    ConfirmIdx = confirm
                });
            }

            return seeds;
        }

        private static void VerifyIndexedLifecycleAgainstReferenceReplay()
        {
            const int barCount = 1200;
            const int randomZoneCount = 700;
            ReplayBar[] bars = BuildReplayBars(barCount, 20261018);
            List<ReplayZoneSeed> seeds = BuildReplaySeeds(bars, randomZoneCount, 112358);
            List<ReplayZone> reference = seeds.Select(NewReplayZone).ToList();
            List<ReplayZone> indexed = seeds.Select(NewReplayZone).ToList();
            SznpBreakMode[] modes =
            {
                SznpBreakMode.CandleClose,
                SznpBreakMode.BodyBeyond,
                SznpBreakMode.WickBeyond,
                SznpBreakMode.CandleClose
            };

            ZoneIntervalIndex<ReplayZone> intervals = NewReplayIntervalIndex();
            ZoneBreakCandidateIndex<ReplayZone> breaks = NewReplayBreakIndex();
            List<ReplayZone> breakScratch = new List<ReplayZone>();
            List<ReplayZone> overlapScratch = new List<ReplayZone>();

            for (int i = 0; i < indexed.Count; i++)
            {
                if (indexed[i].ConfirmIdx < 1)
                    RegisterReplayZone(indexed[i], intervals, breaks);
            }

            for (int barIndex = 1; barIndex < bars.Length; barIndex++)
            {
                ProcessReferenceReplayBar(reference, barIndex, bars[barIndex], modes);
                ProcessIndexedReplayBar(
                    barIndex, bars[barIndex], modes, indexed,
                    intervals, breaks, breakScratch, overlapScratch);

                // Production registers newly discovered zones after processing their confirmation bar.
                for (int i = 0; i < indexed.Count; i++)
                {
                    ReplayZone zone = indexed[i];
                    if (!zone.Indexed && zone.ConfirmIdx == barIndex
                        && zone.State != SznpState.Broken)
                    {
                        RegisterReplayZone(zone, intervals, breaks);
                    }
                }

                AssertReplayStateEqual(reference, indexed, barIndex);

                if (barIndex == 6)
                {
                    Assert(reference[8].State == SznpState.Partial && reference[8].TouchCount == 2,
                        "Crafted touch/exit/re-entry scenario must reach Partial with two touches at bar 6.");
                }
            }
            for (int i = 0; i < 4; i++)
            {
                Assert(reference[i].State == SznpState.Broken,
                    "A downward price gap must break bullish zones without candle overlap.");
                Assert(reference[4 + i].State == SznpState.Broken,
                    "An upward price gap must break bearish zones without candle overlap.");
            }

            Console.WriteLine("PASS: indexed lifecycle matched exhaustive replay for "
                + barCount + " bars and " + seeds.Count
                + " zones across CandleClose, BodyBeyond, WickBeyond, gap-crossing and touch re-entry cases.");
        }

        private static int Main()
        {
            try
            {
                VerifyBoundaryAndMutationCases();
                VerifyIndexedLifecycleAgainstReferenceReplay();
                VerifyRandomizedQueries();
                VerifyBreakModePredicates();
                VerifyLifecycleSequenceAndTouchCounts();
                VerifyInvalidLifecycleInput();
                VerifyBreakerFactory();
                VerifyStructureBreakClassification();
                VerifyLiquiditySweepPredicates();

                Console.WriteLine("PASS: interval index boundary/mutation tests and "
                    + _queryCount + " seeded differential queries over 50,000 operations.");
                Console.WriteLine("PASS: bullish/bearish CandleClose, BodyBeyond and WickBeyond predicates.");
                Console.WriteLine("PASS: Fresh/Touched/Partial/Filled/Broken transitions and touch re-entry.");
                Console.WriteLine("PASS: breaker direction, parent identity, preserved bounds/factors and confirmation timestamp.");
                Console.WriteLine("PASS: BOS/CHoCH classification and strict swing-cross boundaries.");
                Console.WriteLine("PASS: high/low liquidity sweep predicates for Wick/Body measure and thresholds.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("FAIL: " + exception);
                return 1;
            }
        }
    }
}