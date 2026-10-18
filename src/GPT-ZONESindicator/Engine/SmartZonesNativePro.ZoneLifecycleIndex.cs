using System;
using System.Collections.Generic;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        /// <summary>
        /// Augmented treap ordered by immutable zone lower bound. It reports price
        /// intervals intersecting a candle without testing every active zone.
        /// </summary>
        private sealed class ZoneIntervalNode
        {
            public Zone Zone;
            public ZoneIntervalNode Left, Right;
            public uint Priority;
            public double MaxUpper;
        }

        private sealed class ZoneLowerComparer : IComparer<Zone>
        {
            public static readonly ZoneLowerComparer Instance = new ZoneLowerComparer();

            public int Compare(Zone left, Zone right)
            {
                if (ReferenceEquals(left, right))
                    return 0;
                if (left == null)
                    return -1;
                if (right == null)
                    return 1;

                int price = left.Lower.CompareTo(right.Lower);
                return price != 0 ? price : left.Id.CompareTo(right.Id);
            }
        }

        private sealed class ZoneUpperComparer : IComparer<Zone>
        {
            public static readonly ZoneUpperComparer Instance = new ZoneUpperComparer();

            public int Compare(Zone left, Zone right)
            {
                if (ReferenceEquals(left, right))
                    return 0;
                if (left == null)
                    return -1;
                if (right == null)
                    return 1;

                int price = left.Upper.CompareTo(right.Upper);
                return price != 0 ? price : left.Id.CompareTo(right.Id);
            }
        }

        private static uint ZoneIndexPriority(int identity)
        {
            uint value = unchecked((uint)identity + 0x9E3779B9u);
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }

        private static void RefreshIntervalNode(ZoneIntervalNode node)
        {
            if (node == null)
                return;

            double max = node.Zone.Upper;
            if (node.Left != null)
                max = Math.Max(max, node.Left.MaxUpper);
            if (node.Right != null)
                max = Math.Max(max, node.Right.MaxUpper);

            node.MaxUpper = max;
        }

        private static ZoneIntervalNode RotateIntervalRight(ZoneIntervalNode root)
        {
            ZoneIntervalNode pivot = root.Left;
            root.Left = pivot.Right;
            pivot.Right = root;
            RefreshIntervalNode(root);
            RefreshIntervalNode(pivot);
            return pivot;
        }

        private static ZoneIntervalNode RotateIntervalLeft(ZoneIntervalNode root)
        {
            ZoneIntervalNode pivot = root.Right;
            root.Right = pivot.Left;
            pivot.Left = root;
            RefreshIntervalNode(root);
            RefreshIntervalNode(pivot);
            return pivot;
        }

        private static ZoneIntervalNode InsertIntervalNode(ZoneIntervalNode root, Zone zone)
        {
            if (root == null)
            {
                return new ZoneIntervalNode
                {
                    Zone = zone,
                    Priority = ZoneIndexPriority(zone.Id),
                    MaxUpper = zone.Upper
                };
            }

            if (ZoneLowerComparer.Instance.Compare(zone, root.Zone) < 0)
            {
                root.Left = InsertIntervalNode(root.Left, zone);
                if (root.Left.Priority < root.Priority)
                    root = RotateIntervalRight(root);
            }
            else
            {
                root.Right = InsertIntervalNode(root.Right, zone);
                if (root.Right.Priority < root.Priority)
                    root = RotateIntervalLeft(root);
            }

            RefreshIntervalNode(root);
            return root;
        }

        private static ZoneIntervalNode MergeIntervalNodes(ZoneIntervalNode left, ZoneIntervalNode right)
        {
            if (left == null)
                return right;
            if (right == null)
                return left;

            if (left.Priority < right.Priority)
            {
                left.Right = MergeIntervalNodes(left.Right, right);
                RefreshIntervalNode(left);
                return left;
            }

            right.Left = MergeIntervalNodes(left, right.Left);
            RefreshIntervalNode(right);
            return right;
        }

        private static ZoneIntervalNode RemoveIntervalNode(ZoneIntervalNode root, Zone zone)
        {
            if (root == null || zone == null)
                return root;

            int comparison = ZoneLowerComparer.Instance.Compare(zone, root.Zone);
            if (comparison < 0)
            {
                root.Left = RemoveIntervalNode(root.Left, zone);
                RefreshIntervalNode(root);
                return root;
            }

            if (comparison > 0)
            {
                root.Right = RemoveIntervalNode(root.Right, zone);
                RefreshIntervalNode(root);
                return root;
            }

            return MergeIntervalNodes(root.Left, root.Right);
        }

        private static void QueryOverlappingZoneIntervals(
            ZoneIntervalNode node,
            double low,
            double high,
            List<Zone> matches)
        {
            if (node == null)
                return;

            if (node.Left != null && node.Left.MaxUpper >= low)
                QueryOverlappingZoneIntervals(node.Left, low, high, matches);

            if (node.Zone.Lower <= high && node.Zone.Upper >= low)
                matches.Add(node.Zone);

            // Nodes in the right subtree have lower bounds no smaller than this one.
            if (node.Zone.Lower <= high)
                QueryOverlappingZoneIntervals(node.Right, low, high, matches);
        }

        private void ResetZoneLifecycleIndexes()
        {
            _zoneIntervalRoot = null;
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
            _zoneIntervalRoot = InsertIntervalNode(_zoneIntervalRoot, zone);

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

            _zoneIntervalRoot = RemoveIntervalNode(_zoneIntervalRoot, zone);

            SortedSet<Zone> breakIndex = zone.Bull
                ? _bullishBreakIndex[(int)zone.Kind]
                : _bearishBreakIndex[(int)zone.Kind];

            if (breakIndex != null)
                breakIndex.Remove(zone);

            zone.Indexed = false;
        }

        /// <summary>
        /// Processes possible closes through either far edge first, then only zones
        /// whose price intervals intersect the candle. Zone state rules remain in UpdateZone.
        /// </summary>
        private void ProcessIndexedZones(int index)
        {
            ProcessIndexedBreakCandidates(index);

            _overlapScratch.Clear();
            QueryOverlappingZoneIntervals(
                _zoneIntervalRoot,
                L(index),
                H(index),
                _overlapScratch);

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
                _breakScratch.Clear();
                if (bullish != null && bullish.Count > 0)
                {
                    // Bullish zones break when their lower boundary is strictly above
                    // the selected close/body/wick reference. Id disambiguates equal bounds.
                    Zone from = new Zone { Lower = bullishReference, Id = int.MaxValue };
                    Zone to = new Zone { Lower = double.MaxValue, Id = int.MaxValue };
                    foreach (Zone zone in bullish.GetViewBetween(from, to))
                    {
                        if (zone.Indexed && zone.ConfirmIdx < index)
                            _breakScratch.Add(zone);
                    }
                }

                for (int i = 0; i < _breakScratch.Count; i++)
                    UpdateZone(_breakScratch[i], index, true);

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
                _breakScratch.Clear();
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

                for (int i = 0; i < _breakScratch.Count; i++)
                    UpdateZone(_breakScratch[i], index, true);
            }

            _breakScratch.Clear();
        }
    }
}
