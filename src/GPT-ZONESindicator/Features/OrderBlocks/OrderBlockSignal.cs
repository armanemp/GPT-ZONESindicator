namespace cAlgo
{
    /// <summary>
    /// Immutable hand-off snapshot from market structure to the independent order-block
    /// detector. It deliberately contains values, not references to mutable swing/engine state.
    /// </summary>
    internal readonly struct OrderBlockSignal
    {
        public bool Major { get; }
        public bool SwingHigh { get; }
        public double SwingPrice { get; }
        public double Atr { get; }
        public int SwingIndex { get; }
        public int BreakIndex { get; }

        public OrderBlockSignal(bool major, bool swingHigh, double swingPrice,
            double atr, int swingIndex, int breakIndex)
        {
            Major = major;
            SwingHigh = swingHigh;
            SwingPrice = swingPrice;
            Atr = atr;
            SwingIndex = swingIndex;
            BreakIndex = breakIndex;
        }
    }
}
