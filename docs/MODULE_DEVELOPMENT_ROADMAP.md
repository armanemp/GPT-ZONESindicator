# Module-by-Module Development Roadmap

This is the execution plan for continued development of GPT-ZONESindicator. Work one module at a time: inspect the current behavior, define its rules, add deterministic tests, build, then verify the result on a real cTrader chart. Do not start the next module until the current module meets its exit gate or its remaining chart-only checks are explicitly recorded.

## Project-wide acceptance rules

These apply to every module and are not optional:

- [ ] All detection that claims confirmation uses closed bars only; no forming-bar look-ahead.
- [ ] Historical replay and live closed-bar processing produce the same logical results from the same input sequence.
- [ ] Detection state is independent from show/hide toggles, ranking, viewport filtering, and chart-object budgets.
- [ ] Every state transition has a deterministic test for both bullish and bearish directions, including equality/boundary cases.
- [ ] Duplicate signals/zones/events are prevented with explicit rules and tested.
- [ ] Active objects end when their domain state ends; terminal states cannot be accidentally resurrected by scrolling, zooming, rebuilds, or settings changes.
- [ ] Rendering is viewport-aware and instance-prefixed. Scrolling away releases native chart objects; scrolling back restores eligible visible objects without changing logical history.
- [ ] Every module failure is isolated and logged with the module name; a failure in one detector/renderer must not silence unrelated modules.
- [ ] Parameter names/defaults remain compatible unless a deliberate breaking change is documented.
- [ ] A passing unit/regression suite is not treated as proof of visual runtime correctness. Live cTrader chart acceptance remains a separate gate.

## Status summary

| Order | Module | Current status | Dependency / reason for order |
|---:|---|---|---|
| 0 | Baseline, lifecycle and rendering contracts | Core implemented; integration acceptance pending | Shared foundation for all detections and display |
| 1 | Major and Internal Market Structure (BOS/CHoCH) | **Regression tests passed locally; chart acceptance pending** | Feeds structure events to Equal Levels and Order Blocks |
| 2 | Equal Highs / Equal Lows | Existing evaluator tests; full lifecycle/replay acceptance pending | Consumes confirmed structure swings; contributes liquidity pools to Sweeps |
| 3 | Liquidity Sweeps | Predicate tests passed; end-to-end replay pending | Consumes swings and equal-level clusters |
| 4 | Order Blocks | Rule/candidate tests passed; full discovery and replay pending | Consumes confirmed structure-break signals |
| 5 | Breaker Blocks | Factory/lifecycle tests exist; chart lineage verification pending | Created only after an Order Block is broken |
| 6 | Fair Value Gaps (FVG) | Detector threshold tests passed; live chart parity pending | Independent M5/M15 detector rules, shared zone lifecycle |
| 7 | Price Gaps | Detector threshold/combine tests passed; live chart parity pending | Independent detector rules, shared zone lifecycle |
| 8 | M15 Higher-Timeframe Overlay | Timing tests exist; end-to-end chart replay pending | Integrates M15 detections into the M5 zone lifecycle |
| 9 | Reference Levels (prior D/W/M and opens) | Broker-bar/time handling tests pending | Depends on symbol/broker-provided time series |
| 10 | Session Ranges | DST, midnight-crossing and retention tests pending | Depends on configured session/time-zone semantics |
| 11 | Psychological / Round Levels | Configuration-change and chart acceptance pending | Depends on symbol tick/pip/price-step behavior |
| 12 | Cross-module integration, performance and release | Pending | Run only after the module-specific gates are met |

The status above distinguishes completed evaluator tests from unfinished end-to-end or live-chart acceptance. Do not mark a whole module complete based only on compilation.

## Module 0 — Baseline, lifecycle and rendering contracts

### Verify
- [x] Single cTrader indicator entry point with feature-owned source/parameter files.
- [x] Shared zone lifecycle states Fresh, Touched, Partial, Filled and Broken have deterministic evaluator coverage.
- [x] CandleClose, BodyBeyond and WickBeyond break rules have bullish/bearish boundary coverage.
- [x] Active zone selection uses viewport-aware indexes; terminal-zone removal and renderer reconciliation have deterministic policy tests.
- [ ] Review public parameters against the original baseline for name/default parity.
- [ ] On cTrader, verify per-instance cleanup never removes another indicator's objects.
- [ ] Verify changing feature visibility, ranking, overlap decluttering or object caps never changes detection state.
- [ ] Verify scrolling, zooming and resizing remove off-screen native objects and restore eligible objects when panning back.

