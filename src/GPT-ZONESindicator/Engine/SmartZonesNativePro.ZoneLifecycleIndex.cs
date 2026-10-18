using System;
using System.Collections.Generic;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private sealed class ZoneLowerComparer : IComparer<Zone>
        {
            public static readonly ZoneLowerComparer Instance = new ZoneLowerComparer();

            public int Compare(Zone left, Zone right)
            {
                if (ReferenceEquals(left, right)) return 0;
                if (left == null) return -1;
                if (right == null) return 1;

                int price = left.Lower.CompareTo(right.Lower);
                return price != 0 ? price : left.Id.CompareTo(right.Id);
            }
        }

        private sealed class ZoneUpperComparer : IComparer<Zone>
        {
            public static readonly ZoneUpperComparer Instance = new ZoneUpperComparer();

            public int Compare(Zone left, Zone right)
            {
                if (ReferenceEquals(left, right)) return 0;
                if (left == null) return -1;
                if (right == null) return 1;

                int price = left.Upper.CompareTo(right.Upper);
                return price != 0 ? price : left.Id.CompareTo(right.Id);
            }
        }

        private void ResetZoneLifecycleIndexes()
        {
            _zoneIntervalIndex.Clear();
            _overlapScratch.Clear();
            _breakScratch.Clear();

            for (int kind = 0; kind < 4; kind++)
            {
                if (_bullishBreakIndex[kind] == null)
                    _bullishBreakIndex[kind] = new SortedSet<Zone>(ZoneLowerComparer.Instance);
                else
                    _bullishBreakIndex[kind].Clear();

                if (_bearishBreakIndex[kind] == null)
                    _bearishBreakIndex[kind] = new SortedSet<Zone>(ZoneUpperComparer.Instance);
                else
                    _bearishBreakIndex[kind].Clear();
            }
        }

        private void RegisterZoneForLifecycle(Zone zone)
        {
            if (zone == null || zone.Indexed || zone.State == SznpState.Broken)
                return;

            int kind = (int)zone.Kind;
            _zoneIntervalIndex.Add(zone);

            SortedSet<Zone> breakIndex = zone.Bull
                ? _bullishBreakIndex[kind]
                : _bearishBreakIndex[kind];

            breakIndex.Add(zone);
            zone.Indexed = true;
        }

        private void UnregisterZoneForLifecycle(Zone zone)
        {
            if (zone == null || !zone.Indexed)
                return;

            _zoneIntervalIndex.Remove(zone);

            SortedSet<Zone> breakIndex = zone.Bull
                ? _bullishBreakIndex[(int)zone.Kind]
                : _bearishBreakIndex[(int)zone.Kind];

            if (breakIndex != null)
                breakIndex.Remove(zone);

            zone.Indexed = false;
        }

        /// <summary>
        /// Processes far-edge break candidates first, then price-overlapping zones.
        /// The lifecycle state machine remains centralized in UpdateZone.
        /// </summary>
        private void ProcessIndexedZones(int index)
        {
            ProcessIndexedBreakCandidates(index);

            _overlapScratch.Clear();
            _zoneIntervalIndex.Query(L(index), H(index), _overlapScratch);

            for (int i = 0; i < _overlapScratch.Count; i++)
            {
                Zone zone = _overlapScratch[i];
                if (zone == null || !zone.Indexed || zone.State == SznpState.Broken
                    || zone.ConfirmIdx >= index)
                    continue;

                UpdateZone(zone, index, true);
            }
        }

        private void ProcessIndexedBreakCandidates(int index)
        {
            double open = O(index);
            double close = C(index);
            double low = L(index);
            double high = H(index);
            _breakScratch.Clear();

            // Collect candidates from every kind before mutating any index. Sorting by
            // zone identity preserves the old chronological _alive processing order.
            for (int kind = 0; kind < 4; kind++)
            {
                SznpBreakMode mode = ModeFor((SznpKind)kind);

                double bullishReference;
                switch (mode)
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

                SortedSet<Zone> bullish = _bullishBreakIndex[kind];
                if (bullish != null && bullish.Count > 0)
                {
                    // Bullish zones break when their lower boundary is strictly above
                    // the selected close/body/wick reference.
                    Zone from = new Zone { Lower = bullishReference, Id = int.MaxValue };
                    Zone to = new Zone { Lower = double.MaxValue, Id = int.MaxValue };
                    foreach (Zone zone in bullish.GetViewBetween(from, to))
                    {
                        if (zone.Indexed && zone.ConfirmIdx < index)
                            _breakScratch.Add(zone);
                    }
                }

                double bearishReference;
                switch (mode)
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

                SortedSet<Zone> bearish = _bearishBreakIndex[kind];
                if (bearish != null && bearish.Count > 0)
                {
                    // Bearish zones break when their upper boundary is strictly below
                    // the selected close/body/wick reference.
                    Zone from = new Zone { Upper = double.NegativeInfinity, Id = int.MinValue };
                    Zone to = new Zone { Upper = bearishReference, Id = int.MinValue };
                    foreach (Zone zone in bearish.GetViewBetween(from, to))
                    {
                        if (zone.Indexed && zone.ConfirmIdx < index)
                            _breakScratch.Add(zone);
                    }
                }
            }

            _breakScratch.Sort(delegate (Zone left, Zone right)
            {
                return left.Id.CompareTo(right.Id);
            });

            for (int i = 0; i < _breakScratch.Count; i++)
            {
                Zone zone = _breakScratch[i];
                if (zone.Indexed && zone.ConfirmIdx < index)
                    UpdateZone(zone, index, true);
            }

            _breakScratch.Clear();
        }
    }
}