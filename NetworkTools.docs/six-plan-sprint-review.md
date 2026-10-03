# Six-plan sprint: draft stack and individual review

Baseline: merged main `f36d669`, plus planning `eace043`. Worktree: `nt-six-plan-sprint`. All PRs remain draft. **This is not six completed player features:** NT-002 is a guarded prototype with a failed sloped-native prerequisite; NT-022 delivers tested scaffolding and a diagnosed blocker, not a lane-alignment UI.

| Order | Plan / PR | Delivered and current status | Durable implementation / Dan's review |
| --- | --- | --- | --- |
| 1 | NT-001 [#17](https://github.com/danwayneharris/CS2-NetworkTools/pull/17) | Build identity, package manifests, trustworthy offline aggregate. Implemented; human UI/Release qualification pending. | [Development baseline](development-build-identity.md) |
| 2 | NT-021 [#18](https://github.com/danwayneharris/CS2-NetworkTools/pull/18) | Debug Combined junction-height permission and per-operation limit. Rail bound/disabled cases pass; existing road-lane failure retained. | [Junction elevation limits](junction-elevation-limits.md) |
| 3 | NT-023 [#19](https://github.com/danwayneharris/CS2-NetworkTools/pull/19) | Focused architecture prerequisite: immutable Connect candidate and shared Apply gate. Native smoke passes. | [Architecture review](architecture-review-nt023.md) |
| 4 | NT-002 [#20](https://github.com/danwayneharris/CS2-NetworkTools/pull/20) | Debug Connect elevation profile and exact native coverage guard. Equal-height Simple/Complex pass; different-height native mismatch rejects. **Partial.** | [Connect elevation profile](connect-elevation-profile.md) |
| 5 | NT-003 [#21](https://github.com/danwayneharris/CS2-NetworkTools/pull/21) | Explicit approach lanes/groups, endpoint directions, native selected-lane endpoint proofs. Simple/reversed Complex two-way road pass; highway/group matrix incomplete. | [Lane direction](connect-lane-direction.md) |
| 6 | NT-022 [#22](https://github.com/danwayneharris/CS2-NetworkTools/pull/22) | Pure lane-pair translation/plane tools and preview-only diagnostic. Native offset moves fixed nodes ~0.75m. **Blocked player feature; useful partial draft.** | [Lane alignment](connect-lane-alignment.md) |

Each PR targets its predecessor; #17 targets main. Bridge source is unchanged. The original NetworkTools checkout and its unrelated user edits were not used as the implementation worktree.

## Exact review packages

[Machine-readable revisions and DLL/UI hashes](session-notes/six-plan-review-manifests.json) identify each package; full manifests remain alongside the packages. These are local review artifacts, not published playable releases.

| Stage | Tested clean Debug source | Local package under `artifacts/review/` | Result/checkpoint |
| --- | --- | --- | --- |
| NT-001 | `00c05a5b5f38` | `NT-001/Debug` | No new geometry save for housekeeping |
| NT-021 | `7eebfc49830a` | `NT-021/NetworkTools` | `CitiesIIAgentBridge-review-nt021-fixed-height-20261003-134806-ac400174` |
| NT-023 | `b35766a` | `NT-023/NetworkTools` | `CitiesIIAgentBridge-review-nt023-road-connect-20261003-140428-816b028b` |
| NT-002 | `36112148cf97` | `NT-002/NetworkTools` | Simple: `CitiesIIAgentBridge-regression-before-reload-20261003-143533-4d66a61a`; Complex: `CitiesIIAgentBridge-regression-before-reload-20261003-143916-1054d6f2` |
| NT-003 | `deb29941bb2f` | `NT-003/NetworkTools` | Simple: `CitiesIIAgentBridge-regression-before-reload-20261003-151917-abe983f9`; reversed Complex: `CitiesIIAgentBridge-regression-before-reload-20261003-152712-05f91f6f` |
| NT-022 | `fbd91c38cdaa` | `NT-022/NetworkTools` | Unchanged baseline checkpoint: `CitiesIIAgentBridge-nt022-fixed-anchor-probe-20261003-153148-edb0e810`; no aligned result save exists |

Most native cases start from **`bridge test - terrain and elevation v1.1`**. Fixture file `scripts/fixtures/toy-terrain-v11.json` verifies save SHA-256 `6EF6B6E1DF56A8E834EC00313F478F64F47202507A857AEBFC65E87F62220301` and discovers fresh entities from coordinates. NT-021 also used `bridge test - trumpet combined baseline`; its fixture and failed/control evidence are in that stage's guide. Never overwrite those baselines.

For exact earlier-stage review, use a clean worktree at the recorded source revision, initialize the pinned submodule, close CS2, and run `scripts/bootstrap.ps1 -Build` (deploys). `-Build -PackageOnly` compiles/postprocesses/packages without deployment. `-OfflineTest` invokes actual offline suites without contacting the game. Later documentation commits do not change the recorded binaries. Do not infer earlier-stage behavior from only the final deployed build.

Native runs used the explicitly selected `--nt-experimental-finish-height-preparation` flag; the features do not enable it themselves. `scripts/launch-toy-fixture.ps1 -Fixture scripts/fixtures/toy-terrain-v11.json -ExperimentalFinishHeightPreparation` visibly launches a verified baseline; it neither closes another game nor proves readiness. The extra `-ConnectFixedAnchorProbe` is **only** for the NT-022 preview experiment and disables all Connect Apply paths. Restart without it for normal use.

## Tests and important failures

- Offline aggregation progressed from seven suites to fourteen. Final geometry/direction/proof/alignment coverage includes 42 direction, 402 connection-proof, 664 alignment and 228 plane-intersection assertions. Full Debug C#, UI, postprocessing and deployment passed. Exact reports and stage counts live in each guide; no new broad Release/Burst or human UI qualification is claimed.
- NT-021 active 0.5m rail limit, disabled height movement and native preview/Apply passed. Repeating the limited operation grants a fresh 0.5m budget; **this is per-operation, not cumulative/idempotent**. The full trumpet changed one directed road-lane target in both bounded and legacy Unlimited controls. That remains an unresolved connection limitation, not a tolerated positional error.
- NT-023 corrected idle null-input revision churn exposed by the first native attempt. Its Simple Connect gate then passed independent preservation and preview/permanent checks.
- NT-002 different-height native course rebuilding changes the intended vertical cubic. The guard rejects rather than applying a different result. A folded horizontal candidate also rejects its unsupported mapping. Equal-height Simple/Complex and legacy-off controls passed; this is not a finished terrain-aware Connect tool.
- NT-003 native shared node-owned lane ports initially rejected. Source and captures established skipped-junction rewriting; corrected identity/proof handling passed independent permanent checks. Neither endpoint reachability nor counts establish exclusive routing or whole-route vehicle use.
- NT-022 offsetting only the new curve endpoint cannot be declared fixed-node alignment: native NodeAlign moved the two nodes ~0.75m for a 1.5m offset. Apply remained blocked, permanent geometry unchanged. No fake alignment checkbox, neighboring-road edit or forced native Standalone semantics was added.

Position differences up to 5cm are accepted where that policy applies. Broken lane identity/topology, stale evidence and accumulating material drift are not covered by that tolerance. Vehicle traversal and human visual review remain pending throughout.

## Dan's review, in order

1. **NT-001:** compare About/UI, log and provider build identity with the manifest; try non-deploying package/offline commands.
2. **NT-021:** rail-high-branch, Combined, strength1, disabled versus 0.5m movement allowance. Inspect connected branches and decide whether renewable per-operation limits match your intended UX. Review the documented trumpet lane failure separately.
3. **NT-023:** legacy Simple Connect between hill-road end and crest/dip-road start. Inspect preview/Apply and existing approaches; change/reselect before Apply to exercise freshness.
4. **NT-002:** repeat equal-height Simple/Complex profile cases; check that the documented different-height case visibly explains rejection. Do not expect the sloped prototype to be generally usable yet.
5. **NT-003:** inspect arrows and choices on the Small Road result, then create an added-lane highway on/offramp fixture. Compare legacy perpendicular behavior with explicit outer-lane departure; test contiguous groups, reversal, markings and actual vehicle use. This highway/group coverage is not already certified.
6. **NT-022:** review the limitation and diagnostics, not a nonexistent finished feature. Optionally reproduce the explicitly flagged preview-only probe. Decide the next investigation: a different new-road transition/composition strategy that keeps nodes fixed, or a separately scoped policy allowing junction movement. No such policy was silently selected.

A suitable missing highway fixture is a disposable copy of the toy baseline with a straight 2-lane one-way approach, a 3-lane one-way approach and a gap between them; label their travel directions. Retain enough length for junction cuts, then add a branch to create the degree-two/added-lane departure comparison. Save a unique baseline before using Connect. For eventual NT-022, test each explicit left/right pair and reverse direction; selected new-road correspondence must also be specified before accepting a result.

## Follow-up workflow

Review these drafts one by one. If an earlier stage needs a fix, preserve its old tip and review package, fix that branch, then authorize/rebase descendants, rerun affected tests, and refresh package/save provenance. No PR was merged and no published history was rewritten during this sprint.

## Final session

Final deployed source: clean Debug `fbd91c38cdaa` (later edits are documentation/reusable scripts). The game was restarted **without** the NT-022 probe flag, with the terrain finishing flag. The terrain v1.1 baseline is loaded and paused; default legacy Simple Connect has an accepted preview between the two road examples, **not applied**. No unsaved permanent network edits remain. Latest normal-mode checkpoint: `CitiesIIAgentBridge-provider-connect-20261003-153730-652fef0a`. Exact final status and fingerprint are recorded alongside the compact review manifest.

[Final offline outcomes](session-notes/six-plan-final-offline.json), [final paused session](session-notes/six-plan-final-session.json), and [local raw-evidence archive](diagnostic-archives.md#six-plan-sprint-october-3-2026-local-archive) retain the handoff evidence.