### Exit gate
Parameter parity is documented; lifecycle and renderer tests pass; the shared contracts behave correctly on a real chart.

## Module 1 — Major and Internal Market Structure (current module)

### Rules to preserve
- A swing is confirmed only after all configured right-hand bars have closed.
- Existing asymmetric tie rules remain deliberate: equal extremes to the left disqualify, equal extremes to the right are handled by the established rule.
- BOS/CHoCH decisions use closed-bar close-crossing and prior trend state.
- Major and Internal engines retain independent parameters, state, rendering budgets and visibility switches.
- A swept swing must not continue appearing as an active level; silent breaks must not render as BOS/CHoCH.
- Minimum-distance settings must be order-independent, including a candle that qualifies as both a high and low pivot.

### Completed in source / automated suite
- [x] Guard pivot evaluation against NaN, infinity and malformed High/Low ranges.
- [x] Guard structure-break evaluation against non-finite close or swing prices.
- [x] Extract pure pivot, break-classification, visibility and distance evaluators.
- [x] Make dual-pivot distance filtering independent of whether the high or low is added first.
- [x] Add regression tests for confirmation delay, lookback eligibility, tie rules, malformed input, BOS/CHoCH, swept levels and dual-pivot distance rules.
- [x] User's local run of the complete deterministic regression harness reported PASS for every test.

### Remaining chart acceptance
- [ ] Load the newly built indicator on XAUUSD M5 and verify that a pivot marker appears only after the configured right bars have closed.
- [ ] Check several real examples of Major BOS, Major CHoCH, Internal BOS and Internal CHoCH against candle closes and the previous trend.
- [ ] Verify exact level/label anchoring: historical structure labels stay at their event bar/price; active levels stop being active after a sweep or break.
- [ ] Check dual-pivot behavior with minimum swing distance disabled and enabled; no high-first/low-first bias should appear.
- [ ] Pan/zoom through historical data and verify no duplicate swing lines or markers are created and removed objects return only when eligible.
- [ ] Verify the Major and Internal visibility toggles/budgets do not alter either engine's detection state.

### Exit gate
The user confirms the chart results match the closed-bar rules, and a rebuilt Release artifact plus deterministic regression suite both pass. Only then move to Module 2.

## Module 2 — Equal Highs / Equal Lows

### Verify and develop
- [ ] Define and document the exact tolerance formula (ticks/pips/ATR, as implemented) and strict/inclusive boundaries.
- [ ] Test cluster creation from confirmed swings, minimum separation, range cleanliness and maximum search/backscan limits.
- [ ] Test whether a new swing joins one cluster, merges two eligible clusters, or starts a new cluster; prevent ambiguous double ownership.
- [ ] Test cluster count and representative level updates when further highs/lows enter the tolerance band.
- [ ] Distinguish a close-through break from a liquidity sweep; test which terminal state has priority when rules overlap.
- [ ] Ensure a cluster is not swept before its contributing swing/cluster becomes eligible.
- [ ] Test interactions with SweepEqual and the per-swing cluster map to prevent double-counting the same liquidity event.
- [ ] Verify visible cluster lines stop at the correct break/sweep event and their labels do not drift on pan/zoom.
- [ ] Add end-to-end replay tests from swing signals through cluster state transitions.

### Exit gate
Pair/merge/lifecycle replay tests pass and visual equal-level clusters match a curated set of historical chart examples.

## Module 3 — Liquidity Sweeps

### Verify and develop
- [ ] Preserve Wick-versus-Body sweep measurement and test wick-only penetrations.
- [ ] Test minimum penetration, close-back requirement and minimum return distance at just below, equal to and just above thresholds.
- [ ] Test bullish/bearish sweep symmetry, equal-level versus single-swing sweeps and exact event timestamps.
- [ ] Verify a swing/cluster is closed only once and later candles cannot generate repeated sweeps from the same terminal level.
- [ ] Test duplicate suppression for same bar, direction, price tolerance and tick-size boundaries.
- [ ] Verify disabled sweep rendering does not disable sweep detection when other dependent features require sweep state.
- [ ] Confirm a sweep removes its source from active structure/equal-level display, while the sweep event itself is displayed at the correct bar.
- [ ] Replay multi-swing/multi-cluster sequences on both historical bars and live closed bars.

### Exit gate
Predicate and end-to-end event replay tests pass; source levels and sweep-event drawings agree on a real chart.

