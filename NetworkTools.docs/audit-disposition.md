# Audit disposition ? October 1 correctness sprint

Working disposition, not a completed verification report. Baseline is terrain/profile
PR #13, `ee9c3bc`; the two source audits inspected older/different revisions. This
sprint uses `dan/audit-correctness-sprint`. Source and later evidence take precedence
over historical descriptions. Final build/native results and PR identity will be
added after integration; no live qualification of new fixes is claimed here.

Read the [test/documentation audit](test-coverage-audit-2026-10-01-nt-f3998ae6-bridge-e7979207.md)
first, then the [architecture audit](architecture-code-review-2026-10-01-main-ed3ab81-upstream-7d1f3d0.md).
Both are preserved unchanged. The dispositions below assess recommendations rather
than treating every historical TODO as mandatory implementation.

## Architecture findings

| Finding | Disposition at this checkpoint | Evidence / bounded action |
| --- | --- | --- |
| F01 Release junction envelope | In progress; native/Release qualification pending | `RoadShapeToolSystem.Update.cs` restricts Release CurveSmooth degree>2 nodes, including endpoints, because required native junction validators are Debug-only. Surface correction stays Debug-only. Do not equate Release C# compilation with Burst/native execution. |
| F02 transform-input freshness | In progress; live verification pending | `PathData.cs` captures a baseline with cached transform data; `JobMethods.cs` compares originals before fit and copies that baseline into submission. Changed preview inputs refresh; changed Apply inputs reject. No universal native completion fence is claimed. |
| F03 two-ended side-edge edits | Implemented; offline verification only so far | `bc164e6` composes both endpoint deltas into one affected-edge result for preview and Apply. Regression covers distinct two-ended deltas/reversed storage; live topology case remains necessary. |
| F04 arrival-dependent search | Implemented; offline verification | `1ef0440`: production-source graph tests cover incoming-edge-dependent cost, ties and exhaustive small graphs. Unity native-container execution and real-city performance are separate. |
| F05 suite exit aggregation | Implemented; offline verification | `0248c8a`: failed child assertions cannot produce a passing suite; incomplete/uncertain execution remains distinct and stops unsafe continuation. Native rerun pending. |
| F06 Connect oracle coverage | Implemented; offline verification | `0248c8a`: enforce existing-edge preservation and bijective preview/new-edge correspondence; negative cases cover corrupted/missing/duplicate/extra geometry. Does not establish that all native Connect configurations pass. |
| F07 global override ownership | Deferred compatibility design | `BaseToolSystem.cs` still restores global booleans unconditionally. A saved boolean or conditional restore cannot fully resolve concurrent-mod ownership/ABA. Keep documented limitation and plan focused NT handoff/cross-mod tests rather than hide a lifecycle redesign inside geometry cleanup. |
| F08 shared parameter domains | Finite-only guard implemented offline; UI/live validation pending; broad ranges deferred | `dad48b5` rejects nonfinite values through production parameter source; offline parameter tests cover this boundary. Handle drags pass geometry-derived values that may exceed slider metadata, so intentional range/enum policy remains unchanged pending caller review. Broad clamping must not be invented silently. |
| F09 shared Apply policy | In progress; live/UI verification pending | `CandidateAllowsApply` checks revision, original/cache/submission agreement and completed job; execution rechecks before mutation. Debug CurveSmooth/Slope require corresponding native evidence. Straighten does not gain a new native lane oracle merely by sharing the gate. |
| F10 preserve Node fields | Implemented; offline verification only so far | `bc164e6` changes position while retaining original rotation/other fields. Audit proved a field write problem, not universal visible corruption; native reconstruction effects still need observation. |
| F11 history-dependent candidate choice | Intentionally retained; disagree that canonicalization is automatically required | `JunctionCandidateSearch`, `JunctionSearchTests` and `interior-junctions.md` already document/test warm start as a hint, with revalidation. A canonical minimum-rotation objective changes product behavior and requires Dan's judgment. |
| F12 observer performance/status | Deferred broad consolidation; instrumentation retained | Work/allocation pattern is established, timing cost is not. Keep dependency completion and native observations. Measure gather/job/native wait/observer work separately before changing ownership, caching or synchronization. |
| F13 generator diagnostics | Implemented; standalone offline verification | `286080d` rejects unsupported/empty/duplicate metadata with source diagnostics and tests configuration-specific declarations, current Debug/Release output and malformed fixtures. The generator remains small; full mod/UI integration is pending. |
| F14 build/test entry points | Implemented focused offline entry point; stage separation partial | `e1fce2f` adds `run-offline-tests.py` and standalone `bootstrap -OfflineTest`; six fake-child tests and PowerShell parse passed; `100e44f` adds the parameter/codegen stages. Aggregate execution pending. Existing package/deploy coupling remains; no new package-only command is implied. |
| F15 presentation/source organization | Partly reconciled docs; cosmetic refactors deferred | Preserve personal fork tone and local marker. Split localization remains a useful follow-up. Protocol relocation, Point renaming and wholesale formatting are not correctness prerequisites. Diagnostic self-check removal requires equivalent evidence first. |

