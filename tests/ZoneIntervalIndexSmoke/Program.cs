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

        private static int Main()
        {
            try
            {
                VerifyBoundaryAndMutationCases();
                VerifyRandomizedQueries();
                VerifyBreakModePredicates();
                VerifyLifecycleSequenceAndTouchCounts();
                VerifyInvalidLifecycleInput();
                VerifyBreakerFactory();

                Console.WriteLine("PASS: interval index boundary/mutation tests and "
                    + _queryCount + " seeded differential queries over 50,000 operations.");
                Console.WriteLine("PASS: bullish/bearish CandleClose, BodyBeyond and WickBeyond predicates.");
                Console.WriteLine("PASS: Fresh/Touched/Partial/Filled/Broken transitions and touch re-entry.");
                Console.WriteLine("PASS: breaker direction, parent identity, preserved bounds/factors and confirmation timestamp.");
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