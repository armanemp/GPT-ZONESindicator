# Execution Plan — GPT-ZONESindicator

**This is the single source of truth for development order, active phase, acceptance gates and completion evidence.** `docs/ROADMAP.md` is only the short overview. When these documents differ, this file governs execution.

The goal is a reliable cTrader indicator—not the largest possible checklist. We finish one bounded deliverable, prove it, freeze it, and then advance. We do not repeatedly re-audit every completed module without a reproducible defect.

## Operating rules: how we avoid going in circles

1. **One active phase at a time.** The active phase is shown in the status table below. Do not begin new feature work in a later phase while the current phase has a failed test or an unresolved correctness defect.
2. **Each phase has a fixed scope and exit gate.** Work only on that phase's owner files and necessary tests. Shared infrastructure may change only for an explicitly shared invariant.
3. **Correctness before extra features.** First prove detection rules, timing, state transitions, duplicate suppression and historical replay. Then tune visuals/performance.
4. **A completed phase stays closed.** Reopen it only for a reproducible regression, a chart example proving the documented rule is wrong, or a new requirement explicitly added to scope. Record a test that would prevent the issue returning.
5. **Separate defect classes.** A failed build/test, look-ahead, false/missing signal, wrong lifecycle state, cross-module interference or resurrected terminal object blocks phase closure. A cosmetic preference or optional enhancement goes into the backlog unless it makes the current output misleading.
6. **Do not confuse evidence.** Pure evaluator tests prove calculation rules; the full regression harness proves tested interactions; build proves packaging; actual cTrader chart checks prove platform rendering/behavior. One is not a substitute for another.
7. **No silent scope creep.** Do not rename public parameters, change defaults, rewrite a different detector, or add unrelated refactors while fixing one module.
8. **Record a phase closeout.** Before advancing, note the commit, build result, regression result, chart cases checked, and any explicitly accepted limitation.

## Current state

| Item | Status | Evidence / remaining work |
|---|---|---|
| Local Release build | **Passed** | User output: 0 warnings, 0 errors. The subsequent commit changed test data only. |
| Deterministic regression harness | **Passed** | User output after pulling `610de11`: all printed test groups passed, including pivot validation, BOS/CHoCH, swept-level visibility and dual-pivot distance. |
| Current module | **Phase 1 — Market Structure** | Code-level fixes and automated tests are in place. XAUUSD M5 chart acceptance is still required. |
| Equal Highs / Equal Lows | Not started as an active phase | Starts after Phase 1 is accepted and frozen. |
| Shared zone/render integration gate | Pending | Broken-zone removal, viewport/pan/zoom, toggle independence and instance-owned cleanup will be checked together in Phase 11 rather than repeated in every detector phase. |
| Release readiness | Not ready | Live chart acceptance and remaining module gates are incomplete. |

The successful regression output is already recorded as evidence. Do not rerun the same full harness after every documentation edit; rerun it when code or test code changes, and always at each phase gate.

## Phase map and dependencies

| Phase | Deliverable | Depends on | Status |
|---:|---|---|---|
| 0 | Baseline, rules and test workflow locked | — | Complete for current development; original parameter parity audit remains a release task |
| 1 | Major + Internal Market Structure | Phase 0 | **ACTIVE — chart acceptance** |
| 2 | Equal Highs / Equal Lows | Confirmed structure swings | Waiting |
| 3 | Liquidity Sweeps | Structure + equal-level lifecycle | Waiting |
| 4 | Order Blocks | Confirmed structure-break signals | Waiting |
| 5 | Breaker Blocks | Broken Order Block parents | Waiting |
| 6 | Fair Value Gaps (M5) | Shared zone lifecycle | Waiting |
| 7 | Price Gaps (M5) | Shared zone lifecycle | Waiting |
| 8 | M15 Higher-Timeframe overlay | M5 detectors/lifecycle and HTF timing | Waiting |
| 9 | Reference levels (prior D/W/M and opens) | Broker bars and time conventions | Waiting |
| 10 | Session ranges + psychological levels | Time/tick/pip conventions | Waiting |
| 11 | Whole-indicator integration and chart-object correctness | Phases 1–10 | Waiting |
| 12 | Performance, compatibility and release | Phase 11 | Waiting |

