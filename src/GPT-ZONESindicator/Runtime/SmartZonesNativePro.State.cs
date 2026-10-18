using System;
using System.Collections.Generic;
using cAlgo.API;

namespace cAlgo
{
    public partial class SmartZonesNativePro
    {
        private sealed class ZoneLabelSlot
        {
            public DateTime Time;
            public double Y;
        }

        /// <summary>Logical zone record; chart objects are a disposable view of this state.</summary>
        private sealed class Zone
        {
            public int Id;
            public SznpKind Kind;
            public bool Bull;
            public double Lower, Upper;
            public int OriginIdx, ConfirmIdx;
            public int FillIdx = -1, BreakIdx = -1;
            public DateTime StartTime, ConfirmTime, FillTime, BreakTime;
            public SznpState State;
            public int TouchCount;
            public double MaxDepth;
            public int LastEnteredIdx = -1;
            public bool Indexed;
            public double Atr, Momentum, StructFactor;
            public int Confluence;
            public bool Major;
            public bool IsHigherTimeframe;
            public int ParentId = -1, ChildId = -1;
            public double Score;
        }

        /// <summary>Line, optional label, and last-applied view snapshot.</summary>
        /// <summary>
        /// Per-zone chart presentation cache. Detection and lifecycle logic must not
        /// depend on these disposable native chart-object references.
        /// </summary>
        private sealed class ZoneRenderState
        {
            public bool RenderTracked, Want, Shown;
            public double Score;
            public ChartRectangle Rectangle;
            public ChartTrendLine Midline;
            public ChartText Label;
            public DateTime AppliedEndTime;
            public double AppliedLabelY;
            public Color AppliedColor;
            public string AppliedComment, AppliedLabelText;
        }

        private sealed class Item
        {
            public string Name;
            public bool TextOnly;
            public double Y;
            public DateTime T1, T2;
            public Color Col;
            public int Thick = 1;
            public LineStyle Style = LineStyle.Dots;
            public string Label;
            public DateTime LabelTime;
            public double LabelY;
            public bool ExtendToInfinity;
            public HorizontalAlignment LabelH = HorizontalAlignment.Center;
            public VerticalAlignment LabelV = VerticalAlignment.Bottom;
            public bool Follow, Dead, Shown;
            public ChartTrendLine Line;
            public ChartText Txt;
            public DateTime aT1, aT2, aLt;
            public double aY, aLy;
            public Color aCol;
            public string aLabel;
        }

        private sealed class Swing
        {
            public int Idx, ConfIdx;
            public DateTime Time;
            public double Price;
            public bool High, Major;
            public bool Broken, SilentBreak, Choch, BreakBull, Swept;
            public int BreakIdx = -1;
            public DateTime BreakTime;
            public EqCluster Cluster;
            public Item Level, Mark;
        }

        private sealed class Engine
        {
            public bool Major;
            public int Left, Right;
            public double MinAtr, MinPips;
            public readonly List<Swing> Swings = new List<Swing>();
            public readonly List<Swing> Open = new List<Swing>();
            public Swing LastHigh, LastLow;
            public int Trend;
        }

        private sealed class HigherTimeframeSwing
        {
            public int Index;
            public double Price;
            public bool High, Broken;
        }

        private sealed class EqCluster
        {
            public bool High;
            public double Level;
            public int FirstIdx, LastIdx, Count, EndIdx = -1;
            public DateTime FirstTime, LastTime, EndTime;
            public bool Swept, Broken;
            public Item Vis;
        }

        private sealed class SweepEv
        {
            public bool Bull;
            public double Level;
            public DateTime T1, T2;
            public Item Vis;
        }

        private sealed class HtfTrack
        {
            public string Short, OpenName;
            public Bars Data;
            public int Cur = -1, Keep = 1;
            public bool ShowHL, ShowMid, ShowOpen;
            public Color Col;
            public List<Item> Current;
            public readonly Queue<List<Item>> Groups = new Queue<List<Item>>();
        }

        private sealed class SessionTrack
        {
            public string Name;
            public bool Enabled;
            public double StartH, EndH;
            public SznpDst Dst;
            public Color Col;
            public DateTime WinStart = DateTime.MinValue;
            public double Hi, Lo;
            public Item HiItem, LoItem;
            public readonly Queue<Item[]> Past = new Queue<Item[]>();
        }

