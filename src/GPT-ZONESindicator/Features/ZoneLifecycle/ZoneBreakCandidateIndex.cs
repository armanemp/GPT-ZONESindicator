using System;
using System.Collections.Generic;

namespace cAlgo
{
    /// <summary>
    /// Ordered break-candidate indexes for active zones. This component only selects candidates;
    /// the lifecycle evaluator remains responsible for deciding the resulting zone state.
    /// </summary>
    internal sealed class ZoneBreakCandidateIndex<T> where T : class
    {
        private const int KindCount = 4;

        private sealed class PriceComparer : IComparer<T>
        {
            private readonly Func<T, double> _price;
            private readonly Func<T, int> _id;

            public PriceComparer(Func<T, double> price, Func<T, int> id)
            {
                _price = price;
                _id = id;
            }

            public int Compare(T left, T right)
            {
                if (ReferenceEquals(left, right))
                    return 0;
                if (left == null)
                    return -1;
                if (right == null)
                    return 1;

                int price = _price(left).CompareTo(_price(right));
                return price != 0 ? price : _id(left).CompareTo(_id(right));
            }
        }

        private readonly Func<T, double> _lower;
        private readonly Func<T, double> _upper;
        private readonly Func<T, int> _id;
        private readonly Func<T, int> _confirmationIndex;
        private readonly Func<bool, double, int, T> _createProbe;
        private readonly SortedSet<T>[] _bullish = new SortedSet<T>[KindCount];
        private readonly SortedSet<T>[] _bearish = new SortedSet<T>[KindCount];

        public ZoneBreakCandidateIndex(
            Func<T, double> lower,
            Func<T, double> upper,
            Func<T, int> id,
            Func<T, int> confirmationIndex,
            Func<bool, double, int, T> createProbe)
        {
            if (lower == null) throw new ArgumentNullException(nameof(lower));
            if (upper == null) throw new ArgumentNullException(nameof(upper));
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (confirmationIndex == null) throw new ArgumentNullException(nameof(confirmationIndex));
            if (createProbe == null) throw new ArgumentNullException(nameof(createProbe));

            _lower = lower;
            _upper = upper;
            _id = id;
            _confirmationIndex = confirmationIndex;
            _createProbe = createProbe;

            for (int kind = 0; kind < KindCount; kind++)
            {
                _bullish[kind] = new SortedSet<T>(new PriceComparer(_lower, _id));
                _bearish[kind] = new SortedSet<T>(new PriceComparer(_upper, _id));
            }
        }

        public void Clear()
        {
            for (int kind = 0; kind < KindCount; kind++)
            {
                _bullish[kind].Clear();
                _bearish[kind].Clear();
            }
        }

        public void Add(T item, int kind, bool bull)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            ValidateKind(kind);

            SortedSet<T> set = bull ? _bullish[kind] : _bearish[kind];
            if (!set.Add(item))
                throw new InvalidOperationException("A break candidate with the same price and identity is already indexed.");
        }

        public bool Remove(T item, int kind, bool bull)
        {
            if (item == null)
                return false;
            ValidateKind(kind);
            return (bull ? _bullish[kind] : _bearish[kind]).Remove(item);
        }

        /// <summary>
        /// Returns only zones whose directional far boundary has been strictly crossed by this
        /// closed candle. The supplied result list is cleared and sorted by stable zone identity.
        /// </summary>
        public void QueryCandidates(
            int index,
            double open,
            double high,
            double low,
            double close,
            SznpBreakMode[] modes,
            List<T> candidates)
        {
            if (modes == null || modes.Length < KindCount)
                throw new ArgumentException("A break mode is required for each zone kind.", nameof(modes));
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));

            candidates.Clear();

            for (int kind = 0; kind < KindCount; kind++)
            {
                double bullishReference;
                switch (modes[kind])
                {
                    case SznpBreakMode.CandleClose:
                        bullishReference = close;
                        break;
                    case SznpBreakMode.BodyBeyond:
                        bullishReference = Math.Max(open, close);
                        break;
                    default:
                        bullishReference = low;
                        break;
                }

                SortedSet<T> bullish = _bullish[kind];
                if (bullish.Count > 0)
                {
                    T from = _createProbe(true, bullishReference, int.MaxValue);
                    T to = _createProbe(true, double.MaxValue, int.MaxValue);
                    foreach (T item in bullish.GetViewBetween(from, to))
                    {
                        if (_confirmationIndex(item) < index)
                            candidates.Add(item);
                    }
                }

                double bearishReference;
                switch (modes[kind])
                {
                    case SznpBreakMode.CandleClose:
                        bearishReference = close;
                        break;
                    case SznpBreakMode.BodyBeyond:
                        bearishReference = Math.Min(open, close);
                        break;
                    default:
                        bearishReference = high;
                        break;
                }

                SortedSet<T> bearish = _bearish[kind];
                if (bearish.Count > 0)
                {
                    T from = _createProbe(false, double.NegativeInfinity, int.MinValue);
                    T to = _createProbe(false, bearishReference, int.MinValue);
                    foreach (T item in bearish.GetViewBetween(from, to))
                    {
                        if (_confirmationIndex(item) < index)
                            candidates.Add(item);
                    }
                }
            }

            candidates.Sort(delegate (T left, T right)
            {
                return _id(left).CompareTo(_id(right));
            });
        }

        private static void ValidateKind(int kind)
        {
            if (kind < 0 || kind >= KindCount)
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }
}