This order follows actual dependencies: confirmed swings feed equal-level clusters and Order Blocks; swings/clusters feed liquidity sweeps; broken OBs create Breakers; M15 reuses the zone-producing detectors and shared lifecycle. M5 FVG and Price Gap are separate phases because they have independent detection rules, even though both share zone state/rendering.

---

## Phase 0 — Lock baseline and working method

### Baseline agreed
- [x] Maintain one cTrader indicator entry point with feature-owned implementation/parameter files.
- [x] Keep all signal confirmation on closed candles and avoid look-ahead.
- [x] Keep logical detection/state separate from visibility, viewport and chart-object budgets.
- [x] Run the dependency-free deterministic regression harness and the Release build.
- [x] Use the existing branch `phase-9/feature-module-isolation` as the working branch for this sequential pass.
- [ ] Before release, compare public parameter names/defaults against the supplied baseline once and document any intentional differences.

### Standard commands
Run from `C:\Users\armanemp\Desktop\GPT\GPT-ZONESindicator`:

```powershell
dotnet build .\GPT-ZONESindicator.csproj --configuration Release --no-restore
dotnet run --project .\tests\ZoneIntervalIndexSmoke\ZoneIntervalIndexSmoke.csproj --configuration Release
```

If the package cache or restore state changes, run `dotnet restore .\GPT-ZONESindicator.csproj --ignore-failed-sources` before the build.

### Exit gate
The current baseline compiles and the complete deterministic harness passes. This gate has been achieved for the current code/test state based on the user's latest output. Later code changes must run the relevant tests again.

## Phase 1 — Major and Internal Market Structure **(ACTIVE)**

**Purpose:** establish dependable confirmed swings and correct BOS/CHoCH events before developing modules that consume those signals.

### Already implemented and covered by tests
- [x] Pivot confirmation waits for configured right-hand closed bars.
- [x] Preserve the existing asymmetric tie rule: equal extremes on the left disqualify; right-side ties follow the existing rule.
- [x] Reject non-finite prices and malformed High/Low ranges in pivot inputs.
- [x] Reject non-finite close/swing prices before evaluating a structure break.
- [x] Classify BOS versus CHoCH based on prior trend, including neutral-to-trend transitions.
- [x] Make minimum swing-distance filtering independent of high-first/low-first insertion when one candle qualifies as both a high and a low pivot.
- [x] Prevent swept swings and silent breaks from being displayed as live active levels.
- [x] Add deterministic tests for the rules above; the user's latest harness run reports all tests passing.
- [x] Release build succeeded with 0 warnings and 0 errors before the test-fixture-only follow-up; no indicator-source change was made in that follow-up.

### Remaining work: one bounded chart acceptance pass
On cTrader, load the newly built `GPT-ZONESindicator.algo` on **XAUUSD / Gold, M5**. Use the existing/default parameters first; do not tune many settings while testing.

- [ ] **Confirmation timing:** Major swing (default right bars = 15) is not confirmed before the right-hand bars close; Internal swing (default right bars = 3) follows its own setting. The mark is anchored at the pivot candle after confirmation, not at the confirmation candle.
- [ ] **Break correctness:** sample clear bullish and bearish closes through a confirmed swing. BOS/CHoCH classification agrees with the prior trend and labels sit at the actual break event.
- [ ] **Sweep removal:** after a swing is marked swept, its active-level line disappears; the sweep event may remain as a distinct event drawing. A normal active level must not continue into the future.
- [ ] **Distance filter:** with minimum distance disabled, valid swings appear normally; with a minimum set, too-close dual pivots are rejected symmetrically instead of depending on processing order.
- [ ] **Viewport behavior:** pan/zoom left and right. No duplicate structure lines/markers appear; eligible objects are removed off-screen and restored only when they belong in view.
- [ ] **Toggle isolation:** turn Major and Internal lines/markers on/off and confirm that visibility changes do not alter the detected swing/break sequence.

**Evidence to record:** report pass/fail for each of these six checks. If a failure is found, include one screenshot plus symbol/timeframe and the applicable parameter values. Fix only the reproducible structure issue, add a regression test if the rule can be expressed deterministically, rerun the harness/build, and repeat only the failed chart check plus a quick regression check.

