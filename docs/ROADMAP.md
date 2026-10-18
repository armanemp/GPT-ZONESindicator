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

- [x] Restore NuGet packages with the .NET 6 SDK in GitHub Actions.
- [x] Build Release and verify the packaged `GPT-ZONESindicator.algo` artifact in GitHub Actions (0 warnings, 0 errors).
- [ ] Build Debug.
- [ ] Record cTrader version, target framework, and exact build output.
- [ ] Confirm all chart object API members against the installed cTrader API.
- [ ] Load the indicator on a chart and confirm no initialization exceptions.

## Phase 2 — Zone lifecycle correctness

- [x] Add deterministic lifecycle unit cases for Fresh, Touched, Partial, Filled and Broken states (the full history/live replay fixture remains outstanding).
- [x] Test all break modes: CandleClose, BodyBeyond and WickBeyond for bullish/bearish zones, including exact-boundary cases.
- [x] Extract the closed-bar lifecycle decision step into a pure evaluator and add regression cases for both directions, strict break boundaries, fill/break priority, touch re-entry and terminal Broken state (CI passed; full history/live replay remains outstanding).
- [x] Validate breaker eligibility (broken OB parent only, no duplicate child, valid bounds/scoring inputs, confirmation after origin), reverse direction, preserve identity/bounds/scoring context, and start at confirmation time (CI pending for latest runtime guard).
- [ ] Verify in cTrader that the parent's ChildId points to the generated breaker and the rendered breaker begins at the break confirmation time.
- [ ] Verify hiding, ranking, overlap decluttering and chart-object budgets never mutate logical state.

## Phase 3 — Structure and liquidity correctness

- [x] Extract and test pivot confirmation delay, lookback eligibility, and the existing asymmetric left/right tie rules (CI passed; chart replay remains outstanding).
- [x] Test BOS vs CHoCH classification from neutral, bullish and bearish trend states (pure evaluator regression tests pass in CI).
- [ ] Test full equal-high/equal-low cluster merge/lifecycle (pair tolerance, separation, range cleanliness and backward-scan early exits have deterministic tests; duplicate sweep suppression predicate is now covered).
- [x] Extract and test high/low sweep predicates for Wick/Body measurement, minimum penetration, close-back return and exact threshold behavior (CI passed; exhaustive swing/equal-level end-to-end replay remains outstanding).
- [x] Extract and test order-block source filters, source-candle/extreme discovery, full/body bounds, oversize fallback and minimum-size behavior (CI passed; full end-to-end discovery/replay remains outstanding).

## Phase 4 — Reference levels and time handling

- [ ] Test previous day/week/month levels against the broker-provided HTF bars.
- [ ] Test session windows across midnight, year changes and UK/US daylight-saving transitions.
- [ ] Check session high/low updates and retention limits on lower timeframes.
- [ ] Test psychological-level band updates when price changes or the configured step changes.

## Phase 5 — Rendering and object ownership

- [ ] Verify each feature's show/hide toggle only affects rendering, not detection state.
- [ ] Verify chart-object cleanup only removes the instance prefix.
- [x] Implement reconciliation when a zone rectangle or an enabled midline/label is missing, and refresh changed zone comments/labels without rewriting unchanged properties (CI build passed).
- [x] Replace the arbitrary default 300-zone cap with buffered viewport-aware rendering: keep only time/price-intersecting zones plus a small margin as chart objects; refresh on scroll, zoom, and resize events supported by the project's Automate API package; cache comments/labels on viewport-only redraws. Logical detection, ranking semantics for global modes, and lifecycle history are preserved (CI passed; cTrader acceptance pending).
- [ ] Verify deleted chart objects are recreated and partially deleted renderer objects are reconciled in cTrader.
- [ ] Verify panning across history shows all qualifying zones in each viewport, zooming and resizing refresh the visible set, and chart-object count tracks the viewport rather than total historical zones.
- [ ] Verify color, alpha, labels, midlines and line styles update predictably.

## Phase 6 — Performance and fault isolation

- [ ] Replace per-bar full scans of all active zones with a price interval index and per-kind ordered break indexes, preserving all zone states and all three break-mode predicates (Release CI and cTrader replay validation pending).
- [x] Verify indexed lifecycle output against an exhaustive reference replay across CandleClose, BodyBeyond, and WickBeyond, including bars that gap entirely across a zone and re-entry touch counts (CI passed: 1,200 bars and 709 zones; cTrader history/live replay remains outstanding).
- [x] Extract the interval treap into a reusable component and add deterministic boundary/mutation coverage plus 50,000 seeded randomized operations whose queries are compared with a brute-force reference scan (CI passed: 50,000 seeded randomized operations, differential query checks included).
- [ ] Benchmark initial load at several lookbacks (for example 1k, 10k and 50k bars).
- [ ] Measure process time per bar, render time, object count, and allocations.
- [ ] Inspect pivot scans and active-zone updates for avoidable repeated work.
- [ ] Add diagnostics for repeated failures without allowing one failed feature to disable unrelated features.
- [ ] Confirm no unbounded object or history growth after prolonged chart operation.

## Phase 7 — Viewport-aware lightweight rendering

