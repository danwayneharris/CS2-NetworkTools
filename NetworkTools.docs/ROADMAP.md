# NetworkTools roadmap

Status: **reviewed planning baseline**, October 3, 2026. Baseline: main `f36d669`
(PRs #13 through #16 merged). This is the authoritative index of current priorities;
the first six items have individual planning drafts linked below. Implementation has not
started, and the remaining order is not blanket execution authorization.

NetworkTools serves players editing and connecting networks. The Bridge is an
independently released developer/automation product, not a required NT assembly.
The companion roadmap is `docs/ROADMAP.md` in the
[Bridge repository](https://github.com/danwayneharris/cities2-agent-bridge-ndc).
Both roadmaps are local pending publication; add direct main-branch document
links after they are published.

## Current baseline: keep completed work off the backlog

- Smooth Curve, ordinary degree-two split points and interior-junction smoothing
  exist. Combined Curve + Constant Slope is a merged **Debug-only experiment**,
  not a future feature to implement from scratch.
- Combined mode allows interior-junction Y changes while keeping their XZ fixed;
  selected outer/split nodes remain XYZ anchors. Independent Curve retains its
  elevation-preserving contract. A separate user control for interior-junction
  elevation permission/displacement limit (NT-021) and horizontal junction movement
  (NT-005) are still future work.
- Native preview/Apply, repeatability and save/reload have bounded evidence.
  Dan considers current visuals good enough; exceptionally tight junctions with
  substantial elevation changes can still look janky. Do not chase perfect grade.
- The native finishing compatibility experiment is Debug-only, default-off and
  launch-opt-in. It affects native rebuilds including loading, pins installed
  binaries and is not a save-only cure. Its wider scope needs a deliberate policy.
- Generic provider automation and offline native geometry replay exist. The replay
  qualifies five captured cases/eight bounded stages, not a general game runtime,
  arbitrary terrain deformation, rendered appearance or traffic behavior.

Current evidence: [combined review](combined-smoothing-review.md),
[offline execution results](offline-game-execution-results.md), and
[audit dispositions](audit-disposition.md). Later evidence supersedes older
not-run statements; individual passing cases do not establish broad qualification.

## Recommended feature sequence

IDs stay stable when priorities change. Status is **proposed** unless stated otherwise.
The first six planning drafts are indexed in [plans/](plans/README.md); execution awaits the goal-mode instruction.

| Order / ID | Work package and benefit | Dependencies and decisions before execution |
| --- | --- | --- |
| 1 / [NT-001](plans/NT-001-development-baseline.md) | Consolidate the milestone: reconcile current docs, establish this roadmap, add usable development-build identity and retain a compact baseline regression suite. | Roadmap reviewed and recorded; remaining housekeeping is selected for detailed planning. Reuse existing suites. Link [versioning recommendations](versioning-recommendations.md); no per-commit release-number churn. |
| 2 / [NT-021](plans/NT-021-junction-elevation-limits.md) | User-facing "Allow interior junction elevation modification" option in combined smoothing, with a separate maximum-elevation-change slider independent of curve strength. The geometry solver chooses the best junction elevation within the allowed displacement from its original height; the player does not set a target elevation or elevation pin. | Treat the slider as a constraint on the complete profile solve, not a blend toward an unconstrained result or a post-fit clamp. Disabled/zero preserves original junction Y; a nonzero allowance bounds upward/downward movement from the original input height while connectivity and other constraints remain enforced. Refit the profile and incident branches coherently and verify preview/Apply and repeatability. Suggested UI: "Maximum junction elevation change" in meters. Decide range/default/persistence, upper-end behavior and interaction with explicit pins in the detailed plan. Limited movement may require a nonconstant profile or explicit rejection; exact constant grade is not guaranteed. |
| 3 / [NT-023](plans/NT-023-architecture-review.md) | Comprehensive code and architecture review before Connect expansion: separation of concerns, reusable geometry/validation, meaningful de-duplication, human-readable control flow and simpler future merges/rebases. | Review the current merged implementation and NT-021 if delivered; reconcile prior audits rather than repeat stale findings. Map ownership among pure geometry, native adaptation, tool orchestration, UI/provider APIs, diagnostics and test infrastructure. Identify concrete duplication, oversized responsibilities, hidden coupling and upstream conflict hotspots. Produce an evidence-backed disposition and staged refactor plan. Implement only separately scoped, regression-protected prerequisites before NT-002; do not make a wholesale cleanup a release gate. |
| 4 / [NT-002](plans/NT-002-connect-elevation-profile.md) | Bring geometry, elevation, junction-surface and terrain insights into ordinary Connect, producing one coherent horizontal/vertical candidate. | Start from verified current Connect behavior and discriminating fixtures. Share appropriate math, not mutation semantics; no general route planner yet. |
| 5 / [NT-003](plans/NT-003-connect-lane-direction.md) | Lane- and direction-aware Connect: investigate added lane-math lanes and departures that currently start perpendicular instead of following the intended off-ramp direction. | Pair a bounded repro with NT-002. Decide lane intent/selection UX and distinguish node tangent, lane tangent and generated lane connectors. Preserve directed lane identity, not counts alone. |
| 6 / [NT-022](plans/NT-022-connect-lane-alignment.md) | Lane alignment across unequal-width/prefab transitions, starting with Connect: align the continuing lanes rather than automatically centering both networks. A required user-facing control selects which lanes on the two networks should remain aligned; the tool must not silently choose their correspondence. Example: two one-way lanes meet three one-way lanes, with the player choosing the continuing pair and therefore which side gains the extra lane. | Investigate the reported centerline behavior in Connect and other tools before generalizing. This is lateral lane-position alignment, distinct from NT-003 departure direction. Specify the lane-selection control and preview feedback in the detailed plan, including how the selected lane correspondence is represented and retained as the candidate changes; use actual prefab lane positions, travel direction and widths rather than lane counts alone. Coordinate offsets and transition geometry without breaking directed connections or preview/Apply agreement. Extend to shaping/other tools only where evidence supports it; avoid silently changing every tool default. |
| 7 / NT-004 | Qualify supported behavior outside Debug: Release/Burst, validation parity and safe package-only builds. | Decide which features graduate individually. Separate the global finishing compatibility patch's home/activation/update policy from ordinary feature promotion. Bounded historical Release tests are not qualification of all later experiments. |
| 8 / NT-006 | Controlled changes outside the selection, with visible affected geometry and player permission/limits. | Define the minimum shared permission/affected-area contract before NT-005; stage broader neighboring edits later if needed. Distinguish node/curve/tangent/grade allowances from numerical error tolerance. NT-021 only needs the existing incident-branch translation scope and need not wait for this general feature. |
| 9 / NT-005 | Optional horizontal movement of interior junction nodes to improve the selected path. | Build on the NT-006 affected-area policy: displacement bounds, anchors and coordinated incident-branch edits. Preserve intended/forbidden connections. Horizontal relocation is a larger geometric search/validation change than NT-021 vertical permission. |
| 10 / NT-007 | Better pin/split and combined-tool controls: horizontal/elevation/grade pins; junctions as splits; Preserve/Smooth/Straighten and vertical-profile choices. | Decide incompatible pin-grade transitions and junction relative-angle semantics. A junction split is not permission to force a shared tangent or break a connection. Reuse [split points](split-points.md) and [combined options](combined-smoothing-options.md). |
| 11 / NT-008 | Centered signed slider: 0.0 in the middle; positive side retains current smoothing, negative side offers a loopier/other-side alternative. | Product intent is recorded; the geometric rule is not chosen. Longer and opposite-side are not equivalent. Decide reversal/side orientation, deviation bounds and interaction with pins/junctions before implementation. See [side-bias research](smooth-curve-plan.md#side-bias-ux-experiment-2026-09-29). |
| 12 / NT-009 | Integrate remaining upstream tunnel capabilities and apply relevant machinery to Connect. | Re-review the current upstream diff and already adopted pieces. This is not a blind merge or a Connect-only toggle: handle mouths, cover, original terrain, structure transitions, topology-changing preview/Apply and persistence. See [PR #74 research](session-notes/2026-10-01-0055-pr74-review.md). |
| 13 / NT-010 | Smart Connect, first bounded case: identify terrain/network obstacles and offer valid over/under/at-grade alternatives. | Build on NT-002/003/022 and applicable NT-009 machinery. Define clearance, grade, junction intent and alternative-selection UX. Begin with one crossing obstacle, not arbitrary routing. |
| 14 / NT-011 | Smart Connect may modify the obstructing network: keep the requested connection at grade and move the crossing network over/under. | Requires NT-010 and NT-006 affected-area policy; explicit preview and authorization of edits to both networks, combined connectivity and persistence validation. |
| 15 / NT-012 | Broader automatic routing through multiple obstacles and terrain, with bridge/tunnel choices and user preferences. | Expand proven bounded cases; define search objective, limits and honest failure behavior before implementation. |
| 16 / NT-013 | Further vertical-profile and difficult-junction polish: easing, grade transitions, vertical curvature and tighter layouts. | Low priority. Perfect rendered constant slopes and pathological trumpet geometry are not current acceptance requirements. Source/evidence must distinguish authored curves, native surfaces and terrain effects. |

## How priorities are assigned

Balance **player-facing benefit**, **implementation/validation complexity** and
**architectural impact/dependencies**. A useful enabling change may precede a more
visible feature; a small task is not automatically valuable, and a large rewrite
is not automatically a prerequisite. These are relative planning estimates, not
measured delivery-time promises or a numerical scoring formula.

| Work | Player impact / complexity / architecture | Reason for its position |
| --- | --- | --- |
| NT-001 | Indirect enabling benefit / low-to-moderate / low | Keep housekeeping bounded; reuse existing tests and identity surfaces. |
| NT-021 | High direct control / moderate / bounded | Exposes control over an already working vertical behavior; easier to constrain than relocating junctions in XZ. A bounded displacement constraint still needs solver and validation work. |
| NT-023 | High enabling value / bounded review, variable remediation / high leverage | Review after the small junction-height control and before expanding Connect, when sharing and ownership decisions will matter most. A focused follow-up after NT-003/022 checks what the implementation taught us. Review breadth does not imply blanket refactoring. |
| NT-002/003/022 | High practical benefit / medium-to-high / substantial reusable foundation | Improve ordinary connections, ramp departures and lane continuity before automatic routing. NT-022 follows direction handling because they share lane-intent/prefab interpretation, but lateral alignment is a separate geometry constraint. Start with bounded 2-to-3-lane transitions rather than a universal lane-matching system. |
| NT-004 | High normal-use value / high qualification burden / cross-cutting | Promote features deliberately after the near-term controls/Connect scope settles. Address blocking configuration defects sooner; do not defer all validation until this item. |
| NT-006 before NT-005 | High editing freedom / high / shared scope and ownership contract | Define bounded neighboring edits before moving junctions horizontally. Implement only necessary scope first; a generalized graph-edit framework is not required. |
| NT-007/008 | Useful finer control / medium-to-high / constraint and UX decisions | Pins and signed side-bias need deliberate semantics; the existing slider is acceptable for now. |
| NT-009 through NT-012 | High future value / high-to-very-high / topology and route planning | Build tunnel/structure handling and one-obstacle choices before coordinated crossing-network edits or multi-obstacle routing. |
| NT-013 | Incremental polish / potentially high / further native-surface research | Current visuals are accepted; perfection is not worth delaying useful controls and connections. |

The recommended next bounded feature is NT-021, after small NT-001 housekeeping.
Then perform NT-023 before implementing NT-002; use bounded NT-003/NT-022
investigations to inform the review and Connect plan. Implement the
Connect direction and lateral-alignment improvements as distinct, testable steps.
Establish the baseline and
add targeted regression/UI feedback with each feature, not as a final cleanup.
Dan selected the first six items for one autonomous execution sequence after
detailed plans are reviewed. Later priorities remain proposed. See the
[delivery and review workflow](plans/README.md#first-six-plan-delivery-workflow).

## Architecture review checkpoint (NT-023)

Scope the review broadly but remediation deliberately. Inspect production code,
offline geometry/replay, providers, scripts and tests; keep the Bridge product
boundary intact. Include these explicit questions:

- **Separation of concerns:** who owns original inputs, candidates, constraints,
  native observations, acceptance, Apply and resource lifetime? Keep UI/transport
  policy out of math and domain validation out of generic Bridge transport.
- **Reuse and duplication:** what can Curve, Slope and Connect actually share?
  Separate common mathematical/validation contracts from their different editing
  and creation semantics. Do not generalize merely because code looks similar.
- **Human readability:** make state transitions, failure reasons, units, invariants
  and extension points understandable without reconstructing session history.
  Evaluate names, file boundaries and developer explanations with concrete examples.
- **Merge/rebase maintenance:** inspect upstream-sensitive files and prior conflict
  locations. Prefer additive extension points and coherent concern-specific commits;
  separate mechanical moves/renames from behavior changes and avoid incidental
  formatting or generated/dependency churn. Fewer textual conflicts must not hide
  semantic conflicts, and zero-conflict upstream integration is not promised.

Deliver an updated architecture map and a finding-by-finding disposition with
source evidence, benefit, risk, prerequisites and suggested commit/PR boundaries.
Classify each as prerequisite before Connect, useful later, already resolved or
not worth changing. No new runtime framework or broad public API churn is assumed.
Preserve diagnostic coverage and use existing regression evidence to prove any
subsequent refactor; shared code alone is not an independent correctness oracle.

Schedule one **focused follow-up after the Connect direction/alignment milestone**
(NT-003/022) to assess the resulting reuse and remaining conflict hotspots. Do not
wait until the end of Smart Connect to discover ownership problems, or rerun a full
audit after every small feature. Revisit earlier only if a concrete blocker appears.

## Supporting work: attach to the feature it protects

| ID | Work and timing |
| --- | --- |
| NT-014 | Regression breadth: realistic road/rail merges, slip lanes, non-merging rail crossings, elevation transitions, bridges/tunnels, reversed selections, repeated Apply and reload. Add cases with each affected feature, and retain topology/lane/unselected-geometry checks. Reconcile historical gaps with later passing evidence before scheduling them again. |
| NT-015 | Ordinary UI and vehicle traversal: independently assess usability/appearance and actual traffic use. Provider success and connector sets do not substitute for either. Dan performs visual/traversal review where automation is not qualified. |
| NT-016 | Clear UX status/rejection feedback, including fixed junctions, unavailable split selection, incompatible constraints and disabled Apply. Add focused messages alongside the related feature; avoid a large standalone UI rewrite. |
| NT-017 | Ongoing targeted architecture/performance work follows NT-023 findings: measure candidate/observer costs before optimizing and maintain clear ownership as features land. Review does not authorize dropping instrumentation or weakening native lifetime/dependency guarantees. Avoid duplicating the comprehensive review as a separate cleanup backlog. |
| NT-018 | Cross-mod lifecycle/override ownership, tool handoff and broader playset testing. Address during normal-use qualification; restoring a saved boolean alone does not solve concurrent ownership. |
| NT-019 | Configurable geometric acceptance tolerance, initially 5 cm if exposed. Separate it from intentional neighbor-edit allowances; connectivity, topology, stale evidence and material cumulative drift remain strict. Caller-specific parameter bounds also need deliberate policy rather than blanket clamping. |
| NT-020 | Deferred housekeeping/distribution: archive/history size, links, localization, broader non-smoothing tool coverage, and fork identity/permissions/publishing metadata. No history rewrite or mod publication is authorized by this roadmap. |

## Planning and evidence rules

- Create reviewed implementation plans under [plans/](plans/README.md), named by
  stable ID, only as work approaches execution. Reuse existing feature research;
  label superseded sections instead of erasing lessons or treating every old TODO
  as unfinished. Keep this file as the index, not a copy of every experiment.
- Status vocabulary: proposed, needs decision, planned, in progress, blocked,
  implemented/verification pending, verified within stated scope, deferred.
  Record a plan link and evidence/PR links when changing status.
- Each executable plan specifies scope, dependencies, decisions, tests, autonomy
  permissions, stop conditions and the exact human review required. Roadmap entries
  do not authorize live mutation or publication.
- Position/displacement errors through 5 cm are accepted for agreed geometric
  checks. Preserve stricter analytical contracts and historical measurements;
  this does not excuse broken connections or accumulating material drift.
- Track offline, native preview, permanent Apply, repeats/reload, Release/Burst,
  human visuals and vehicle traversal separately. Keep reusable tests/compact
  evidence in Git; retain raw captures and proprietary inputs outside source control.
- Bridge work supports NT when it removes a concrete blocker; it remains a separate
  product, release and backlog. No NT-specific commands/schemas belong in Bridge.

Known non-blocking defects and repro steps: [bug backlog](BUG-BACKLOG.md).
