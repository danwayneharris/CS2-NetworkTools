# Research integration and finishing investigation — 2026-10-02

Fast-forwarded dan/combined-smoothing-sprint from d03bdc5 to c198a7c; Bridge local
branch fast-forwarded from9ffceb8 to e2472d1. Preserved unrelated package-lock and
original checkout changes. Reviewed harness storage/dependency boundary, captured
pipeline propagation, native PE lookup adaptation, tests and Bridge stage wrappers.

Fresh local source generation and standalone harness build passed with zero
warnings/errors. Five-case immutable cohort passed; ramp discrepancy1.844440m
predicted versus1.844445m observed. All16 contract/negative checks passed. Evidence:
artifacts/native-integrated-cohort-01 and native-integrated-contracts-01.

Managed-finishing counterfactual from original initial state: anchor preview matches
native observation; permanent intentionally differs from the old native observation.
Do not relabel this old fixture as passing a correction. Compare the two newly
computed outputs and test the hypothesis with a separately identified live execution
intervention. The code remains a bounded source/storage/native-lookup adaptation,
not a complete offline game.

A persistent tool-only correction risks disappearing at load/rebuild. Before choosing
its scope, test the ORIGINAL managed finishing Execute with identical data via a
bounded generic Bridge experiment, explicitly recorded as mutation, not capture-only.
No new NetworkTools runtime fix yet. No global Burst setting change planned.

## Bounded intervention verified

Offline managed finishing on BOTH anchor inputs predicts exactly equal generated
EdgeGeometry for all five mapped edges. Managed permanent replay intentionally
fails against the OLD native result; old expected data was not edited.

Bridge adds explicit finishExecution=managed to bounded scheduling capture (default
native). It completes the original dependency, resolves a local copy's deferred
entity array, calls original managed Finish Execute, and preserves the original
scheduler for other stages/unarmed passes. This is a mutation experiment, not a
read-only probe. No global Burst setting was changed. NT's harness rejects mixed
execution metadata and rejects managed-intervention captures in native-lookup mode.
Eighteen negative/contract checks pass, including those two new rejection cases.

Bridge compilation passed (two existing obsolete-updater warnings), 29 component
contracts and 23 helper-method checks passed, and all eight native scheduler
signatures were tested with missing/extra-site negatives. This qualifies the bounded
experiment, not a permanent Harmony compatibility patch.

Preserved research session before graceful restart:
CitiesIIAgentBridge-regression-before-reload-20261002-141548-91ded3ce.cok.
Deployed experimental Bridge SHA256:
70D14ED0705962498522559694903D935F67AEA2F21AD2C4A819D0C48EA519CE.
Original Bridge DLL/PDB backed up under sibling Bridge worktree
artifacts/managed-finish-deployment-backup. NetworkTools deployment unchanged04792dd.
Loaded checksummed baseline ending070428-c983f9ef with Burst enabled, verified original
curves through prepare-surface-preview-replay. Wantagh, population0, paused.

Live artifacts/managed-finish-preview-01: accepted Combined1.0 preview after one
managed-finishing pass. artifacts/managed-finish-apply-01: fresh checkpoint before
Apply submission4; all five authored curves AND generated surfaces match exactly
between preview and permanent results, stable at later observation. Nearby terrain
sample heights NOW change by up to1.836336m (1548points). This supports terrain being
a downstream visible effect; old unchanged samples did not establish irrelevance.
Directed lane-transition identities at nodes54969,54968,54966 are unchanged
(3,4,14transitions respectively; zero added/removed). Not vehicle-traversal evidence.
Both new captures independently pass all eight source-driven offline stages using
managed lookup semantics. Compact results:2026-10-02-managed-finish-result.json.

Final verified checkpoint:
CitiesIIAgentBridge-managed-finish-experiment-completed-20261002-142424-207b8a77.cok.
Game remains paused, selection cleared after Apply. CitySession
0ab65c9f145047f693e031f2f667e6b1, process52740 at verification. The experimental result
is checkpointed; no subsequent game edits. Trace ended/unpatched, no pending pass.
No persistent fix yet: ordinary rebuild/reload may restore the native discrepancy.
No new human visual review, traversal, performance or broad-world qualification.

## Pending scope decision

Asked Dan whether to implement a version-pinned Debug-only compatibility fix for the
native finishing job. A durable fix must also handle save/load and ordinary rebuilds,
so it reaches beyond active NetworkTools operations. Await that choice before changing
persistent runtime behavior. Do not silently adopt a global managed finishing path:
consider targeted repair of affected lookup results, dependency/lifetime preservation,
version guards, inter-mod patch ordering, timing measurement, repeated rebuild and
save/reload tests. Do not alter target curves or terrain math to hide the lookup bug.
