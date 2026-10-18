# Feature modules and lightweight rendering

This document describes the source ownership boundaries for the composite Smart Zones indicator.

## Entry point and execution

- `SmartZonesNativePro.cs` is the cTrader entry point.
- `Runtime/SmartZonesNativePro.Lifecycle.cs` owns cTrader lifecycle events, closed-bar scheduling and viewport invalidation.
- `Engine/FeatureModulePipeline.cs` executes each processing/rendering module in its own exception boundary. A failure in a feature is logged against that feature name and does not abort the remaining modules.
- `Runtime/SmartZonesNativePro.FeatureModules.cs` is the composition root: it registers modules and their ordering. Keep algorithms out of this file.

## Feature ownership

| Feature | Algorithm source | Parameter source | Rendering/state |
|---|---|---|---|
| Fair Value Gaps | `Features/FairValueGaps/SmartZonesNativePro.Fvg.cs`, `FairValueGapDetectorEvaluator.cs`, `SmartZonesNativePro.M15Fvg.cs` | `Configuration/Parameters.FairValueGaps.cs` | Dedicated detector rules; shared zone lifecycle and zone renderer |
| Price Gaps | `Features/PriceGaps/SmartZonesNativePro.PriceGaps.cs`, `PriceGapDetectorEvaluator.cs`, `SmartZonesNativePro.M15Gaps.cs` | `Configuration/Parameters.PriceGaps.cs` | Dedicated detector rules; shared zone lifecycle and zone renderer |
| Market Structure (BOS/CHoCH) | `Features/MarketStructure/SmartZonesNativePro.Structure.cs`, `PivotEvaluator.cs`, `StructureBreakEvaluator.cs`, `StructureSwingVisibilityEvaluator.cs`, `StructureSwingDistanceEvaluator.cs` | `Configuration/Parameters.MarketStructure.cs` | Own engine/state; emits immutable `OrderBlockSignal` and `EqualLevelSignal` snapshots; pivot OHLC inputs are validated; dual-pivot distance filtering is order-independent; swept levels never remain in active-level rendering |
| Order Blocks | `Features/OrderBlocks/SmartZonesNativePro.OrderBlocks.cs`, `SmartZonesNativePro.M15OrderBlocks.cs`, `OrderBlockSignal.cs`, candidate/rule evaluators | `Configuration/Parameters.OrderBlocks.cs` | Consumes break snapshots; M5 and M15 handlers are separate; shared zone lifecycle |
| Breaker Blocks | Breaker creation in `Features/ZoneLifecycle/SmartZonesNativePro.ZoneStateAndScoring.cs` and `ZoneBreakerFactory.cs` | `Configuration/Parameters.Breakers.cs` | Shared zone lifecycle and zone renderer |
| Equal Highs/Lows | `Features/EqualLevels/SmartZonesNativePro.EqualLevels.cs`, `EqualLevelSignal.cs`, `EqualLevelEvaluator.cs` | `Configuration/Parameters.EqualLevels.cs` | Consumes value-only confirmed-swing snapshots; cluster-to-swing ownership stays in equal-level state; render items are separate |
| Liquidity Sweeps | `Features/LiquiditySweeps/SmartZonesNativePro.LiquiditySweeps.cs`, sweep evaluators | `Configuration/Parameters.LiquiditySweeps.cs` | Explicit dependency on open swings/equal clusters; event data and render items are separate |
| M15 overlay | `Features/HigherTimeframe/SmartZonesNativePro.M15Zones.cs` schedules closed bars; FVG, Price Gap and OB handlers live under their respective feature folders | `Configuration/Parameters.HigherTimeframe.cs` plus the owning detector settings | Own HTF state/history; per-detector exception boundaries; shared zone lifecycle only at registration |
| Reference levels | `Features/ReferenceLevels/SmartZonesNativePro.ReferenceLevels.cs` | `Configuration/Parameters.KeyLevels.cs` and `Parameters.Sessions.cs` | `Rendering/SmartZonesNativePro.ReferenceRenderer.cs` |
| Psychological levels | `Rendering/SmartZonesNativePro.RoundLevelsRenderer.cs` | `Configuration/Parameters.RoundLevels.cs` | Shared `Rendering/SmartZonesNativePro.ChartItemRenderer.cs` |

The shared rendering contract lives in `Rendering/SmartZonesNativePro.ChartItemRenderer.cs`; feature-specific renderers only calculate their own drawing items and ask the shared renderer to virtualize/update them.

