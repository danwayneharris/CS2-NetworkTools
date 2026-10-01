# Offline and automated in-game test coverage audit

Audit date: **2026-10-01**, approximately 03:30–03:43 America/Los_Angeles.

This is a source-and-evidence audit, **not a new test run**. No builds, restores,
test executables, game queries, deployments, Git mutations, or save operations
were performed. This report is the only file written. The explicit read-only
request overrides the repositories' usual session-note/commit workflow.

## Findings at a glance

- **Offline coverage is substantial but concentrated.** Network Tools tests its
  extracted geometry and diagnostic oracles; newer worktrees add production Slope
  regressions. Bridge tests cover transport recovery, policy, data handling,
  provider contracts, and installed-assembly compatibility. Neither repository has
  a measured line/branch coverage percentage available in the inspected evidence.
- **Real automated in-game evidence exists.** The newer NT provider suite records
  **13 cases: 11 strict passes and 2 failures**. A separate terrain suite records
  **7 cases: 6 passes and 1 failure**. These are historical executions, not current
  HEAD certification or independent coverage percentages.
- **The main NT checkout is behind its newer worktrees.** Its runner still uses
  removed bridge `nt_*` commands. Pairing that runner with the current bridge is
  incompatible. The provider migration and newer evidence live in other NT
  worktrees; this audit identifies them rather than conflating revisions.
- **The largest remaining gaps are native behavior beyond local geometry:**
  vehicle traversal, rendered/terrain behavior, broad assets and compatibility,
  UI interactions, and current Release/Burst execution. Some smoke-test fields
  are recorded without being enforced as assertions.

## Scope and revision identity

| Checkout | Branch | Audited HEAD |
|---|---|---|
| `CS2-NetworkTools` — report destination | `dan/autonomous-regressions` | `f3998ae6d26e4a6cf2106c95ec3e19c3370b9386` |
| `cities2-agent-bridge-ndc` | `dan/debug-plugin-setup` | `e79792077318c8ffef662b2ee8497b00488e1439` |
| `nt-history-review` — newer NT implementation/evidence | `dan/terrain-regressions` | `03e956afbabe43165e5f7be185a28aca20bd1563` |
| `nt-terrain-profile` — active profile work | `dan/terrain-profile-sprint` | `cf9fab05e8d4c2cc2feb22319c901197a0c88342` |
| `bridge-terrain-profile` | `dan/terrain-observation-sprint` | `99f2de077206f2f645c5fbe09cdcdd749d25530a` |

The main NT checkout already had a modified UI lockfile and untracked notes/log/cache
files; all were preserved. The main bridge and `nt-history-review` were clean at
inspection. The active profile worktree changed during the audit: vertical-profile
tests, its plan and sequence runner acquired edits, and a fixture was untracked.
Profile coverage below is pinned to **committed `cf9fab0`**, with later edits excluded
from pass claims. Other historical/research worktrees were located but not audited
as separate current implementations. No claim is made about the currently loaded DLL.

Links into sibling worktrees are local evidence links. They may move later; the
hashes above identify the corresponding committed source. Raw local artifacts are
explicitly distinguished from tracked reports. No remote archives were downloaded.

## 1. Offline coverage

### Network Tools