### Exit gate
All six checks pass or any non-blocking visual preference is explicitly deferred. Then freeze this phase with a commit and begin Phase 2. No extra structure feature should be added before this gate.

## Phase 2 — Equal Highs / Equal Lows

**Purpose:** ensure confirmed swing points produce stable liquidity clusters.

- [ ] Document the exact tolerance formula and units used by the current implementation.
- [ ] Test pair matching at just below, exactly at and just above the tolerance.
- [ ] Test minimum separation, clean-range predicate and backward-search early-exit conditions.
- [ ] Test cluster count/representative-level updates as new confirmed swings are added.
- [ ] Test merge versus no-merge cases, including the case where a new swing could appear to match more than one cluster.
- [ ] Ensure each swing maps to the intended single cluster, with no stale mappings after a cluster closes.
- [ ] Test close-through break versus wick/body sweep; terminal state and end time must be deterministic.
- [ ] Test history rebuild and replay produce the same clusters as forward processing.
- [ ] On chart, confirm equal levels start/end at the intended bars and panning does not duplicate them.

### Exit gate
Pair/merge/lifecycle tests pass; historical replay matches live closed-bar behavior on curated examples; chart display is confirmed. Then freeze and move to Phase 3.

## Phase 3 — Liquidity Sweeps

**Purpose:** turn eligible swing/equal levels into unique, correctly timed sweep events.

- [ ] Cover Wick and Body measurement, bullish and bearish symmetry.
- [ ] Test penetration and return thresholds below/equal/above boundary.
- [ ] Confirm close-back requirement and exact source/event times.
- [ ] Test that closed/swept sources cannot emit the same sweep repeatedly.
- [ ] Test duplicate suppression by direction, event bar, tick tolerance and equal-level/swing overlap.
- [ ] Confirm `SweepEqual` and per-engine sweep toggles affect only intended sources.
- [ ] Verify the source level exits active display and a separate sweep event is drawn at the correct candle.
- [ ] Run event-level replay from confirmed swing and equal-cluster inputs.
- [ ] Verify on chart that wick-only sweeps and body-only sweeps match the chosen measure.

### Exit gate
All predicate and end-to-end replay tests pass and chart events match expected source levels. Then freeze Phase 3.

## Phase 4 — Order Blocks

**Purpose:** reliably identify the source candle for confirmed structure displacement.

- [ ] Verify MajorOnly/InternalOnly/MajorAndInternal source modes.
- [ ] Test leg length, break strength and displacement constraints.
- [ ] Test deterministic candidate/extreme selection when multiple opposite candles qualify.
- [ ] Test FullCandle/BodyOnly bounds, minimum size, maximum size and body fallback.
- [ ] Prevent duplicate OBs from the same source identity without suppressing valid distinct OBs.
- [ ] Replay every bar from source/origin to confirmation so retrospectively found OBs are not incorrectly Fresh if already filled or broken.
- [ ] Confirm M5 and M15 identities remain separate while both use the same lifecycle rules.
- [ ] Test OB visibility toggles do not alter signal creation/lifecycle or Breaker eligibility.
- [ ] Verify candle origin, confirmation time, rectangle bounds and subsequent fills/breaks on chart.

### Exit gate
Candidate/rule tests and full discovery replay pass; at least a bullish and a bearish chart example match the defined rules. Then freeze Phase 4.

## Phase 5 — Breaker Blocks

**Purpose:** create a single correctly timed reverse-direction child from an eligible broken OB.

- [ ] Require a Broken OB parent and prevent duplicate child creation.
- [ ] Verify `ParentId` and parent `ChildId` remain reciprocal through replay/rebuild.
- [ ] Confirm bounds are preserved, direction reverses, and the child begins at break confirmation time.
- [ ] Cover invalid/small bounds, invalid scoring inputs and repeated creation attempts.
- [ ] Test the Breaker’s own break mode and subsequent lifecycle.
- [ ] Verify hidden/broken parent history cannot be accidentally drawn as an active zone.
- [ ] Test combinations of OB and Breaker visibility switches.
- [ ] Verify M15 OB → Breaker timing with no look-ahead.
- [ ] Check parent and child on the actual chart.

