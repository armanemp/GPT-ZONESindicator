# Architecture

## Runtime model

The indicator is a single cTrader Automate type, `cAlgo.SmartZonesNativePro`. The entry-point declaration and `[Indicator]` attribute live in `src/GPT-ZONESindicator/SmartZonesNativePro.cs`. Implementation is distributed across C# partial-class files. This keeps runtime state and cTrader API access in one instance while separating source responsibilities.

## Source map

| Area | Responsibility |
| --- | --- |
| `Domain/ZoneTypes.cs` | Public parameter enums and internal zone-kind/state enums |
| `Configuration/Parameters.*.cs` | Parameter declarations grouped by feature; names/defaults remain the user-facing contract |
| `Runtime/SmartZonesNativePro.State.cs` | Logical domain records, common zone repository/indexes, indicator-runtime fields; no per-swing/equal/sweep chart-item handles |
| `Runtime/SmartZonesNativePro.FeatureStateOwners.cs` | Mutable state owners grouped by feature plus separate structure/equal/sweep/zone rendering-cache owners |
| `Runtime/SmartZonesNativePro.Lifecycle.cs` | Initialize/destroy, history loading/rebuild, closed-bar scheduling, independent feature execution |
| `Runtime/SmartZonesNativePro.FeatureModules.cs` | Pipeline composition and viewport calculation; keeps ordering/fault boundaries, not detector rules |
| `Engine/FeatureModulePipeline.cs` | Ordered closed-bar and render modules, each executed within its own exception boundary |
| `Features/MarketStructure/` | Pivot and BOS/CHoCH processing; pure pivot/break evaluators; emits value-only signal snapshots |
| `Features/OrderBlocks/` | M5 order-block handler/rules/candidate search and independent M15 order-block adapter |
| `Features/FairValueGaps/` | M5 FVG adapter, pure FVG detector evaluator and M15 FVG adapter |
| `Features/PriceGaps/` | M5 price-gap adapter, pure price-gap evaluator and M15 gap adapter |
| `Features/EqualLevels/` | Confirmed-swing snapshot handling, equal-pair evaluator and state-owned cluster/swing association |
| `Features/LiquiditySweeps/` | Sweep predicates, deduplication and sweep events; consumes confirmed swing/equal-level state |
| `Features/HigherTimeframe/SmartZonesNativePro.M15Zones.cs` | M15 history/timing and higher-timeframe structure; detector handlers themselves belong to their feature folders |
| `Features/ZoneLifecycle/` | Shared zone registration, lifecycle state transitions, indexes, viewport selection and breaker factory |
| `Features/ReferenceLevels/` | Prior D/W/M levels, period opens and session ranges |
| `Rendering/SmartZonesNativePro.ZoneRenderer.cs` | Zone visibility policy, buffered viewport filtering, ranking/decluttering and rectangle/midline/label synchronization |
| `Rendering/SmartZonesNativePro.StructureRenderer.cs` | Structure swing/BOS/CHoCH objects; uses rendering-owned state, not chart handles in swing logic |
| `Rendering/SmartZonesNativePro.LiquidityRenderer.cs` | Equal-level and sweep chart objects with separate render caches |
| `Rendering/SmartZonesNativePro.ReferenceRenderer.cs`, `RoundLevelsRenderer.cs`, `ChartItemRenderer.cs` | Reference/psychological drawing and shared native chart-item synchronization |
| `Tests/ZoneIntervalIndexSmoke/Program.cs` | Deterministic differential, lifecycle, detector-threshold and pipeline fault-isolation tests |

## Dependency direction

- Feature handlers produce or update logical records; pure evaluators isolate deterministic rule decisions from the platform adapter.
- Rendering reads logical state and owns chart object creation/update/removal; the zone/swing/equal/sweep logical records contain no chart-item handles.
- The composition root coordinates engines and immutable hand-off signals in deterministic closed-bar order; each processor and renderer has a fault boundary.
- The parameter files are declarations only; they do not contain execution logic.
- All object names are prefixed per indicator instance. Cleanup must never remove objects belonging to other indicators.

## Invariants to preserve

1. Detection is based on closed bars. Do not read the forming candle as a confirmed signal.
2. A zone's `Lower` and `Upper` boundaries are fixed after creation.
3. Rendering is viewport-aware: only zones intersecting the visible time/price range plus a small buffer receive chart objects. Out-of-view zones remain in the full logical history and are reconsidered on scroll, zoom, and chart-resize events supported by the project's current Automate API package. The optional rectangle budget defaults to `0` (unlimited within the viewport); it never alters zone detection or lifecycle state.
4. Historical discovery and live discovery must converge to the same state when supplied the same closed-bar sequence.
5. Swing levels are only confirmed after the configured right-hand bars close.
6. Historical HTF levels must use a completed HTF bar; never read future HTF data.
7. Full rendering is driven by closed-bar updates; zone-only viewport reconciliation runs on chart viewport events and reuses cached comments/labels when zone state has not changed.
8. Zone lifecycle updates use indexed price-overlap candidates plus indexed far-edge break candidates. Preserve the exact `UpdateZone` break predicates for CandleClose, BodyBeyond, and WickBeyond; do not skip gaps that cross a whole zone. Touch counts must still detect re-entry after an arbitrary number of untouched bars.

## Build

The project uses the official `cTrader.Automate` NuGet package and targets .NET 6. Build it from a recent .NET SDK environment:

```powershell
dotnet restore GPT-ZONESindicator.csproj
dotnet build GPT-ZONESindicator.csproj --no-restore
```

For platform packaging and runtime acceptance, also rebuild/load it in cTrader Windows. A successful generic build is not a substitute for checking actual drawings and lifecycle behavior on a chart.

Reference: https://help.ctrader.com/ctrader-algo/documentation/migration-from-net-framework/
Reference: https://help.ctrader.com/ctrader-algo/compiling/