?Implemented? means code and the stated offline evidence exist, not release-ready.
Session notes retain failed attempts and test boundaries. No finding is closed by
merely compiling a project or recording an assertion without enforcing it.

## Coverage recommendations

| Audit recommendation | Disposition / remaining boundary |
| --- | --- |
| Compatible checkout/API pair and actual offline entry point | Generic provider migration already exists on this baseline; retain independent bridge/consumer protocol compatibility. New aggregate replaces guessing from the empty legacy NUnit project, while legacy `-Test` still deploys and now warns. |
| Connect preservation and unique coverage | Addressed by F06; final native results pending. |
| Slope incident-edge and lane assertions | Baseline profile runner already has stronger incident-translation checks than the old audited smoke path. Keep translation-permitted semantics; F03 extends correct two-ended composition. New live matrix pending. |
| Debug versus Release coverage | F01 restricts unsupported junction behavior; full Release/Burst qualification remains separate, never inferred from old logs. |
| Historical strict node-center drift | Preserve historical failures/measurements. Dan's current live position/displacement tolerance is 5 cm; no millimeter-only blocker is revived. Topology, lane identity/connectivity, stale evidence and accumulating material drift remain strict. |
| Vehicle traversal and non-merging rail crossings | Deferred: native lane sets do not prove actual routing/access or traffic movement. Need a verified crossing fixture and bounded traversal oracle/human review. |
| Lifecycle races, rapid edits, city changes | Focused original-input/revision regressions support F02/F09. Exhaustive native ECS disposal/race stress is not established and remains follow-up. |
| Add/Remove Node, Parallel, Arch, broad Connect and UI/editor coverage | Deferred broader tool/asset matrix. Provider calls do not stand in for ordinary selection/UI behavior. |
| Terrain/grade/clearance correctness | Continue diagnostics and captured ramp repeatability; perfect slopes explicitly lower priority. Terrain-following/obstacle routing and universal grade limits are not promised. |
| Bridge schema composition and independent-provider lifecycle | Deferred to independent bridge product sprint; no NetworkTools-specific bridge changes or release dependency introduced. |
| Machine-readable evidence identity | Offline aggregate records source revision/dirty state, commands, tool context, logs/statuses. Final native report must additionally record deployed hash, scenario/checkpoint/fixture identity and assertion policy. No coverage percentage is inferred from source/test counts. |
| Clean-checkout inputs and archive provenance | Keep compact essential fixtures tracked and bulk captures archived/ignored. New audit copies are tracked. Do not assume local raw paths or remote archive availability without checking. |

Offline analytical tolerances remain appropriate for exact mathematical contracts;
they are distinct from accepting up to 5 cm in live geometric displacement. The
5 cm policy is not an instruction to loosen every assertion.

## Documentation recommendations and ownership

