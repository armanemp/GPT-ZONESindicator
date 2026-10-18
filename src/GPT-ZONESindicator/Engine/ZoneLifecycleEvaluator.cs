namespace cAlgo
{
    /// <summary>Closed-bar input for the pure zone lifecycle decision step.</summary>
    internal struct ZoneLifecycleInput
    {
        public int Index;
        public bool Bull;
        public double Lower, Upper;
        public double Open, High, Low, Close;
        public SznpBreakMode BreakMode;
        public SznpState State;
        public double MaxDepth;
        public int TouchCount;
        public int LastEnteredIndex;
        public double PartialFillPct;
        public bool CountTouch;
    }

    /// <summary>
    /// Result of evaluating one closed candle against one zone. This value does not
    /// perform chart drawing, index mutation, timestamps, or breaker creation.
    /// </summary>
    internal struct ZoneLifecycleResult
    {
        public bool Processed;
        public bool Entered;
        public bool FullyFilled;
        public bool BrokeNow;
        public bool FilledNow;
        public double Depth, MaxDepth;
        public int TouchCount, LastEnteredIndex;
        public SznpState State;
    }

    /// <summary>
    /// Pure zone-state rules shared by the cTrader runtime and dependency-free regression tests.
    /// A zone's price boundaries are inputs and are never moved by this evaluator.
    /// </summary>
    internal static class ZoneLifecycleEvaluator
    {
        public static ZoneLifecycleResult Evaluate(ZoneLifecycleInput input)
        {
            ZoneLifecycleResult result = new ZoneLifecycleResult
            {
                State = input.State,
                MaxDepth = input.MaxDepth,
                TouchCount = input.TouchCount,
                LastEnteredIndex = input.LastEnteredIndex
            };

            if (input.State == SznpState.Broken)
                return result;

            double size = input.Upper - input.Lower;
            if (size <= 0)
                return result;

            result.Processed = true;

            double depth;
            bool entered;
            bool fullyFilled;
            bool broken;

            if (input.Bull)
            {
                entered = input.Low < input.Upper;
                depth = entered ? Math.Min(input.Upper - input.Low, size) : 0.0;
                fullyFilled = input.Low <= input.Lower;

                switch (input.BreakMode)
                {
                    case SznpBreakMode.CandleClose:
                        broken = input.Close < input.Lower;
                        break;
                    case SznpBreakMode.BodyBeyond:
                        broken = Math.Max(input.Open, input.Close) < input.Lower;
                        break;
                    default:
                        broken = input.Low < input.Lower;
                        break;
                }
            }
            else
            {
                entered = input.High > input.Lower;
                depth = entered ? Math.Min(input.High - input.Lower, size) : 0.0;
                fullyFilled = input.High >= input.Upper;

                switch (input.BreakMode)
                {
                    case SznpBreakMode.CandleClose:
                        broken = input.Close > input.Upper;
                        break;
                    case SznpBreakMode.BodyBeyond:
                        broken = Math.Min(input.Open, input.Close) > input.Upper;
                        break;
                    default:
                        broken = input.High > input.Upper;
                        break;
                }
            }

            result.Entered = entered;
            result.FullyFilled = fullyFilled;
            result.Depth = depth;

            // Break predicates take priority over touch/fill transitions, matching
            // the live indicator's existing lifecycle contract.
            if (broken)
            {
                result.MaxDepth = Math.Max(input.MaxDepth, depth);
                result.State = SznpState.Broken;
                result.BrokeNow = true;
                return result;
            }

            if (entered)
            {
                if (depth > result.MaxDepth)
                    result.MaxDepth = depth;

                if (input.CountTouch && input.LastEnteredIndex != input.Index - 1)
                    result.TouchCount++;

                result.LastEnteredIndex = input.Index;
            }

            if (input.State == SznpState.Filled)
                return result;

            if (fullyFilled)
            {
                result.State = SznpState.Filled;
                result.FilledNow = true;
                return result;
            }

            if (result.MaxDepth > 0)
            {
                result.State = result.MaxDepth / size >= input.PartialFillPct / 100.0
                    ? SznpState.Partial
                    : SznpState.Touched;
            }

            return result;
        }
    }
}