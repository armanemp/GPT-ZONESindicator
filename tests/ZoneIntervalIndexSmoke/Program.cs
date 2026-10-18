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
                ParentKind = SznpKind.OrderBlock,
                ParentState = SznpState.Broken,
                ExistingChildId = -1,
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
            input.Lower = 1.1000;
            input.Upper = 1.1050;

            bool rejectedNonOrderBlock = false;
            try
            {
                input.ParentKind = SznpKind.Fvg;
                ZoneBreakerFactory.Create(input);
            }
            catch (InvalidOperationException)
            {
                rejectedNonOrderBlock = true;
            }
            Assert(rejectedNonOrderBlock, "Only a broken order block may create a breaker.");
            input.ParentKind = SznpKind.OrderBlock;

            bool rejectedUnbroken = false;
            try
            {
                input.ParentState = SznpState.Fresh;
                ZoneBreakerFactory.Create(input);
            }
            catch (InvalidOperationException)
            {
                rejectedUnbroken = true;
            }
            Assert(rejectedUnbroken, "An unbroken order block must not create a breaker.");
            input.ParentState = SznpState.Broken;

            bool rejectedDuplicate = false;
            try
            {
                input.ExistingChildId = 43;
                ZoneBreakerFactory.Create(input);
            }
            catch (InvalidOperationException)
            {
                rejectedDuplicate = true;
            }
            Assert(rejectedDuplicate, "A parent order block must not create more than one breaker.");
            input.ExistingChildId = -1;

            bool rejectedTimeline = false;
            try
            {
                input.ConfirmationIndex = input.OriginIndex;
                ZoneBreakerFactory.Create(input);
            }
            catch (ArgumentException)
            {
                rejectedTimeline = true;
            }
            Assert(rejectedTimeline, "A breaker must be confirmed after its order-block origin.");
            input.ConfirmationIndex = 17;

            bool rejectedAtr = false;
            try
            {
                input.Atr = double.NaN;
                ZoneBreakerFactory.Create(input);
            }
            catch (ArgumentException)
            {
                rejectedAtr = true;
            }
            Assert(rejectedAtr, "Breaker scoring context must reject invalid ATR values.");
            input.Atr = 0.0035;
        }



        private static void VerifyFutureBarTime()
        {
            TimeSpan fiveMinutes = TimeSpan.FromMinutes(5);
            TimeSpan regularFortyBars = FutureBarTimeEvaluator.GetFortyBarSpan(fiveMinutes);
            Assert(regularFortyBars == TimeSpan.FromMinutes(200),
                "Forty M5 bar intervals must extend the zone by exactly 200 nominal minutes.");

            DateTime lastBarOpen = new DateTime(2026, 10, 18, 18, 0, 0);
            Assert(FutureBarTimeEvaluator.GetFortyBarEnd(lastBarOpen, fiveMinutes)
                    == lastBarOpen.AddMinutes(200),
                "The visual right edge must be exactly forty M5 bar intervals after the latest candle.");

            // A large historical span can contain weekend/session gaps. It must not
            // change this forty-nominal-candle future margin.
            TimeSpan afterSessionGap = FutureBarTimeEvaluator.GetFortyBarSpan(fiveMinutes);
            Assert(afterSessionGap == TimeSpan.FromMinutes(200),
                "Historical session gaps must not expand the future margin beyond forty nominal candles.");

            TimeSpan minimal = FutureBarTimeEvaluator.GetFortyBarSpan(TimeSpan.Zero);
            Assert(minimal == TimeSpan.FromTicks(40),
                "A missing nominal interval must remain safe and positive.");

            Console.WriteLine("PASS: zone, line and label projection uses an exact forty-bar future endpoint.");
        }


        private static void VerifyHigherTimeframeTiming()
        {
            DateTime[] m5OpenTimes =
            {
                new DateTime(2026, 10, 19, 9, 0, 0),
                new DateTime(2026, 10, 19, 9, 5, 0),
                new DateTime(2026, 10, 19, 9, 10, 0),
                new DateTime(2026, 10, 19, 9, 15, 0),
                new DateTime(2026, 10, 19, 9, 20, 0)
            };

            DateTime m15Close = new DateTime(2026, 10, 19, 9, 15, 0);
            Assert(HigherTimeframeTimingEvaluator.IsClosed(m15Close, m15Close),
                "An M15 candle should be eligible exactly when the matching M5 close is reached.");
            Assert(!HigherTimeframeTimingEvaluator.IsClosed(
                    m15Close, m15Close.AddTicks(-1)),
                "An M15 candle must not be processed even a tick before its close.");
            Assert(HigherTimeframeTimingEvaluator.FindClosedIndex(
                    m5OpenTimes.Length, m15Close,
                    delegate (int index) { return m5OpenTimes[index].AddMinutes(5); }) == 2,
                "M15 confirmation at 09:15 must map to the M5 candle that just closed at 09:15.");
            Assert(HigherTimeframeTimingEvaluator.FindClosedIndex(
                    m5OpenTimes.Length, m15Close.AddTicks(-1),
                    delegate (int index) { return m5OpenTimes[index].AddMinutes(5); }) == 1,
                "Before 09:15, the index mapping must stop at the prior closed M5 candle.");

            Console.WriteLine("PASS: M15 confirmation waits for a closed candle and maps to the exact M5 close.");
        }


        private static void VerifyZoneVisibilityPolicy()
        {
            Assert(ZoneVisibilityPolicy.ShouldRender(SznpState.Fresh, false, false),
                "Fresh zones should render regardless of touched/filled display switches.");
            Assert(!ZoneVisibilityPolicy.ShouldRender(SznpState.Touched, false, false),
                "Touched zones should respect the touched-zone display switch.");
            Assert(ZoneVisibilityPolicy.ShouldRender(SznpState.Touched, true, false),
                "Touched zones should render when enabled.");
            Assert(!ZoneVisibilityPolicy.ShouldRender(SznpState.Partial, false, false),
                "Partially filled zones should respect the touched-zone display switch.");
            Assert(ZoneVisibilityPolicy.ShouldRender(SznpState.Partial, true, false),
                "Partially filled zones should render when enabled.");
            Assert(!ZoneVisibilityPolicy.ShouldRender(SznpState.Filled, true, false),
                "Filled zones should remain hidden unless filled-zone history is enabled.");
            Assert(ZoneVisibilityPolicy.ShouldRender(SznpState.Filled, true, true),
                "Filled zones should render when filled-zone history is enabled.");

            Assert(!ZoneVisibilityPolicy.ShouldRender(SznpState.Broken, false, false),
                "Broken zones must never render when history toggles are off.");
            Assert(!ZoneVisibilityPolicy.ShouldRender(SznpState.Broken, true, false),
                "Broken zones must never render when touched zones are enabled.");
            Assert(!ZoneVisibilityPolicy.ShouldRender(SznpState.Broken, false, true),
                "Broken zones must never render when filled history is enabled.");
            Assert(!ZoneVisibilityPolicy.ShouldRender(SznpState.Broken, true, true),
                "Broken zones must never render under any supported display combination.");

            Console.WriteLine("PASS: broken zones are terminal and cannot be rendered by any state visibility combination.");
        }

        private static void VerifyZoneViewportSelectionPolicy()
        {
            Assert(ZoneViewportSelectionPolicy.UseActiveZoneIndex(true, false),
                "Active-only viewport rendering should use the interval index.");
            Assert(!ZoneViewportSelectionPolicy.UseActiveZoneIndex(true, true),
                "Enabling filled zones must fall back to the full zone history.");
            Assert(ZoneViewportSelectionPolicy.UseActiveZoneIndex(true, false),
                "Legacy broken-zone and breaker-parent-history toggles must not force a full-history scan.");
            Assert(!ZoneViewportSelectionPolicy.UseActiveZoneIndex(false, false),
                "Without a valid viewport, rendering must not claim active-index viewport mode.");

            Console.WriteLine("PASS: viewport-local ranking uses the active index; only opt-in filled history needs a full-history selection.");
        }

        private static void VerifyZoneMidlineColor()
        {
            ZoneMidlineRgb source = ZoneMidlineColorEvaluator.Match(22, 160, 166);
            Assert(source.R == 22 && source.G == 160 && source.B == 166,
                "Zone midline RGB must match the zone exactly.");
            ZoneMidlineRgb darkZone = ZoneMidlineColorEvaluator.Match(94, 12, 37);
            Assert(darkZone.R == 94 && darkZone.G == 12 && darkZone.B == 37,
                "Exact matching must work for dark bearish zone colours too.");
            ZoneMidlineRgb neutral = ZoneMidlineColorEvaluator.Match(128, 128, 128);
            Assert(neutral.R == 128 && neutral.G == 128 && neutral.B == 128,
                "Exact matching must preserve neutral colours.");

            Console.WriteLine("PASS: zone midlines use the exact zone RGB without tinting.");
        }

        private static void VerifyEqualLevelPairRules()
        {
            double[] highs = { 99.0, 100.9, 100.5, 100.9, 99.0, 99.0, 99.0, 99.0 };
            double[] lows  = { 98.0, 99.1, 99.5, 99.1, 99.3, 99.2, 98.0, 98.0 };
            Func<int, double> highAt = delegate (int index) { return highs[index]; };
            Func<int, double> lowAt = delegate (int index) { return lows[index]; };

            EqualLevelPairEvaluation highPair = EqualLevelEvaluator.Evaluate(
                true, 1, 100.0, true, 6, 100.4, 5, 10, 0.5, highAt, lowAt);
            Assert(highPair.Accepted && !highPair.StopSearch,
                "Equal-high pair at the exact minimum separation and exact tolerance band should qualify.");
            Assert(Math.Abs(highPair.Level - 100.4) < 1e-9,
                "Equal-high level should use the higher of the two swing prices.");

            double[] dirtyHighs = { 99.0, 100.9, 101.0, 100.9, 99.0, 99.0, 99.0, 99.0 };
            EqualLevelPairEvaluation dirtyHigh = EqualLevelEvaluator.Evaluate(
                true, 1, 100.0, true, 6, 100.4, 5, 10, 0.5,
                delegate (int index) { return dirtyHighs[index]; }, lowAt);
            Assert(!dirtyHigh.Accepted && dirtyHigh.StopSearch,
                "A high inside the intervening range that pierces the tolerance band must stop the search.");

            EqualLevelPairEvaluation lowPair = EqualLevelEvaluator.Evaluate(
                false, 1, 100.0, false, 6, 99.6, 5, 10, 0.5, highAt, lowAt);
            Assert(lowPair.Accepted && Math.Abs(lowPair.Level - 99.6) < 1e-9,
                "Equal-low pair should use the lower swing price and accept the exact clean-band boundary.");

            double[] dirtyLows = { 98.0, 99.1, 98.9, 99.1, 98.0, 98.0, 98.0, 98.0 };
            EqualLevelPairEvaluation dirtyLow = EqualLevelEvaluator.Evaluate(
                false, 1, 100.0, false, 6, 99.6, 5, 10, 0.5, highAt,
                delegate (int index) { return dirtyLows[index]; });
            Assert(!dirtyLow.Accepted && dirtyLow.StopSearch,
                "A lower wick piercing below the equal-low tolerance band must stop the search.");

            EqualLevelPairEvaluation tooClose = EqualLevelEvaluator.Evaluate(
                true, 2, 100.0, true, 6, 100.1, 5, 10, 0.5, highAt, lowAt);
            Assert(!tooClose.Accepted && !tooClose.StopSearch,
                "A pair inside the configured minimum bar separation must be skipped, not terminate the scan.");

            EqualLevelPairEvaluation tooOld = EqualLevelEvaluator.Evaluate(
                true, 1, 100.0, true, 13, 100.1, 5, 10, 0.5, highAt, lowAt);
            Assert(!tooOld.Accepted && tooOld.StopSearch,
                "A matching swing beyond maximum separation must terminate backward search.");

            EqualLevelPairEvaluation wrongType = EqualLevelEvaluator.Evaluate(
                false, 1, 100.0, true, 6, 100.1, 5, 10, 0.5, highAt, lowAt);
            Assert(!wrongType.Accepted && !wrongType.StopSearch,
                "A high swing must not be paired with a low swing.");
            Console.WriteLine("PASS: equal-high/low pair tolerance, clean-range, separation and early-exit predicates.");
        }


        private static void VerifyOrderBlockCandidateSearch()
        {
            double[] highs = { 110, 109, 108, 115, 112, 111, 120 };
            double[] lows = { 100, 98, 95, 99, 97, 96, 105 };
            double[] opens = { 105, 106, 104, 101, 103, 104, 110 };
            double[] closes = { 106, 104, 105, 103, 104, 105, 115 };
            Func<int, double> highAt = delegate (int index) { return highs[index]; };
            Func<int, double> lowAt = delegate (int index) { return lows[index]; };
            Func<int, double> openAt = delegate (int index) { return opens[index]; };
            Func<int, double> closeAt = delegate (int index) { return closes[index]; };

            OrderBlockCandidateSearch bullish = OrderBlockCandidateEvaluator.Find(
                true, 0, 6, highAt, lowAt, openAt, closeAt);
            Assert(bullish.ExtremeIndex == 2 && bullish.CandidateIndex == 1,
                "Bullish OB search must choose the leg's lowest low then scan backward to the nearest bearish candle.");

            OrderBlockCandidateSearch bearish = OrderBlockCandidateEvaluator.Find(
                false, 0, 6, highAt, lowAt,
                delegate (int index) { return opens[index]; },
                delegate (int index) { return closes[index]; });
            Assert(bearish.ExtremeIndex == 3 && bearish.CandidateIndex == 3,
                "Bearish OB search must choose the leg's highest high and select the qualifying bullish candle at that extreme.");

            double[] allBullishCloses = { 2, 3, 4, 5 };
            double[] allBullishOpens = { 1, 2, 3, 4 };
            OrderBlockCandidateSearch none = OrderBlockCandidateEvaluator.Find(
                true, 0, 3,
                delegate (int index) { return 10.0 + index; },
                delegate (int index) { return 5.0 - index; },
                delegate (int index) { return allBullishOpens[index]; },
                delegate (int index) { return allBullishCloses[index]; });
            Assert(none.CandidateIndex == -1,
                "OB search must report no candidate when no opposite candle exists between swing and extreme.");

            OrderBlockCandidateSearch shortLeg = OrderBlockCandidateEvaluator.Find(
                true, 2, 3, highAt, lowAt, openAt, closeAt);
            Assert(shortLeg.ExtremeIndex == -1 && shortLeg.CandidateIndex == -1,
                "OB search must not inspect legs shorter than the minimum two-bar separation.");

            Console.WriteLine("PASS: order-block extreme discovery, opposite-candle selection and negative cases.");
        }

        private static void VerifyDuplicateSweepSuppressionPredicate()
        {
            DateTime barTime = new DateTime(2026, 10, 18, 13, 30, 0, DateTimeKind.Utc);
            DateTime nextBar = barTime.AddMinutes(5);

            Assert(LiquiditySweepDuplicateEvaluator.IsDuplicate(
                true, 100.0, barTime, true, 100.004, barTime, 0.01),
                "Same-direction same-bar levels closer than one tick must be duplicates.");
            Assert(!LiquiditySweepDuplicateEvaluator.IsDuplicate(
                true, 100.0, barTime, true, 100.01, barTime, 0.01),
                "A separation exactly equal to one tick must not be a duplicate.");
            Assert(!LiquiditySweepDuplicateEvaluator.IsDuplicate(
                true, 100.0, barTime, false, 100.004, barTime, 0.01),
                "Opposite-direction sweeps must not be merged.");
            Assert(!LiquiditySweepDuplicateEvaluator.IsDuplicate(
                true, 100.0, nextBar, true, 100.004, barTime, 0.01),
                "Sweeps on different bars must not be merged.");

            Console.WriteLine("PASS: same-bar duplicate sweep suppression direction, time and tick thresholds.");
        }

        private static void VerifyOrderBlockRules()
        {
            Assert(OrderBlockRules.IsSourceAllowed(SznpStructureSource.MajorAndInternal, true)
                && OrderBlockRules.IsSourceAllowed(SznpStructureSource.MajorAndInternal, false),
                "MajorAndInternal source must allow both structure engines.");
            Assert(OrderBlockRules.IsSourceAllowed(SznpStructureSource.MajorOnly, true)
                && !OrderBlockRules.IsSourceAllowed(SznpStructureSource.MajorOnly, false),
                "MajorOnly source must reject internal structure.");
            Assert(!OrderBlockRules.IsSourceAllowed(SznpStructureSource.InternalOnly, true)
                && OrderBlockRules.IsSourceAllowed(SznpStructureSource.InternalOnly, false),
                "InternalOnly source must reject major structure.");

            OrderBlockBoundsResult full = OrderBlockRules.EvaluateBounds(
                95.0, 110.0, 101.0, 104.0, SznpObRange.FullCandle, 0.0, true, 1.0);
            Assert(full.Accepted && !full.UsedBodyFallback
                && full.Lower == 95.0 && full.Upper == 110.0,
                "FullCandle mode with no maximum must preserve wick bounds.");

            OrderBlockBoundsResult body = OrderBlockRules.EvaluateBounds(
                95.0, 110.0, 101.0, 104.0, SznpObRange.BodyOnly, 5.0, true, 1.0);
            Assert(body.Accepted && !body.UsedBodyFallback
                && body.Lower == 101.0 && body.Upper == 104.0,
                "BodyOnly mode must use the candle body and should not be re-labelled as a fallback.");

            OrderBlockBoundsResult fallback = OrderBlockRules.EvaluateBounds(
                95.0, 110.0, 101.0, 104.0, SznpObRange.FullCandle, 5.0, true, 1.0);
            Assert(fallback.Accepted && fallback.UsedBodyFallback
                && fallback.Lower == 101.0 && fallback.Upper == 104.0,
                "Oversized full-candle ranges must fall back to body when enabled and body fits.");

            OrderBlockBoundsResult stillOversized = OrderBlockRules.EvaluateBounds(
                95.0, 110.0, 101.0, 108.0, SznpObRange.FullCandle, 5.0, true, 1.0);
            Assert(!stillOversized.Accepted && stillOversized.UsedBodyFallback
                && stillOversized.Failure == OrderBlockBoundsFailure.Oversize,
                "A body that remains above the maximum must still be rejected.");

            OrderBlockBoundsResult noFallback = OrderBlockRules.EvaluateBounds(
                95.0, 110.0, 101.0, 104.0, SznpObRange.FullCandle, 5.0, false, 1.0);
            Assert(!noFallback.Accepted && !noFallback.UsedBodyFallback
                && noFallback.Failure == OrderBlockBoundsFailure.Oversize,
                "Oversized full candles must be rejected when body fallback is disabled.");

            OrderBlockBoundsResult bodyOversize = OrderBlockRules.EvaluateBounds(
                95.0, 110.0, 101.0, 108.0, SznpObRange.BodyOnly, 5.0, true, 1.0);
            Assert(!bodyOversize.Accepted && !bodyOversize.UsedBodyFallback
                && bodyOversize.Failure == OrderBlockBoundsFailure.Oversize,
                "BodyOnly mode should not perform a redundant body fallback.");

            OrderBlockBoundsResult tooSmall = OrderBlockRules.EvaluateBounds(
                99.0, 101.0, 99.8, 100.2, SznpObRange.BodyOnly, 0.0, false, 1.0);
            Assert(!tooSmall.Accepted && tooSmall.Failure == OrderBlockBoundsFailure.TooSmall,
                "An order block below the minimum size must be rejected.");

            Console.WriteLine("PASS: order-block source filters, full/body ranges, oversize fallback and minimum-size rules.");
        }

        private static void VerifyPivotConfirmationAndTieRules()
        {
            double[] highs = { 5, 8, 10, 8, 10, 7, 6 };
            double[] lows  = { 6, 4, 2, 4, 2, 5, 7 };
            Func<int, double> highAt = delegate (int index) { return highs[index]; };
            Func<int, double> lowAt = delegate (int index) { return lows[index]; };

            PivotEvaluation beforeConfirmation = PivotEvaluator.Evaluate(
                2, 2, 2, 3, 0, highAt, lowAt);
            Assert(!beforeConfirmation.IsReady,
                "A pivot must not be confirmed before its right-hand bars close.");

            PivotEvaluation confirmed = PivotEvaluator.Evaluate(
                2, 2, 2, 4, 0, highAt, lowAt);
            Assert(confirmed.IsReady && confirmed.PivotIndex == 2,
                "A pivot must become eligible exactly when the configured right bars are closed.");
            Assert(confirmed.IsHigh,
                "A pivot high must preserve the existing right-side equal-high rule.");
            Assert(confirmed.IsLow,
                "A pivot low must preserve the existing right-side equal-low rule.");

            double[] tiedLeftHighs = { 10, 8, 10, 8, 7 };
            double[] ordinaryLows = { 7, 8, 9, 8, 7 };
            PivotEvaluation leftHighTie = PivotEvaluator.Evaluate(
                2, 2, 2, 4, 0,
                delegate (int index) { return tiedLeftHighs[index]; },
                delegate (int index) { return ordinaryLows[index]; });
            Assert(leftHighTie.IsReady && !leftHighTie.IsHigh,
                "An equal high to the left must disqualify a pivot high.");

            double[] ordinaryHighs = { 7, 8, 9, 8, 7 };
            double[] tiedLeftLows = { 2, 4, 2, 4, 5 };
            PivotEvaluation leftLowTie = PivotEvaluator.Evaluate(
                2, 2, 2, 4, 0,
                delegate (int index) { return ordinaryHighs[index]; },
                delegate (int index) { return tiedLeftLows[index]; });
            Assert(leftLowTie.IsReady && !leftLowTie.IsLow,
                "An equal low to the left must disqualify a pivot low.");

            PivotEvaluation outsideLookback = PivotEvaluator.Evaluate(
                1, 2, 2, 4, 2, highAt, lowAt);
            Assert(!outsideLookback.IsReady,
                "Pivots before the configured lookback start must be rejected.");

            PivotEvaluation missingLeftHistory = PivotEvaluator.Evaluate(
                1, 2, 1, 4, 0, highAt, lowAt);
            Assert(!missingLeftHistory.IsReady,
                "A pivot without enough left-hand history must be rejected.");

            double[] invalidPivotHighs = { 5, 8, double.NaN, 8, 6 };
            double[] validPivotLows = { 8, 6, 7, 6, 8 };
            PivotEvaluation invalidHigh = PivotEvaluator.Evaluate(
                2, 1, 1, 3, 0,
                delegate (int index) { return invalidPivotHighs[index]; },
                delegate (int index) { return validPivotLows[index]; });
            Assert(invalidHigh.IsReady && !invalidHigh.IsHigh,
                "A non-finite pivot high must not be accepted as a swing high.");

            double[] validPivotHighs = { 8, 9, 10, 9, 8 };
            double[] invalidPivotLows = { 8, 6, double.PositiveInfinity, 6, 8 };
            PivotEvaluation invalidLow = PivotEvaluator.Evaluate(
                2, 1, 1, 3, 0,
                delegate (int index) { return validPivotHighs[index]; },
                delegate (int index) { return invalidPivotLows[index]; });
            Assert(invalidLow.IsReady && !invalidLow.IsLow,
                "A non-finite pivot low must not be accepted as a swing low.");

            double[] invalidLeftHighs = { 8, double.NaN, 10, 8, 7 };
            PivotEvaluation invalidLeftHigh = PivotEvaluator.Evaluate(
                2, 2, 2, 4, 0,
                delegate (int index) { return invalidLeftHighs[index]; },
                delegate (int index) { return ordinaryLows[index]; });
            Assert(invalidLeftHigh.IsReady && !invalidLeftHigh.IsHigh,
                "A non-finite neighboring high must not let a pivot pass through failed comparisons.");

            double[] invalidRightLows = { 8, 6, 5, double.NegativeInfinity, 7 };
            PivotEvaluation invalidRightLow = PivotEvaluator.Evaluate(
                2, 1, 2, 4, 0,
                delegate (int index) { return ordinaryHighs[index]; },
                delegate (int index) { return invalidRightLows[index]; });
            Assert(invalidRightLow.IsReady && !invalidRightLow.IsLow,
                "A non-finite neighboring low must not let a pivot pass through failed comparisons.");

            double[] invertedCenterHighs = { 8, 9, 4, 9, 8 };
            double[] invertedCenterLows = { 8, 6, 7, 6, 8 };
            PivotEvaluation invertedCenter = PivotEvaluator.Evaluate(
                2, 1, 1, 3, 0,
                delegate (int index) { return invertedCenterHighs[index]; },
                delegate (int index) { return invertedCenterLows[index]; });
            Assert(invertedCenter.IsReady && !invertedCenter.IsHigh && !invertedCenter.IsLow,
                "A center bar with High below Low must not form either kind of pivot.");

            double[] invertedNeighborHighs = { 8, 4, 10, 8, 7 };
            double[] invertedNeighborLows = { 8, 6, 7, 6, 8 };
            PivotEvaluation invertedNeighbor = PivotEvaluator.Evaluate(
                2, 1, 1, 3, 0,
                delegate (int index) { return invertedNeighborHighs[index]; },
                delegate (int index) { return invertedNeighborLows[index]; });
            Assert(invertedNeighbor.IsReady && !invertedNeighbor.IsHigh && !invertedNeighbor.IsLow,
                "A neighboring bar with High below Low must invalidate a pivot candidate.");

            Console.WriteLine("PASS: pivot confirmation delay, lookback eligibility, asymmetric ties, and malformed OHLC rejection.");
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
            Assert(!StructureBreakEvaluator.IsLevelCrossed(true, double.NaN, 100.0),
                "A non-finite close must not trigger a bullish structure break.");
            Assert(!StructureBreakEvaluator.IsLevelCrossed(false, double.PositiveInfinity, 100.0),
                "An infinite close must not trigger a bearish structure break.");
            Assert(!StructureBreakEvaluator.IsLevelCrossed(true, 100.01, double.NaN),
                "A non-finite swing level must not trigger a structure break.");

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

        private static void VerifyStructureSwingVisibility()
        {
            Assert(StructureSwingVisibilityEvaluator.ShouldShowLevel(
                    false, false, false, false, false, false, true),
                "An unbroken swing level should render when active levels are enabled.");
            Assert(!StructureSwingVisibilityEvaluator.ShouldShowLevel(
                    false, true, false, false, false, false, true),
                "A swept swing must not remain visible as an active level.");
            Assert(!StructureSwingVisibilityEvaluator.ShouldShowLevel(
                    true, false, true, false, true, true, true),
                "A silently broken swing must not render a BOS/CHoCH line.");
            Assert(StructureSwingVisibilityEvaluator.ShouldShowLevel(
                    true, false, false, false, true, false, false),
                "A broken swing must render BOS when BOS is enabled.");
            Assert(!StructureSwingVisibilityEvaluator.ShouldShowLevel(
                    true, false, false, false, false, true, true),
                "A BOS line must respect its own visibility switch.");
            Assert(StructureSwingVisibilityEvaluator.ShouldShowLevel(
                    true, false, false, true, false, true, false),
                "A broken swing must render CHoCH when CHoCH is enabled.");
            Assert(!StructureSwingVisibilityEvaluator.ShouldShowLevel(
                    true, true, false, true, false, true, true),
                "Swept state must suppress a structure level even if other flags would show it.");

            Console.WriteLine("PASS: structure swing visibility respects swept, silent-break, BOS and CHoCH states.");
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

        private static void VerifyFairValueGapDetector()
        {
            DateTime firstTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            FairValueGapInput input = new FairValueGapInput
            {
                OriginIndex = 10,
                ConfirmationIndex = 12,
                FirstCandleTime = firstTime,
                ConfirmationCandleTime = firstTime.AddMinutes(10),
                TickSize = 0.01,
                PipSize = 0.01,
                Atr = 2.0,
                MinSizePips = 2.0,
                MinSizeAtr = 0.1,
                RequireDisplacement = false,
                MatchDirection = true,
                MinBodyAtr = 0.3,
                MinBodyRatio = 0.5,
                StartMode = SznpStartMode.FirstCandle,
                FirstHigh = 100.0,
                FirstLow = 98.0,
                MiddleOpen = 100.0,
                MiddleClose = 101.0,
                MiddleHigh = 101.5,
                MiddleLow = 99.5,
                ThirdHigh = 102.0,
                ThirdLow = 101.0
            };

            FairValueGapEvaluation bullish = FairValueGapDetectorEvaluator.Evaluate(input);
            Assert(bullish.Detected && bullish.Accepted && bullish.Bullish,
                "FVG evaluator must accept a valid bullish three-candle gap.");
            Assert(Math.Abs(bullish.Lower - 100.0) < 1e-9
                && Math.Abs(bullish.Upper - 101.0) < 1e-9,
                "Bullish FVG bounds must match first high and third low.");
            Assert(Math.Abs(bullish.Momentum - 0.5) < 1e-9 && bullish.StartTime == firstTime,
                "FVG evaluator must preserve momentum and selected start time.");

            input.FirstHigh = 102.0;
            input.FirstLow = 101.0;
            input.MiddleOpen = 100.0;
            input.MiddleClose = 99.0;
            input.ThirdHigh = 100.0;
            input.ThirdLow = 99.0;
            FairValueGapEvaluation bearish = FairValueGapDetectorEvaluator.Evaluate(input);
            Assert(bearish.Detected && bearish.Accepted && !bearish.Bullish,
                "FVG evaluator must accept a valid bearish three-candle gap.");
            Assert(Math.Abs(bearish.Lower - 100.0) < 1e-9
                && Math.Abs(bearish.Upper - 101.0) < 1e-9,
                "Bearish FVG bounds must match third high and first low.");

            input.FirstHigh = 100.0;
            input.FirstLow = 98.0;
            input.ThirdHigh = 102.0;
            input.ThirdLow = 99.0;
            FairValueGapEvaluation absent = FairValueGapDetectorEvaluator.Evaluate(input);
            Assert(!absent.Detected, "FVG evaluator must reject candles with no three-candle gap.");

            input.ThirdLow = 100.004;
            FairValueGapEvaluation belowTick = FairValueGapDetectorEvaluator.Evaluate(input);
            Assert(belowTick.Detected && !belowTick.Accepted && belowTick.Rejection == "fvg: below tick",
                "FVG evaluator must reject gaps smaller than half a tick.");

            input.ThirdLow = 100.1;
            FairValueGapEvaluation belowMinimum = FairValueGapDetectorEvaluator.Evaluate(input);
            Assert(!belowMinimum.Accepted && belowMinimum.Rejection == "fvg: min size",
                "FVG evaluator must apply the configured pip/ATR size floor.");

            input.ThirdLow = 101.0;
            input.RequireDisplacement = true;
            input.MinBodyAtr = 0.75;
            FairValueGapEvaluation weakDisplacement = FairValueGapDetectorEvaluator.Evaluate(input);
            Assert(!weakDisplacement.Accepted && weakDisplacement.Rejection == "fvg: displacement body",
                "FVG evaluator must reject insufficient middle-candle displacement.");
        }

        private static void VerifyPriceGapDetector()
        {
            PriceGapInput input = new PriceGapInput
            {
                PipSize = 0.01,
                TickSize = 0.01,
                Atr = 2.0,
                MinSizePips = 2.0,
                MinSizeAtr = 0.1,
                Method = SznpGapMethod.Both,
                Combine = SznpGapCombine.KeepBoth,
                PreviousClose = 100.0,
                CurrentOpen = 101.0,
                PreviousHigh = 100.5,
                PreviousLow = 99.5,
                CurrentHigh = 102.5,
                CurrentLow = 102.0
            };

            PriceGapEvaluation both = PriceGapDetectorEvaluator.Evaluate(input);
            Assert(both.HasOpenClose && both.HasRange && both.OpenCloseBullish && both.RangeBullish,
                "Price gap evaluator must detect both valid bullish gap definitions.");
            Assert(Math.Abs(both.OpenCloseLower - 100.0) < 1e-9
                && Math.Abs(both.OpenCloseUpper - 101.0) < 1e-9
                && Math.Abs(both.RangeLower - 100.5) < 1e-9
                && Math.Abs(both.RangeUpper - 102.0) < 1e-9,
                "Price gap evaluator must preserve candidate boundaries.");

            input.Combine = SznpGapCombine.PreferRangeGap;
            PriceGapEvaluation preferRange = PriceGapDetectorEvaluator.Evaluate(input);
            Assert(!preferRange.HasOpenClose && preferRange.HasRange,
                "PreferRangeGap must keep only the range gap when both candidates exist.");

            input.Combine = SznpGapCombine.PreferOpenCloseGap;
            PriceGapEvaluation preferOpenClose = PriceGapDetectorEvaluator.Evaluate(input);
            Assert(preferOpenClose.HasOpenClose && !preferOpenClose.HasRange,
                "PreferOpenCloseGap must keep only the open/close gap when both candidates exist.");

            input.Combine = SznpGapCombine.KeepBoth;
            input.CurrentOpen = 100.05;
            PriceGapEvaluation minimum = PriceGapDetectorEvaluator.Evaluate(input);
            Assert(minimum.RejectedOpenCloseMinimum,
                "Price gap evaluator must report an open/close gap rejected by the configured size floor.");
        }

        private static void VerifyFeatureModuleFaultIsolation()
        {
            List<string> errors = new List<string>();
            int processed = 0;
            int rendered = 0;
            FeatureModulePipeline pipeline = new FeatureModulePipeline(
                delegate (string name, Exception error) { errors.Add(name); });

            pipeline.AddProcessor("failing processor", delegate { return true; },
                delegate (int index, double atr) { throw new InvalidOperationException("expected test failure"); });
            pipeline.AddProcessor("independent processor", delegate { return true; },
                delegate (int index, double atr) { processed++; });
            pipeline.ProcessBar(10, 1.5);

            pipeline.AddRenderer("failing renderer", delegate { return true; },
                delegate (bool refresh) { throw new InvalidOperationException("expected test failure"); });
            pipeline.AddRenderer("independent renderer", delegate { return true; },
                delegate (bool refresh) { rendered++; });
            pipeline.Render(false);

            Assert(processed == 1, "A failed processor must not prevent the next feature processor from running.");
            Assert(rendered == 1, "A failed renderer must not prevent the next feature renderer from running.");
            Assert(errors.SequenceEqual(new[] { "failing processor", "failing renderer" }),
                "The pipeline must report failures with the corresponding feature name.");
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
                VerifyZoneMidlineColor();
                VerifyZoneVisibilityPolicy();
                VerifyZoneViewportSelectionPolicy();
                VerifyHigherTimeframeTiming();
                VerifyFutureBarTime();
                VerifyPivotConfirmationAndTieRules();
                VerifyEqualLevelPairRules();
                VerifyOrderBlockRules();
                VerifyOrderBlockCandidateSearch();
                VerifyDuplicateSweepSuppressionPredicate();
                VerifyStructureBreakClassification();
                VerifyStructureSwingVisibility();
                VerifyLiquiditySweepPredicates();
                VerifyFairValueGapDetector();
                VerifyPriceGapDetector();
                VerifyFeatureModuleFaultIsolation();

                Console.WriteLine("PASS: interval index boundary/mutation tests and "
                    + _queryCount + " seeded differential queries over 50,000 operations.");
                Console.WriteLine("PASS: bullish/bearish CandleClose, BodyBeyond and WickBeyond predicates.");
                Console.WriteLine("PASS: Fresh/Touched/Partial/Filled/Broken transitions and touch re-entry.");
                Console.WriteLine("PASS: breaker direction, parent identity, preserved bounds/factors and confirmation timestamp.");
                Console.WriteLine("PASS: BOS/CHoCH classification and strict swing-cross boundaries.");
                Console.WriteLine("PASS: swept and silently-broken swing levels are not rendered as active levels.");
                Console.WriteLine("PASS: high/low liquidity sweep predicates for Wick/Body measure and thresholds.");
                Console.WriteLine("PASS: shared FVG detector rules, thresholds and start-time semantics.");
                Console.WriteLine("PASS: shared price-gap detector rules, combine modes and minimum-size filters.");
                Console.WriteLine("PASS: independent feature pipeline continues after processor/renderer failures.");
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