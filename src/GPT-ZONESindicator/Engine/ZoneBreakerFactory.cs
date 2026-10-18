using System;

namespace cAlgo
{
    internal struct ZoneBreakerInput
    {
        public int ParentId;
        public SznpKind ParentKind;
        public SznpState ParentState;
        public int ExistingChildId;
        public bool ParentBull, Major;
        public double Lower, Upper;
        public int OriginIndex, ConfirmationIndex;
        public DateTime ConfirmationTime;
        public double Atr, Momentum, StructFactor;
    }

    /// <summary>
    /// Platform-independent attributes required to create a breaker from a confirmed,
    /// invalidated order block. The runtime assigns the new identity and links both zones.
    /// </summary>
    internal struct ZoneBreakerSpec
    {
        public int ParentId;
        public bool Bull, Major;
        public double Lower, Upper;
        public int OriginIndex, ConfirmationIndex;
        public DateTime StartTime;
        public double Atr, Momentum, StructFactor;
    }

    internal static class ZoneBreakerFactory
    {
        public static ZoneBreakerSpec Create(ZoneBreakerInput input)
        {
            if (input.ParentId <= 0)
                throw new ArgumentOutOfRangeException(nameof(input), "Parent zone identity must be positive.");

            if (input.ParentKind != SznpKind.OrderBlock)
                throw new InvalidOperationException("Only a broken order block can create a breaker.");

            if (input.ParentState != SznpState.Broken)
                throw new InvalidOperationException("A breaker requires a parent order block in Broken state.");

            if (input.ExistingChildId > 0)
                throw new InvalidOperationException("The parent order block already has a breaker child.");

            if (input.OriginIndex < 0 || input.ConfirmationIndex <= input.OriginIndex)
                throw new ArgumentException("Breaker confirmation must occur after the parent zone origin.", nameof(input));

            if (input.ConfirmationTime == DateTime.MinValue)
                throw new ArgumentException("Breaker confirmation time must be present.", nameof(input));

            if (double.IsNaN(input.Lower) || double.IsInfinity(input.Lower)
                || double.IsNaN(input.Upper) || double.IsInfinity(input.Upper)
                || input.Lower >= input.Upper)
                throw new ArgumentException("A breaker must preserve finite, positive-width parent bounds.", nameof(input));

            if (double.IsNaN(input.Atr) || double.IsInfinity(input.Atr) || input.Atr <= 0
                || double.IsNaN(input.Momentum) || double.IsInfinity(input.Momentum)
                || double.IsNaN(input.StructFactor) || double.IsInfinity(input.StructFactor))
                throw new ArgumentException("Breaker scoring context must contain finite values and positive ATR.", nameof(input));

            return new ZoneBreakerSpec
            {
                ParentId = input.ParentId,
                Bull = !input.ParentBull,
                Lower = input.Lower,
                Upper = input.Upper,
                OriginIndex = input.OriginIndex,
                ConfirmationIndex = input.ConfirmationIndex,
                StartTime = input.ConfirmationTime,
                Atr = input.Atr,
                Momentum = input.Momentum,
                StructFactor = input.StructFactor,
                Major = input.Major
            };
        }
    }
}