## Module 4 — Order Blocks

### Verify and develop
- [ ] Verify OB source mode (major/internal/both) with each structure source toggle.
- [ ] Test source swing and break timing, minimum leg length, break strength and displacement constraints.
- [ ] Confirm the chosen opposing candle/extreme is deterministic when several candidates qualify.
- [ ] Test FullCandle versus BodyOnly bounds, minimum zone size, maximum ATR size and body fallback.
- [ ] Prevent duplicate OBs from the same candle/direction while still allowing the intended distinct source identities.
- [ ] Replay bars between OB origin and confirmation so retrospectively discovered zones are not incorrectly displayed as Fresh if they were already filled or broken.
- [ ] Test normal lifecycle rules from creation through touch, partial fill, fill or break.
- [ ] Verify M5 and M15 OBs keep separate identities but use the intended shared lifecycle.
- [ ] Confirm the OB feature's visibility toggle does not suppress signal handling or mutate state needed by Breakers.

### Exit gate
Rule/candidate tests and deterministic full-discovery replay pass; source candles, boundaries, confirmation times and terminal states match chart examples.

## Module 5 — Breaker Blocks

### Verify and develop
- [ ] Create a Breaker only from an eligible, Broken Order Block parent and only once.
- [ ] Verify parent ChildId and child ParentId remain consistent after history rebuild/replay.
- [ ] Confirm bounds are preserved, direction is reversed and the child starts at the break-confirmation time.
- [ ] Test invalid bounds, too-small zones, invalid scoring inputs and duplicate creation attempts.
- [ ] Verify breaker-specific break mode and lifecycle once the breaker is created.
- [ ] Ensure a broken parent remains non-renderable; its record is retained only for lineage/diagnostics and cannot be resurrected by history/display toggles.
- [ ] Test the display combination where OB visibility is off but Breaker visibility is on, and vice versa.
- [ ] Verify M15 OB parents create appropriately timed breakers on the lower-timeframe lifecycle without look-ahead.

### Exit gate
Parent/child lineage and timing pass replay tests and are verified visually on cTrader.

## Module 6 — Fair Value Gaps (FVG)

### Verify and develop
- [ ] Test the three-candle imbalance formula in both directions, including equal-boundary cases.
- [ ] Test minimum size in pips and ATR, displacement/body ratio, direction matching and combination of filters.
- [ ] Verify each StartMode places the left edge at the intended candle/time.
- [ ] Test duplicate detection for overlapping or near-identical gaps without suppressing valid distinct gaps.
- [ ] Verify post-detection behavior: touches, partial fill, full fill and break under the selected break mode.
- [ ] Ensure filled/broken FVGs cannot remain active or be recreated by viewport changes.
- [ ] Compare M5 and M15 adapters against the same pure evaluator with corresponding timeframe-specific bars.
- [ ] Build a curated set of historical examples for valid gaps and false positives.

### Exit gate
Threshold tests, direction/boundary tests and retrospective replay pass; FVG ranges and terminal states match chart examples.

## Module 7 — Price Gaps

### Verify and develop
- [ ] Verify Open/Close and High/Low range gap methods separately.
- [ ] Test each combine mode (either, both and other available enum choices) without double-creating a gap.
- [ ] Test minimum size thresholds in pips and ATR and exact-size boundaries.
- [ ] Check direction, source bar, start time and duplicate identity.
- [ ] Test gaps that are crossed completely between candles, filled, or broken under all configured break modes.
- [ ] Verify M5/M15 adapters use the same evaluator and keep identities separate.
- [ ] Check market/session/weekend gaps against symbols whose trading hours create discontinuities.

### Exit gate
All method/combine modes have deterministic coverage; historical and live-replay outcomes agree.

## Module 8 — M15 Higher-Timeframe Overlay

### Verify and develop
- [ ] Detect only from completed M15 bars; do not use a forming M15 candle.
- [ ] Verify M15 close times map to the correct M5 close regardless of session gaps and bar alignment.
- [ ] Test insufficient HTF history, history loading, bars reloaded, lookback boundaries and restart/rebuild paths.
- [ ] Validate each detector path independently: M15 pivots, structure breaks, OB, FVG and Price Gap.
- [ ] Confirm an exception in one M15 detector does not suppress the other M15 detectors.
- [ ] Ensure hiding the M15 overlay hides its drawings but does not silently disable its detection when that detection is meant to remain active.
- [ ] Replay historical M15 detections through subsequent M5 bars so zones already filled/broken do not appear Fresh.
- [ ] Verify M15 zone start/end, duplicate handling and lifecycle against the M5 chart.

