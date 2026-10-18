# Architecture

## Runtime model

The indicator is a single cTrader Automate type, `cAlgo.SmartZonesNativePro`. The entry-point declaration and `[Indicator]` attribute live in `src/GPT-ZONESindicator/SmartZonesNativePro.cs`. Implementation is distributed across C# partial-class files. This keeps runtime state and cTrader API access in one instance while separating source responsibilities.

## Source map

| Area | Responsibility |
| --- | --- |
| `Domain/ZoneTypes.cs` | Public parameter enums and internal zone-kind/state enums |
| `Configuration/Parameters.*.cs` | Parameter definitions grouped by feature; parameter names/defaults are part of the user-facing contract |
| `Runtime/SmartZonesNativePro.State.cs` | Zone, swing, liquidity, HTF/session records and instance-owned state |
| `Runtime/SmartZonesNativePro.Lifecycle.cs` | Initialize/destroy, history loading/rebuild, closed-bar calculation pipeline |
| `Runtime/SmartZonesNativePro.Helpers.cs` | OHLC/ATR, color, naming, object cleanup, diagnostics and DST-date helpers |
| `Engine/SmartZonesNativePro.Structure.cs` | Confirmed pivots, BOS/CHoCH and structure-context updates |
| `Engine/SmartZonesNativePro.ZoneRegistration.cs` | Duplicate keys, zone registration and confluence bookkeeping |
| `Engine/SmartZonesNativePro.Gaps.cs` | Three-candle FVGs and separately configured price gaps |
| `Engine/SmartZonesNativePro.OrderBlocks.cs` | Structure-triggered order-block candidate detection |
| `Engine/SmartZonesNativePro.Liquidity.cs` | Equal highs/lows, sweep detection and sweep event records |
| `Engine/SmartZonesNativePro.ZoneStateAndScoring.cs` | Zone state machine, breaker creation, explainable score factors |
| `References/SmartZonesNativePro.ReferenceLevels.cs` | Previous D/W/M levels, period opens, session ranges |
| `Rendering/SmartZonesNativePro.ZoneRenderer.cs` | Zone visibility policy, ranking/decluttering and rectangle/midline/label synchronization |
| `Rendering/SmartZonesNativePro.ItemAndStructureRenderer.cs` | Reusable line/text synchronization, structure, equality and sweep drawing |
| `Rendering/SmartZonesNativePro.StatusAndRoundLevels.cs` | Psychological levels, status panel and one-time diagnostics summary |

## Dependency direction

- Detection/state code creates or updates logical records; it does not directly draw chart objects.
- Rendering reads logical state and owns chart object creation/update/removal.
- The lifecycle file coordinates engines in a deterministic order for each closed bar.
- The parameter files are declarations only; they do not contain execution logic.
- All object names are prefixed per indicator instance. Cleanup must never remove objects belonging to other indicators.

## Invariants to preserve

1. Detection is based on closed bars. Do not read the forming candle as a confirmed signal.
2. A zone's `Lower` and `Upper` boundaries are fixed after creation.
3. Hiding a zone or hitting a drawing budget never deletes the logical zone or alters its lifecycle state.
4. Historical discovery and live discovery must converge to the same state when supplied the same closed-bar sequence.
5. Swing levels are only confirmed after the configured right-hand bars close.
6. Historical HTF levels must use a completed HTF bar; never read future HTF data.
7. Rendering work must not run on every tick unless an explicit performance-tested change justifies it.

## Build

The project uses the official `cTrader.Automate` NuGet package and targets .NET 6. Build it from a recent .NET SDK environment:

```powershell
dotnet restore src/GPT-ZONESindicator/GPT-ZONESindicator.csproj
dotnet build src/GPT-ZONESindicator/GPT-ZONESindicator.csproj --no-restore
```

For platform packaging and runtime acceptance, also rebuild/load it in cTrader Windows. A successful generic build is not a substitute for checking actual drawings and lifecycle behavior on a chart.

Reference: https://help.ctrader.com/ctrader-algo/documentation/migration-from-net-framework/
Reference: https://help.ctrader.com/ctrader-algo/compiling/