# GPT-ZONESindicator

A modular cTrader Automate indicator for Smart Money Concepts (SMC) zones, market structure, liquidity, and reference levels.

## Status

The repository was created empty. The supplied `SmartZonesNativePro` source is the functional baseline for the initial modularization. The implementation is being organized by responsibility using C# partial classes so the cTrader indicator remains a single runtime component while the source is maintainable across focused files.

## Design constraints

- Detect from closed bars to avoid intrabar look-ahead and history/live divergence.
- Keep zone records and zone state independent from chart-object visibility.
- Keep fixed zone boundaries; interaction changes state rather than shifting boundaries.
- Scope chart-object cleanup to the current instance prefix.
- Preserve parameter names/defaults and feature behavior during the structural extraction.
- Keep platform-facing API calls at the indicator/rendering boundary.
- Validate compilation against the installed cTrader Automate API and validate chart behavior in cTrader before claiming runtime acceptance.

## Development workflow

Development is organized into gated phases, one module at a time. The single source of truth for phase order, current status, scope, tests, exit gates, and the change-control/closeout log is [docs/MODULE_DEVELOPMENT_ROADMAP.md](docs/MODULE_DEVELOPMENT_ROADMAP.md). The short overview is [docs/ROADMAP.md](docs/ROADMAP.md).

Do not start a later module until the active phase passes its required tests and its chart-only acceptance is either completed or explicitly recorded. Reopen a completed phase only for a reproducible defect or an approved requirement change.

## Planned source layout

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and [docs/ROADMAP.md](docs/ROADMAP.md).
