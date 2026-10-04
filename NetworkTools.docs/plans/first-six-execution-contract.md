# First six plans: shared execution and review contract

Status: reviewed planning draft, October 3, 2026. Saving these plans does not start
implementation. Execution begins under the subsequent approved goal-mode instruction.
Read the [plan index](README.md), applicable repository instructions, and each plan.

## Order, baseline, and publication

Execute NT-001 → NT-021 → NT-023 → NT-002 → NT-003 → NT-022. Planning baseline:
merged main f36d669 plus local roadmap-consolidation documentation. Verify actual
main/remotes/worktree state before execution, preserve unrelated changes, and use an
isolated development worktree. Reconcile intervening changes rather than blindly resetting.

Each stage produces documentation, incremental commits, and one draft PR in
danwayneharris/CS2-NetworkTools stacked on its predecessor. The first targets verified
main and includes unpublished planning documentation if necessary. Dan reviews all
six afterward; there is no intermediate manual-validation gate.

The approved execution sprint may push its new branches and open those draft PRs.
Do not merge, publish a mod, or rewrite existing published history. Saving these files
alone does not start that sprint or authorize current publication.

Preserve independently reproducible stage checkpoints. After later authorization to
fix/rebase the stack, correct the affected stage, preserve old tips, rebase descendants,
resolve semantic changes, rerun affected tests, and refresh provenance. This planning
instruction is not current force-push authorization.

## Scope and engineering rules

- Keep Bridge generic and independently releasable. NT-specific interfaces, schemas,
  tests, and behavior belong in NT.
- Preserve defaults unless explicitly changed by a plan.
- Keep existing and new experimental features Debug-only. Explain unsupported
  controls; Release promotion is a later roadmap item.
- Retain useful diagnostics, replay hooks, original/candidate/native identities,
  timings, rejection reasons, lane observations, and provider access. Required
  validation must remain independent of optional logging.
- Reuse existing runners and ground native behavior in installed source/version.
- Preserve native lifetimes, dependencies, traversal orientation, offsets, topology,
  and declared incident-edge behavior.
- Exclude general routing, tunnels, arbitrary lane-connector editing, broad neighbor
  modifications, release preparation, and perfect slopes.
- Accept geometric discrepancies through 5 cm for agreed positional checks.
  Analytical contracts may be tighter. Connectivity, lane identity, topology,
  stale evidence, and material cumulative drift remain strict.

## Testing and autonomous game work

Record revision, configuration, activation flags, deployed hashes, fixture
identity/fingerprint, assertion policy, results, and limitations in machine-readable
summaries where practical. Distinguish offline tests, native preview, permanent Apply,
repeatability/reload, Release/Burst, human visuals/UI, and vehicle traversal.
Compilation, command acceptance, and offline replay do not prove native success.

The execution sprint may build, deploy, visibly launch and gracefully restart CS2,
load verified toy baselines, make unique checkpoints, and preview/Apply toy changes.
Use the reduced playset. Never load real cities, overwrite baselines, change unrelated
system/agent settings, or force-kill an unresponsive game. Respect STOP and controls.

Verify the toy scenario and recoverable checkpoint before mutation. Preserve unsaved
toy work before restarting. Keep simulation paused except for justified bounded
tests, then restore pause. Discover fresh entity IDs after load, poll boundedly,
and independently inspect permanent geometry and directed connections after Apply.

Construct missing fixtures or adapt disposable toy copies (e.g. delete segments
for Connect endpoints). Inspect the resulting layout and record edits/prefabs/baseline.
Budget 45 minutes per distinct missing fixture and two hours total setup/infrastructure
troubleshooting per stage. This is not a feature-debugging/test-execution time limit.
When exhausted, preserve evidence, provide a manual recipe, and continue offline work.

Missing live evidence permits draft PRs with explicit limitations. Known failed
prerequisites must be fixed or marked blocked; never weaken checks to proceed.
Continue independent work when a dependency is blocked.

## Required implementation document: every stage

Every stage must create or update a durable feature and review document, linked
from its draft PR. Plans and session notes are not substitutes. Include:

1. **Features and usage:** delivered behavior, defaults, controls, supported scenarios.
2. **Architecture:** responsibilities, data flow, interfaces, design decisions.
3. **Testing:** what actually ran, reproduction, outcomes, and separate evidence categories.
4. **Limitations:** unsupported cases, known issues, missing evidence, deferred decisions.
5. **Dan's review:** exact stage build/configuration/flags, baseline/result save
   identities, location/selection/settings, numbered actions, expected behavior,
   accepted imperfections, and remaining visual/UI/vehicle checks.

Documentation-only stages use this structure for findings/recommendations where
applicable. Keep handoff docs current as implementation changes.
Commit incremental session notes including failed experiments and lessons.
Keep compact fixtures/scripts/manifests/summaries in Git; use the existing archive
mechanism for bulk captures and retain proprietary inputs outside source control.

## Final handoff

Provide a six-stage review index with PR URLs/statuses, commits, implemented behavior,
deferred findings, tests actually run, failures, and missing evidence. Preserve each
stage's rebuild/deploy instructions and baseline/result checkpoints; testing only
the final build does not validate earlier PRs individually.

Give Dan numbered checklists in stack order. When live setup failed, provide a real
construction recipe rather than an invented save name. Report final deployed identity,
loaded save, pause state, checkpoints, and unsaved changes. Leave the game paused.
