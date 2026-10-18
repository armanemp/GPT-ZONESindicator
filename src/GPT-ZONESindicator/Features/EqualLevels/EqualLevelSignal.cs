namespace cAlgo
{
    /// <summary>
    /// Immutable confirmed-swing snapshot sent from market structure to equal-level
    /// detection. This keeps the equal-level rules independent from swing mutation.
    /// </summary>
    internal struct EqualLevelSignal
    {
        public bool Major;
        public bool High;
        public int Index;
        public int ConfirmedIndex;
        public System.DateTime Time;
        public double Price;
        public double Atr;
    }
}
