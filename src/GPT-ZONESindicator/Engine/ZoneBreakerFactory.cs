using System;

namespace cAlgo
{
    internal struct ZoneBreakerInput
    {
        public int ParentId;
        public bool ParentBull, Major;
        public double Lower, Upper;
        public int OriginIndex, ConfirmationIndex;
        public DateTime ConfirmationTime;
        public double Atr, Momentum, StructFactor;
    }

    /// <summary>
    /// Platform-independent attributes required to create a breaker after an order block breaks.
    /// The runtime remains responsible for assigning the new zone identity and linking ParentId
    /// to the parent's ChildId.
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

            if (double.IsNaN(input.Lower) || double.IsNaN(input.Upper)
                || input.Lower >= input.Upper)
                throw new ArgumentException("A breaker must preserve a valid positive-width parent zone.", nameof(input));

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