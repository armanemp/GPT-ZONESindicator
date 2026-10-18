using System;
using System.Collections.Generic;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private void ResetZoneLifecycleIndexes()
        {
            _zoneIntervalIndex.Clear();
            _zoneBreakCandidateIndex.Clear();
            _overlapScratch.Clear();
            _breakScratch.Clear();
        }

        private void RegisterZoneForLifecycle(Zone zone)
        {
            if (zone == null || zone.Indexed || zone.State == SznpState.Broken)
                return;

            _zoneIntervalIndex.Add(zone);
            _zoneBreakCandidateIndex.Add(zone, (int)zone.Kind, zone.Bull);
            zone.Indexed = true;
        }

        private void UnregisterZoneForLifecycle(Zone zone)
        {
            if (zone == null || !zone.Indexed)
                return;

            _zoneIntervalIndex.Remove(zone);
            _zoneBreakCandidateIndex.Remove(zone, (int)zone.Kind, zone.Bull);
            zone.Indexed = false;
        }

        /// <summary>
        /// Processes far-edge break candidates first, then price-overlapping zones.
        /// The lifecycle state machine remains centralized in UpdateZone.
        /// </summary>
        private void ProcessIndexedZones(int index)
        {
            ProcessIndexedBreakCandidates(index);

            _overlapScratch.Clear();
            _zoneIntervalIndex.Query(L(index), H(index), _overlapScratch);

            for (int i = 0; i < _overlapScratch.Count; i++)
            {
                Zone zone = _overlapScratch[i];
                if (zone == null || !zone.Indexed || zone.State == SznpState.Broken
                    || zone.ConfirmIdx >= index)
                    continue;

                UpdateZone(zone, index, true);
            }
        }

        private void ProcessIndexedBreakCandidates(int index)
        {
            _breakModes[0] = FvgBreakMode;
            _breakModes[1] = GapBreakMode;
            _breakModes[2] = ObBreakMode;
            _breakModes[3] = BreakerBreakMode;

            _zoneBreakCandidateIndex.QueryCandidates(
                index, O(index), H(index), L(index), C(index), _breakModes, _breakScratch);

            for (int i = 0; i < _breakScratch.Count; i++)
            {
                Zone zone = _breakScratch[i];
                if (zone.Indexed && zone.ConfirmIdx < index)
                    UpdateZone(zone, index, true);
            }

            _breakScratch.Clear();
        }
    }
}