### Exit gate
Factory/lifecycle/replay tests pass and parent-child linkage/start time is visibly correct. Then freeze Phase 5.

## Phase 6 — Fair Value Gaps (M5)

- [ ] Test bullish and bearish three-candle imbalance with exact boundary cases.
- [ ] Test minimum pips/ATR size, displacement/body ratio, direction filter and all combine modes.
- [ ] Test every start-time mode.
- [ ] Test duplicates without collapsing distinct legitimate gaps.
- [ ] Confirm touch, partial fill, full fill and break transitions under each relevant break mode.
- [ ] Ensure hidden/broken gaps are not recreated by viewport/history redraw.
- [ ] Replay curated historical gaps through later bars and compare with forward processing.
- [ ] Verify several valid and rejected examples on XAUUSD M5.

### Exit gate
Rules/boundaries, duplicate tests and historical replay pass; chart examples match documented FVG rules. Then freeze Phase 6.

## Phase 7 — Price Gaps (M5)

- [ ] Test Open/Close and High/Low methods independently.
- [ ] Cover every combine mode and prove it does not duplicate the same logical gap.
- [ ] Test pip/ATR minimum-size boundaries, direction and source/start time.
- [ ] Test full gap-crossing candles and all zone break modes.
- [ ] Verify lifecycle, identity and duplicate suppression through replay.
- [ ] Validate weekend/session discontinuities where applicable.
- [ ] Compare valid/rejected chart examples with the selected method and combine parameters.

### Exit gate
Each method/combine combination has deterministic tests; replay and chart outcomes agree. Then freeze Phase 7.

## Phase 8 — M15 Higher-Timeframe Overlay

**Purpose:** reuse detector rules on closed M15 candles without future information or duplicate identities.

- [ ] Confirm a signal is not produced from a forming M15 candle.
- [ ] Test exact M15-to-M5 close mapping, including gaps and market-session boundaries.
- [ ] Test insufficient HTF history, additional history loads, reload and rebuild.
- [ ] Test M15 pivots/structure, OB, FVG and Price Gap paths independently.
- [ ] Prove one M15 detector exception does not suppress the other detectors.
- [ ] Hiding the overlay affects drawing only when that is the configured behavior, not the underlying detection.
- [ ] Replay M15 discoveries through subsequent M5 bars so already filled/broken zones do not appear Fresh.
- [ ] Keep M5/M15 duplicate identity policy explicit and tested.
- [ ] Verify M15 rectangles/times/bounds on the M5 chart.

### Exit gate
Timing/replay tests pass and chart boundaries align with completed M15 bars. Then freeze Phase 8.

## Phase 9 — Reference Levels

- [ ] Verify previous day/week/month high/low against broker-provided higher-timeframe bars.
- [ ] Document broker timezone and week/period boundary conventions.
- [ ] Test previous-period levels when there are holidays or missing bars.
- [ ] Test period opens and rollover at the next period.
- [ ] Ensure timeframe history loads/reloads do not leave stale lines.
- [ ] Verify display toggles and viewport budgets are presentation-only.
- [ ] Validate lines/labels on chart against source bars.

### Exit gate
Reference levels match broker source bars across representative period boundaries and at least two symbols. Then freeze Phase 9.

## Phase 10 — Session Ranges and Psychological Levels

This phase contains two separate sub-gates. Do not change session time logic while testing round-level math, or vice versa.

### 10A — Session Ranges
- [ ] Explicitly document the session timezone and daylight-saving assumptions.
- [ ] Test start/end inclusion, overnight sessions, year/month boundaries, weekends and missing bars.
- [ ] Test UK/US daylight-saving transition dates with a reference table.
- [ ] Validate intraday high/low updates and retention limits.
- [ ] Ensure a render toggle cleans its objects without erasing logical range state.

**10A exit gate:** all defined time-window cases match the reference table and chart.

### 10B — Psychological / Round Levels
- [ ] Test tick/pip/price-step selection across different symbol price scales.
- [ ] Test center/level updates when price jumps across multiple steps or configured step changes.
- [ ] Bound work and object cleanup after large price jumps.
- [ ] Verify major/minor distinction, labels, styles and overlap.
- [ ] Confirm hide/show and pan/zoom do not alter calculations or create duplicate objects.

