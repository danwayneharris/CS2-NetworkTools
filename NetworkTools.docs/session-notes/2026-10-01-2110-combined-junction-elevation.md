# Combined interior junction elevation follow-up

Dan reported that combined Constant Slope preserves unwanted interior junction heights. Read-only live selection: seven edges, two three-way interior junctions, combined enabled, ready, strength 1. Source confirms full XYZ and separate original grades were pinned.

Authorized policy adjustment: keep interior junction XZ, release Y and selected-path vertical grades; preserve outer endpoints and explicit split XYZ. Reuse existing unique incident-edge endpoint/adjacent-handle translations for side branches, with both endpoint edits composed. Validate predicted side-road changes rather than disabling outside-selection checks. Native directed connections remain mandatory.

Initial implementation adds a production-fit counterexample with a junction ten metres above the desired grade. Native testing and deployment pending; do not claim success from compilation.

Offline implementation verified: all seven aggregate suites passed, including 34 combined production-transform assertions and 11 live-runner unit tests. Incident oracle independently computes per-end Y translation and rejects missing translations, XZ edits, altered handles and nonfinite controls. Initial compile caught an observer parameter-name mistake; corrected before passing tests. PowerShell stderr handling stopped the first aggregate wrapper on a harmless uv warning; direct invocation of the same aggregate passed. No test was relabeled.

Captured baseline: `CitiesIIAgentBridge-combined-interior-junction-baseline-20261002-040829-f049c06c.cok`, 40 nodes/41 edges in bounded region; fixture records package SHA and geometry fingerprint. Native deployment pending.
