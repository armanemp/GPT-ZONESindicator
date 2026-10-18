using System;

namespace cAlgo
{
    /// <summary>
    /// Immutable confirmed-swing snapshot sent from market structure to equal-level
    /// detection. No mutable Swing object or rendering reference crosses this boundary.
    /// </summary>
    internal readonly struct EqualLevelSignal
    {
        public bool Major { get; }
        public bool High { get; }
        public int Index { get; }
        public int ConfirmedIndex { get; }
        public DateTime Time { get; }
        public double Price { get; }
        public double Atr { get; }

        public EqualLevelSignal(bool major, bool high, int index, int confirmedIndex,
            DateTime time, double price, double atr)
        {
            Major = major;
            High = high;
            Index = index;
            ConfirmedIndex = confirmedIndex;
            Time = time;
            Price = price;
            Atr = atr;
        }
    }
}
