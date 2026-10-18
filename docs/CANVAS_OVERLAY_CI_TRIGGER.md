# Canvas overlay CI validation

This branch is a temporary validation head created from `phase-9/feature-module-isolation` to run the repository's existing Release build and deterministic regression harness against the experimental Canvas zone overlay.

The Canvas option remains disabled by default until the overlay is accepted on a live cTrader Windows chart. Automated CI does not validate chart z-order, screen coordinates, pan/zoom behavior, or runtime performance. This note was refreshed after the parameter-group compile fix to trigger a new merge-ref validation.