**10B exit gate:** level calculations remain correct across a metals and a non-metal symbol, and rendering remains bounded.

## Phase 11 — Whole-indicator integration and chart-object correctness

This is the one place for broad cross-module checks. Avoid repeating this full matrix in every feature phase.

- [ ] Structure → Equal Levels → Sweeps event timing and no double ownership.
- [ ] Structure → OB → Breaker parent/child lineage and confirmation timing.
- [ ] FVG, Price Gap, OB, Breaker and M15 zone types follow the shared lifecycle without sharing their detector thresholds.
- [ ] **Broken-zone contract:** broken zone rectangles/midlines/labels are removed on the next render reconciliation and cannot be revived by pan/zoom, rank/display settings or legacy broken-history parameters.
- [ ] Filled zones remain terminal, with optional historical display only where configured.
- [ ] Each show/hide toggle is rendering-only and leaves unrelated modules unchanged.
- [ ] Delete some chart objects manually and verify the renderer reconciles missing enabled objects.
- [ ] Scroll/zoom/resize across distant history; visible objects are restored correctly and invisible objects are released.
- [ ] Object budgets only limit drawing, not detector state. Record visible count versus configured budget.
- [ ] Two indicator instances with different prefixes do not delete or overwrite each other's objects.
- [ ] History load/reload/rebuild produces the same logical results as replay from the same closed bars.
- [ ] No duplicate objects/signals after rebuild or repeated viewport changes.

### Exit gate
All integration cases pass on cTrader; shared zone lifecycle and object ownership are accepted. Then freeze behavior before performance work.

## Phase 12 — Performance, compatibility and release

Performance changes begin only after correctness is stable. Every optimization must preserve reference outputs.

- [ ] Benchmark load and replay on approximately 1k, 10k and 50k bars.
- [ ] Record per-bar processing time, render time, logical records, active-index records, visible chart objects and cache sizes.
- [ ] Check for linear/unbounded cache or chart-object growth during long operation.
- [ ] Compare optimized replay against a brute-force/reference result for seeded and curated data.
- [ ] Test at least Gold/XAUUSD M5 and one non-metal symbol/timeframe with different pip/tick characteristics.
- [ ] Verify clean restore/build with the installed cTrader Automate package.
- [ ] Review public parameter names/defaults once against the supplied baseline; document intentional differences.
- [ ] Verify artifact filename and published path; record output size/date/hash for the accepted build.
- [ ] Update the Known Limitations section and release notes. Do not tag a final release with open correctness defects.

### Exit gate
Release build/test pass; integration chart acceptance is complete; no unresolved blocker remains; parameters, artifact and limitations are documented.

## Backlog / change-control rule

A newly discovered request or issue must be assigned to one of these before coding:

- **Blocker for current phase:** incorrect signal/timing/state, missing/extra active level, broken/filled zone resurrected, look-ahead, test/build failure, or cross-module corruption. Fix now with a reproducer.
- **Required within current phase:** behavior named in the current phase checklist. Fix before exit.
- **Later enhancement:** new strategy/rule, cosmetic preference, optional score/filter, or performance polish that does not make current outputs incorrect. Record it and do not derail the active phase.
- **Regression in a closed phase:** reopen only the owning phase; add a test reproducing the failure; rerun that phase's gate and the integration checks directly affected. Do not restart the whole roadmap.

When a phase is complete, add a short closeout record below: phase number, commit SHA, build/test result, chart evidence and remaining accepted limitations.

## Phase closeout log

| Phase | Commit | Build + tests | Chart acceptance | Notes |
|---|---|---|---|---|
| 0 — Baseline | Current working branch | Release build passed; deterministic harness passed in user's latest output | Shared chart integration checks are scheduled for Phase 11 | Parameter parity audit remains a release gate |
| 1 — Market Structure | `610de11` includes the corrected test fixture; current branch has subsequent docs commits | Release build passed on unchanged indicator source; full harness passed after the test fixture correction | **Pending: six XAUUSD M5 checks above** | Current active phase |