### Exit gate
No-look-ahead timing and cross-timeframe replay tests pass; each M15 detector agrees with its source rules on the target chart.

## Module 9 — Reference Levels

### Verify and develop
- [ ] Validate previous day/week/month levels against broker-provided higher-timeframe bars, including market holidays and missing periods.
- [ ] Test period-open levels across session boundaries and chart reloads.
- [ ] Verify the correct broker timezone and week-start conventions are used.
- [ ] Test duplicate level suppression and level rollover at the next period.
- [ ] Check line endpoints, labels and active/history semantics during zooming and panning.
- [ ] Ensure reference-level visibility and object budgets do not change calculated level values.

### Exit gate
A curated set of D/W/M/open levels matches broker bars, across at least two symbols and multiple history boundaries.

## Module 10 — Session Ranges

### Verify and develop
- [ ] Document the session timezone and daylight-saving assumptions explicitly.
- [ ] Test sessions that cross midnight, year/month boundaries and UK/US daylight-saving transitions.
- [ ] Verify high/low updates and exact start/end inclusion rules on lower timeframes.
- [ ] Test missing bars, short sessions, weekends and history reload/rebuild.
- [ ] Validate current versus past session retention and prevent unbounded drawing/cache growth.
- [ ] Confirm hidden session renderers clean up their own chart objects without resetting range calculation state.

### Exit gate
Session timestamps and extrema match a timezone-aware reference table and remain correct through chart reloads.

## Module 11 — Psychological / Round Levels

### Verify and develop
- [ ] Test round-step inference/configuration for different tick sizes and price scales.
- [ ] Validate center/band updates when price crosses multiple steps or the configured step changes.
- [ ] Ensure price jumps do not require iterating an unbounded range of obsolete level objects.
- [ ] Test major/minor level distinction, labels, color/opacity and overlap behavior.
- [ ] Verify viewport virtualization and removal/recreation when panning.
- [ ] Confirm changing visibility affects drawings only, not the chosen step or calculated levels.

### Exit gate
Step changes, large price jumps and viewport operations produce bounded, correct chart objects on multiple symbols.

## Module 12 — Cross-module integration, performance and release

### Integration
- [ ] Verify Structure → Equal Levels → Sweeps signal timing and no duplicate ownership.
- [ ] Verify Structure → Order Block → Breaker lineage and replay order.
- [ ] Verify FVG, Price Gap, OB, Breaker and M15 zones share lifecycle semantics without sharing detector filters.
- [ ] Test all meaningful feature-toggle combinations for accidental coupling.
- [ ] Run a full rebuild/replay after loading more history and after a chart reload; compare logical state summaries.
- [ ] Confirm all Broken zones remain non-renderable regardless of legacy broken-history switches. Decide whether those old public parameters should be formally deprecated/removed in a compatibility-reviewed change.

### Performance and release
- [ ] Benchmark startup, closed-bar processing and render time at approximately 1k, 10k and 50k bars.
- [ ] Record logical record counts, active indexed counts, visible objects, cache sizes and allocations.
- [ ] Run prolonged chart operations and confirm no unbounded chart-object/cache growth.
- [ ] Verify multiple indicator instances with distinct prefixes do not delete or overwrite each other's objects.
- [ ] Rebuild Release with the local cTrader Automate package; confirm published .algo path, size and timestamp.
- [ ] Keep CI/regression tests green; document remaining limitations before tagging/releasing.

### Exit gate
All module gates are satisfied or remaining limitations are explicitly accepted; historical replay and live/demo chart behavior agree; no known cross-module regression remains.

## Working protocol for each module

For every module, use this sequence:

1. Inspect the owning detector, parameter file, state owner, renderer and its consumers before editing.
2. Write down intended behavior and exact boundary conditions before changing rules.
3. Add or update pure evaluator tests first where practical.
4. Make the smallest change that belongs to that module; update shared infrastructure only for a clearly shared invariant.
5. Run the deterministic regression harness and Release build. Fix every failure before continuing.
6. Check the rendered result on cTrader, including pan/zoom, show/hide and historical replay.
7. Update this file with concrete evidence and unresolved cases, then proceed to the next module.

Do not mark a checkbox complete merely because code was written. Mark it complete only when the required test or chart acceptance has actually been performed.
