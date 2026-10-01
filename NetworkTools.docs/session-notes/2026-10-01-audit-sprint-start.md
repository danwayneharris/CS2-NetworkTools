# Audit sprint — October 1, 2026

Base: PR #13 open, `dan/terrain-profile-sprint` at `ee9c3bcd7272e5f18f41166e2dad2a4d12744ed6`.
Worktree: `nt-audit-sprint`; new branch: `dan/audit-correctness-sprint`.

Original checkout has user lockfile edits, untracked audits and personal files. None were modified. Audit documents were copied unchanged into this worktree. The sprint targets correctness, trustworthy tests and current documentation; required instrumentation is retained. Geometric errors through 5 cm are acceptable under the current policy, but topology, semantic connections, stale inputs and accumulating drift remain independent checks.

Initial source inspection confirms F03 (whole-curve writes at two moved incident nodes can compete) and F10 (position replacement defaults Node rotation). F02's transform cache and independently captured submission inputs also remain separate. No new build or game verification yet.

Delegation: test-runner/Connect oracle fixes, path-search state, and read-only remaining findings. Only the parent controls the game and shared build/deployment.
## Incident-edge composition and node fields

F03/F10: Preview and Apply now gather each unselected incident edge once and compose both endpoint deltas from its original curve. This also removes the old sub-millimeter Preview/Apply translation threshold discrepancy. Node writes copy the original component and change position only. Native reconstruction remains independent and still needs live verification.

Non-deploying Debug compile passed with existing warnings. Compiled production helper tests passed for both-end edits, reversed storage, unchanged opposite end, preserved endpoint tangents and rotation (including unchanged position). Existing eight slope fixtures / 96 assertions also passed. These are not ECS scheduling or native-output tests.


## Calculation snapshot and Apply ownership

F02/F09: the original-component capture is now configuration-independent. Path gathering and its comparison baseline occur together after dependency completion. Before a new preview, changed originals trigger regathering instead of certifying an old cache against newer ECS values. Submitted originals are copied from that cache baseline. Shared UI/provider Apply checks require matching cache/submission/current values and parameter revision; execution rechecks after the request before scheduling the permanent job. Provider reports rejection if the shared request is no longer accepted. Debug native observations remain required for Curve Smooth and Slope; Straighten has no claimed native-observation contract. Diagnostic copy-corruption checks and exports are retained.

Existing capture bounds (128 nodes / 512 incident edges / 64 edges per node) now also bound ordinary manual operations rather than allowing unchecked Apply. Prefab values and all generated simulation state are not exhaustively fingerprinted; this is a scoped authored-input guard. Before-submit/during-rebuild/pre-Apply and stale-revision helper checks pass (16 total). Debug and Release C# compile passed; neither establishes native verification. Release endpoint/interior junction smoothing is provisionally gated pending a player-facing explanation and full validation.

## Release supported envelope and UI explanation

F01: Release rejects Curve Smooth candidates containing degree > 2 nodes, including selection endpoints, before emitting preview geometry and again at Apply. A localized UI hint explains that junction smoothing is unavailable in that build. Debug junction validators/search and the intentionally Debug-only surface correction remain untouched. Canonical English split labels were added (F15 bounded localization cleanup). Release C# compilation passes; UI build, native UI and Burst execution remain pending.

Read-only live preflight matched the existing settled toy snapshot exactly: fingerprint ddd31890adfa9fbe7cb861d45eb67938c9610c893a4ed677e059e41fc313e150 and all 37 shared-node normalized lane mappings. Game remains paused, citySession 65dfb793e5a04bdc9f3bffdffcb8fb3b; no game mutations yet.


## Independent review and integrated offline run

Review caught deleted selected endpoints reaching direct lookup during regather. Explicit selected-node/edge liveness, component presence, ordered adjacency and both incident-buffer memberships now reject before gathering, preventing missing-edge zero states from acquiring a fresh baseline. Snapshot bounds remain a deliberate supported envelope for this iteration; broader selection support is deferred. The shared panel explains invalid/oversized selection. The first UI text edit script stopped on an exact-match assertion after removing the curve-only hint; this follow-up completes the shared-panel hint. No deployment occurred between those edits.

All seven offline stages passed before the final preflight/UI edits: geometry, path search, parameters, codegen, production slope/incident tests, original-input comparison, and Python. Debug compile and production slope tests also pass after preflight. The UI float callback restores rejected optimistic values without emitting a parameter change. Native UI and final integrated tests remain pending.

## Integrated build and first native verification

Final code at 77f8c64 passed all seven offline suites. The first aggregate lacked Git on its process PATH; a second run with Git available also passed and records revision/working-tree metadata (artifacts/audit-offline-provenance). Full Debug C#/postprocessing/UI/deployment passed. DLL SHA256: CB9213E6E829C1A50B70E243CCDCE26070BCFEDBFE168EC661211090F8DACCDA. Full Release build and Burst compilation of 22 methods passed, but native Release execution remains unverified. See the packaging note for the failed staging isolation attempt and subsequent full Debug redeploy. npm changed the isolated worktree lock only; declared root dependencies were unchanged. The changed lock/diff were backed up before restoring committed content.

The settled ramp baseline reloaded with exact geometry and all 37 normalized shared-node lane mappings unchanged. The first repeat attempt mistakenly selected highway-ramp-out (mainline plus ramp), not the historical offramp-only case. That different operation moved controls 14.23 m and nodes 10.10 m, so its repeat audit correctly failed; it is not evidence of an identical-operation regression. Its completed Apply preserved topology, directed/physical lane mappings and exact preview/permanent agreement. It was checkpointed before restoring the baseline.

The exact reversed offramp-only Constant Slope repeat then passed: region fingerprint unchanged, zero control/node displacement, exact preview/Apply agreement, lane mappings and unselected geometry preserved. A stale provider revision was rejected. The compact settled fixture is now tracked as scripts/fixtures/toy-surface-settled.json; it identifies a separately retained local toy save, not a bundled city. Provider verification does not establish ordinary UI behavior or vehicle traversal.
