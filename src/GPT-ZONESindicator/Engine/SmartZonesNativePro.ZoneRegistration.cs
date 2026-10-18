using System;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private static long MakeKey(SznpKind kind, bool bull, long lower, long upper)
        {
            unchecked
            {
                long hash = (int)kind * 2 + (bull ? 1 : 0);
                hash = hash * 1000003L + lower;
                hash = hash * 1000003L + upper;
                return hash;
            }
        }

        private bool IsDuplicate(SznpKind kind, bool bull, double lower, double upper)
        {
            if (!FilterDuplicateZones)
                return false;

            long key = MakeKey(
                kind,
                bull,
                (long)Math.Round(lower / _tick),
                (long)Math.Round(upper / _tick));

            Zone existing;
            return _dupe.TryGetValue(key, out existing)
                && existing.State != SznpState.Broken
                && Math.Abs(existing.Lower - lower) < _tick * 0.5
                && Math.Abs(existing.Upper - upper) < _tick * 0.5;
        }

        private void AddZone(Zone zone)
        {
            zone.Id = _nextId++;
            zone.ConfirmTime = CloseTimeOf(zone.ConfirmIdx);

            for (int k = 0; k < _alive.Count; k++)
            {
                Zone other = _alive[k];
                if (other.State == SznpState.Broken || other.Kind == zone.Kind || other.Bull != zone.Bull)
                    continue;

                double overlap = Math.Min(other.Upper, zone.Upper) - Math.Max(other.Lower, zone.Lower);
                if (overlap > 0)
                {
                    other.Confluence++;
                    zone.Confluence++;
                }
            }

            _zones.Add(zone);
            _alive.Add(zone);
            RegisterZoneForLifecycle(zone);

            if (FilterDuplicateZones)
            {
                long key = MakeKey(
                    zone.Kind,
                    zone.Bull,
                    (long)Math.Round(zone.Lower / _tick),
                    (long)Math.Round(zone.Upper / _tick));
                _dupe[key] = zone;
            }
        }
    }
}