- [x] Reuse the active-zone price interval index to discover zones intersecting the visible price range in the common viewport mode.
- [x] Synchronize only the previous and current viewport candidates during ordinary rendering; global ranking and opt-in terminal-history modes retain complete-history selection.
- [x] Make each zone midline's RGB exactly match the zone RGB; keep alpha separately configurable. Live-chart visual verification remains pending.
- [x] Set dotted line styles as editable defaults rather than forcibly overriding a user's saved style parameters.
- [x] Extend active valid zone rectangles and their midlines by forty nominal intervals of the active chart timeframe; terminal filled/broken history ends at the actual lifecycle event. Keep line endpoints separate from semantic text locations so BOS/CHoCH labels remain at their breaks. Live-chart verification remains pending.
- [x] Add an M15 overlay on lower-timeframe charts for closed-bar FVGs, price gaps, and major-structure order blocks; M15 order blocks can produce breakers after M5 lifecycle confirmation. Keep M5/M15 duplicate identities separate and align M15 detection history with loaded M5 history. Replay closed M5 candles for retrospective M15 OBs and gaps so zones already filled/broken are not shown as fresh. No-lookahead close timing has deterministic boundary tests; live chart acceptance remains pending.
- [x] Keep broken/fill history hidden by default while retaining the existing opt-in display switches.
- [x] Include phase/* branches in the build workflow trigger.
- [ ] Verify all valid zones appear while panning and zooming in cTrader, including breaker zones through the chart's right edge.
- [ ] Compare indexed viewport selection against full-history selection and validate terminal-zone removal, chart-object count and visual contrast on a live cTrader chart.
- [ ] Confirm equivalent feature availability to the prior renderer; cTrader still uses its native chart-object API, not MetaTrader 5's internal drawing engine.

**Exit gate:** CI build passes and a cTrader chart acceptance/replay check confirms visual and logical parity. Native chart objects are created only for the active viewport in the ordinary mode; no custom canvas replacement is claimed.


## Phase 8 — Feature boundaries and lightweight chart-object management

- [x] Register closed-bar processing and render passes through a feature pipeline with per-module exception isolation; preserve the deliberate dependency order.
- [x] Render budget and drawing cache changes are presentation-only; zone boundaries and lifecycle remain held in the logical zone state.
- [x] Separate FVG, Price Gap, Market Structure, Order Block, Breaker, Equal Level, Liquidity Sweep, Higher-Timeframe, Zone Lifecycle and Reference Level source files into feature-owned folders.
- [x] Separate parameter files by feature family, including visibility, scoring, colors, line presentation, key levels, sessions and psychological levels. Initialization no longer forcibly rewrites each feature's toggle/style settings.
- [x] Apply viewport/price filtering to line and label objects and release off-screen chart objects while preserving logical history for panning back.
- [x] Give each auxiliary renderer an independent viewport budget (major structure 12, internal structure 8, equal levels 6, sweeps 6, reference levels 10 by default); off-screen objects do not consume those budgets.
- [x] Ensure disabling a renderer cleans up its existing chart objects and ensure filled-state display uses the full zone collection instead of the active-only index.
- [x] Use separate configurable viewport budgets for structure, liquidity and reference items; off-screen history no longer consumes these budgets.
- [x] Reduce zone label-collision scanning to a cached per-render label layout rather than repeatedly scanning every chart object for each candidate.
- [x] Separate zone chart-object handles, visibility flags and render-time ranking scores into renderer-owned state rather than the logical zone record.
- [x] Filter to viewport-intersecting zones before applying ranking or visible-zone quotas; invalid viewport states no longer trigger full-history object creation.
- [x] Make zone rectangles the lightweight default; labels and midlines are opt-in extra chart objects.
- [x] Separate M15 detection from M15 visibility so hiding that overlay does not stop calculation.
- [ ] Move all remaining feature-private detector state out of the shared partial indicator host into independent module service classes with explicit immutable events/snapshots. The shared zone lifecycle remains intentional, but full state encapsulation is not yet complete.
- [ ] Verify that no valid active zone is omitted because of viewport selection or saved display parameters; use the status/diagnostic counts to distinguish detection rejection from display filtering. Rank and per-kind quotas are now viewport-local by design.
- [ ] Confirm scrolling/zooming behavior and live object counts on the actual cTrader chart.

**Exit gate:** All CI build and deterministic tests pass, feature toggles do not affect unrelated modules, and live cTrader testing confirms viewport virtualization and zone visibility. Do not describe the modules as fully state-isolated until the first open item above is complete.

## Phase 9 — Release acceptance

- [ ] Run the same closed-bar sequence through history replay and live/demo operation and compare resulting logical states.
- [ ] Verify behavior on at least one time-based chart and one symbol with a different pip/tick configuration.
- [ ] Confirm no guard-page/stack exhaustion, sustained UI stalls, or runaway chart-object growth.
- [ ] Document remaining known limitations and publish a tagged release only after all required gates pass.

## Change protocol

- Keep each phase small enough to review independently.
- Do not introduce duplicate methods/fields, copied backup classes, or dead legacy fragments.
- Prefer focused automated tests for deterministic calculations; keep cTrader chart/API acceptance as a separate integration gate.
- Update this file in the same change as phase progress.
