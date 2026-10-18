using System;
using System.Collections.Generic;

namespace cAlgo
{
    /// <summary>
    /// Augmented treap for interval-overlap lookups. The caller owns lifecycle state;
    /// this index only stores references and never mutates indexed items.
    /// </summary>
    internal sealed class ZoneIntervalIndex<T> where T : class
    {
        private sealed class Node
        {
            public T Item;
            public Node Left, Right;
            public uint Priority;
            public double MaxUpper;
        }

        private readonly Func<T, double> _lower;
        private readonly Func<T, double> _upper;
        private readonly Func<T, int> _id;
        private Node _root;

        public int Count { get; private set; }

        public ZoneIntervalIndex(
            Func<T, double> lower,
            Func<T, double> upper,
            Func<T, int> id)
        {
            if (lower == null) throw new ArgumentNullException(nameof(lower));
            if (upper == null) throw new ArgumentNullException(nameof(upper));
            if (id == null) throw new ArgumentNullException(nameof(id));

            _lower = lower;
            _upper = upper;
            _id = id;
        }

        public void Clear()
        {
            _root = null;
            Count = 0;
        }

        public void Add(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            double lower = _lower(item);
            double upper = _upper(item);
            if (double.IsNaN(lower) || double.IsNaN(upper) || lower > upper)
                throw new ArgumentException("An indexed interval must have finite-orderable bounds with lower <= upper.", nameof(item));

            if (ContainsKey(item))
                throw new InvalidOperationException("An interval with the same lower bound and identity is already indexed.");

            _root = Insert(_root, item);
            Count++;
        }

        public bool Remove(T item)
        {
            if (item == null || _root == null)
                return false;

            bool removed;
            _root = RemoveNode(_root, item, out removed);
            if (removed)
                Count--;
            return removed;
        }

        /// <summary>
        /// Appends all intervals intersecting [low, high] in lower-bound/identity order.
        /// The caller may reuse and clear the supplied list between queries.
        /// </summary>
        public void Query(double low, double high, List<T> matches)
        {
            if (matches == null) throw new ArgumentNullException(nameof(matches));
            if (double.IsNaN(low) || double.IsNaN(high) || low > high)
                return;

            QueryNode(_root, low, high, matches);
        }

        private int CompareLower(T left, T right)
        {
            int price = _lower(left).CompareTo(_lower(right));
            return price != 0 ? price : _id(left).CompareTo(_id(right));
        }

        private bool ContainsKey(T item)
        {
            Node node = _root;
            while (node != null)
            {
                int comparison = CompareLower(item, node.Item);
                if (comparison == 0)
                    return true;
                node = comparison < 0 ? node.Left : node.Right;
            }

            return false;
        }

        private static uint PriorityFor(int identity)
        {
            uint value = unchecked((uint)identity + 0x9E3779B9u);
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }

        private void Refresh(Node node)
        {
            if (node == null) return;

            double max = _upper(node.Item);
            if (node.Left != null) max = Math.Max(max, node.Left.MaxUpper);
            if (node.Right != null) max = Math.Max(max, node.Right.MaxUpper);
            node.MaxUpper = max;
        }

        private Node RotateRight(Node root)
        {
            Node pivot = root.Left;
            root.Left = pivot.Right;
            pivot.Right = root;
            Refresh(root);
            Refresh(pivot);
            return pivot;
        }

        private Node RotateLeft(Node root)
        {
            Node pivot = root.Right;
            root.Right = pivot.Left;
            pivot.Left = root;
            Refresh(root);
            Refresh(pivot);
            return pivot;
        }

        private Node Insert(Node root, T item)
        {
            if (root == null)
            {
                return new Node
                {
                    Item = item,
                    Priority = PriorityFor(_id(item)),
                    MaxUpper = _upper(item)
                };
            }

            if (CompareLower(item, root.Item) < 0)
            {
                root.Left = Insert(root.Left, item);
                if (root.Left.Priority < root.Priority)
                    root = RotateRight(root);
            }
            else
            {
                root.Right = Insert(root.Right, item);
                if (root.Right.Priority < root.Priority)
                    root = RotateLeft(root);
            }

            Refresh(root);
            return root;
        }

        private Node Merge(Node left, Node right)
        {
            if (left == null) return right;
            if (right == null) return left;

            if (left.Priority < right.Priority)
            {
                left.Right = Merge(left.Right, right);
                Refresh(left);
                return left;
            }

            right.Left = Merge(left, right.Left);
            Refresh(right);
            return right;
        }

        private Node RemoveNode(Node root, T item, out bool removed)
        {
            if (root == null)
            {
                removed = false;
                return null;
            }

            int comparison = CompareLower(item, root.Item);
            if (comparison < 0)
            {
                root.Left = RemoveNode(root.Left, item, out removed);
                Refresh(root);
                return root;
            }

            if (comparison > 0)
            {
                root.Right = RemoveNode(root.Right, item, out removed);
                Refresh(root);
                return root;
            }

            removed = true;
            return Merge(root.Left, root.Right);
        }

        private void QueryNode(Node node, double low, double high, List<T> matches)
        {
            if (node == null) return;

            if (node.Left != null && node.Left.MaxUpper >= low)
                QueryNode(node.Left, low, high, matches);

            if (_lower(node.Item) <= high && _upper(node.Item) >= low)
                matches.Add(node.Item);

            // Every item in the right subtree has a lower bound >= this node's lower bound.
            if (_lower(node.Item) <= high)
                QueryNode(node.Right, low, high, matches);
        }
    }
}