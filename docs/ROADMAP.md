# Development Roadmap

This roadmap is intentionally split into small reviewable phases. Do not mark a phase complete merely because source files were created; record the actual validation performed.

## Phase 0 — Repository and modular extraction

- [x] Create an explicit repository entry point and source layout.
- [x] Separate configuration parameters by feature group.
- [x] Separate lifecycle, domain state, detection engines, reference levels and renderers into partial-class files.
- [x] Keep the existing public indicator type and feature parameter names/defaults.
- [x] Build against the target cTrader Automate package and resolve compile-time API incompatibilities (GitHub Actions restore + Release build passed).
- [ ] Compare the modular source against the pasted baseline for parameter and feature parity.

**Exit gate:** successful build and source-level parity review. Runtime acceptance remains pending until loaded in cTrader.

## Phase 1 — Compile and API compatibility

- [ ] Restore NuGet packages with a current .NET SDK.
- [ ] Build Debug and Release.
- [ ] Record cTrader version, target framework, and exact build output.
- [ ] Confirm all chart object API members against the installed cTrader API.
- [ ] Load the indicator on a chart and confirm no initialization exceptions.

## Phase 2 — Zone lifecycle correctness

- [ ] Add deterministic tests/replay fixtures for Fresh, Touched, Partial, Filled and Broken transitions.
- [ ] Test all break modes: CandleClose, BodyBeyond and WickBeyond.
- [ ] Verify breaker creation begins at the break confirmation time and preserves parent/child identity.
- [ ] Verify hiding, ranking, overlap decluttering and chart-object budgets never mutate logical state.

## Phase 3 — Structure and liquidity correctness

- [ ] Test major and internal pivot confirmation delay.
- [ ] Test BOS vs CHoCH classification from neutral, bullish and bearish trend states.
- [ ] Test equal-high/equal-low clustering, range cleanliness, sweeps and duplicate sweep suppression.
- [ ] Test order-block source filters and oversize/body fallback behavior.

## Phase 4 — Reference levels and time handling

- [ ] Test previous day/week/month levels against the broker-provided HTF bars.
- [ ] Test session windows across midnight, year changes and UK/US daylight-saving transitions.
- [ ] Check session high/low updates and retention limits on lower timeframes.
- [ ] Test psychological-level band updates when price changes or the configured step changes.

## Phase 5 — Rendering and object ownership

- [ ] Verify each feature's show/hide toggle only affects rendering, not detection state.
- [ ] Verify chart-object cleanup only removes the instance prefix.
- [ ] Verify deleted chart objects are recreated and partially deleted renderer objects are reconciled.
- [ ] Verify color, alpha, labels, midlines and line styles update predictably.

## Phase 6 — Performance and fault isolation

- [ ] Benchmark initial load at several lookbacks (for example 1k, 10k and 50k bars).
- [ ] Measure process time per bar, render time, object count, and allocations.
- [ ] Inspect pivot scans and active-zone updates for avoidable repeated work.
- [ ] Add diagnostics for repeated failures without allowing one failed feature to disable unrelated features.
- [ ] Confirm no unbounded object or history growth after prolonged chart operation.

## Phase 7 — Release acceptance

- [ ] Run the same closed-bar sequence through history replay and live/demo operation and compare resulting logical states.
- [ ] Verify behavior on at least one time-based chart and one symbol with a different pip/tick configuration.
- [ ] Confirm no guard-page/stack exhaustion, sustained UI stalls, or runaway chart-object growth.
- [ ] Document remaining known limitations and publish a tagged release only after all required gates pass.

## Change protocol

- Keep each phase small enough to review independently.
- Do not introduce duplicate methods/fields, copied backup classes, or dead legacy fragments.
- Prefer focused automated tests for deterministic calculations; keep cTrader chart/API acceptance as a separate integration gate.
- Update this file in the same change as phase progress.
