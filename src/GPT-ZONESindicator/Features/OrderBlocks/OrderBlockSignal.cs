namespace cAlgo
{
    /// <summary>
    /// Immutable hand-off snapshot from market structure to the independent order-block
    /// detector. It deliberately contains values, not references to mutable swing/engine state.
    /// </summary>
    internal struct OrderBlockSignal
    {
        public bool Major;
        public bool SwingHigh;
        public double SwingPrice;
        public double Atr;
        public int SwingIndex;
        public int BreakIndex;
    }
}