        private Func<int, double> _highPriceAt, _lowPriceAt, _openPriceAt, _closePriceAt;
        private FeatureModulePipeline _featurePipeline;
        private bool _itemViewportValid;
        private DateTime _itemViewStart, _itemViewEnd;
        private double _itemViewTop, _itemViewBottom;
        private Bars _m15Bars;
        private int _m15StartIndex, _lastM15Processed = -1;
        private int _m15HistoryRequests;
        private bool _m15HistoryLoading;
        private DateTime _m15ZoneStartTime;
        private double _m15TrSum;
        private int _m15Trend, _m15LastBullBreak = -1, _m15LastBearBreak = -1;
        private HigherTimeframeSwing _m15LastHigh, _m15LastLow;
        private readonly List<double> _m15Atr = new List<double>();
        private readonly List<HigherTimeframeSwing> _m15Swings = new List<HigherTimeframeSwing>();
        private readonly List<HigherTimeframeSwing> _m15OpenSwings = new List<HigherTimeframeSwing>();
        private string _prefix = "SZNP_";
        private double _pip = 0.01, _tick = 0.01;
        private TimeSpan _barSpan = TimeSpan.FromMinutes(5);
        private DateTime _futureTime;
        private int _startIdx, _lastProcessed;
        private DateTime _firstOpenTime;
        private bool _rebuildPending, _histLoading, _renderDirty, _viewportDirty;
        private int _histRequests, _errorCount, _nextId = 1;
        private double _lastClose;
        private DateTime _lastBarTime;

        private readonly List<double> _atr = new List<double>();
        private double _trSum;
        private readonly List<Zone> _zones = new List<Zone>();
        private readonly List<Zone> _alive = new List<Zone>();
        private readonly ZoneIntervalIndex<Zone> _zoneIntervalIndex =
            new ZoneIntervalIndex<Zone>(
                delegate (Zone zone) { return zone.Lower; },
                delegate (Zone zone) { return zone.Upper; },
                delegate (Zone zone) { return zone.Id; });
        private readonly ZoneBreakCandidateIndex<Zone> _zoneBreakCandidateIndex =
            new ZoneBreakCandidateIndex<Zone>(
                delegate (Zone zone) { return zone.Lower; },
                delegate (Zone zone) { return zone.Upper; },
                delegate (Zone zone) { return zone.Id; },
                delegate (Zone zone) { return zone.ConfirmIdx; },
                delegate (bool lowerBound, double price, int id)
                {
                    return lowerBound
                        ? new Zone { Lower = price, Id = id }
                        : new Zone { Upper = price, Id = id };
                });
        private readonly SznpBreakMode[] _breakModes = new SznpBreakMode[4];
        private readonly List<Zone> _overlapScratch = new List<Zone>();
        private readonly List<Zone> _breakScratch = new List<Zone>();
        private readonly List<Zone> _renderQueryScratch = new List<Zone>();
        private readonly List<Zone> _renderTracked = new List<Zone>();
        private readonly Dictionary<int, ZoneRenderState> _zoneRenderStates =
            new Dictionary<int, ZoneRenderState>();
        private readonly List<ZoneLabelSlot> _zoneLabelRows = new List<ZoneLabelSlot>(128);
        private readonly Dictionary<long, Zone> _dupe = new Dictionary<long, Zone>();
        private readonly Dictionary<int, Zone> _obByCandle = new Dictionary<int, Zone>();
        private Engine _engMajor, _engInt;
        private readonly List<EqCluster> _eqs = new List<EqCluster>();
        private readonly List<EqCluster> _eqOpen = new List<EqCluster>();
        private readonly List<SweepEv> _sweeps = new List<SweepEv>();
        private int _lastBullBreak, _lastBearBreak, _lastSweepBull, _lastSweepBear;
        private int _nameSeq;
        private readonly List<Item> _refItems = new List<Item>();
        private readonly List<HtfTrack> _htf = new List<HtfTrack>();
        private readonly List<SessionTrack> _sess = new List<SessionTrack>();
        private readonly Dictionary<long, Item> _round = new Dictionary<long, Item>();
        private long _roundCenter = long.MinValue;
        private double _roundStepUsed;
        private ChartStaticText _status;
        private readonly Dictionary<string, ChartObject> _reg = new Dictionary<string, ChartObject>();

        private readonly Color[] _zoneBase = new Color[8];
        private readonly Color[] _midBase = new Color[4];
        private Color _cFilled, _cBroken;
        private Color _cStructBull, _cStructBear, _cChoch, _cSwingHigh, _cSwingLow, _cLiquidity;
        private Color _cPrevDay, _cPrevWeek, _cPrevMonth, _cOpen, _cAsia, _cLondon, _cNy, _cRound, _cRoundMajor;

        private readonly Dictionary<string, int> _rej = new Dictionary<string, int>();
        private int _hiddenByBudget, _shownZones;
        private long _procTicks, _procBars, _renderTicks, _renderCount, _initTicks;

        private static readonly Predicate<Swing> IsClosedSwing = delegate (Swing s) { return s.Broken || s.Swept; };
        private static readonly Predicate<EqCluster> IsClosedCluster = delegate (EqCluster c) { return c.Swept || c.Broken; };
        private readonly double[] _fac = new double[7];
        private readonly List<Zone> _cand = new List<Zone>();
        private readonly List<Zone> _tmp = new List<Zone>();
        private readonly List<Zone> _acc = new List<Zone>();
    }
}