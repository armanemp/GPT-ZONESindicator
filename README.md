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

## Planned source layout

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and [docs/ROADMAP.md](docs/ROADMAP.md).
