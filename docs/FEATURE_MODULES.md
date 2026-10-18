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
| Fair Value Gaps | `Features/FairValueGaps/SmartZonesNativePro.Fvg.cs` | `Configuration/Parameters.FairValueGaps.cs` | Shared zone lifecycle and `Rendering/SmartZonesNativePro.ZoneRenderer.cs` |
| Price Gaps | `Features/PriceGaps/SmartZonesNativePro.PriceGaps.cs` | `Configuration/Parameters.PriceGaps.cs` | Shared zone lifecycle and zone renderer |
| Market Structure (BOS/CHoCH) | `Features/MarketStructure/` | `Configuration/Parameters.MarketStructure.cs` | `Rendering/SmartZonesNativePro.ItemAndStructureRenderer.cs` |
| Order Blocks | `Features/OrderBlocks/` | `Configuration/Parameters.OrderBlocks.cs` | Shared zone lifecycle; structure breaks provide OB confirmation |
| Breaker Blocks | Breaker creation in `Features/ZoneLifecycle/SmartZonesNativePro.ZoneStateAndScoring.cs` and `ZoneBreakerFactory.cs` | `Configuration/Parameters.Breakers.cs` | Shared zone lifecycle and zone renderer |
| Equal Highs/Lows | `Features/EqualLevels/` | `Configuration/Parameters.EqualLevels.cs` | Item renderer; clusters are built from confirmed swings |
| Liquidity Sweeps | `Features/LiquiditySweeps/` | `Configuration/Parameters.LiquiditySweeps.cs` | Item renderer; consumes swing/equal-level state |
| M15 overlay | `Features/HigherTimeframe/` | `Configuration/Parameters.HigherTimeframe.cs` | Shared zone lifecycle, with closed-M15 confirmation |
| Reference levels | `Features/ReferenceLevels/SmartZonesNativePro.ReferenceLevels.cs` | `Configuration/Parameters.KeyLevels.cs` and `Parameters.Sessions.cs` | `Rendering/SmartZonesNativePro.ItemAndStructureRenderer.cs` |
| Psychological levels | `Rendering/SmartZonesNativePro.StatusAndRoundLevels.cs` | `Configuration/Parameters.RoundLevels.cs` | Item renderer |

## Intentional shared contracts

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
4. Apply separate per-viewport budgets to structure, liquidity and reference drawings (defaults: 12 structure, 6 liquidity, 10 reference items). These budgets must not delete zones from logical state.
5. Zone rectangles/midlines use viewport-aware zone selection. Only active valid zones project to the forty-bar future edge; terminal filled/broken history ends at the actual lifecycle event. When filled-zone display is enabled, selection falls back to the full logical collection because the active interval index excludes terminal zones.
6. Reuse chart objects and update only changed fields where practical. Feature renderers always get a cleanup pass, even when their show toggle is off. Do not add a new object each tick and do not print repetitive per-bar diagnostics.
7. Keep BOS/CHoCH labels at their break location. A line endpoint and the semantic position of its text are different concepts.

## Remaining isolation work

The current feature algorithms live in separate source files and are scheduled through the fault-isolated pipeline, but they still compile as partial members of the same cTrader indicator class. The shared zone model and intentional cross-feature dependencies need a stable data contract before their state can be moved into standalone service classes. Treat extraction of each detector's private state into independent service objects, with event/snapshot interfaces, as a separate refactor phase and validate it with replay tests. Do not claim complete state encapsulation until that phase is finished.

## Acceptance still required

CI proves compilation and deterministic evaluator tests, not chart rendering. In cTrader, verify:
- all expected active zones show while scrolling and zooming;
- off-screen structure/liquidity/reference objects are removed and restored on panning;
- BOS/CHoCH labels stay at their event prices/times;
- chart object count remains proportional to viewport and configured budgets;
- disabled features leave unrelated features unchanged.
