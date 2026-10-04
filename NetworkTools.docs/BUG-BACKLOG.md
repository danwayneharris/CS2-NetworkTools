# NetworkTools bug backlog

This is the authoritative defect log until we deliberately migrate status to GitHub Issues. It includes reported bugs, captured correctness failures and accepted visual limitations; non-blocking is not synonymous with resolved. Feature priorities remain in [ROADMAP.md](ROADMAP.md).

Reconciled October 3, 2026 against main `d892fb2` and the dated evidence linked below. No bugs were reproduced or fixed during this documentation pass.

| ID | Symptom | Status / priority |
| --- | --- | --- |
| [BUG-001](#bug-001--connect-initially-disabled-after-a-fresh-game-launch) | First Connect selection disabled after cold launch | Reported; minor workflow, accepted non-blocker |
| [BUG-002](#bug-002--complex-connect-preview-can-be-obscured-by-terrain) | Terrain obscures Complex Connect preview | Reported; low visual, accepted non-blocker |
| [BUG-003](#bug-003--smoothing-changes-a-directed-road-lane-target-at-an-ordinary-join) | Road-lane target changes after smoothing | Captured; high investigation priority, current repro needed |
| [BUG-004](#bug-004--residual-surfacegrade-jank-at-tight-elevated-junctions) | Residual tight-junction surface/grade jank | Accepted limitation; low, focused repro needed |

## BUG-001 — Connect initially disabled after a fresh game launch

- Status: reported; original disabled state not captured.
- Priority: minor / non-blocking for PR #19, per Dan's review on October 3, 2026.
- Build observed: `1.5.7+g7dfb76ffeb88.clean.Debug`.
- Repro save: `bridge test - connect repro` (user-created; the later PR #20 restoration workflow checksummed this baseline, but did not reproduce this cold-start symptom).
- Reproduction reported by Dan: restart CS2, load this save, select the two endpoints in Simple Connect. The first preview appears but Create Connection is disabled. Right-click once to deselect only the ending node, then reselect it: the button enables and the connection works. Reloading the save within the same game process does not reproduce the first-attempt failure.
- Workaround: deselect and reselect the ending node.
- Evidence: Dan reported the cold-start/reload distinction. Agent inspection after reselection found preview accepted, CanApply and GetAllowApply true, UI binding Enabled, and a green button. The original disabled state was not captured; cause and introduction revision are unknown. No code change or restart caused that inspected recovery.
- Follow-up: capture the first disabled state after a fresh process launch, including candidate/input/submission identities, producer and observer readiness, native errors, and UI binding. Compare against the same selection after ending-node reselection. Do not bypass validation to hide the symptom.
- Broader complex Connect, vehicle traversal, and cold-start regression testing remain pending. Dan confirmed basic working-case behavior appears correct; this is not exhaustive qualification.

## BUG-002 — Complex Connect preview can be obscured by terrain

- Status: reported and visually accepted; cause not established.
- Priority: low / non-blocking for PR #20, accepted by Dan on October 3, 2026.
- Reviewed fix build: `1.5.7+g02b899b89f9c.clean.Debug`.
- Observation: with Complex Curve, surrounding terrain can obscure portions of the preview road. Dan confirmed that the obscured road is correctly preserved after Apply and the game adjusts the terrain.
- Scope: a visual preview limitation; the exact terrain/rendering cause is not yet established. Do not infer missing road geometry from occlusion alone or bypass geometry/connectivity checks.
- Follow-up: compare preview and permanent terrain/surface generation on a compact reproduction. Low priority; does not block the accepted Connect profile fix. Visual acceptance does not establish vehicle traversal or broad terrain/prefab coverage.

## BUG-003 — Smoothing changes a directed road-lane target at an ordinary join

- Status: **open; captured failure, current-baseline reproduction needed**. Investigation priority: high relative to cosmetic defects. This is not accepted under the geometry tolerance and is not blanket approval for road-lane preservation.
- Last concrete evidence: Debug `7eebfc49830a`, NT-021 full trumpet mainline. At degree-two node `(-1534.82361, 624.103, -2388.344)`, a lane target changed from lane 3 to lane 2 in both bounded and unchanged Unlimited modes. An earlier combined-mode case also reproduced a lane change in a Curve-only control.
- Expected: preserve intended directed lane correspondence through the edited network. Actual: captured correspondence differs after smoothing. Whether traffic is affected and whether this is native regeneration, intended lane-layout change, or a mod defect remain unestablished.
- Repro starting point: toy `bridge test - trumpet combined baseline`; use the exact selection/configuration in [NT-021 evidence](session-notes/nt021-native-evidence.json) and [bounded-profile session](session-notes/2026-10-03-nt021-bounded-profile.md). Reload the untouched baseline, discover fresh entities, compare normalized lane identities before/preview/Apply. Do not reuse historical entity IDs.
- Sources: [current qualification](junction-elevation-limits.md#native-qualification-on-october-3), [earlier controlled comparison](session-notes/2026-10-01-2110-combined-junction-elevation.md).
- Workaround: none verified. Passing rail tests or visual smoothness does not clear this failure.
- Acceptance/blocking: not grounds to undo already accepted PRs without investigation, but blocks a general road-lane-preservation claim and any fix claiming to resolve this case until discriminating checks pass. No current-build rerun was performed during reconciliation.
- Closure: capture the same baseline and intended lane pairs on the fix, verify preview and permanent directed connections independently, document any deliberate correspondence policy, and retain a regression. Vehicle traversal remains separate evidence.

## BUG-004 — Residual surface/grade jank at tight elevated junctions

- Status: **open accepted limitation; needs focused reproduction**. Priority: low, per Dan's acceptance of current visuals; not a blanket slope or surface guarantee.
- Observation: sufficiently tight ramp/junction layouts with substantial elevation changes can retain imperfect grades or minor surface jank. Dan considers the current improvement good enough and can address small remaining issues with MoveIt. No single cause or exact residual magnitude is established.
- Expected: visually smooth joins within the supported envelope. Current limit: authored curve/profile agreement does not guarantee ideal generated surface grade or surrounding terrain.
- Evidence: [terrain/profile review](terrain-profile-review.md), [combined review](combined-smoothing-review.md), and roadmap NT-013. These retain build-specific results and review checkpoints. Do not conflate this residual limitation with the corrected structural/finishing mismatch or repeatability failure below.
- Repro: identify a remaining compact example from the terrain v1.1 or trumpet toy baseline, record exact build/launch flags and operations, and separate authored geometry, generated surfaces and terrain samples. A before/after screenshot alone does not establish cause.
- Workaround: Dan reported that reducing curvature or changing a pin to move the ramp away from steep terrain can help; not a universally validated remedy.
- Closure: narrow the symptom into reproducible subcases; show the chosen improvement in native surfaces and visual review without connectivity loss or cumulative drift. Perfect constant generated grade remains deferred.

## Reconciliation: things that are not new open defects

This table records the disposition of recurring notes reviewed on October 3, 2026. It is a bounded reconciliation of current guides and their evidence, not a claim that every historical sentence or every untested feature has been audited.

| Recorded concern | Current disposition and source |
| --- | --- |
| Connect profile rejected because native course sampling rewrote Y | Fixed for the captured repro in merged PR #20; [restoration and replay](connect-course-height-replay.md). The older different-height fixture was not retroactively rerun; unsupported mappings remain explicit limitations. BUG-002 covers visual occlusion only. |
| Combined preview/Apply structure mismatch | Captured structural case corrected; [structure fix](session-notes/2026-10-01-2305-preview-structure-fix.md). Do not generalize one passing case to all surfaces. |
| Native finishing surface mismatch after rebuild/reload | Bounded correction exists only with the explicit, binary-pinned Debug compatibility experiment; [combined review](combined-smoothing-review.md#latest-review-checkpoint-october-3). Default-off behavior and broader qualification remain NT-004 decisions, not a claim of universal resolution. |
| Surface-correction cumulative drift / non-idempotence | Captured settling/repeat/reload case corrected; [repeatability evidence](session-notes/2026-10-01-0552-profile-repeatability.md). New material accumulating drift would be a new/reopened defect. |
| Rail split/interior strict drift failures of 3.008/5.174 mm; later 23.5183 mm drift | Historical strict failures, acceptable under current 5 cm position policy. [Original results](session-notes/2026-09-30-0200-provider-regressions.md), [later policy](terrain-profile-review.md). Connectivity and lane identity are never excused by that tolerance. |
| Audit F02–F10 correctness findings and runner failures | Consult individual [audit dispositions](audit-disposition.md); implemented scoped corrections are not automatically open bugs. Broader parameter/ownership policies and missing live checks remain explicitly qualified there. |
| Preview freshness/completion and junction search history | Implemented revision/input checks with bounded native observations; [freshness successor](preview-freshness.md). Universal completion proof and history-independent candidate choice are not promised. Record a concrete stale/oscillating repro as a defect; do not resurrect old proposals as current failures. |
| Junction elevation allowance accumulates across operations | Explicit per-operation budget, not an idempotence guarantee; [junction limits](junction-elevation-limits.md). Cumulative-budget UX is a policy/feature decision. |
| Fixed-node lane-alignment probe moves nodes ~0.75 m | Unfinished NT-022 prerequisite in [draft PR #22](https://github.com/danwayneharris/CS2-NetworkTools/pull/22), with Apply blocked; not a shipped alignment feature or a tolerated 5 cm failure. |
| Signed slider, junction split semantics, neighboring edits, tunnel/smart routing, lane alignment | Features/decisions in [ROADMAP](ROADMAP.md), not defects solely because absent. |
| Release/Burst, traffic traversal, wider road/prefab matrix, save/reload beyond captured cases | Qualification gaps. Record not-run honestly; create a defect only when an expected contract actually fails. |

## Adding or updating an entry

When Dan or an executor says “note this bug,” update this file in the same change as relevant evidence. Search for an existing symptom before allocating the next unused BUG number. Do not renumber or silently delete closed entries. Use:

- **Status:** reported, reproduced, investigating, fixed-awaiting-verification, resolved, or deferred/accepted limitation. State whether evidence is current or historical.
- **Priority and blocking scope:** cosmetic, workflow, or correctness impact; identify exactly which claim or PR it blocks. Non-blocking does not mean resolved.
- **Expected / actual:** player-visible symptom, separately from any hypothesized cause.
- **Identity and reproduction:** build/configuration/flags, save/fixture, controls and operation order; record missing details explicitly.
- **Evidence:** compact reports and source/session links; keep bulk captures in archives. Separate offline, native preview, permanent Apply, reload, visual and vehicle evidence.
- **Workaround and next check:** include an objective closure condition and regression where practical. Never bypass an invariant to close a report.
- **Resolution:** fixing commit/PR, verified scope and remaining limits. Reopen the same ID when the same symptom returns unless investigation establishes a distinct defect.

Later GitHub Issues migration should preserve these IDs, link the evidence, and move authoritative status there deliberately. Until then, PRs link to this file and record any new review findings here; there is no parallel issue tracker to keep synchronized.

Public GitHub Issues will also make known limitations discoverable to other mod developers, reviewers and eventual users. Migration is intended to communicate awareness and scope clearly, not just move internal bookkeeping. No issues are created by this reconciliation.

## BUG-005 — Expanded Connect controls extend below the screen

- Status: fixed in source, awaiting native UI verification. Priority: review-blocking usability for lane-aware Connect (#21).
- Reported build: clean Debug d78c172. Enabling lane-aware direction/profile controls can push required lane selections and Apply below the screen without an overall scrollbar.
- Source cause: action panel lacked a viewport height limit and overall scrolling area. Individual list overflow did not bound the combined panel.
- Correction: use the game's Scrollable for controls, bound panel height, keep header/Apply outside the scrolling area, remove nested scrolling from choice lists inside this area. Geometry/Apply validation is unchanged.
- Evidence and review checklist: [panel scrolling session](session-notes/2026-10-04-0300-connect-panel-scroll.md). Gameface inspection endpoint was unavailable; initial diagnosis combines user's observation, bridge state and source rather than a live layout measurement.
- Closure: at the reported UI scale, all start/end approach and lane choices must be reachable via wheel and scrollbar, and Apply must stay visible; check shorter tools and prefab picker too. No accepted connection is implied merely by making the missing controls accessible.

## BUG-006 — Lane-choice labels make Connect setup difficult to reproduce

- Status: reported UX issue; priority medium, no geometry failure established in this capture.
- On clean Debug 531d970, a three-lane highway lists composition lane indices 2/3/4, and a one-lane arrival lists lane 2. These are not familiar player-facing lane ordinals. Required selection can therefore look like a native geometry error.
- Captured rejection was lane_choice_required; selecting physical rightmost incoming lane 4 and outgoing lane 2 produced a stable accepted preview. No Apply performed. [Evidence and saved checkpoint](session-notes/2026-10-04-0331-highway-lane-review.md).
- Workaround: explicitly select an incoming departure and outgoing arrival lane after selecting approaches; choices are transient and reset with tool selection. Do not infer them from the saved network alone.
- Follow-up: clearer player-facing lane labels/orientation and required-selection guidance, keeping internal IDs available diagnostically. Verify a user can reconstruct the intended slip-lane setup without agent intervention. Reload causality and geometry/traffic remain separate checks.

BUG-005 review update (October 4): Dan confirms scrolling works on the deployed fix 531d970. Broader footer/picker/UI-scale checks remain pending; this is partial human validation rather than a claim of every layout case passing.
