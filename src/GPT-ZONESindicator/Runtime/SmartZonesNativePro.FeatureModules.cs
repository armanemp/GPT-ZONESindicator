using System;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        /// <summary>
        /// Registers independent processing and rendering boundaries once during initialization.
        /// The detection files own their algorithms; the pipeline only manages ordering and fault isolation.
        /// </summary>
        private void ConfigureFeaturePipeline()
        {
            _featurePipeline = new FeatureModulePipeline(
                delegate (string name, Exception error)
                {
                    LogError("Feature module [" + name + "]", error);
                });

            // Closed-bar processing. Order is intentional: update existing zones, detect
            // new structure/OBs, then discover new gaps, and finally synchronize HTF zones.
            _featurePipeline.AddProcessor(
                "Zone lifecycle",
                delegate { return true; },
                delegate (int index, double atr) { ProcessIndexedZones(index); });

            _featurePipeline.AddProcessor(
                "Structure and order blocks",
                delegate { return true; },
                delegate (int index, double atr) { ProcessStructure(index, atr); });

            _featurePipeline.AddProcessor(
                "Liquidity sweeps",
                delegate { return DetectSweeps; },
                delegate (int index, double atr) { ProcessSweeps(index, atr); });

            _featurePipeline.AddProcessor(
                "Fair value gaps",
                delegate { return DetectFvg; },
                delegate (int index, double atr) { DetectFvgAt(index, atr); });

            _featurePipeline.AddProcessor(
                "Price gaps",
                delegate { return DetectGap; },
                delegate (int index, double atr) { DetectGapAt(index, atr); });

            _featurePipeline.AddProcessor(
                "Reference level updates",
                delegate { return true; },
                delegate (int index, double atr) { UpdateReference(index); });

            _featurePipeline.AddProcessor(
                "M15 overlay",
                delegate { return DetectM15Zones && _higherTimeframeState.Bars != null; },
                delegate (int index, double atr) { ProcessClosedM15Bars(index); });

            // Chart rendering is also independent. A renderer failure is logged and isolated;
            // later feature modules still get their render pass.
            _featurePipeline.AddRenderer(
                "Zones",
                delegate { return true; },
                delegate (bool refreshMetadata) { RenderZones(refreshMetadata); });

            _featurePipeline.AddRenderer(
                "BOS and CHoCH",
                delegate { return true; },
                delegate (bool refreshMetadata) { RenderStructure(); });

            _featurePipeline.AddRenderer(
                "Equal levels and sweeps",
                delegate { return true; },
                delegate (bool refreshMetadata) { RenderEqualAndSweeps(); });

            _featurePipeline.AddRenderer(
                "Reference levels",
                delegate { return true; },
                delegate (bool refreshMetadata) { RenderRef(); });

            _featurePipeline.AddRenderer(
                "Psychological levels",
                delegate { return true; },
                delegate (bool refreshMetadata) { RenderRound(); });

            _featurePipeline.AddRenderer(
                "Status",
                delegate { return true; },
                delegate (bool refreshMetadata) { RenderStatus(); });
        }

        /// <summary>Computes one cached viewport shared by every line/label renderer.</summary>
        private void UpdateItemViewport()
        {
            _chartViewportState.Valid = false;
            int count = Bars.Count;
            if (count <= 0 || Chart.Width <= 0 || Chart.Height <= 0)
                return;

            int first = Chart.FirstVisibleBarIndex;
            int last = Chart.LastVisibleBarIndex;
            if (first < 0 || last < first || first >= count)
                return;

            first = Math.Max(0, first - 3);
            last = Math.Min(count - 1, last + 3);

            double top = Chart.TopY;
            double bottom = Chart.BottomY;
            if (double.IsNaN(top) || double.IsInfinity(top)
                || double.IsNaN(bottom) || double.IsInfinity(bottom)
                || top <= bottom)
                return;

            double pad = (top - bottom) * 0.06;
            _chartViewportState.Top = top + pad;
            _chartViewportState.Bottom = bottom - pad;
            _chartViewportState.Start = Bars.OpenTimes[first];

            try
            {
                // A small time buffer is enough: future line endpoints can extend past this
                // window, and segment/viewport intersection still keeps the visible section.
                _chartViewportState.End = Bars.OpenTimes[last].Add(_barSpan.Ticks > 0
                    ? TimeSpan.FromTicks(checked(_barSpan.Ticks * 3L))
                    : TimeSpan.FromMinutes(15));
            }
            catch (ArgumentOutOfRangeException)
            {
                _chartViewportState.End = DateTime.MaxValue;
            }
            catch (OverflowException)
            {
                _chartViewportState.End = DateTime.MaxValue;
            }

            _chartViewportState.Valid = true;
        }

        private bool ItemLineIntersectsViewport(Item item, DateTime end)
        {
            if (!_chartViewportState.Valid)
                return true;

            if (item.Y < _chartViewportState.Bottom || item.Y > _chartViewportState.Top)
                return false;

            DateTime start = item.T1;
            return start <= _chartViewportState.End && end >= _chartViewportState.Start;
        }

        private bool ItemLabelIntersectsViewport(DateTime time, double price)
        {
            if (!_chartViewportState.Valid)
                return true;

            return time >= _chartViewportState.Start && time <= _chartViewportState.End
                && price >= _chartViewportState.Bottom && price <= _chartViewportState.Top;
        }
    }
}
