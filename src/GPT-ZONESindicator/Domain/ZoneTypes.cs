namespace cAlgo
{
    public enum SznpBreakMode { CandleClose, BodyBeyond, WickBeyond }
    public enum SznpDisplayMode { AllInLookback, FreshOnly, TopRanked, NearPrice }
    public enum SznpGapMethod { OpenCloseGap, RangeGap, Both }
    public enum SznpGapCombine { KeepBoth, PreferRangeGap, PreferOpenCloseGap }
    public enum SznpObRange { FullCandle, BodyOnly }
    public enum SznpStructureSource { MajorOnly, InternalOnly, MajorAndInternal }
    public enum SznpToleranceMode { Atr, Pips }
    public enum SznpDst { None, UK, US }
    public enum SznpStartMode { FirstCandle, ConfirmationCandle }
    public enum SznpSweepMeasure { Wick, Body }

    internal enum SznpKind { Fvg = 0, Gap = 1, OrderBlock = 2, Breaker = 3 }
    internal enum SznpState { Fresh, Touched, Partial, Filled, Broken }
}