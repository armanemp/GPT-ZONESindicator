namespace cAlgo
{
    internal enum OrderBlockBoundsFailure
    {
        None,
        Oversize,
        TooSmall
    }

    internal struct OrderBlockBoundsResult
    {
        public bool Accepted;
        public bool UsedBodyFallback;
        public double Lower;
        public double Upper;
        public OrderBlockBoundsFailure Failure;
    }

    /// <summary>Pure order-block source and candle-range filters shared by runtime and tests.</summary>
    internal static class OrderBlockRules
    {
        public static bool IsSourceAllowed(SznpStructureSource source, bool major)
        {
            switch (source)
            {
                case SznpStructureSource.MajorAndInternal:
                    return true;
                case SznpStructureSource.MajorOnly:
                    return major;
                case SznpStructureSource.InternalOnly:
                    return !major;
                default:
                    return false;
            }
        }

        public static OrderBlockBoundsResult EvaluateBounds(
            double candleLow,
            double candleHigh,
            double open,
            double close,
            SznpObRange rangeMode,
            double maxSize,
            bool useBodyIfOversize,
            double minSize)
        {
            OrderBlockBoundsResult result = new OrderBlockBoundsResult();

            bool fullCandle = rangeMode == SznpObRange.FullCandle;
            result.Lower = fullCandle ? candleLow : System.Math.Min(open, close);
            result.Upper = fullCandle ? candleHigh : System.Math.Max(open, close);

            if (maxSize > 0 && result.Upper - result.Lower > maxSize)
            {
                if (useBodyIfOversize && fullCandle)
                {
                    result.Lower = System.Math.Min(open, close);
                    result.Upper = System.Math.Max(open, close);
                    result.UsedBodyFallback = true;

                    if (result.Upper - result.Lower > maxSize)
                    {
                        result.Failure = OrderBlockBoundsFailure.Oversize;
                        return result;
                    }
                }
                else
                {
                    result.Failure = OrderBlockBoundsFailure.Oversize;
                    return result;
                }
            }

            if (result.Upper - result.Lower < minSize)
            {
                result.Failure = OrderBlockBoundsFailure.TooSmall;
                return result;
            }

            result.Accepted = true;
            result.Failure = OrderBlockBoundsFailure.None;
            return result;
        }
    }
}