| Document(s) | Disposition |
| --- | --- |
| README | Added current sprint status, task links and delivered-versus-proposed roadmap interpretation; preserved fork voice. |
| AGENTS | Added actual offline entry point, worktree/exact-path staging rules, read-only exception, tolerance and instrumentation policy. Historical verification retained as dated evidence. |
| BOOTSTRAP / build-system | Added command/side-effect matrix, actual suite scope, legacy deployment warning, compile-only versus package/deploy distinction and configuration limits. No fabricated package-only implementation. |
| system-architecture | Corrected three-way horizontal dispatch, dated old Release claims, and described evolving original-input/shared-Apply/affected-edge boundaries with pending native status. Keep source-specific profile appendices rather than a broad rewrite. |
| smooth-curve-plan | Added current implemented/proposed boundary; labelled single-target body historical and removed stale bridge-fork instructions from current policy. |
| curve-geometry / preview-freshness | Current source dispatch and evolving freshness corrections are stated in architecture and this disposition; dedicated-page reconciliation remains a targeted follow-up. Preserve original algorithm/ABA reasoning and lack of universal completion fence. |
| automation-provider / live-regression-runner | Provider migration exists; historical Slope mismatch was corrected by prior profile work. Dedicated prose should lead with generic transport and preserve accepted-request versus independent-native-result distinction. Final run/command recap pending. |
| offline-validation-confidence / interior-junctions / split-points | Retain existing bounded evidence and history-dependent search caveat. Final sprint evidence/configuration matrix pending; historical strict drift and old bridge-command counts must not certify new code. Minor range/localization prose cleanup deferred. |
| slope-improvement-plan / terrain-regressions | Profile/surface changes belong to this PR branch, not automatically merged main. Recorded paused-game/save facts are historical handoffs, not live state. Perfect grades and terrain causation/generalization stay unresolved. |
| autonomous-game-lifecycle-investigation / programmatic-mod-ui-control-investigation | Historical investigations: current generic provider/reload workflow supersedes launch experiments and NT-specific bridge proposals. Durable-save/package verification and UI-versus-provider distinction remain relevant. Dedicated banner updates deferred. |
| next-development-priorities / sprint-architecture-review | Historical planning/review, not today's backlog. Provider migration is already implemented; earlier ?no Slope bug established? is scoped to its date. Use current plan/disposition and source. |
| deferred-debugging-plugins | Historical recommendation; installation/connection belongs to bridge setup documentation. Do not reinstall from stale machine assumptions or claim UI behavior tests from reachability alone. |
| junction-diagnostics / rail-junction-rules | Retain trace/replay/native-rule references; old fork/push/?next session? instructions are historical. Current ownership is generic bridge plus NT-owned provider; later schema evidence supersedes earlier proxies. Dedicated banner updates deferred. |
| diagnostic-archives | Retain checksum/provenance workflow. Earlier 41-file checkpoint precedes later aggregate archive; no remote archive revalidation claimed. History rewrite remains a separate deferred task. |
| Source links / clean checkout | This sprint uses current symbol/file references where changed; comprehensive repository-wide link checking and all historical local-path repair remain deferred, not silently declared complete. |

These limited edits address entry-point ambiguity first. Dedicated historical pages
are not rewritten wholesale during correctness work; their remaining actions are
explicitly accounted for above. Build commands belong in BOOTSTRAP/build-system,
implemented behavior in architecture/features, API in provider/source, confidence
in evidence summaries, proposals in plans, chronology in session notes.

## Instrumentation retained and open verification

Retain original/candidate/native captures, submission and Apply identities,
rejection reasons, lane observations, replay/export hooks, provider access and
bounded timing. Debug-only does not mean disposable. Optional logging and expensive
capture can be gated independently; required safety validation cannot be gated off
with them. Record deliberate removal and verify consumers before replacing probes.
No native-container lifetime fence is removed for cosmetic simplification.

Pending final integration: aggregate execution, Debug build/native cases, Release
stage outcomes, ordinary UI/manual review, final deployed identity and game state.
Dan's visual checklist will be added after the actual candidate is known. Broader
bridge, traversal, asset compatibility, terrain routing and generalized combined
tool work remain outside this sprint.
