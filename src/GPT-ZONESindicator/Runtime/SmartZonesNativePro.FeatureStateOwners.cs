using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        // Every group is owned by one feature. Cross-feature access is now explicit
        // at the call site; only the logical zone repository remains intentionally shared.
        private sealed class MarketStructureFeatureState
        {
            public Engine MajorEngine;
            public Engine InternalEngine;
            public int LastBullBreak;
            public int LastBearBreak;
        }

        private sealed class OrderBlockFeatureState
        {
            public readonly Dictionary<int, Zone> ByCandle = new Dictionary<int, Zone>();
            public readonly List<OrderBlockSignal> PendingSignals = new List<OrderBlockSignal>();
        }

        private sealed class EqualLevelsFeatureState
        {
            public readonly List<EqCluster> Clusters = new List<EqCluster>();
            public readonly List<EqCluster> OpenClusters = new List<EqCluster>();
            public readonly List<EqualLevelSignal> PendingSwings = new List<EqualLevelSignal>();
            public readonly Dictionary<Swing, EqCluster> ClusterBySwing = new Dictionary<Swing, EqCluster>();
        }

        private sealed class LiquiditySweepFeatureState
        {
            public readonly List<SweepEv> Events = new List<SweepEv>();
            public int LastBullSweep;
            public int LastBearSweep;
        }

        private sealed class HigherTimeframeFeatureState
        {
            public Bars Bars;
            public int StartIndex;
            public int LastProcessed = -1;
            public int HistoryRequests;
            public bool HistoryLoading;
            public DateTime ZoneStartTime;
            public double TrueRangeSum;
            public int Trend;
            public int LastBullBreak = -1;
            public int LastBearBreak = -1;
            public HigherTimeframeSwing LastHigh;
            public HigherTimeframeSwing LastLow;
            public readonly List<double> Atr = new List<double>();
            public readonly List<HigherTimeframeSwing> Swings = new List<HigherTimeframeSwing>();
            public readonly List<HigherTimeframeSwing> OpenSwings = new List<HigherTimeframeSwing>();
        }

        private sealed class ReferenceLevelFeatureState
        {
            public readonly List<Item> Items = new List<Item>();
            public readonly List<HtfTrack> Tracks = new List<HtfTrack>();
            public readonly List<SessionTrack> Sessions = new List<SessionTrack>();
        }

        private sealed class PsychologicalLevelFeatureState
        {
            public readonly Dictionary<long, Item> Items = new Dictionary<long, Item>();
            public long Center = long.MinValue;
            public double StepUsed;
        }

        private sealed class ChartViewportFeatureState
        {
            public bool Valid;
            public DateTime Start, End;
            public double Top, Bottom;
        }

        private sealed class ZoneRenderingFeatureState
        {
            public readonly Dictionary<int, ZoneRenderState> States = new Dictionary<int, ZoneRenderState>();
            public readonly List<Zone> QueryScratch = new List<Zone>();
            public readonly List<Zone> Tracked = new List<Zone>();
            public readonly List<ZoneLabelSlot> LabelRows = new List<ZoneLabelSlot>(128);
            public int HiddenByBudget;
            public int ShownZones;
        }

        private readonly MarketStructureFeatureState _marketStructureState = new MarketStructureFeatureState();
        private readonly OrderBlockFeatureState _orderBlockState = new OrderBlockFeatureState();
        private readonly EqualLevelsFeatureState _equalLevelsState = new EqualLevelsFeatureState();
        private readonly LiquiditySweepFeatureState _liquiditySweepState = new LiquiditySweepFeatureState();
        private readonly HigherTimeframeFeatureState _higherTimeframeState = new HigherTimeframeFeatureState();
        private readonly ReferenceLevelFeatureState _referenceLevelState = new ReferenceLevelFeatureState();
        private readonly PsychologicalLevelFeatureState _psychologicalLevelState = new PsychologicalLevelFeatureState();
        private readonly ChartViewportFeatureState _chartViewportState = new ChartViewportFeatureState();
        private readonly ZoneRenderingFeatureState _zoneRenderingState = new ZoneRenderingFeatureState();
    }
}
