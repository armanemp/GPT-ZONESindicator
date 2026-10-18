# Project Roadmap

This file is the short project overview. The **authoritative phase order, scope, tests, exit gates, change-control rules and closeout log** live in [MODULE_DEVELOPMENT_ROADMAP.md](MODULE_DEVELOPMENT_ROADMAP.md). Do not maintain a second, overlapping checkbox list here.

## Current status

- **Local Release build:** passed with 0 warnings and 0 errors, as reported by the user.
- **Deterministic regression harness:** passed all test groups in the user's latest run after correcting the malformed pivot-test fixture.
- **Active phase:** Phase 1 — Major and Internal Market Structure.
- **Current blocker before advancing:** confirm the structure behaviors on a real cTrader XAUUSD M5 chart. Automated tests are not a substitute for chart acceptance.
- **Next phase after acceptance:** Phase 2 — Equal Highs / Equal Lows.

## Phase order

| Phase | Scope | Exit condition |
|---:|---|---|
| 0 | Baseline and test workflow | Baseline build and regression harness are green |
| 1 | Major/Internal Market Structure | Confirmed swings, BOS/CHoCH, distance filter and swept-level behavior pass tests and chart checks |
| 2 | Equal Highs / Equal Lows | Cluster matching, merging, lifecycle and chart history pass |
| 3 | Liquidity Sweeps | Thresholds, source-level closure, duplicates and event timing pass |
| 4 | Order Blocks | Source selection, bounds, thresholds, duplicates and historical replay pass |
| 5 | Breaker Blocks | Parent/child link, reverse direction, confirmation time and lifecycle pass |
| 6 | FVG | M5 detector rules, lifecycle and replay pass |
| 7 | Price Gaps | Method/combine rules, lifecycle and replay pass |
| 8 | M15 overlay | Closed-bar timing and M15-to-M5 replay pass |
| 9 | Reference Levels | Prior periods and opens match broker bars/time conventions |
| 10 | Session Ranges + Psychological Levels | Separate time-window and round-step checks pass |
| 11 | Cross-module integration | Toggles, shared lifecycle, pan/zoom, object cleanup and rebuild checks pass |
| 12 | Performance and release | Benchmarks, compatibility, artifact and known limitations recorded |

## Rules that prevent looping

1. Only one phase is active at a time.
2. Each phase has a fixed scope and an explicit exit gate.
3. The full test harness runs whenever code or test code changes; a Release build runs at phase gates and after source changes.
4. After a phase is accepted, do not revisit it unless a reproducible regression, a missed requirement or a specifically approved new requirement is recorded.
5. Fix correctness blockers now. Put unrelated cosmetic improvements and new strategy ideas into a backlog, rather than expanding the current phase.
6. Record the commit, build/test result, chart evidence and accepted limitations in the detailed plan before moving forward.

Start here: [Phase execution plan and current checklist](MODULE_DEVELOPMENT_ROADMAP.md).