All zone-producing features (M5/M15 FVG, Price Gap, Order Block, and Breaker) use the same terminal visibility policy. A `Broken` zone cannot be re-created by scrolling, ranking, display-mode changes, or legacy broken-history switches. Keep its logical record only where needed for diagnostics and breaker lineage; never use that record as a visible chart zone.\n\n## Intentional shared contracts

Zone types intentionally share one lifecycle evaluator, interval index, zone identity, and renderer. That is required so an FVG, gap, OB, M15 zone, and breaker obey the same Fresh/Touched/Partial/Filled/Broken transitions. This shared zone core is not a reason for the detectors to share filtering rules.

A few data dependencies are also intentional:
- order blocks are confirmed by structure breaks;
- equal-high/low clusters consume confirmed swing points;
- liquidity sweeps may use swings and equal levels;
- breakers are children of broken order blocks;
- higher-timeframe zones enter the same zone lifecycle after a closed M15 candle is confirmed.

Do not duplicate those shared evaluations inside each detector. Change the owning module and its tests instead.

## Lightweight chart-object policy

1. Calculate logical state on closed bars; chart drawing is deferred to the render pass.
2. Cache one time/price viewport per render pass. Lines and labels outside that viewport are removed from the chart-object layer but their logical history remains available.
3. The renderer re-creates the visible object when the user pans back to its time/price area.
4. Apply independent per-viewport budgets by feature: major structure 12, internal structure 8, equal levels 6, sweeps 6 and reference levels 10 by default. These auxiliary budgets must not delete zones from logical state.
5. Zone rectangles/midlines use viewport-aware zone selection. Only active, non-broken zones project to the forty-bar future edge. Broken zones are never eligible for drawing, regardless of the legacy `ShowBrokenZones` or `ShowObHistoryOfBreakers` settings; any existing rectangle, midline, and label are removed in the same bar's render reconciliation. Broken logical records remain in internal history only for lifecycle diagnostics and breaker parent/child lineage. Filled zones may still be shown when `ShowFilledZones` is enabled; in that opt-in case, selection falls back to the full logical collection because the active interval index excludes filled zones.
6. Reuse chart objects and update only changed fields where practical. Feature renderers always get a cleanup pass, even when their show toggle is off. Do not add a new object each tick and do not print repetitive per-bar diagnostics.
7. Keep BOS/CHoCH labels at their break location. A line endpoint and the semantic position of its text are different concepts.

## Isolation status and boundaries

The source/state separation pass is implemented on `phase-9/feature-module-isolation`:

- Mutable detector state is grouped by owner in `Runtime/SmartZonesNativePro.FeatureStateOwners.cs`; the rendering caches for zones, structure swings, equal clusters, sweep events, higher-timeframe references, session ranges and psychological levels are kept in separate render-state owners.
- Logical `Zone`, `Swing`, `EqCluster`, `SweepEv`, `HtfTrack` and `SessionTrack` records no longer carry their chart-item/line/text handles. Rendering is a disposable view of the logical records.
- Structure sends **immutable value-only** `OrderBlockSignal` and `EqualLevelSignal` messages. Equal-level clusters map to swings in the equal-level owner rather than mutating a cluster pointer embedded inside `Swing`.
- FVG and Price Gap rules are pure detector evaluators reused by the M5 and M15 adapters. M15 OB, FVG and Price Gap adapters are in their owning feature folders.
- Each pipeline processor/renderer has an exception boundary; major/internal structure, equal levels, sweeps, reference levels and sessions have separate processor/render boundaries. M15 pivot, structure-break, OB, FVG and gap paths are wrapped separately. Auxiliary line/label caches are allocated only after viewport checks and are released when objects move off-screen; an invalid viewport does not trigger a history-wide chart-object burst.
- Deterministic tests cover detector thresholds/combine modes and pipeline error isolation; CI also builds/packages the cTrader artifact.

The cTrader-facing indicator remains one `SmartZonesNativePro` type. Its thin partial-class adapters still read platform bars/parameters and register logical zones, but detector decisions, mutable feature state, event hand-offs, and drawing state are separated by responsibility. The zone repository/lifecycle/registration and shared chart-item synchronizer are intentionally common infrastructure, not per-detector logic.

This is **module isolation inside one indicator**, not separate deployable DLLs. The code stage is complete; actual chart acceptance remains a separate integration gate because CI cannot render a live cTrader chart.

## Acceptance still required

CI proves compilation and deterministic evaluator tests, not chart rendering. In cTrader, verify:
- all expected active zones show while scrolling and zooming;
- off-screen structure/liquidity/reference objects are removed and restored on panning;
- BOS/CHoCH labels stay at their event prices/times;
- chart object count remains proportional to viewport and configured budgets;
- disabled features leave unrelated features unchanged.