| Layer / entry point | What is actually exercised | Limit / evidence status |
|---|---|---|
| [Geometry executable](../NetworkTools.Geometry.Tests/NetworkTools.Geometry.Tests.csproj) | Links actual production geometry source into a .NET 8 executable. Analytic fairing solution, stationarity, pins, reversal/translation/rotation, nonuniform spacing, 257-node input, invalid values; cubic handles/subdivision and a captured handle-jump regression. | Runs via `dotnet run`, not ordinary test discovery. No ECS, native allocation, job scheduling, terrain, or Burst execution. Some fairing coverage protects an older helper, not the active Smooth Curve path. |
| [Failure](../NetworkTools.Geometry.Tests/FailureTests.cs), [boundary rotation](../NetworkTools.Geometry.Tests/BoundaryRotationTests.cs), [split](../NetworkTools.Geometry.Tests/SplitTargetTests.cs), [junction](../NetworkTools.Geometry.Tests/JunctionTargetTests.cs) tests | Rejection reasons/indices; endpoint/tangent/zero-strength behavior; split endpoint offsets, pinning, G1 continuity and strength sweep; junction ports, reversal, bounded handle scale/rotation, ordinary splits alongside junctions, multiple junctions, invalid data. | Meaningful numerical invariants, but do not execute `CurveSmoothTransform`'s complete native integration or validate native lane reconstruction. Junction-as-split is covered as rejection, not supported behavior. |
| [TargetReplay](../NetworkTools.Geometry.Tests/TargetReplay.cs:28) | Optional captured two-edge fit: production-versus-independent reconstruction, reversed traversal, strength sweep, gap closure and float-conversion error. | **No-argument execution skips the captured fixture checks.** They require input and output paths. |
| [TraceReplay](../NetworkTools.Geometry.Tests/TraceReplay.cs:16) | Replays recorded inputs through `PlanarPathTarget` and compares acceptance booleans. | Explicit `--trace-log` mode; does not compare all output geometry or replay native validation. [Program](../NetworkTools.Geometry.Tests/Program.cs:23) returns after this mode, so it does not also run the default suite. |
| Python diagnostics and runner tests | **12 scripts / 49 declared `test_*` methods** in this checkout; breakdown below. | Counted statically, not executed in this audit. Several tests validate Python diagnostic models or saved captures rather than production C# behavior. |
| [Original-input C# comparison](../scripts/test-original-input-comparison.ps1) | Compiles the actual shared helper; 11 explicit checks for identity/value/order/count differences, missing data and stale revisions. | Synthetic boxed values, not a running Unity world. Separate PowerShell entry point. |
| [Legacy NUnit project](../NetworkTools.Tests/NetworkTools.Tests.csproj) | Packages and game references are configured. | **No test source files found.** [bootstrap.ps1:108](../scripts/bootstrap.ps1:108) builds/deploys the solution and then invokes this project; it does not run the geometry executable or Python suites. A successful invocation is not evidence those suites ran. |
| UI, code generator, other tools | UI build/type checking can catch syntax/type failures. | No dedicated UI behavior, code-generator, or broad Add Node/Remove Node/Parallel test suite found in the audited checkout. Builds are not behavior tests. |

Python inventory (main NT checkout):

| Test script | Declared methods | Principal checks |
|---|---:|---|
| `test-junction-angle-projection.py` | 7 | Joint angle constraints, infeasible/unselected failures, wraparound, valid candidate preservation, capture, bad data |
| `test-junction-loss.py` | 2 | Captured direction eligibility and arm-order independence |
| `test-lane-connectivity.py` | 5 | Connector/direct-join equivalence; broken joins, physical lane swaps, equal-count swaps and lost U-turns |
| `test-live-regression.py` | 6 | Reload-independent fingerprints, geometry mismatch, ambiguous nodes, incomplete snapshots, exact connection sets, stability signatures |
| `test-original-freshness.py` | 5 | Original-input mutation, metadata independence, incomplete/duplicate owners, delayed observations and city changes |
| `test-preview-freshness.py` | 5 | Same-entity changed geometry, revision/city/tool/completion gates, metadata ordering, invalid data, slider return |
| `test-rail-composition-analysis.py` | 3 | Recorded working/broken merge and detached-preview interpretation |
| `test-rail-composition-inputs.py` | 4 | Endpoint orientation, reversed/inverted lane mapping, fail-closed data, disconnected/two-way cases |
| `test-rail-junction-constraints.py` | 4 | Preserve versus repair, bad identity/nonfinite data, reserve cannot waive failure |
| `test-rail-junction.py` | 4 | Analytic curvature, 3D-distance/planar-direction distinction, degenerate tangent, captured hypothesis |
| `test-reload-toy-baseline.py` | 2 | Exact save identity/checksum and required archive metadata |
| `test-smoothing-controls.py` | 2 | Negative extrapolation counterexample and split-range edge coverage; research semantics |

The Sept. 29 [handoff](session-notes/2026-09-29-sprint-report.md) records geometry
and **31 targeted Python tests** passing. That historical subset must not be
presented as a fresh all-49-method pass.

### Newer NT worktree additions

At `nt-history-review` / `03e956a`:

- [JunctionSearchTests](../../nt-history-review/NetworkTools.Geometry.Tests/JunctionSearchTests.cs)
  exercise the actual candidate controller: 31 unique bounded candidates, warm
  start and fallback completeness, identity invalidation, zero strength, and
  three matching set observations with reset behavior. This improves controller
  coverage; it still does not establish a native rebuild-completion fence.
- Python coverage is **13 scripts / 54 methods**: two additional provider-routing/
  activation runner tests and three analytic terrain-metric tests. The
  [explicit runner](../../nt-history-review/scripts/run-offline-python-tests.py)
  executes hyphen-named scripts and fails on no scripts or a failed script.
  The [archive audit](../../nt-history-review/NetworkTools.docs/session-notes/2026-10-01-0305-historical-artifact-audit.md)
  records all 13 passing from an export containing only tracked files. Ordinary
  unittest discovery had found zero tests; that false-success trap was identified.
- [Slope suite](../../nt-history-review/NetworkTools.Slope.Tests/Program.cs) has
  **8 formula fixtures / 96 assertions**: flat/hillside/ridge/valley, both traversal
  directions, physical endpoint grades, handle ratios, endpoint-to-node offsets,
  tangent preservation and unchanged XZ. It calls compiled production code.
  [test-slope.ps1](../../nt-history-review/scripts/test-slope.ps1) first compiles
  the mod without deploying, then tests its `obj/Debug/net48` assembly. This is an
  important stale-binary safeguard, but still writes build output.
- [CapturedSlopeReplay](../../nt-history-review/NetworkTools.Slope.Tests/CapturedSlopeReplay.cs)
  adds an explicit replay mode comparing a bounded managed reconstruction against
  captured permanent controls at 1 mm. Its node averaging is modeled; it does not
  run the complete ECS pipeline. Default fixtures primarily exercise Ease, not
  comprehensive Linear/Arch mode coverage.

At committed `nt-terrain-profile` / `cf9fab0`,
[VerticalProfileTests](../../nt-terrain-profile/NetworkTools.Geometry.Tests/VerticalProfileTests.cs)
add analytic unequal-length/offset cases, translated elevations, positive/zero/
negative rise, four boundary-smoothing combinations, reverse heights with smoothing
off, analytic horizontal lengths and invalid-input rejection. The
[integration record](../../nt-terrain-profile/NetworkTools.docs/session-notes/2026-10-01-0331-constant-slope-integration.md)
reports **338 profile assertions** and the existing 96 Slope assertions passing.
These test the pure fitter; native adapter coverage comes from the live cases below.
Additional test edits observed during this audit are not included in that total.

### Bridge

| Suite | Coverage and execution boundary | Retained evidence |
|---|---|---|
| [MailboxTests](../../cities2-agent-bridge-ndc/tests/MailboxTests.csproj) | Links production mailbox/tick/simulation/geometry/pagination/district/terrain/placement helpers. Synthetic mailboxes, real file operations and Windows locks; fake command dispatch, no ECS world. | [Sept. 30 log](../../cities2-agent-bridge-ndc/docs/session-notes/2026-09-30-mailbox-tests.log): **184 checks**: mailbox 11, simulation/geometry 22, pagination 12, district census/assignment 21, district drawing 36, terrain 25, placement 24, recovery 23, real PowerShell client 10. |
| Transport recovery | Atomic publication, request/response/cleanup locks, bounded retries, STOP/deadline handling while locked, no mutation replay, recovery without re-enabling controls, unknown-outcome timeout correlation. | Strong offline fault-injection coverage in [RecoveryTests](../../cities2-agent-bridge-ndc/tests/RecoveryTests.cs) and [MailboxClientTests](../../cities2-agent-bridge-ndc/tests/MailboxClientTests.cs); these are **not live-game fault tests**. |
| [Provider registry](../../cities2-agent-bridge-ndc/tests/Providers/Program.cs) | Two providers, discovery idempotence, duplicate identities, invalid descriptors, revision/argument guards, read/write policy before dispatch. Actual production registry. | **13 checks** recorded passing in [hardening notes](../../cities2-agent-bridge-ndc/docs/session-notes/2026-09-30-0200-automation-hardening.md). |
| [Python/MCP adapter](../../cities2-agent-bridge-ndc/adapter/test_adapter.py) | **18 methods**: real client against fake mailbox; durable intent recovery/conflicts, uncertain unpublished intent, identity/size/schema/nonfinite guards, unavailable game, dynamic tools, cancellation/serialization, failed rediscovery, local-only schema references. | Hardening notes record 18 passing. [stdio_smoke.py](../../cities2-agent-bridge-ndc/adapter/stdio_smoke.py) also exercises actual SDK stdio against a synthetic provider. Neither requires a game. |
| [Independent example](../../cities2-agent-bridge-ndc/tests/Example/Program.cs) | Actual example provider discovery, unloaded/disposed rejection, read-only greeting and invalid input. | [Example note](../../cities2-agent-bridge-ndc/docs/session-notes/2026-09-30-1821-provider-example.md) records a pass; loaded state is modeled, not native mod loading. |
| [Atlas/export](../../cities2-agent-bridge-ndc/tests/atlas.test.mjs) | **15 Node tests**: pagination, mixed/stale/partial snapshots, duplicates, missing data, capacity accounting, SVG/CSV escaping, overwrite refusal, real PowerShell exporter against fake mailbox. | [VALIDATION.txt](../../cities2-agent-bridge-ndc/VALIDATION.txt) records 15 passing for the older release checkpoint. |
| PowerShell client/journal/package/save tests | Client success/rejection/error/deadline; map export safety; journal filtering/deduplication/escaping; installer check-only/backup/running-game/hash/tampering guards in isolated directories; save metadata identity malformed/missing/ambiguous/oversized rejection. | Tests exist separately under [tests](../../cities2-agent-bridge-ndc/tests). Older validation records client/journal/package checks; no new all-script run was performed or inferred. |
| [Map browser smoke](../../cities2-agent-bridge-ndc/tests/map-viewer.cjs) | Playwright synthetic map loading, details, fit/layers, overlay/export and browser-error checks. | A browser test **outside CS2**, not in-game UI coverage. Older validation explicitly says it was not rerun for that release. |
| [Junction resolver](../../cities2-agent-bridge-ndc/tests/PreviewJunctionResolverTests.ps1) | Compiles actual pure resolver; recorded replacement-node identity, missing/duplicate/ambiguous/incomplete/version/reversal cases. | Default capture path depends on the sibling NT checkout. Tests topology resolution, not freshness or lane rebuild completion. |
| Native API/IL checks | [verify-api.ps1](../../cities2-agent-bridge-ndc/verify-api.ps1), Junction/District/Terrain API scripts inspect installed game and compiled bridge assemblies: members/signatures, expected calls, snapshot read-operation allowlist and dispatch guards. | Useful version/contract checks, **not in-game integration tests**. Generic-provider notes report a 56-command inventory; older documents' 52/61 counts describe earlier revisions. Command inventory is not behavioral command coverage. |

Mailbox tests default to .NET 10, with an opt-in framework override. The retained
Sept. 30 run used a .NET 8 build followed by explicit DLL execution and a shell
override. It should not be described as evidence that every documented launch
command works on every SDK. No automated CI workflow was found in the inspected
current trees; suites are fragmented across console, Python, PowerShell, Node and
browser entry points. No aggregate collector or enforced coverage threshold was found.

## 2. Automated in-game coverage

### What the strict Smooth Curve runner proves

[live-regression.py](../scripts/live-regression.py:144) requires a paused,
control-enabled empty toy city; validates a save package/hash and local geometry;
resolves fresh entity identities; creates and verifies a checkpoint; sweeps
strength; independently captures native preview curves; checks revision/submission
identity; applies once; and inspects permanent output.

Its assertions include node/edge identities, topology/prefabs, node elevations,
fixed-node positions, unselected edge controls, selected preview/permanent controls,
split positions/G1 tangents, physical lane composition mappings, and exact directed
lane-transition sets. This is considerably stronger than matching connection counts.
It retains deliberate negative tests for lane swaps and direct-join loss offline.

Numerical thresholds include **1 mm** for fixed-node/elevation/curve checks.
Permanent output requires three matching observations without observed
Updated/Created owners. That is stability evidence, not a native completion fence.
Coverage is bounded to the queried region. Strength sweeps precede a final Apply;
they are not separate Apply tests at every strength unless explicitly listed.

The main checkout uses flat `nt_*` calls at [lines 125–218](../scripts/live-regression.py:125).
Current bridge dispatch exposes `list_providers` / `invoke_provider`
([Mod.cs:173](../../cities2-agent-bridge-ndc/src/Mod.cs:173)). The newer NT
[routing layer](../../nt-history-review/scripts/live-regression.py:80) supplies the
migration. **Use compatible revisions before attempting any future run.**

### Retained strict results: generic-provider suite

The Sept. 30 suite's raw report booleans were inspected, not merely its prose summary.

| Case | Recorded strict result |
|---|---|
| Rail merge endpoint | PASS |
| Four-way road branch | PASS |
| Highway on-ramp / off-ramp | PASS / PASS |
| Rail one split | **FAIL: 3.008 mm fixed-node drift** |
| Rail two splits, full / half / zero strength | PASS / PASS / PASS |
| Road two splits | PASS |
| Interior rail merge | **FAIL: 5.174 mm fixed-node drift** |
| Interior road junction | PASS |
| Two interior highway merges | PASS |
| Dedicated slip lane | PASS; selected last two bypass edges through exit junction |

**13 cases, 11 passes, 2 failures.** Both failures retained the checked directed
connections and matching preview/Apply curves; that does not waive their failed
position invariant. Evidence: [suite record](../../nt-history-review/NetworkTools.docs/session-notes/2026-09-30-0200-provider-regressions.md),
[split/interior summary](../../nt-history-review/NetworkTools.docs/session-notes/captures/sprint-splits-interiors-20260930/summary.json),
[road/ramp reports](../../nt-history-review/NetworkTools.docs/session-notes/captures/sprint-road-ramps-20260930),
[final rail report](../../nt-history-review/NetworkTools.docs/session-notes/captures/sprint-final-rail-20260930/01-rail-merge/report.json).
The main NT checkout retains the earlier Sept. 29 version of this evidence.

### Terrain and newly exposed tools

| Scope | Recorded result | Boundary of the claim |
|---|---|---|
| Terrain v1.1 Smooth Curve, 7 selections | Hill road, crest/dip road, highway mainline, rail low branch and both ramps PASS. Rail high branch **FAIL: 23.518 mm fixed-center drift**. | Strengths 0/0.5/1, final full-strength Apply. Sampled grade/ground offsets are diagnostic, not terrain clearance or grade-policy assertions. |
| Ramp-out save/reload | Recorded exact regional geometry fingerprint and directed connection agreement at three junctions after reload. | One bounded persistence check; not universal save/reload coverage. |
| Slope Ease | Hill-road pass; ramp initially blocked before Apply by native endpoint mismatch, then passed after a production-code correction. | Current smoke asserts topology and selected preview/Apply agreement. Lane preservation/side-edge translation were additionally audited for the captured run, not all automatically gated by that version of the smoke. |
| Connect SimpleCurve, road and rail | Inherited expected prefab, native preview/permanent controls matched, stale revision rejected. Road added 2 edges/1 node; rail 4 edges/3 nodes. | Physical graph connectivity, not vehicle routing. Limited assets/mode; existing Anarchy setting was retained. |
| Constant Slope, active profile branch | Curve then Linear Slope, boundary smoothing off; forward/reverse run produced the same permanent regional fingerprint. | One seven-edge ramp/mainline selection. Native preview/permanent, lane mappings/transitions, horizontal positions and endpoints checked. Does not reproduce every archived ramp-only case or certify other ordering/layouts. |

Sources: [terrain results](../../nt-history-review/NetworkTools.docs/session-notes/2026-10-01-0000-terrain-regressions.md),
[terrain first reports](../../nt-history-review/NetworkTools.docs/session-notes/captures/terrain-v11-first-suite),
[terrain branch reports](../../nt-history-review/NetworkTools.docs/session-notes/captures/terrain-v11-branches-suite),
[Slope/Connect correction and results](../../nt-history-review/NetworkTools.docs/session-notes/2026-10-01-0820-selected-fixes-slope.md),
[Constant Slope native record](../../nt-terrain-profile/NetworkTools.docs/session-notes/2026-10-01-0338-profile-native-validation.md).
The Constant Slope [report](../../nt-terrain-profile/artifacts/profile-curve-then-linear/report.json)
and [reverse sequence summary](../../nt-terrain-profile/artifacts/profile-reverse-ramp/summary.json)
were available locally but are **ignored artifacts**, not committed fixtures.

### Bridge-specific live coverage

- The provider-driven NT cases exercise live provider discovery/dispatch,
  observation, checkpoint/polling and selected native network paths. They do not
  establish behavior for all 56 bridge commands or arbitrary third-party providers.
- Actual MCP stdio read-only consumer discovery/state is recorded in the
  [hardening handoff](../../cities2-agent-bridge-ndc/docs/session-notes/2026-09-30-0200-automation-hardening.md).
  The geometry mutation suite uses the generic provider transport; this is not
  evidence that all mutations were driven through MCP stdio end to end.
- The optional [debugger smoke client](../../cities2-agent-bridge-ndc/scripts/verify-debug-plugins.py)
  and [Oct. 1 record](../../cities2-agent-bridge-ndc/docs/session-notes/2026-10-01-0232-debug-plugin-runtime.md)
  establish live Gameface reachability and Unity type discovery at the main menu.
  They do not test tool UI interactions, gameplay, or breakpoints.
- Older bridge district/building/service observations and manual acceptance
  instructions are not counted as a repeatable automated live suite. In particular,
  [DEVELOPMENT.md](../../cities2-agent-bridge-ndc/DEVELOPMENT.md) still describes
  relocation-followed-by-create acceptance work that offline fixtures cannot prove.
- `bridge-terrain-profile` had only its new sprint note beyond the shared baseline;
  it adds no new bridge test coverage at the audited revision.

## 3. Concrete gaps and recommended next steps

| Priority | Finding | Focused improvement |
|---|---|---|
| High | Checkout/API mismatch and misleading legacy `-Test` entry point. | Document a compatible NT/bridge pair and add a single non-deploying offline entry point. Make it fail on zero tests and report each suite's actual execution. The newer Python runner is a useful start. |
| High | Connect smoke **records `unchangedExistingEdges` but never uses it in its final failure predicate**. [Source: lines 89–95](../../nt-history-review/scripts/exercise-tool-provider.py:89); still present in profile branch. | Assert preservation explicitly; add a negative harness test demonstrating a changed existing edge fails. |
| High | Connect's [curve comparison](../../nt-history-review/scripts/exercise-tool-provider.py:10) maps each new edge to its nearest preview curve without enforcing a one-to-one match or equal cardinality. | Assert exact preview/permanent coverage, so duplicate/missing/extra geometry cannot hide behind nearest-match success. This is a test-oracle weakness, not evidence of a current game defect. |
| High | Slope smoke in `03e956a` captures junction lanes and changed unselected edges but does not enforce their semantics. | Carry forward the stronger `cf9fab0` lane/incident-curve/position gates; assert the precise allowed side-branch translation rather than a blanket unchanged-outside rule. |
| High | Latest features are validated primarily in Debug. Historical Release compilation/build records do not certify current Burst/native runtime paths. | Run a deliberate compatible Release/Burst build and live matrix; distinguish compile, postprocess and execution outcomes. |
| Medium | Three retained strict drift failures remain. Other observations may have different drift magnitudes. | Preserve individual failures, establish native-center cause and accumulation/reload behavior, then decide a narrowly scoped policy. Do not silently relax all geometry tolerances. |
| Medium | No automated vehicle traversal/pathfinding/access-permission oracle found. Non-merging rail crossing fixture remains untested. | Add a usable crossing fixture and bounded traversal checks separate from lane-graph checks. Expand prefabs, reversed paths and mixed networks incrementally. |
| Medium | Async coverage is stronger in bridge transport than native tool lifecycle. No comprehensive ECS job disposal/race/rapid-edit/city-change stress suite found. | Inject stale revisions, changed original inputs, interrupted previews, selection churn and transitions in a disposable live suite; retain expected failure outcomes. |
| Medium | Add/Remove Node, Parallel, Arch and broader Connect configurations, editor controls and manual selection flows have little/no dedicated coverage found. | Add representative tool-specific invariants and UI automation; provider invocation does not exercise the UI path. |
| Medium | Terrain offsets and sampled grades are mostly diagnostics. | Define grade/clearance/terrain expectations before claiming terrain correctness; independently observe native generated geometry and rendering. Test Curve/Slope ordering and boundary options beyond the single profile example. |
| Medium | Provider schema composition and independent mod lifecycle coverage are incomplete. | Exercise closed `allOf`/reserved `_bridge` payload combinations, stale/changed catalogs and actual independent-provider load/dispose. Existing example only models its loaded flag. |
| Medium | Evidence is spread across commits, reports, archived raw captures and ignored artifacts. | Emit machine-readable manifests with code revisions, deployed DLL hashes, fixture hashes, mode, assertion version and result; distinguish replay from live runs. Keep required offline inputs in the tracked tree. |

The newer [diagnostic archive index](../../nt-history-review/NetworkTools.docs/diagnostic-archives.md)
explains why many historical raw files are now ignored/archived while reports and
required offline inputs remain tracked. Those archive verification claims were read,
not independently rerun here. Source counts, assertion counts, historic passes and
live scenario counts should remain separate: adding them together would not produce
a meaningful coverage percentage.

## 4. Documentation audit — October 1, 2026

### Scope and verdict

This follow-up audits **all 21 tracked Markdown guides under `NetworkTools.docs/`
outside `session-notes/`, plus `README.md` and `AGENTS.md`: 23 documents**. Session
notes and captures were used to establish chronology and verification evidence;
their writing, organization and completeness were **not audit targets**. BOOTSTRAP,
source, project files, scripts and bridge documentation were supporting references.
This audit report itself is not included in the document count.

Unlike the primary-checkout portion of the test audit, this section is explicitly
against **latest NT main at inspection**: remote `main` was `ed3ab81fa71a94a9474ac5eb4cebcf6acde6c20f`.
Its tracked tree exactly matches the clean `nt-history-review` worktree at
`03e956afbabe43165e5f7be185a28aca20bd1563`, whose files were read. Remote identity was
checked with `ls-remote`; nothing was fetched. Unmerged Constant Slope/profile work
is not treated as implemented on main. Bridge main was `8843041`; debugger setup
at `e797920` is identified separately where relevant. Inspection was approximately
04:07–04:09 America/Los_Angeles. Only this report was edited.

**Verdict: useful technical documentation with significant current-state drift.**
The explanations of ECS, geometry, build stages, observation versus validation,
and experimental limits are unusually helpful. The newer feature guides often
describe the code accurately. However, a newcomer cannot reliably determine the
current feature/test state by reading the entry points or a single guide. Older
“current” sections, newer addenda, completed proposals and session-specific
instructions coexist. Agents face a greater risk than humans: obsolete action
items and unconditional workflow instructions can be mistaken for work to perform.

This does **not** mean the dated research was wrong when written. Preserve its
evidence and reasoning; give it an explicit historical role and a link to the
current replacement. Current guides should state the final implemented behavior
first, without requiring readers to reconstruct several days of experiments.

### Highest-impact accuracy findings

1. **The geometry guide misstates the active dispatch.**
   [curve-geometry.md](../../nt-history-review/NetworkTools.docs/curve-geometry.md:3)
   describes the current integration as one `PlanarPathTarget` fit and says
   positive-strength interior junctions are rejected. Current
   [CurveSmoothTransform.cs:44](../../nt-history-review/NetworkTools.Mod/Systems/Tools/RoadShape/Transforms/CurveSmoothTransform.cs:44)
   selects `PlanarJunctionTarget`, `PlanarSplitTarget`, or `PlanarPathTarget`.
   Interior support is Debug-only; ordinary splits have their own semantics,
   including hard constraints at strength zero. The current algorithm description
   needs all three paths, with old fairing/single-target experiments below it.

2. **The freshness guide calls implemented mechanisms proposals.**
   [preview-freshness.md:22](../../nt-history-review/NetworkTools.docs/preview-freshness.md:22)
   labels its contract unimplemented, recommends a diagnostic-only next step and
   says interior rejection must remain. Main already captures revisions and original
   inputs, observes after the modification barrier, correlates submission/curves,
   and uses native observations for Debug automation/search. See
   [registration](../../nt-history-review/NetworkTools.Mod/NetworkToolsMod.cs:62),
   [submission capture](../../nt-history-review/NetworkTools.Mod/Systems/Tools/RoadShape/RoadShapeToolSystem.JobMethods.cs:39),
   and [probe acceptance](../../nt-history-review/NetworkTools.Mod/Systems/Tools/RoadShape/RoadShapeToolSystem.PreviewProbe.cs:76).
   **The warning about lacking a universal native completion fence remains valid.**
   Separate implemented token/input/curve checks from the unresolved completion proof;
   do not “fix” the prose by claiming the latter now exists. Its local-decompile
   setup statement also predates the documented full source setup.

3. **The automation guide still presents a resolved Slope failure as current.**
   [automation-provider.md:53](../../nt-history-review/NetworkTools.docs/automation-provider.md:53)
   says two off-ramp preview curves currently disagree and the limitation is
   unresolved. Main contains the handle-metric correction and endpoint-height
   alignment, and its retained
   [post-fix report](../../nt-history-review/NetworkTools.docs/session-notes/captures/selected-fixes-slope-after/report.json)
   records exact preview/Apply agreement. [Jobs.cs:109](../../nt-history-review/NetworkTools.Mod/Systems/Tools/RoadShape/RoadShapeToolSystem.Jobs.cs:109)
   applies the alignment. Update the verification section with the fixed case,
   its permitted side-branch movement, and road **and rail** Connect evidence.
   Keep the grade-dip/terrain limitations in the Slope plan; consistency with native
   preview is not proof of a good vertical profile. Provider 1.1.0, Debug-only
   exposure, command groups and revision requirements otherwise match source.

4. **Build/test guidance is incomplete at the main entry points.**
   README, AGENTS, build-system and architecture retain Sept. 26/29 baselines while
   newer suites and results are scattered elsewhere. `bootstrap.ps1 -Test` really
   does invoke the legacy project, but that project has no test source. Presenting
   it without the geometry/Python/Slope entry points is operationally misleading.
   List the actual commands, prerequisites and side effects, and distinguish
   historical Release/Burst build success from current Release compile-only and
   unverified runtime behavior. The Slope appendix in build-system is helpful but
   does not fill the missing overall test map.

5. **Research and deferred-work pages need explicit supersession.**
   `next-development-priorities.md` still proposes the provider/MCP migration and
   describes removed bridge-specific code; `autonomous-game-lifecycle-investigation.md`
   still calls the metadata extractor absent and controls unconditionally reset;
   `deferred-debugging-plugins.md` says nothing has been installed. Actual bridge
   [start-game.ps1:26](../../cities2-agent-bridge-ndc/start-game.ps1:26) uses the metadata
   helper, [Mod.cs:75](../../cities2-agent-bridge-ndc/src/Mod.cs:75) respects the optional
   RememberControl setting, and NT has a checkpointed reload/suite workflow.
   Optional debugger installation/connection was recorded on the bridge setup branch.
   Preserve these pages as dated investigations; link to current ownership and
   workflows. **The save-task-versus-durable-save caution remains relevant**:
   bridge `Workflow.cs:94` still consumes the task result, while runners add package
   verification. Do not mark every historical concern resolved wholesale.

6. **Several current-looking pages contain obsolete action or status text.**
   Rail rules and Smooth Curve plan still tell the reader to defer forking/pushing
   the bridge. Junction diagnostics says bridge installation/live queries are the
   next step. Terrain regressions states that the game is currently paused on a
   specific checkpoint. Those are historical session facts or past instructions,
   not durable repository state. Date them explicitly or move them into historical
   sections; current operational guidance must derive live state when needed.

### Per-document assessment

Ratings below concern usefulness as **current documentation**, not the quality or
validity of the original experiment. “Historical” is a useful document type, not a
failure rating. All filenames link to the audited latest-main tree.

| Document | State / usefulness | Recommended update |
|---|---|---|
| [README.md](../../nt-history-review/README.md) | Useful fork motivation and approachable voice; checkpoint is Sept. 29 and does not surface later provider, terrain or Slope/Connect work. Steps 1 and 4 read as future goals despite substantial implementation. | Keep the voice; add a concise current capability/limitation summary and a task-based docs index. Mark original roadmap items completed, partial or planned. Link current testing and automation guides. |
| [AGENTS.md](../../nt-history-review/AGENTS.md) | Strong safeguards for source grounding, submodules, user changes, deployment and preview/Apply evidence. Weak current test guidance; broad unconditional session-note/commit requirement lacks a read-only/concurrent-work exception. | Preserve the workflow intent, but explicitly scope writes/commits to authorized implementation work. Add worktree/branch ownership, exact-path staging guidance, current test entry points and task-specific reading links. Replace duplicated verification prose with one status reference. |
| [automation-provider.md](../../nt-history-review/NetworkTools.docs/automation-provider.md) | Largely accurate API contract with a materially stale Slope result. Good distinction between accepted Apply and verified output. | Correct the resolved off-ramp claim, add rail Connect verification, put the complete command-group overview near the top, and separate provider API version from artifact/commit identity. |
| [autonomous-game-lifecycle-investigation.md](../../nt-history-review/NetworkTools.docs/autonomous-game-lifecycle-investigation.md) | Valuable, well-grounded Sept. 28 investigation; no longer a current runbook. Some identified defects were fixed or worked around. | Add an “historical investigation; current workflow is …” banner and a resolution table. Keep package-vs-metadata and durable-save reasoning. Link the actual reload helper; avoid presenting candidate launch experiments as the preferred present workflow. |
| [build-system.md](../../nt-history-review/NetworkTools.docs/build-system.md) | Strong explanation of MSBuild, host/target runtimes, reference resolution, generation and deployment. Verification summary and test inventory lag; new Slope appendix is accurate. | Integrate all offline test commands and safe compile-only options into the main flow. Date configuration-specific evidence. Refresh source anchors rather than relying on old line offsets. |
| [curve-geometry.md](../../nt-history-review/NetworkTools.docs/curve-geometry.md) | Good mathematics and limitations, but the “current” algorithm omits split/junction dispatch and incorrectly rejects all interior junctions. | Rewrite current behavior around the three target paths; distinguish Debug/Release and planar versus vertical constraints. Retain fairing history under clearly historical headings. Document optional fixture replay separately from the default test command. |
| [deferred-debugging-plugins.md](../../nt-history-review/NetworkTools.docs/deferred-debugging-plugins.md) | Undated conversational recommendation with now-stale machine/setup assertions and no proper title. | Replace its entry section with a dated status and link to bridge-owned setup. Keep the suggested UI-plus-bridge experiment as pending test work, distinct from completed connection setup. |
| [diagnostic-archives.md](../../nt-history-review/NetworkTools.docs/diagnostic-archives.md) | One of the strongest operational guides: scope, checksums, recovery, archive identity, tracked-vs-historical distinction and deferred rewrite limits. The 114 retained captures match the current tracked tree. | Retain structure. Mark earlier 41-file terrain checkpoint as a stage preceding the 114-file aggregate; distinguish policy for future raw runs from historical paths. Remote archive contents were not reverified in this audit. |
| [interior-junctions.md](../../nt-history-review/NetworkTools.docs/interior-junctions.md) | Good current algorithm/search contract, warm-start caveat, exact native gate and honest strict failures, including the later ~29.88 mm case. | Add checked commit/date and a compact evidence matrix; fix literal `0.5?1.5` and `3?8` ranges. Specify scope/date of pending road/highway revalidation rather than a timeless pending statement. |
| [junction-diagnostics.md](../../nt-history-review/NetworkTools.docs/junction-diagnostics.md) | Helpful trace schema, units, bounds and replay limitations; stale bridge-review/“next session” framing obscures present capabilities. | Keep the trace/replay reference current; label upstream review historical and link current generic bridge queries/provider workflow plus archive recovery. |
| [live-regression-runner.md](../../nt-history-review/NetworkTools.docs/live-regression-runner.md) | Substantially accurate and useful; strong mutation safeguards and oracle boundaries. Old adapter terminology and early trial history precede the current generic transport/suite sections. | Lead with present prerequisites, transport and commands; summarize 13-case outcomes and link terrain results. Distinguish game-observational discovery from no-filesystem-write operations, and isolated runner setup from suite-managed reload. |
| [next-development-priorities.md](../../nt-history-review/NetworkTools.docs/next-development-priorities.md) | Clearly identified saved planning response, but its filename suggests a current backlog. First priorities and hardcoded integration description have been superseded. | Preserve the dated plan; add current item statuses and links to completed provider/MCP/performance work. Clearly separate still-open work from completed implementation and historical migration suggestions. |
| [offline-validation-confidence.md](../../nt-history-review/NetworkTools.docs/offline-validation-confidence.md) | Excellent claim-by-claim framing and strong Oct. 1 Slope/terrain addenda. Top table still reports one live search/Apply case and proposes extracting a controller that now has tests. | Reconcile the table with later evidence, add revision/configuration/scope columns, and retain failure rows. Replace completed opportunities with current gaps. Avoid a confidence percentage. |
| [preview-freshness.md](../../nt-history-review/NetworkTools.docs/preview-freshness.md) | Good explanation of ABA, identity versus revision, and incomplete completion evidence; materially stale implementation/proposal boundary. | Document the actual probe, tokens, original-input checks and Debug gates, then list what is still unproven. Link source symbols and original research separately. |
| [programmatic-mod-ui-control-investigation.md](../../nt-history-review/NetworkTools.docs/programmatic-mod-ui-control-investigation.md) | Strong dated analysis of trigger direction, binding versus application state, and UI-only limitations. Typed automation is now implemented; generic arbitrary-mod UI control still is not. | Add a supersession link to automation-provider and distinguish backend/provider testing from actual UI interaction testing. Preserve binding research without suggesting a new NT-specific bridge adapter. |
| [rail-junction-rules.md](../../nt-history-review/NetworkTools.docs/rail-junction-rules.md) | Useful source-specific derivation and careful limits; later schema-2 evidence explicitly supersedes an earlier hypothesis. Old “next evidence” and repository-management directions remain in the main flow. | Lead with established schema-2 findings and unresolved generalization; group proxy/default-limit history separately. Remove stale fork/push instructions from current guidance. |
| [slope-improvement-plan.md](../../nt-history-review/NetworkTools.docs/slope-improvement-plan.md) | Accurate as a main-branch plan: grade/station/offset problems and ordering counterexample remain relevant. Correctly distinguishes native consistency from good geometry. | Add an as-of revision and links to implemented Ease/endpoint fixes versus deferred profile redesign. Do **not** claim unmerged profile work is shipped on main. Keep terrain causation a hypothesis. |
| [smooth-curve-plan.md](../../nt-history-review/NetworkTools.docs/smooth-curve-plan.md) | Valuable design decisions and user clarifications, but top status coexists with “current” body text rejecting implemented junction support and saying acceptance tolerances remain unspecified. | Separate implemented baseline, unresolved product decisions and dated design history. Reference the actual runner's 1 mm assertions without presenting them as an agreed universal user-facing tolerance. Preserve unresolved side-bias/junction-as-split semantics. |
| [split-points.md](../../nt-history-review/NetworkTools.docs/split-points.md) | Clear human-facing usage and important zero-strength hard-constraint semantics; implementation matches source. Verification still names a development branch and old 61-command bridge catalog. | Keep the explanation; reference merged feature/source identity and generic provider contract. Mark the command-count check historical or remove it from present verification. Add direct test/evidence links. |
| [sprint-architecture-review.md](../../nt-history-review/NetworkTools.docs/sprint-architecture-review.md) | Properly dated bounded review; useful separation of geometry, adapter, bridge and runner. Its “no slope bug established” statement is true for that review, not the current repo. | Keep historical, add links to subsequently demonstrated/fixed Slope issues and the current improvement plan. Do not rewrite the old review as though it had observed later results. |
| [system-architecture.md](../../nt-history-review/NetworkTools.docs/system-architecture.md) | Best conceptual onboarding: ECS identities, partial classes, patterns, jobs and preview/Apply. New Slope/prefab/topology appendices are useful, but current responsibilities remain fragmented. | Integrate addenda into sections 3/6/8/9, add geometry/test/provider/probe/search areas to the repo map, and update open work. Include ordinary split dispatch in the main pipeline explanation. Repair source-line anchors. |
| [terrain-regressions.md](../../nt-history-review/NetworkTools.docs/terrain-regressions.md) | Strong bounded results: seven-case matrix, diagnostics versus assertions, grade/terrain limits and one-case persistence. | Retain evidence. Replace “the game is paused on…” with a dated historical handoff reference; link archives before raw paths and label raw files as recovered/local rather than guaranteed checkout content. |

### Helpfulness for humans and agents

**For humans:** preserve the concrete technical explanations. The architecture and
build guides attach names to useful systems concepts; geometry documents explain
G1 versus curvature/grade, pins versus endpoints, and why preview differs from
Apply. The split guide explains a surprising user-visible behavior rather than
burying it in implementation details. Terrain and confidence guides generally
state what an observation does and does not establish.

The main usability gap is navigation. README lists only a few early guides; there
is no complete task-oriented map for “build,” “run offline tests,” “run an authorized
live regression,” “understand a feature,” and “read historical research.” Dates
help, but repeated chronological addenda make readers do reconciliation work.
Some guides read like saved chat replies rather than durable references. A short
current-state section and explicit document type would improve them more than
adding another long overview.

**For agents:** AGENTS already provides good source precedence and repository-care
rules. Its mandatory opening reading, however, sends agents through several
stale summaries before they reach current test/automation docs. Conditional reading
by task would be more useful after a short common baseline. Explicitly identify
which file owns each fact: build commands in BOOTSTRAP/build-system; runtime behavior
in architecture/feature guides; automation schema in provider/source; current
verification in one evidence matrix; proposals in plans; chronology in session notes.

The session/commit instruction should acknowledge read-only audits and another
agent's active worktree. That is a recommendation to clarify scope, **not a proposal
to abandon the deliberate record-of-failed-experiments workflow**. Old research
instructions such as “do not open a junction PR yet” should not compete with current
repository policy or explicit user instructions.

### Links, reproducibility and presentation

- A path-level scan of Markdown links in all 23 targets found **no missing local
  targets and no in-repo file links relying solely on ignored files**. This check
  did not verify remote URLs, heading fragments, every line anchor, inline-code
  paths or downloaded archives. It is not a claim that every reference is current.
- Source line anchors have drifted. For example architecture's Apply link points
  to `RoadShapeToolSystem.Jobs.cs#L420`, now near composition handling;
  `OutputApply` starts at **line 470**. Prefer a named symbol plus a maintained
  source link; use immutable revision links for historical evidence.
- Inline historical capture paths can exist locally while being absent from a
  clean clone. The archive guide explains this well, but several consuming pages
  mention raw captures before explaining recovery. Link the archive index at the
  point of use and separate minimal tracked replay inputs from full raw evidence.
- Cross-repo links depend on checkout layout and revision. In supporting BOOTSTRAP,
  `../cities2-agent-bridge-ndc/docs/DEBUGGING-PLUGINS.md` exists on the inspected setup
  branch but **not in bridge main `8843041`**. Document that revision dependency or
  link an immutable setup revision until merged. A locally resolving link is not
  necessarily reproducible from two fresh main clones.
- Fix actual ambiguous typography such as the literal `?` range separators in
  interior-junctions. General mojibake seen in default Windows PowerShell output
  was not treated as a file defect without checking UTF-8. The important edits are
  factual structure and scope, not cosmetic rewriting of the maintainer's voice.

### Recommended update sequence

1. **Correct current behavioral claims:** geometry dispatch/interior support,
   freshness implementation, resolved off-ramp Slope mismatch, and debugger/lifecycle
   supersession. Preserve remaining limits and original experiment evidence.
2. **Make verification discoverable:** add a current offline/live command matrix
   and results index; explicitly explain the empty legacy `-Test` project and
   each command's build/deploy/game-write effects. Report compile, postprocess,
   Burst execution, automated checks and human observations separately.
3. **Refresh README and AGENTS:** a current capability summary, task-based reading
   map, read-only/concurrency scope, and links to authoritative status. Keep machine
   state and short-lived branch names out of durable instructions.
4. **Reconcile the living guides:** integrate the Oct. 1 architecture and confidence
   addenda into their main sections; remove duplicate contradictory “current” text.
   Do the same for the Smooth Curve plan's implemented-versus-proposed boundary.
5. **Label historical research:** give lifecycle/UI-control/priority/sprint-review
   pages a short status banner, audited revision and successor link. Historical
   findings should remain readable without becoming today's task list.
6. **Standardize evidence identity:** each current guide should state its reviewed
   source revision, applicable configuration, relevant game fingerprint/version,
   known failures and links to bounded results. “Current” or “passes” alone is not
   an artifact identity.
7. **Maintain references:** repair source anchors, clarify archived raw paths and
   cross-repo revision requirements, then add a lightweight link/reference check
   suitable for a clean checkout. Avoid replacing source evidence with duplicated
   prose that will drift again.

No recommended documentation edits above were applied. The only change for this
follow-up is this appended audit section.
