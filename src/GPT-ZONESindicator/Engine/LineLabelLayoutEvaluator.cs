using System;
using System.Collections.Generic;

namespace cAlgo
{
    /// <summary>
    /// Pure vertical label placement helper. It searches nearest free prices first, which
    /// keeps labels close to their level while separating labels by at least the requested gap.
    /// </summary>
    internal static class LineLabelLayoutEvaluator
    {
        public static double Place(
            double targetY,
            double visibleBottom,
            double visibleTop,
            double minimumGap,
            IList<double> occupiedY)
        {
            if (occupiedY == null)
                throw new ArgumentNullException(nameof(occupiedY));

            if (double.IsNaN(targetY) || double.IsInfinity(targetY))
                return targetY;

            if (double.IsNaN(visibleBottom) || double.IsInfinity(visibleBottom)
                || double.IsNaN(visibleTop) || double.IsInfinity(visibleTop)
                || visibleTop <= visibleBottom
                || double.IsNaN(minimumGap) || double.IsInfinity(minimumGap)
                || minimumGap <= 0
                || targetY < visibleBottom || targetY > visibleTop)
                return targetY;

            double margin = Math.Min(minimumGap * 0.5, (visibleTop - visibleBottom) * 0.1);
            double min = visibleBottom + margin;
            double max = visibleTop - margin;
            double origin = Clamp(targetY, min, max);
            int steps = (int)Math.Min(100000, Math.Ceiling((max - min) / minimumGap));

            for (int distance = 0; distance <= steps; distance++)
            {
                if (distance == 0)
                {
                    if (IsFree(origin, minimumGap, occupiedY))
                        return origin;
                    continue;
                }

                double up = origin + distance * minimumGap;
                if (up <= max && IsFree(up, minimumGap, occupiedY))
                    return up;

                double down = origin - distance * minimumGap;
                if (down >= min && IsFree(down, minimumGap, occupiedY))
                    return down;
            }

            // A very dense chart may have less space than labels. Keep every label visible
            // and return the in-range position with the fewest close neighbors as a fallback.
            double best = origin;
            int bestConflicts = int.MaxValue;
            for (int slot = 0; slot <= steps; slot++)
            {
                double candidate = min + slot * minimumGap;
                int conflicts = CountConflicts(candidate, minimumGap, occupiedY);
                if (conflicts < bestConflicts)
                {
                    best = candidate;
                    bestConflicts = conflicts;
                    if (conflicts == 0)
                        break;
                }
            }

            return best;
        }

        private static bool IsFree(double candidate, double gap, IList<double> occupiedY)
        {
            for (int index = 0; index < occupiedY.Count; index++)
            {
                if (Math.Abs(candidate - occupiedY[index]) < gap * 0.98)
                    return false;
            }

            return true;
        }

        private static int CountConflicts(double candidate, double gap, IList<double> occupiedY)
        {
            int count = 0;
            for (int index = 0; index < occupiedY.Count; index++)
            {
                if (Math.Abs(candidate - occupiedY[index]) < gap * 0.98)
                    count++;
            }

            return count;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}