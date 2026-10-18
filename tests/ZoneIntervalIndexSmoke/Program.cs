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

        private static int Main()
        {
            try
            {
                VerifyBoundaryAndMutationCases();
                VerifyRandomizedQueries();
                Console.WriteLine("PASS: boundary, removal, clear, invalid-input and "
                    + _queryCount + " randomized/reference interval queries.");
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